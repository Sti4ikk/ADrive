using UnityEngine;

public class SteeringWheelRotation : MonoBehaviour
{
    [Header("Настройки руля")]
    [Tooltip("Максимальный угол поворота руля в градусах")]
    public float maxSteeringAngle = 450f; // Обычно руль поворачивается на 1.25 оборота в каждую сторону

    [Tooltip("Скорость возврата руля в нейтральное положение")]
    public float returnSpeed = 200f;

    [Tooltip("Скорость поворота руля")]
    public float rotationSpeed = 300f;

    [Header("Ссылки")]
    [Tooltip("Ссылка на скрипт управления автомобилем или Rigidbody")]
    public Rigidbody carRigidbody;

    private float currentSteeringAngle = 0f;
    private float targetSteeringAngle = 0f;

    void Update()
    {
        // Получаем input от игрока
        float horizontalInput = Input.GetAxis("Horizontal");

        // Вычисляем целевой угол поворота руля
        targetSteeringAngle = horizontalInput * maxSteeringAngle;

        // Плавно поворачиваем руль к целевому углу
        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            currentSteeringAngle = Mathf.MoveTowards(
                currentSteeringAngle,
                targetSteeringAngle,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            // Возврат руля в центральное положение при отпускании кнопок
            currentSteeringAngle = Mathf.MoveTowards(
                currentSteeringAngle,
                0f,
                returnSpeed * Time.deltaTime
            );
        }

        // Применяем вращение к рулю (вращение вокруг оси Z для стандартной ориентации)
        transform.localRotation = Quaternion.Euler(0f, 0f, -currentSteeringAngle);
    }
}