using UnityEngine;
using Mirror;

[RequireComponent(typeof(PrometeoCarController))]
public class NetworkCarController : NetworkBehaviour
{
    [Header("Camera")]
    [SerializeField] private GameObject playerCamera;

    [Header("Sync Settings")]
    [SerializeField] private int sendRate = 5;

    [SyncVar] private float syncThrottleAxis;
    [SyncVar] private float syncSteeringAxis;
    [SyncVar] private bool syncHandbrake;

    private PrometeoCarController _car;
    private int _frameCounter;

    private void Awake()
    {
        _car = GetComponent<PrometeoCarController>();

        // Камера всегда выключена по умолчанию
        // Включится только у владельца в OnStartLocalPlayer
        if (playerCamera != null)
            playerCamera.SetActive(false);
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        Debug.Log($"[Network] OnStartLocalPlayer вызван! isOwned={isOwned}, isLocalPlayer={isLocalPlayer}");
        if (playerCamera != null)
            playerCamera.SetActive(true);
        else
            Debug.LogError("[Network] playerCamera == null!");
    }

    private void Update()
    {
        if (!isOwned) return;

        _frameCounter++;
        if (_frameCounter >= sendRate)
        {
            _frameCounter = 0;
            CmdSendCarState(
                _car.throttleAxis_Network,
                _car.steeringAxis_Network,
                _car.isTractionLocked);
        }
    }

    [Command(requiresAuthority = true)]
    private void CmdSendCarState(float throttle, float steering, bool handbrake)
    {
        syncThrottleAxis = throttle;
        syncSteeringAxis = steering;
        syncHandbrake = handbrake;
    }
}