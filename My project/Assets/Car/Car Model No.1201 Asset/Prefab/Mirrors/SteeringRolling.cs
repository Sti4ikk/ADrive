using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections;


// Класс-обработчик событий в игре
public class EventManager : MonoBehaviour
{
    [SerializeField] private GameObject speedometer;

    //[Header("Turn Signals")]
    //[SerializeField] private Image leftArrow;
    //[SerializeField] private Image rightArrow;
    //[SerializeField] private Sprite whiteArrow;
    //[SerializeField] private Sprite yellowArrow;
    //[SerializeField] private float blinkInterval = 0.5f;

    //[Header("Time and Date")]
    //[SerializeField] private Text timeText;
    //[SerializeField] private Text dateText;

    [Header("Steering Wheel")]
    [SerializeField] private Transform steeringWheel;
    [SerializeField] private Transform pivotPoint; // Точка вращения
    [SerializeField] private float maxSteeringAngle = 450f; // Максимальный угол поворота руля
    [SerializeField] private float steeringSpeed = 5f; // Скорость поворота руля

    private bool isSpeedometerHidden = false;
    private bool leftSignalActive = false;
    private bool rightSignalActive = false;
    private Coroutine leftBlinkCoroutine;
    private Coroutine rightBlinkCoroutine;
    private float currentSteeringAngle = 0f;
    private float targetSteeringAngle = 0f;

    //void Start()
    //{
    //    UpdateTimeAndDate();
    //    InvokeRepeating(nameof(UpdateTimeAndDate), 1f, 1f);
    //}

    void Update()
    {
        // Скрытие/показ спидометра
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            if (speedometer != null)
            {
                isSpeedometerHidden = !isSpeedometerHidden;
                speedometer.SetActive(!isSpeedometerHidden);
            }
        }

        //// Поворотники работают только когда спидометр скрыт
        //if (isSpeedometerHidden && Keyboard.current != null)
        //{
        //    // Левый поворотник (Q)
        //    if (Keyboard.current.qKey.wasPressedThisFrame)
        //    {
        //        ToggleLeftSignal();
        //    }

        //    // Правый поворотник (E)
        //    if (Keyboard.current.eKey.wasPressedThisFrame)
        //    {
        //        ToggleRightSignal();
        //    }
        //}

        // Управление рулем
        HandleSteeringWheel();
    }

    private void HandleSteeringWheel()
    {
        if (Keyboard.current == null || steeringWheel == null || pivotPoint == null) return;

        if (Keyboard.current.aKey.isPressed)
        {
            targetSteeringAngle = maxSteeringAngle;
        }
        else if (Keyboard.current.dKey.isPressed)
        {
            targetSteeringAngle = -maxSteeringAngle;
        }
        else
        {
            targetSteeringAngle = 0f;
        }

        // Вычисляем разницу между текущим и целевым углом
        float angleDelta = targetSteeringAngle - currentSteeringAngle;

        // Ограничиваем скорость поворота постоянной величиной
        float maxDeltaThisFrame = steeringSpeed * maxSteeringAngle * Time.deltaTime;
        float smoothDelta = Mathf.Clamp(angleDelta, -maxDeltaThisFrame, maxDeltaThisFrame);

        currentSteeringAngle += smoothDelta;

        // Вращаем вокруг точки pivotPoint
        steeringWheel.RotateAround(pivotPoint.position, pivotPoint.forward, smoothDelta);
    }

    //private void UpdateTimeAndDate()
    //{
    //    DateTime now = DateTime.Now;

    //    if (timeText != null)
    //    {
    //        timeText.text = now.ToString("HH:mm");
    //    }

    //    if (dateText != null)
    //    {
    //        dateText.text = now.ToString("dd.MM.yyyy");
    //    }
    //}

    // Обработка левого поворотника
    //private void ToggleLeftSignal()
    //{
    //    leftSignalActive = !leftSignalActive;

    //    GameLogger.LogEvent("TurnSignal",
    //    $"Side: Left",
    //    $"State: {(leftSignalActive ? "ON" : "OFF")}");

    //    if (leftSignalActive)
    //    {
    //        if (rightSignalActive)
    //        {
    //            ToggleRightSignal();
    //        }

    //        leftBlinkCoroutine = StartCoroutine(BlinkArrow(leftArrow));
    //    }
    //    else
    //    {
    //        if (leftBlinkCoroutine != null)
    //        {
    //            StopCoroutine(leftBlinkCoroutine);
    //        }
    //        if (leftArrow != null)
    //        {
    //            leftArrow.sprite = whiteArrow;
    //        }
    //    }
    //}

    //// Обработка правого поворотника
    //private void ToggleRightSignal()
    //{
    //    rightSignalActive = !rightSignalActive;

    //    GameLogger.LogEvent("TurnSignal",
    //    $"Side: Right",
    //    $"State: {(rightSignalActive ? "ON" : "OFF")}");

    //    if (rightSignalActive)
    //    {
    //        if (leftSignalActive)
    //        {
    //            ToggleLeftSignal();
    //        }

    //        rightBlinkCoroutine = StartCoroutine(BlinkArrow(rightArrow));
    //    }
    //    else
    //    {
    //        if (rightBlinkCoroutine != null)
    //        {
    //            StopCoroutine(rightBlinkCoroutine);
    //        }
    //        if (rightArrow != null)
    //        {
    //            rightArrow.sprite = whiteArrow;
    //        }
    //    }
    //}

    //// Перемещение стрелки при изменении скорости
    //private IEnumerator BlinkArrow(Image arrow)
    //{
    //    while (true)
    //    {
    //        if (arrow != null)
    //        {
    //            arrow.sprite = yellowArrow;
    //        }
    //        yield return new WaitForSeconds(blinkInterval);

    //        if (arrow != null)
    //        {
    //            arrow.sprite = whiteArrow;
    //        }
    //        yield return new WaitForSeconds(blinkInterval);
    //    }
    //}

    //private void OnDisable()
    //{
    //    CancelInvoke(nameof(UpdateTimeAndDate));

    //    if (leftBlinkCoroutine != null)
    //    {
    //        StopCoroutine(leftBlinkCoroutine);
    //    }
    //    if (rightBlinkCoroutine != null)
    //    {
    //        StopCoroutine(rightBlinkCoroutine);
    //    }
    //}

    [ContextMenu("Create Steering Wheel Pivot")]
    private void CreateSteeringWheelPivot()
    {
        // Найдите руль в сцене
        Transform steeringWheel = GameObject.Find("Ford:SteeringWheel5_Mesh_007_M_Interior_Max")?.transform;

        if (steeringWheel == null)
        {
            Debug.LogError("Руль не найден!");
            return;
        }

        // Получаем центр меша руля
        MeshFilter meshFilter = steeringWheel.GetComponent<MeshFilter>();
        if (meshFilter != null)
        {
            Bounds bounds = meshFilter.sharedMesh.bounds;
            Vector3 centerWorld = steeringWheel.TransformPoint(bounds.center);

            // Создаем пустой объект в центре
            GameObject pivot = new GameObject("SteeringWheelPivot");
            pivot.transform.position = centerWorld;
            pivot.transform.rotation = steeringWheel.rotation;

            // Делаем руль дочерним объектом pivot
            Transform parent = steeringWheel.parent;
            pivot.transform.SetParent(parent);
            steeringWheel.SetParent(pivot.transform);

            Debug.Log("Pivot создан в центре руля!");
        }
    }
}