using UnityEngine;


// Класс логики падения знаков
public class SignBreaker : MonoBehaviour
{
    [Header("Ссылка на сам знак (не на основание!)")]
    public GameObject sign;
    [Header("Сила отбрасывания")]
    public float forceMultiplier = 1f;

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Car")) return;

        float velocity = collision.relativeVelocity.magnitude;

        if (velocity > 8f)
        {
            // ЛОГИРОВАНИЕ СТОЛКНОВЕНИЯ
            GameLogger.LogCollision($"Sign: {gameObject.name}", velocity);
            GameLogger.LogEvent("SignBroken", $"Sign: {gameObject.name}", $"Force: {forceMultiplier}");

            // Ломаем соединение
            var joint = sign.GetComponent<FixedJoint>();
            if (joint != null) Destroy(joint);

            // Включаем физику знака
            var rb = sign.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.AddForce(collision.relativeVelocity * forceMultiplier, ForceMode.Impulse);
            }

            Destroy(gameObject, 0.3f);
        }
        else
        {
            // Логируем слабое столкновение
            GameLogger.Log(GameLogger.LogCategory.Game,
                $"Weak collision with sign (velocity: {velocity:F2})");
        }
    }
}