using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Speedometer : MonoBehaviour
{
    public Rigidbody target;
    public float maxSpeed = 0.0f; // The maximum speed of the target ** IN KM/H **
    public float minSpeedArrowAngle;
    public float maxSpeedArrowAngle;
    [Header("UI")]
    public Text speedLabel; // The label that displays the speed;
    public RectTransform arrow; // The arrow in the speedometer

    private float speed = 0.0f;

    private void Update()
    {
        // Получаем скорость в м/с
        Vector3 velocity = target.linearVelocity;
        float speedMagnitude = velocity.magnitude * 3.6f; // конвертируем в км/ч

        // Определяем направление движения (вперед или назад)
        float direction = Vector3.Dot(velocity, target.transform.forward);

        // Если движемся назад, делаем скорость отрицательной
        if (direction < 0)
        {
            speed = -speedMagnitude;
        }
        else
        {
            speed = speedMagnitude;
        }

        if (speedLabel != null)
            speedLabel.text = ((int)speed) + " km/h";

        if (arrow != null)
            arrow.localEulerAngles =
                new Vector3(0, 0, Mathf.Lerp(minSpeedArrowAngle, maxSpeedArrowAngle, Mathf.Abs(speed) / maxSpeed / 2.3f));
    }
}