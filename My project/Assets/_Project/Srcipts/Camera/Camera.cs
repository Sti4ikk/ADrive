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
    [SerializeField] private float _turnSpeed = 5f;

    private const float defaultFOV = 82f;
    private const float minFOV = 40f;

    private Camera _camera;
    private bool isBehindView = true;
    private bool isFirstPerson = false;
    private Vector3 currentOffset;
    private Quaternion originalLocalRotation;
    private Quaternion targetLocalRotation;
    private float targetFOV = defaultFOV;

    private void Start()
    {
        _camera = GetComponent<Camera>();
        if (_camera != null)
        {
            _camera.enabled = true;
            _camera.fieldOfView = defaultFOV;
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
        HandleFOVScroll();
    }

    private void FixedUpdate()
    {
        if (_car == null || isFirstPerson)
            return;

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
                // Сброс FOV при выходе из первого лица
                targetFOV = defaultFOV;
                if (_camera != null)
                    _camera.fieldOfView = defaultFOV;
                transform.SetParent(null);
            }
        }
    }

    private void HandleFirstPersonLookSide()
    {
        if (!isFirstPerson) return;

        if (Keyboard.current.xKey.isPressed)
        {
            targetLocalRotation = originalLocalRotation * Quaternion.Euler(0f, 60f, 0f);
        }
        else if (Keyboard.current.zKey.isPressed)
        {
            targetLocalRotation = originalLocalRotation * Quaternion.Euler(0f, -45f, 0f);
        }
        else
        {
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

    private void HandleFOVScroll()
    {
        if (_camera == null || !isFirstPerson) return;

        float scroll = Mouse.current.scroll.ReadValue().y;
        if (scroll != 0f)
        {
            targetFOV -= scroll*9;
            targetFOV = Mathf.Clamp(targetFOV, minFOV, defaultFOV);
        }

        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, targetFOV, Time.deltaTime * 10f);
    }

    public void SetTarget(Transform car)
    {
        _car = car;
    }
}