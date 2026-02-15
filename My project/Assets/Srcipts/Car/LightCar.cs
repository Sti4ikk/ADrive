using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class LightCar : MonoBehaviour
{
    public GameObject Forward;
    public GameObject Back;
    public GameObject Turnsignal_R;
    public GameObject Turnsignal_L;

    private Keyboard kb;
    private bool isRightSignalOn = false;
    private bool isLeftSignalOn = false;

    private bool forwardOn = false;
    private bool backOn = false;

    private void Start()
    {
        // Проверка на null
        SafeSetActive(Forward, false);
        SafeSetActive(Back, false);
        SafeSetActive(Turnsignal_R, false);
        SafeSetActive(Turnsignal_L, false);

        kb = Keyboard.current;

        if (kb == null)
        {

        }
    }

    void Update()
    {
        if (kb == null)
            return;

        // Передний свет
        if (kb.hKey.wasPressedThisFrame)
            forwardOn = true;

        if (kb.gKey.wasPressedThisFrame)
            forwardOn = false;

        // Задний свет
        if (kb.sKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
            backOn = true;

        if (kb.sKey.wasReleasedThisFrame || kb.spaceKey.wasReleasedThisFrame)
            backOn = false;

        // Правый поворотник
        if (kb.eKey.wasPressedThisFrame)
        {
            ToggleRightSignal();
        }

        // Левый поворотник
        if (kb.qKey.wasPressedThisFrame)
        {
            ToggleLeftSignal();
        }

        SafeSetActive(Forward, forwardOn);
        SafeSetActive(Back, backOn);
    }

    void ToggleRightSignal()
    {
        if (!isRightSignalOn)
        {
            if (isLeftSignalOn)
            {
                isLeftSignalOn = false;
                CancelInvoke(nameof(BlinkLeftSignal));
                SafeSetActive(Turnsignal_L, false);
            }

            isRightSignalOn = true;
            InvokeRepeating(nameof(BlinkRightSignal), 0f, 0.5f);
        }
        else
        {
            isRightSignalOn = false;
            CancelInvoke(nameof(BlinkRightSignal));
            SafeSetActive(Turnsignal_R, false);
        }
    }

    void ToggleLeftSignal()
    {
        if (!isLeftSignalOn)
        {
            if (isRightSignalOn)
            {
                isRightSignalOn = false;
                CancelInvoke(nameof(BlinkRightSignal));
                SafeSetActive(Turnsignal_R, false);
            }

            isLeftSignalOn = true;
            InvokeRepeating(nameof(BlinkLeftSignal), 0f, 0.5f);
        }
        else
        {
            isLeftSignalOn = false;
            CancelInvoke(nameof(BlinkLeftSignal));
            SafeSetActive(Turnsignal_L, false);
        }
    }


    void BlinkRightSignal()
    {
        SafeToggle(Turnsignal_R);
    }

    void BlinkLeftSignal()
    {
        SafeToggle(Turnsignal_L);
    }


    void SafeSetActive(GameObject obj, bool state)
    {
        if (obj != null)
            obj.SetActive(state);
    }

    void SafeToggle(GameObject obj)
    {
        if (obj != null)
            obj.SetActive(!obj.activeSelf);
    }
}
