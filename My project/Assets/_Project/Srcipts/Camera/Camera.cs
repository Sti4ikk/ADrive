using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class FollowCamera : MonoBehaviour
{
    [SerializeField] private Transform _car;

    [Header("Camera Offsets")]
    [SerializeField] private Vector3 behindOffset = new Vector3(0f, 2f, -5f);
    [SerializeField] private Vector3 frontOffset = new Vector3(0f, 2f, 5f);
    [SerializeField] private Transform driverSeat;

    [SerializeField] private float _followSpeed = 10f;
    [SerializeField] private float _rotationSpeed = 5f;
    [SerializeField] private float _turnSpeed = 5f; // скорость поворота головы

    private Camera _camera;
    private bool isBehindView = true;
    private bool isFirstPerson = false;
    private Vector3 currentOffset;

    private Quaternion originalLocalRotation; // исходный локальный поворот
    private Quaternion targetLocalRotation;   // целевой локальный поворот

    private void Start()
    {
        _camera = GetComponent<Camera>();

        if (_camera != null)
        {
            _camera.enabled = true;
            _camera.fieldOfView = 77f;
            AudioListener listener = _camera.GetComponent<AudioListener>();
            if (listener != null) listener.enabled = true;
        }

        currentOffset = behindOffset;
    }

    private void Update()
    {
        HandleViewSwitch();
        HandleFirstPersonToggle();
        HandleFirstPersonLookSide();
        SmoothHeadTurn();
    }

    private void FixedUpdate()
    {
        if (_car == null || isFirstPerson)
            return; // в режиме 1-го лица камера НЕ двигается этим скриптом

        Vector3 targetPosition = _car.position + _car.rotation * currentOffset;
        transform.position = Vector3.Lerp(transform.position, targetPosition, _followSpeed * Time.deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(_car.position - transform.position, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
    }

    private void HandleViewSwitch()
    {
        if (!isFirstPerson && Keyboard.current.cKey.wasPressedThisFrame)
        {
            isBehindView = !isBehindView;
            currentOffset = isBehindView ? behindOffset : frontOffset;
        }
    }

    private void HandleFirstPersonToggle()
    {
        if (Keyboard.current.vKey.wasPressedThisFrame)
        {
            isFirstPerson = !isFirstPerson;

            if (isFirstPerson)
            {
                transform.SetParent(driverSeat);
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;

                originalLocalRotation = transform.localRotation;
                targetLocalRotation = originalLocalRotation;
            }
            else
            {
                transform.SetParent(null);
            }
        }
    }

    private void HandleFirstPersonLookSide()
    {
        if (!isFirstPerson) return;

        if (Keyboard.current.xKey.isPressed)
        {
            // Поворот вправо на 90 градусов
            targetLocalRotation = originalLocalRotation * Quaternion.Euler(0f, 60f, 0f);
        }
        else if (Keyboard.current.zKey.isPressed)
        {
            // Поворот влево на 90 градусов
            targetLocalRotation = originalLocalRotation * Quaternion.Euler(0f, -45f, 0f);
        }
        else
        {
            // Возврат в исходное положение
            targetLocalRotation = originalLocalRotation;
        }
    }

    private void SmoothHeadTurn()
    {
        if (isFirstPerson)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetLocalRotation, _turnSpeed * Time.deltaTime);
        }
    }

    public void SetTarget(Transform car)
    {
        _car = car;
    }
}
