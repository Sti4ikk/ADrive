using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PrometeoCarController : MonoBehaviour
{
    [Header("Speed Settings")]
    [Range(20, 190)] public int maxSpeed = 120;
    [Range(10, 120)] public int maxReverseSpeed = 40;
    [Range(1, 10)] public int accelerationMultiplier = 4;

    [Header("Steering Settings")]
    [Range(10, 45)] public int maxSteeringAngle = 32;
    [Range(0.1f, 1f)] public float steeringSpeed = 0.6f;
    // Насколько руль становится "тупее" на высокой скорости (0 = не зависит от скорости)
    [Range(0f, 1f)] public float steeringSpeedSensitivity = 0.6f;

    [Header("Braking & Deceleration")]
    [Range(100, 2000)] public int brakeForce = 800;
    [Range(100, 2000)] public int rearBrakeForce = 560; // 70% от передних — небольшой занос при торможении
    [Range(0, 10)] public int decelerationMultiplier = 3;
    [Range(1, 10)] public int handbrakeDriftMultiplier = 5;

    [Header("Engine")]
    // Инерция двигателя: насколько быстро меняется реальный момент (0.1 = очень мягко, 1 = мгновенно)
    [Range(0.1f, 1f)] public float engineInertia = 0.35f;

    [Space(10)] public Vector3 bodyMassCenter;

    [Header("Wheel Colliders & Meshes")]
    public GameObject frontLeftMesh;
    public WheelCollider frontLeftCollider;
    public GameObject frontRightMesh;
    public WheelCollider frontRightCollider;
    public GameObject rearLeftMesh;
    public WheelCollider rearLeftCollider;
    public GameObject rearRightMesh;
    public WheelCollider rearRightCollider;

    [Header("Effects")]
    public bool useEffects = false;
    public ParticleSystem RLWParticleSystem;
    public ParticleSystem RRWParticleSystem;
    public TrailRenderer RLWTireSkid;
    public TrailRenderer RRWTireSkid;

    [Header("UI")]
    public bool useUI = false;
    public Text carSpeedText;

    [Header("Sounds")]
    public bool useSounds = false;
    public AudioSource carEngineSound;
    public AudioSource tireScreechSound;
    float initialCarEngineSoundPitch;

    [Header("Touch Controls")]
    public bool useTouchControls = false;
    public GameObject throttleButton; PrometeoTouchInput throttlePTI;
    public GameObject reverseButton; PrometeoTouchInput reversePTI;
    public GameObject turnRightButton; PrometeoTouchInput turnRightPTI;
    public GameObject turnLeftButton; PrometeoTouchInput turnLeftPTI;
    public GameObject handbrakeButton; PrometeoTouchInput handbrakePTI;

    [HideInInspector] public float carSpeed;
    [HideInInspector] public bool isDrifting;
    [HideInInspector] public bool isTractionLocked;

    Rigidbody carRigidbody;
    float steeringAxis;
    float throttleAxis;       // желаемый дроссель (-1..1)
    float currentEngineForce; // реальный момент с инерцией двигателя
    float driftingAxis;
    float localVelocityZ;
    float localVelocityX;
    bool deceleratingCar;
    bool touchControlsSetup = false;

    WheelFrictionCurve FLwheelFriction; float FLWextremumSlip;
    WheelFrictionCurve FRwheelFriction; float FRWextremumSlip;
    WheelFrictionCurve RLwheelFriction; float RLWextremumSlip;
    WheelFrictionCurve RRwheelFriction; float RRWextremumSlip;

    void Start()
    {
        carRigidbody = GetComponent<Rigidbody>();
        carRigidbody.centerOfMass = bodyMassCenter;

        // Сохраняем оригинальные кривые трения
        CacheWheelFriction(frontLeftCollider, ref FLwheelFriction, ref FLWextremumSlip);
        CacheWheelFriction(frontRightCollider, ref FRwheelFriction, ref FRWextremumSlip);
        CacheWheelFriction(rearLeftCollider, ref RLwheelFriction, ref RLWextremumSlip);
        CacheWheelFriction(rearRightCollider, ref RRwheelFriction, ref RRWextremumSlip);

        if (carEngineSound != null) initialCarEngineSoundPitch = carEngineSound.pitch;
        if (useUI) InvokeRepeating("CarSpeedUI", 0f, 0.1f);
        if (useSounds) InvokeRepeating("CarSounds", 0f, 0.1f);

        if (!useEffects)
        {
            RLWParticleSystem?.Stop();
            RRWParticleSystem?.Stop();
            if (RLWTireSkid != null) RLWTireSkid.emitting = false;
            if (RRWTireSkid != null) RRWTireSkid.emitting = false;
        }

        if (useTouchControls)
        {
            if (throttleButton && reverseButton && turnRightButton && turnLeftButton && handbrakeButton)
            {
                throttlePTI = throttleButton.GetComponent<PrometeoTouchInput>();
                reversePTI = reverseButton.GetComponent<PrometeoTouchInput>();
                turnLeftPTI = turnLeftButton.GetComponent<PrometeoTouchInput>();
                turnRightPTI = turnRightButton.GetComponent<PrometeoTouchInput>();
                handbrakePTI = handbrakeButton.GetComponent<PrometeoTouchInput>();
                touchControlsSetup = true;
            }
        }
    }

    void CacheWheelFriction(WheelCollider col, ref WheelFrictionCurve curve, ref float extremum)
    {
        curve = new WheelFrictionCurve
        {
            extremumSlip = col.sidewaysFriction.extremumSlip,
            extremumValue = col.sidewaysFriction.extremumValue,
            asymptoteSlip = col.sidewaysFriction.asymptoteSlip,
            asymptoteValue = col.sidewaysFriction.asymptoteValue,
            stiffness = col.sidewaysFriction.stiffness
        };
        extremum = col.sidewaysFriction.extremumSlip;
    }

    void Update()
    {
        carSpeed = (2 * Mathf.PI * frontLeftCollider.radius * frontLeftCollider.rpm * 60f) / 1000f;
        localVelocityX = transform.InverseTransformDirection(carRigidbody.linearVelocity).x;
        localVelocityZ = transform.InverseTransformDirection(carRigidbody.linearVelocity).z;

        if (useTouchControls && touchControlsSetup)
            HandleTouchInput();
        else
            HandleKeyboardInput();

        AnimateWheelMeshes();
        if (useEffects) DriftCarPS();
    }

    // ─── Ввод ─────────────────────────────────────────────────────────────────

    void HandleKeyboardInput()
    {
        bool gas = Keyboard.current.wKey.isPressed;
        bool reverse = Keyboard.current.sKey.isPressed;
        bool left = Keyboard.current.aKey.isPressed;
        bool right = Keyboard.current.dKey.isPressed;
        bool handbrake = Keyboard.current.spaceKey.isPressed;

        if (gas) { CancelInvoke("DecelerateCar"); deceleratingCar = false; GoForward(); }
        if (reverse) { CancelInvoke("DecelerateCar"); deceleratingCar = false; GoReverse(); }

        if (left) TurnLeft();
        if (right) TurnRight();

        if (handbrake) { CancelInvoke("DecelerateCar"); deceleratingCar = false; Handbrake(); }
        if (!handbrake) RecoverTraction();

        if (!gas && !reverse) ThrottleOff();

        if (!gas && !reverse && !handbrake && !deceleratingCar)
        {
            InvokeRepeating("DecelerateCar", 0f, 0.1f);
            deceleratingCar = true;
        }

        if (!left && !right && steeringAxis != 0f) ResetSteeringAngle();
    }

    void HandleTouchInput()
    {
        if (throttlePTI.buttonPressed) { CancelInvoke("DecelerateCar"); deceleratingCar = false; GoForward(); }
        if (reversePTI.buttonPressed) { CancelInvoke("DecelerateCar"); deceleratingCar = false; GoReverse(); }
        if (turnLeftPTI.buttonPressed) TurnLeft();
        if (turnRightPTI.buttonPressed) TurnRight();

        if (handbrakePTI.buttonPressed) { CancelInvoke("DecelerateCar"); deceleratingCar = false; Handbrake(); }
        if (!handbrakePTI.buttonPressed) RecoverTraction();

        if (!throttlePTI.buttonPressed && !reversePTI.buttonPressed) ThrottleOff();

        if (!throttlePTI.buttonPressed && !reversePTI.buttonPressed && !handbrakePTI.buttonPressed && !deceleratingCar)
        {
            InvokeRepeating("DecelerateCar", 0f, 0.1f);
            deceleratingCar = true;
        }

        if (!turnLeftPTI.buttonPressed && !turnRightPTI.buttonPressed && steeringAxis != 0f)
            ResetSteeringAngle();
    }

    // ─── Руление (скорость поворота зависит от скорости авто) ─────────────────

    float GetDynamicSteeringSpeed()
    {
        float speedFactor = Mathf.Clamp01(Mathf.Abs(carSpeed) / maxSpeed);
        // На высокой скорости руль тяжелее, на низкой — острее
        return steeringSpeed * Mathf.Lerp(1f, 1f - steeringSpeedSensitivity, speedFactor);
    }

    public void TurnLeft()
    {
        float dynSpeed = GetDynamicSteeringSpeed();
        steeringAxis = Mathf.MoveTowards(steeringAxis, -1f, Time.deltaTime * 10f * dynSpeed);
        ApplySteer();
    }

    public void TurnRight()
    {
        float dynSpeed = GetDynamicSteeringSpeed();
        steeringAxis = Mathf.MoveTowards(steeringAxis, 1f, Time.deltaTime * 10f * dynSpeed);
        ApplySteer();
    }

    public void ResetSteeringAngle()
    {
        float dynSpeed = GetDynamicSteeringSpeed();
        steeringAxis = Mathf.MoveTowards(steeringAxis, 0f, Time.deltaTime * 10f * dynSpeed);
        ApplySteer();
    }

    void ApplySteer()
    {
        float angle = steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, angle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, angle, steeringSpeed);
    }

    // ─── Тяга (RWD — задний привод) ───────────────────────────────────────────

    void ApplyRWDTorque(float torque)
    {
        // Плавная инерция двигателя
        currentEngineForce = Mathf.Lerp(currentEngineForce, torque, engineInertia);

        // Передние колёса — только тормоза/рулевое
        frontLeftCollider.motorTorque = 0f;
        frontRightCollider.motorTorque = 0f;
        frontLeftCollider.brakeTorque = 0f;
        frontRightCollider.brakeTorque = 0f;

        rearLeftCollider.brakeTorque = 0f;
        rearRightCollider.brakeTorque = 0f;
        rearLeftCollider.motorTorque = currentEngineForce;
        rearRightCollider.motorTorque = currentEngineForce;
    }

    public void GoForward()
    {
        isDrifting = Mathf.Abs(localVelocityX) > 2.5f;
        throttleAxis = Mathf.MoveTowards(throttleAxis, 1f, Time.deltaTime * 5f);

        if (localVelocityZ < -1f)
        {
            Brakes();
            return;
        }

        if (Mathf.RoundToInt(carSpeed) < maxSpeed)
            ApplyRWDTorque(accelerationMultiplier * 50f * throttleAxis);
        else
            ApplyRWDTorque(0f);
    }

    public void GoReverse()
    {
        isDrifting = Mathf.Abs(localVelocityX) > 2.5f;
        throttleAxis = Mathf.MoveTowards(throttleAxis, -1f, Time.deltaTime * 5f);

        if (localVelocityZ > 1f)
        {
            Brakes();
            return;
        }

        if (Mathf.Abs(Mathf.RoundToInt(carSpeed)) < maxReverseSpeed)
            ApplyRWDTorque(accelerationMultiplier * 50f * throttleAxis);
        else
            ApplyRWDTorque(0f);
    }

    // ─── Ручной тормоз (только задние колёса — реалистичный дрифт) ───────────

    public void Handbrake()
    {
        CancelInvoke("RecoverTraction");

        isDrifting = Mathf.Abs(localVelocityX) > 2.5f;

        // Ручник блокирует только задние колёса
        rearLeftCollider.brakeTorque = brakeForce * 2f;
        rearRightCollider.brakeTorque = brakeForce * 2f;
        rearLeftCollider.motorTorque = 0f;
        rearRightCollider.motorTorque = 0f;

        // Передние — свободны
        frontLeftCollider.brakeTorque = 0f;
        frontRightCollider.brakeTorque = 0f;

        // Снижаем боковое сцепление задних колёс
        driftingAxis += Time.deltaTime;
        float secureStartingPoint = driftingAxis * RLWextremumSlip * handbrakeDriftMultiplier;
        if (secureStartingPoint < RLWextremumSlip)
            driftingAxis = RLWextremumSlip / (RLWextremumSlip * handbrakeDriftMultiplier);
        if (driftingAxis > 1f) driftingAxis = 1f;

        if (driftingAxis < 1f)
        {
            RLwheelFriction.extremumSlip = RLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            rearLeftCollider.sidewaysFriction = RLwheelFriction;
            RRwheelFriction.extremumSlip = RRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            rearRightCollider.sidewaysFriction = RRwheelFriction;
        }

        isTractionLocked = true;
    }

    // ─── Стандартные тормоза (с распределением 100% перед / 70% зад) ─────────

    public void Brakes()
    {
        frontLeftCollider.brakeTorque = brakeForce;
        frontRightCollider.brakeTorque = brakeForce;
        rearLeftCollider.brakeTorque = rearBrakeForce;
        rearRightCollider.brakeTorque = rearBrakeForce;
    }

    public void ThrottleOff()
    {
        frontLeftCollider.motorTorque = 0f;
        frontRightCollider.motorTorque = 0f;
        rearLeftCollider.motorTorque = 0f;
        rearRightCollider.motorTorque = 0f;
    }

    // ─── Накат (engine braking через трение коллайдеров) ─────────────────────

    public void DecelerateCar()
    {
        isDrifting = Mathf.Abs(localVelocityX) > 2.5f;

        throttleAxis = Mathf.MoveTowards(throttleAxis, 0f, Time.deltaTime * 10f);
        currentEngineForce = Mathf.MoveTowards(currentEngineForce, 0f, Time.deltaTime * 200f);

        // Engine braking — небольшой тормозной момент
        float engineBrake = decelerationMultiplier * 15f;
        frontLeftCollider.brakeTorque = engineBrake * 0.3f;
        frontRightCollider.brakeTorque = engineBrake * 0.3f;
        rearLeftCollider.brakeTorque = engineBrake;
        rearRightCollider.brakeTorque = engineBrake;

        rearLeftCollider.motorTorque = 0f;
        rearRightCollider.motorTorque = 0f;

        if (carRigidbody.linearVelocity.magnitude < 0.3f)
        {
            carRigidbody.linearVelocity = Vector3.zero;
            CancelInvoke("DecelerateCar");
        }
    }

    // ─── Восстановление сцепления ─────────────────────────────────────────────

    public void RecoverTraction()
    {
        isTractionLocked = false;
        driftingAxis = Mathf.MoveTowards(driftingAxis, 0f, Time.deltaTime / 1.5f);

        if (RLwheelFriction.extremumSlip > RLWextremumSlip)
        {
            RLwheelFriction.extremumSlip = RLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            rearLeftCollider.sidewaysFriction = RLwheelFriction;
            RRwheelFriction.extremumSlip = RRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            rearRightCollider.sidewaysFriction = RRwheelFriction;
            Invoke("RecoverTraction", Time.deltaTime);
        }
        else
        {
            RLwheelFriction.extremumSlip = RLWextremumSlip; rearLeftCollider.sidewaysFriction = RLwheelFriction;
            RRwheelFriction.extremumSlip = RRWextremumSlip; rearRightCollider.sidewaysFriction = RRwheelFriction;
            driftingAxis = 0f;
        }
    }

    // ─── Анимация колёс ───────────────────────────────────────────────────────

    void AnimateWheelMeshes()
    {
        try
        {
            UpdateWheelMesh(frontLeftCollider, frontLeftMesh, false);
            UpdateWheelMesh(frontRightCollider, frontRightMesh, true);
            UpdateWheelMesh(rearLeftCollider, rearLeftMesh, false);
            UpdateWheelMesh(rearRightCollider, rearRightMesh, true);
        }
        catch { }
    }

    void UpdateWheelMesh(WheelCollider col, GameObject mesh, bool mirrorY)
    {
        col.GetWorldPose(out Vector3 pos, out Quaternion rot);
        mesh.transform.position = pos;
        mesh.transform.rotation = mirrorY ? rot * Quaternion.Euler(0f, 180f, 0f) : rot;
    }

    // ─── Эффекты ──────────────────────────────────────────────────────────────

    public void DriftCarPS()
    {
        if (!useEffects) return;

        try
        {
            if (isDrifting) { RLWParticleSystem.Play(); RRWParticleSystem.Play(); }
            else { RLWParticleSystem.Stop(); RRWParticleSystem.Stop(); }
        }
        catch { }

        try
        {
            bool skid = (isTractionLocked || Mathf.Abs(localVelocityX) > 5f) && Mathf.Abs(carSpeed) > 12f;
            if (RLWTireSkid != null) RLWTireSkid.emitting = skid;
            if (RRWTireSkid != null) RRWTireSkid.emitting = skid;
        }
        catch { }
    }

    // ─── UI ───────────────────────────────────────────────────────────────────

    public void CarSpeedUI()
    {
        if (!useUI) return;
        try { carSpeedText.text = Mathf.RoundToInt(Mathf.Abs(carSpeed)).ToString(); }
        catch (Exception ex) { Debug.LogWarning(ex); }
    }

    // ─── Звук ─────────────────────────────────────────────────────────────────

    public void CarSounds()
    {
        if (!useSounds) return;
        try
        {
            if (carEngineSound != null)
                carEngineSound.pitch = initialCarEngineSoundPitch + (Mathf.Abs(carRigidbody.linearVelocity.magnitude) / 25f);

            bool screech = (isDrifting || (isTractionLocked && Mathf.Abs(carSpeed) > 12f));
            if (screech && !tireScreechSound.isPlaying) tireScreechSound.Play();
            else if (!screech && tireScreechSound.isPlaying) tireScreechSound.Stop();
        }
        catch { }
    }
}