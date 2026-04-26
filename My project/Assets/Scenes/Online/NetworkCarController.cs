using System.Collections;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PrometeoCarController))]
[RequireComponent(typeof(PhotonView))]
public class NetworkCarController : MonoBehaviourPun, IPunObservable, IOnEventCallback
{
    [Header("Камеры")]
    public GameObject studentCameraObject;
    public GameObject instructorCameraObject;

    [Header("UI (необязательно)")]
    public GameObject studentDrivingUI;
    public GameObject instructorDrivingUI;

    private const byte TAKE_CONTROL_EVENT = 11;

    private int currentDriverActorNumber = -1;
    private bool iAmDriver = false;

    private struct Snapshot
    {
        public Vector3 pos;
        public Quaternion rot;
        public Vector3 vel;
        public double time;
    }
    private Snapshot[] _buf = new Snapshot[3];
    private int _bufCount = 0;

    private PrometeoCarController _car;
    private Rigidbody _rb;
    private bool _ready = false;

    private bool _instrBraking = false;
    private bool _instrSteering = false;

    private float _logTimer = 0f;
    private int _packetsReceived = 0;
    private float _lastReceivedGap = 0f;
    private double _lastSnapTime = 0;
    private Vector3 _lastSnapPos = Vector3.zero;
    private float _maxPosDeltaPerPacket = 0f;

    void Awake()
    {
        _car = GetComponent<PrometeoCarController>();
        _rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        PhotonNetwork.AddCallbackTarget(this);
        StartCoroutine(InitAfterPhoton());
    }

    void OnDestroy()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    IEnumerator InitAfterPhoton()
    {
        yield return new WaitUntil(() => PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null);
        yield return null;

        Debug.Log($"[NET] InitAfterPhoton | Actor: {PhotonNetwork.LocalPlayer.ActorNumber} | InRoom: {PhotonNetwork.InRoom} | MasterClient: {PhotonNetwork.IsMasterClient}");

        // Проверяем PhotonTransformView — если есть, это причина дёрганья!
        var ptv = GetComponent<Photon.Pun.PhotonTransformView>();
        if (ptv != null)
            Debug.LogError("[NET] НАЙДЕН PhotonTransformView! Он конфликтует с NetworkCarController и вызывает дёрганье. УДАЛИ его из компонентов машины!");
        else
            Debug.Log("[NET] PhotonTransformView не найден — OK");

        // Проверяем PhotonRigidbodyView
        var prv = GetComponent<PhotonRigidbodyView>();
        if (prv != null)
            Debug.LogError("[NET] НАЙДЕН PhotonRigidbodyView! Он тоже может конфликтовать. УДАЛИ его!");
        else
            Debug.Log("[NET] PhotonRigidbodyView не найден — OK");

        // Проверяем Observed Components в PhotonView
        var pv = GetComponent<PhotonView>();
        for (int i = 0; i < pv.ObservedComponents.Count; i++)
            Debug.Log($"[NET]   [{i}] {pv.ObservedComponents[i]?.GetType().Name ?? "NULL"}");

        SetupMyCamera();
        SetDriverLocally(1);
        _ready = true;

    }

    void SetupMyCamera()
    {
        int myActor = PhotonNetwork.LocalPlayer.ActorNumber;
        GameObject other = myActor == 1 ? instructorCameraObject : studentCameraObject;
        if (other != null)
        {
            var c = other.GetComponent<Camera>(); if (c != null) c.enabled = false;
            var al = other.GetComponent<AudioListener>(); if (al != null) al.enabled = false;
        }

        GameObject mine = myActor == 1 ? studentCameraObject : instructorCameraObject;
        if (mine == null) { Debug.LogError($"[NET] Камера Actor {myActor} не назначена!"); return; }
        if (!mine.activeInHierarchy) mine.SetActive(true);

        var cam = mine.GetComponent<Camera>();
        if (cam != null) { cam.enabled = true; cam.depth = 0; }

        var listener = mine.GetComponent<AudioListener>();
        if (listener != null) listener.enabled = true;

        var follow = mine.GetComponent<FollowCamera>();
        if (follow != null) { follow.SetTarget(transform); follow.enabled = true; }
    }

    void SetDriverLocally(int driverActor)
    {
        if (driverActor == currentDriverActorNumber) return;
        currentDriverActorNumber = driverActor;
        iAmDriver = PhotonNetwork.LocalPlayer.ActorNumber == driverActor;

        if (iAmDriver)
        {
            _rb.isKinematic = false;
            _car.enabled = true;
        }
        else
        {
            _rb.isKinematic = true;
            _car.enabled = false;
            _bufCount = 0;
        }

        if (studentDrivingUI != null) studentDrivingUI.SetActive(driverActor == 1);
        if (instructorDrivingUI != null) instructorDrivingUI.SetActive(driverActor == 2);

    }

    void Update()
    {
        if (!_ready) return;

        int myActor = PhotonNetwork.LocalPlayer.ActorNumber;

        if (myActor == 2)
        {
            if (Keyboard.current.fKey.wasPressedThisFrame) SendControlEvent(2);
            if (Keyboard.current.fKey.wasReleasedThisFrame) SendControlEvent(1);
        }

        if (!iAmDriver)
        {
            ApplyInterpolation();

            if (myActor == 2)
                HandleInstructorInput();
        }

        // ── Периодический лог состояния (раз в секунду) ──
        _logTimer += Time.deltaTime;
        if (_logTimer >= 1f)
        {
            _logTimer = 0f;
            if (!iAmDriver)
            {
                Snapshot s1 = _buf[(_bufCount - 1) % 3];
                double staleness = PhotonNetwork.Time - s1.time;
                Debug.Log($"[NET][Client2] пакетов/сек: {_packetsReceived} | " +
                          $"давность снимка: {staleness * 1000:F0}мс | " +
                          $"макс Δpos/пакет: {_maxPosDeltaPerPacket:F2}м | " +
                          $"rb.isKinematic: {_rb.isKinematic} | car.enabled: {_car.enabled} | " +
                          $"bufCount: {_bufCount}");
                _packetsReceived = 0;
                _maxPosDeltaPerPacket = 0f;
            }
            else
            {
                Debug.Log($"[NET][Driver] carSpeed: {_car.carSpeed:F1} | vel: {_rb.linearVelocity.magnitude:F2}м/с | " +
                          $"pos: {transform.position}");
            }
        }
    }

    void ApplyInterpolation()
    {
        // Ждём минимум 2 снимка — до этого вообще не двигаемся (иначе скачок 90м при старте)
        if (_bufCount < 2) return;

        Snapshot s0 = _buf[(_bufCount - 2) % 3];
        Snapshot s1 = _buf[(_bufCount - 1) % 3];

        double renderTime = PhotonNetwork.Time - 0.1;
        double span = s1.time - s0.time;
        if (span <= 0.0001) span = 0.1;

        float t = Mathf.Clamp01((float)((renderTime - s0.time) / span));

        // Целевая позиция = интерполяция + небольшая экстраполяция по скорости
        float overShoot = Mathf.Max(0f, (float)(renderTime - s1.time));
        Vector3 targetPos = Vector3.Lerp(s0.pos, s1.pos, t) + s1.vel * overShoot;
        Quaternion targetRot = Quaternion.Slerp(s0.rot, s1.rot, t);

        float dist = Vector3.Distance(transform.position, targetPos);

        // Телепорт только если скачок > 5м (respawn или первый пакет после задержки)
        if (dist > 5f)
        {
            transform.position = targetPos;
            transform.rotation = targetRot;
            Debug.LogWarning($"Телепорт ({dist:F1}м) — позиция сброшена к сетевой");
            return;
        }

        // MoveTowards — скорость пропорциональна отставанию, нет рывков от Lerp
        float speed = Mathf.Clamp(dist * 8f, 2f, 30f) * Mathf.Max(1f, s1.vel.magnitude);
        transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime * speed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 12f);
    }

    void HandleInstructorInput()
    {
        bool brakeNow = Keyboard.current.sKey.isPressed;
        bool leftNow = Keyboard.current.aKey.isPressed;
        bool rightNow = Keyboard.current.dKey.isPressed;

        if (brakeNow != _instrBraking)
        {
            _instrBraking = brakeNow;
            photonView.RPC(nameof(RPC_InstructorBrake), RpcTarget.All, brakeNow);
        }

        if (leftNow)
        {
            _instrSteering = true;
            photonView.RPC(nameof(RPC_InstructorSteer), RpcTarget.All, -1);
        }
        else if (rightNow)
        {
            _instrSteering = true;
            photonView.RPC(nameof(RPC_InstructorSteer), RpcTarget.All, 1);
        }
        else if (_instrSteering)
        {
            _instrSteering = false;
            photonView.RPC(nameof(RPC_InstructorSteer), RpcTarget.All, 0);
        }
    }

    [PunRPC] void RPC_InstructorBrake(bool braking) { if (braking) _car.Brakes(); }
    [PunRPC]
    void RPC_InstructorSteer(int dir)
    {
        if (dir < 0) _car.TurnLeft();
        else if (dir > 0) _car.TurnRight();
        else _car.ResetSteeringAngle();
    }

    void SendControlEvent(int actorNumber)
    {
        var opts = new RaiseEventOptions { Receivers = ReceiverGroup.All };
        PhotonNetwork.RaiseEvent(TAKE_CONTROL_EVENT, actorNumber, opts,
            new SendOptions { Reliability = true });
    }

    public void OnEvent(EventData photonEvent)
    {
        if (photonEvent.Code == TAKE_CONTROL_EVENT)
            SetDriverLocally((int)photonEvent.CustomData);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(_rb.linearVelocity);
            stream.SendNext(_car.carSpeed);
            stream.SendNext(currentDriverActorNumber);
        }
        else
        {
            Vector3 pos = (Vector3)stream.ReceiveNext();
            Quaternion rot = (Quaternion)stream.ReceiveNext();
            Vector3 vel = (Vector3)stream.ReceiveNext();
            _car.carSpeed = (float)stream.ReceiveNext();
            int syncedDriver = (int)stream.ReceiveNext();

            // Считаем скачок позиции между пакетами
            float posDelta = Vector3.Distance(pos, _lastSnapPos);
            if (posDelta > _maxPosDeltaPerPacket) _maxPosDeltaPerPacket = posDelta;
            _lastSnapPos = pos;

            // Считаем интервал между пакетами
            double now = PhotonNetwork.Time;
            if (_lastSnapTime > 0)
                _lastReceivedGap = (float)((now - _lastSnapTime) * 1000f);
            _lastSnapTime = now;
            _packetsReceived++;


            var snap = new Snapshot { pos = pos, rot = rot, vel = vel, time = now };
            _buf[_bufCount % 3] = snap;
            _bufCount++;

            if (syncedDriver != currentDriverActorNumber)
                SetDriverLocally(syncedDriver);
        }
    }
}