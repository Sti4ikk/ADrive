using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;


public class PrometeoCarController : MonoBehaviour
{
    [Space(10)]
    [Range(20, 190)] public int maxSpeed = 90;
    [Range(10, 120)] public int maxReverseSpeed = 45;
    [Range(1, 10)] public int accelerationMultiplier = 2;

    [Space(10)]
    [Range(10, 45)] public int maxSteeringAngle = 27;
    [Range(0.1f, 1f)] public float steeringSpeed = 0.5f;

    [Space(10)]
    [Range(100, 1000)] public int brakeForce = 350;
    [Range(0, 10)] public int decelerationMultiplier = 2;
    [Range(1, 10)] public int handbrakeDriftMultiplier = 5;

    [Space(10)] public Vector3 bodyMassCenter;

    public GameObject frontLeftMesh;
    public WheelCollider frontLeftCollider;

    public GameObject frontRightMesh;
    public WheelCollider frontRightCollider;

    public GameObject rearLeftMesh;
    public WheelCollider rearLeftCollider;

    public GameObject rearRightMesh;
    public WheelCollider rearRightCollider;

    [Space(10)] public bool useEffects = false;
    public ParticleSystem RLWParticleSystem;
    public ParticleSystem RRWParticleSystem;

    public TrailRenderer RLWTireSkid;
    public TrailRenderer RRWTireSkid;

    [Space(10)] public bool useUI = false;
    public Text carSpeedText;

    [Space(10)] public bool useSounds = false;
    public AudioSource carEngineSound;
    public AudioSource tireScreechSound;
    float initialCarEngineSoundPitch;

    [Space(10)] public bool useTouchControls = false;
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
    float throttleAxis;
    float driftingAxis;
    float localVelocityZ;
    float localVelocityX;
    bool deceleratingCar;
    bool touchControlsSetup = false;

    WheelFrictionCurve FLwheelFriction; float FLWextremumSlip;
    WheelFrictionCurve FRwheelFriction; float FRWextremumSlip;
    WheelFrictionCurve RLwheelFriction; float RLWextremumSlip;
    WheelFrictionCurve RRwheelFriction; float RRWextremumSlip;


    private Vector3 lastPosition;

    void Start()
    {
        lastPosition = transform.position;


        carRigidbody = gameObject.GetComponent<Rigidbody>();
        carRigidbody.centerOfMass = bodyMassCenter;

        FLwheelFriction = new WheelFrictionCurve();
        FLwheelFriction.extremumSlip = frontLeftCollider.sidewaysFriction.extremumSlip;
        FLWextremumSlip = frontLeftCollider.sidewaysFriction.extremumSlip;
        FLwheelFriction.extremumValue = frontLeftCollider.sidewaysFriction.extremumValue;
        FLwheelFriction.asymptoteSlip = frontLeftCollider.sidewaysFriction.asymptoteSlip;
        FLwheelFriction.asymptoteValue = frontLeftCollider.sidewaysFriction.asymptoteValue;
        FLwheelFriction.stiffness = frontLeftCollider.sidewaysFriction.stiffness;

        FRwheelFriction = new WheelFrictionCurve();
        FRwheelFriction.extremumSlip = frontRightCollider.sidewaysFriction.extremumSlip;
        FRWextremumSlip = frontRightCollider.sidewaysFriction.extremumSlip;
        FRwheelFriction.extremumValue = frontRightCollider.sidewaysFriction.extremumValue;
        FRwheelFriction.asymptoteSlip = frontRightCollider.sidewaysFriction.asymptoteSlip;
        FRwheelFriction.asymptoteValue = frontRightCollider.sidewaysFriction.asymptoteValue;
        FRwheelFriction.stiffness = frontRightCollider.sidewaysFriction.stiffness;

        RLwheelFriction = new WheelFrictionCurve();
        RLwheelFriction.extremumSlip = rearLeftCollider.sidewaysFriction.extremumSlip;
        RLWextremumSlip = rearLeftCollider.sidewaysFriction.extremumSlip;
        RLwheelFriction.extremumValue = rearLeftCollider.sidewaysFriction.extremumValue;
        RLwheelFriction.asymptoteSlip = rearLeftCollider.sidewaysFriction.asymptoteSlip;
        RLwheelFriction.asymptoteValue = rearLeftCollider.sidewaysFriction.asymptoteValue;
        RLwheelFriction.stiffness = rearLeftCollider.sidewaysFriction.stiffness;

        RRwheelFriction = new WheelFrictionCurve();
        RRwheelFriction.extremumSlip = rearRightCollider.sidewaysFriction.extremumSlip;
        RRWextremumSlip = rearRightCollider.sidewaysFriction.extremumSlip;
        RRwheelFriction.extremumValue = rearRightCollider.sidewaysFriction.extremumValue;
        RRwheelFriction.asymptoteSlip = rearRightCollider.sidewaysFriction.asymptoteSlip;
        RRwheelFriction.asymptoteValue = rearRightCollider.sidewaysFriction.asymptoteValue;
        RRwheelFriction.stiffness = rearRightCollider.sidewaysFriction.stiffness;

        if (carEngineSound != null) initialCarEngineSoundPitch = carEngineSound.pitch;

        if (useUI) InvokeRepeating("CarSpeedUI", 0f, 0.1f);

        if (useSounds) InvokeRepeating("CarSounds", 0f, 0.1f);

        if (!useEffects)
        {
            if (RLWParticleSystem != null) RLWParticleSystem.Stop();
            if (RRWParticleSystem != null) RRWParticleSystem.Stop();
            if (RLWTireSkid != null) RLWTireSkid.emitting = false;
            if (RRWTireSkid != null) RRWTireSkid.emitting = false;
        }

        if (useTouchControls)
        {
            if (throttleButton != null && reverseButton != null &&
                turnRightButton != null && turnLeftButton != null && handbrakeButton != null)
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

    void Update()
    {
        carSpeed = (2 * Mathf.PI * frontLeftCollider.radius * frontLeftCollider.rpm * 60) / 1000;
        localVelocityX = transform.InverseTransformDirection(carRigidbody.linearVelocity).x;
        localVelocityZ = transform.InverseTransformDirection(carRigidbody.linearVelocity).z;

        // Touch controls
        if (useTouchControls && touchControlsSetup)
        {
            if (throttlePTI.buttonPressed) { CancelInvoke("DecelerateCar"); deceleratingCar = false; GoForward(); }
            if (reversePTI.buttonPressed) { CancelInvoke("DecelerateCar"); deceleratingCar = false; GoReverse(); }

            if (turnLeftPTI.buttonPressed) TurnLeft();
            if (turnRightPTI.buttonPressed) TurnRight();

            if (handbrakePTI.buttonPressed) { CancelInvoke("DecelerateCar"); deceleratingCar = false; Handbrake(); }
            if (!handbrakePTI.buttonPressed) RecoverTraction();

            if (!throttlePTI.buttonPressed && !reversePTI.buttonPressed) ThrottleOff();

            if ((!reversePTI.buttonPressed && !throttlePTI.buttonPressed) && !handbrakePTI.buttonPressed && !deceleratingCar)
            {
                InvokeRepeating("DecelerateCar", 0f, 0.1f);
                deceleratingCar = true;
            }

            if (!turnLeftPTI.buttonPressed && !turnRightPTI.buttonPressed && steeringAxis != 0f)
                ResetSteeringAngle();
        }
        else
        {
            // Keyboard controls
            if (Keyboard.current.wKey.isPressed) { CancelInvoke("DecelerateCar"); deceleratingCar = false; GoForward(); }
            if (Keyboard.current.sKey.isPressed) { CancelInvoke("DecelerateCar"); deceleratingCar = false; GoReverse(); }

            if (Keyboard.current.aKey.isPressed) TurnLeft();
            if (Keyboard.current.dKey.isPressed) TurnRight();

            if (Keyboard.current.spaceKey.isPressed) { CancelInvoke("DecelerateCar"); deceleratingCar = false; HandBrake(); }

            if (!Keyboard.current.wKey.isPressed && !Keyboard.current.sKey.isPressed) ThrottleOff();

            if (!Keyboard.current.wKey.isPressed && !Keyboard.current.sKey.isPressed && !Keyboard.current.spaceKey.isPressed && !deceleratingCar)
            {
                InvokeRepeating("DecelerateCar", 0f, 0.1f);
                deceleratingCar = true;
            }

            if (!Keyboard.current.aKey.isPressed && !Keyboard.current.dKey.isPressed && steeringAxis != 0f)
                ResetSteeringAngle();
        }

        AnimateWheelMeshes();
        if (useEffects) DriftCarPS();


        // Считаем расстояние
        float distance = Vector3.Distance(transform.position, lastPosition);


        lastPosition = transform.position;
    }

    public void CarSpeedUI()
    {
        if (useUI)
        {
            try
            {
                float absoluteCarSpeed = Mathf.Abs(carSpeed);
                carSpeedText.text = Mathf.RoundToInt(absoluteCarSpeed).ToString();
            }
            catch (Exception ex) { Debug.LogWarning(ex); }
        }
    }

    public void CarSounds()
    {
        if (!useSounds) return;

        try
        {
            if (carEngineSound != null)
            {
                float engineSoundPitch = initialCarEngineSoundPitch + (Mathf.Abs(carRigidbody.linearVelocity.magnitude) / 25f);
                carEngineSound.pitch = engineSoundPitch;
            }
            if ((isDrifting) || (isTractionLocked && Mathf.Abs(carSpeed) > 12f))
            {
                if (!tireScreechSound.isPlaying) tireScreechSound.Play();
            }
            else
            {
                if (tireScreechSound.isPlaying) tireScreechSound.Stop();
            }
        }
        catch {}
    }

    public void TurnLeft()
    {
        steeringAxis -= (Time.deltaTime * 10f * steeringSpeed);
        if (steeringAxis < -1f) steeringAxis = -1f;
        var steeringAngle = steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    public void TurnRight()
    {
        steeringAxis += (Time.deltaTime * 10f * steeringSpeed);
        if (steeringAxis > 1f) steeringAxis = 1f;
        var steeringAngle = steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    public void ResetSteeringAngle()
    {
        if (steeringAxis < 0f) steeringAxis += (Time.deltaTime * 10f * steeringSpeed);
        else if (steeringAxis > 0f) steeringAxis -= (Time.deltaTime * 10f * steeringSpeed);
        if (Mathf.Abs(frontLeftCollider.steerAngle) < 1f) steeringAxis = 0f;
        var steeringAngle = steeringAxis * maxSteeringAngle;
        frontLeftCollider.steerAngle = Mathf.Lerp(frontLeftCollider.steerAngle, steeringAngle, steeringSpeed);
        frontRightCollider.steerAngle = Mathf.Lerp(frontRightCollider.steerAngle, steeringAngle, steeringSpeed);
    }

    void AnimateWheelMeshes()
    {
        try
        {
            Quaternion FLWRotation; Vector3 FLWPosition;
            frontLeftCollider.GetWorldPose(out FLWPosition, out FLWRotation);
            frontLeftMesh.transform.position = FLWPosition;
            frontLeftMesh.transform.rotation = FLWRotation;

            Quaternion FRWRotation; Vector3 FRWPosition;
            frontRightCollider.GetWorldPose(out FRWPosition, out FRWRotation);
            frontRightMesh.transform.position = FRWPosition;
            frontRightMesh.transform.rotation = FRWRotation * Quaternion.Euler(0f, 180f, 0f);

            Quaternion RLWRotation; Vector3 RLWPosition;
            rearLeftCollider.GetWorldPose(out RLWPosition, out RLWRotation);
            rearLeftMesh.transform.position = RLWPosition;
            rearLeftMesh.transform.rotation = RLWRotation;

            Quaternion RRWRotation; Vector3 RRWPosition;
            rearRightCollider.GetWorldPose(out RRWPosition, out RRWRotation);
            rearRightMesh.transform.position = RRWPosition;
            rearRightMesh.transform.rotation = RRWRotation * Quaternion.Euler(0f, 180f, 0f);
        }
        catch {}
    }

    public void GoForward()
    {
        if (Mathf.Abs(localVelocityX) > 2.5f) { isDrifting = true; }
        else { isDrifting = false; }

        throttleAxis += (Time.deltaTime * 3f);
        if (throttleAxis > 1f) throttleAxis = 1f;

        if (localVelocityZ < -1f) Brakes();
        else
        {
            if (Mathf.RoundToInt(carSpeed) < maxSpeed)
            {
                frontLeftCollider.brakeTorque = 0; frontLeftCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
                frontRightCollider.brakeTorque = 0; frontRightCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
                rearLeftCollider.brakeTorque = 0; rearLeftCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
                rearRightCollider.brakeTorque = 0; rearRightCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
            }
            else
            {
                frontLeftCollider.motorTorque = 0; frontRightCollider.motorTorque = 0;
                rearLeftCollider.motorTorque = 0; rearRightCollider.motorTorque = 0;
            }
        }
    }

    public void GoReverse()
    {
        if (Mathf.Abs(localVelocityX) > 2.5f) { isDrifting = true; }
        else { isDrifting = false; }

        throttleAxis -= (Time.deltaTime * 3f);
        if (throttleAxis < -1f) throttleAxis = -1f;

        if (localVelocityZ > 1f) Brakes();
        else
        {
            if (Mathf.Abs(Mathf.RoundToInt(carSpeed)) < maxReverseSpeed)
            {
                frontLeftCollider.brakeTorque = 0; frontLeftCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
                frontRightCollider.brakeTorque = 0; frontRightCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
                rearLeftCollider.brakeTorque = 0; rearLeftCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
                rearRightCollider.brakeTorque = 0; rearRightCollider.motorTorque = (accelerationMultiplier * 50f) * throttleAxis;
            }
            else
            {
                frontLeftCollider.motorTorque = 0; frontRightCollider.motorTorque = 0;
                rearLeftCollider.motorTorque = 0; rearRightCollider.motorTorque = 0;
            }
        }
    }

    public void HandBrake()
    {
        if (Mathf.Abs(localVelocityX) > 2.5f)
        {
            isDrifting = true;
        }
        else
        {
            isDrifting = false;
        }

        // Проверка: если скорость почти нулевая, просто тормозим и не едем назад
        if (Mathf.Abs(carSpeed) < 1f)
        {
            Brakes();
            throttleAxis = 0f;
            return; // Выходим из метода, не применяя движение назад
        }

        throttleAxis -= (Time.deltaTime * 3f);
        if (throttleAxis < -1f) throttleAxis = -1f;

        if (localVelocityZ > 1f)
        {
            Brakes();
        }
        else
        {
            if (Mathf.Abs(Mathf.RoundToInt(carSpeed)) < maxReverseSpeed)
            {
                frontLeftCollider.brakeTorque = 0;
                frontLeftCollider.motorTorque = (accelerationMultiplier * 200f) * throttleAxis;
                frontRightCollider.brakeTorque = 0;
                frontRightCollider.motorTorque = (accelerationMultiplier * 200f) * throttleAxis;
                rearLeftCollider.brakeTorque = 0;
                rearLeftCollider.motorTorque = (accelerationMultiplier * 200f) * throttleAxis;
                rearRightCollider.brakeTorque = 0;
                rearRightCollider.motorTorque = (accelerationMultiplier * 200f) * throttleAxis;
            }
            else
            {
                frontLeftCollider.motorTorque = 0;
                frontRightCollider.motorTorque = 0;
                rearLeftCollider.motorTorque = 0;
                rearRightCollider.motorTorque = 0;
            }
        }
    }

    public void ThrottleOff()
    {
        frontLeftCollider.motorTorque = 0;
        frontRightCollider.motorTorque = 0;
        rearLeftCollider.motorTorque = 0;
        rearRightCollider.motorTorque = 0;
    }

    public void DecelerateCar()
    {
        if (Mathf.Abs(localVelocityX) > 2.5f) isDrifting = true;
        else isDrifting = false;

        if (throttleAxis != 0f)
        {
            if (throttleAxis > 0f) throttleAxis -= (Time.deltaTime * 10f);
            else if (throttleAxis < 0f) throttleAxis += (Time.deltaTime * 10f);
            if (Mathf.Abs(throttleAxis) < 0.15f) throttleAxis = 0f;
        }

        carRigidbody.linearVelocity = carRigidbody.linearVelocity * (1f / (1f + (0.025f * decelerationMultiplier)));

        frontLeftCollider.motorTorque = 0;
        frontRightCollider.motorTorque = 0;
        rearLeftCollider.motorTorque = 0;
        rearRightCollider.motorTorque = 0;

        if (carRigidbody.linearVelocity.magnitude < 0.25f)
        {
            carRigidbody.linearVelocity = Vector3.zero;
            CancelInvoke("DecelerateCar");
        }
    }

    public void Brakes()
    {
        frontLeftCollider.brakeTorque = brakeForce;
        frontRightCollider.brakeTorque = brakeForce;
        rearLeftCollider.brakeTorque = brakeForce;
        rearRightCollider.brakeTorque = brakeForce;
    }

    public void Handbrake()
    {
        CancelInvoke("RecoverTraction");

        driftingAxis += Time.deltaTime;
        float secureStartingPoint = driftingAxis * FLWextremumSlip * handbrakeDriftMultiplier;
        if (secureStartingPoint < FLWextremumSlip)
            driftingAxis = FLWextremumSlip / (FLWextremumSlip * handbrakeDriftMultiplier);
        if (driftingAxis > 1f) driftingAxis = 1f;

        isDrifting = Mathf.Abs(localVelocityX) > 2.5f;

        if (driftingAxis < 1f)
        {
            FLwheelFriction.extremumSlip = FLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            frontLeftCollider.sidewaysFriction = FLwheelFriction;

            FRwheelFriction.extremumSlip = FRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            frontRightCollider.sidewaysFriction = FRwheelFriction;

            RLwheelFriction.extremumSlip = RLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            rearLeftCollider.sidewaysFriction = RLwheelFriction;

            RRwheelFriction.extremumSlip = RRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            rearRightCollider.sidewaysFriction = RRwheelFriction;
        }

        isTractionLocked = true;
    }

    public void DriftCarPS()
    {
        if (!useEffects) return;

        try
        {
            if (isDrifting) { RLWParticleSystem.Play(); RRWParticleSystem.Play(); }
            else { RLWParticleSystem.Stop(); RRWParticleSystem.Stop(); }
        }
        catch {}

        try
        {
            if ((isTractionLocked || Mathf.Abs(localVelocityX) > 5f) && Mathf.Abs(carSpeed) > 12f)
            {
                RLWTireSkid.emitting = true;
                RRWTireSkid.emitting = true;
            }
            else
            {
                RLWTireSkid.emitting = false;
                RRWTireSkid.emitting = false;
            }
        }
        catch {}
    }

    public void RecoverTraction()
    {
        isTractionLocked = false;
        driftingAxis -= (Time.deltaTime / 1.5f);
        if (driftingAxis < 0f) driftingAxis = 0f;

        if (FLwheelFriction.extremumSlip > FLWextremumSlip)
        {
            FLwheelFriction.extremumSlip = FLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            frontLeftCollider.sidewaysFriction = FLwheelFriction;

            FRwheelFriction.extremumSlip = FRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            frontRightCollider.sidewaysFriction = FRwheelFriction;

            RLwheelFriction.extremumSlip = RLWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            rearLeftCollider.sidewaysFriction = RLwheelFriction;

            RRwheelFriction.extremumSlip = RRWextremumSlip * handbrakeDriftMultiplier * driftingAxis;
            rearRightCollider.sidewaysFriction = RRwheelFriction;

            Invoke("RecoverTraction", Time.deltaTime);
        }
        else if (FLwheelFriction.extremumSlip < FLWextremumSlip)
        {
            FLwheelFriction.extremumSlip = FLWextremumSlip; frontLeftCollider.sidewaysFriction = FLwheelFriction;
            FRwheelFriction.extremumSlip = FRWextremumSlip; frontRightCollider.sidewaysFriction = FRwheelFriction;
            RLwheelFriction.extremumSlip = RLWextremumSlip; rearLeftCollider.sidewaysFriction = RLwheelFriction;
            RRwheelFriction.extremumSlip = RRWextremumSlip; rearRightCollider.sidewaysFriction = RRwheelFriction;
            driftingAxis = 0f;
        }
    }
}
