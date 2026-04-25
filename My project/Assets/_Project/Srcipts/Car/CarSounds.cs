using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // новый Input System

public class CarSounds : MonoBehaviour
{
    [Header("Engine Settings")]
    public float minSpeed;
    public float maxSpeed;
    private float currentSpeed;

    private Rigidbody carRb;
    private AudioSource carAudio;

    public float minPitch;
    public float maxPitch;
    private float pitchFromCar;

    [Header("Extra Sounds")]
    public AudioClip brakeSound;
    public AudioClip leftIndicatorSound;
    public AudioClip rightIndicatorSound;

    private AudioSource brakeAudio;
    private AudioSource leftIndicatorAudio;
    private AudioSource rightIndicatorAudio;

    private bool leftIndicatorOn = false;
    private bool rightIndicatorOn = false;

    void Start()
    {
        carAudio = GetComponent<AudioSource>();
        carRb = GetComponent<Rigidbody>();

        // отдельные источники для независимого воспроизведения
        brakeAudio = gameObject.AddComponent<AudioSource>();
        brakeAudio.loop = true; // тормозной звук будет бесконечным

        leftIndicatorAudio = gameObject.AddComponent<AudioSource>();
        leftIndicatorAudio.loop = true; // поворотник бесконечный

        rightIndicatorAudio = gameObject.AddComponent<AudioSource>();
        rightIndicatorAudio.loop = true; // поворотник бесконечный
    }

    void Update()
    {
        EngineSound();
        HandleBrakeSound();
        HandleIndicatorSound();
    }

    void EngineSound()
    {
        currentSpeed = carRb.linearVelocity.magnitude;
        pitchFromCar = carRb.linearVelocity.magnitude / 60f;

        if (currentSpeed < minSpeed)
        {
            carAudio.pitch = minPitch;
        }
        else if (currentSpeed >= minSpeed && currentSpeed <= maxSpeed)
        {
            carAudio.pitch = minPitch + pitchFromCar;
        }
        else if (currentSpeed > maxSpeed)
        {
            carAudio.pitch = maxPitch;
        }
    }

    void HandleBrakeSound()
    {
        if (Keyboard.current.sKey.isPressed)
        {
            if (!brakeAudio.isPlaying && brakeSound != null)
            {
                brakeAudio.clip = brakeSound;
                brakeAudio.Play();
            }
        }
        else
        {
            if (brakeAudio.isPlaying)
                brakeAudio.Stop();
        }
    }

    void HandleIndicatorSound()
    {
        // Левый поворотник (Q)
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            leftIndicatorOn = !leftIndicatorOn;
            if (leftIndicatorOn && leftIndicatorSound != null)
            {
                leftIndicatorAudio.clip = leftIndicatorSound;
                leftIndicatorAudio.Play();
            }
            else
            {
                leftIndicatorAudio.Stop();
            }
        }

        // Правый поворотник (E)
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            rightIndicatorOn = !rightIndicatorOn;
            if (rightIndicatorOn && rightIndicatorSound != null)
            {
                rightIndicatorAudio.clip = rightIndicatorSound;
                rightIndicatorAudio.Play();
            }
            else
            {
                rightIndicatorAudio.Stop();
            }
        }
    }
}
