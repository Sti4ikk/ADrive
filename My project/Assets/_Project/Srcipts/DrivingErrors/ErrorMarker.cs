using UnityEngine;

/// <summary>
/// Вешается на prefab маркера (конус/знак).
/// При въезде игрока в триггер — открывает окно разбора ошибки.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ErrorMarker : MonoBehaviour
{
    [Header("Визуал")]
    [Tooltip("Анимация пульсации маркера (необязательно)")]
    public bool pulseAnimation = true;
    public float pulseSpeed = 2f;
    public float pulseScale = 0.15f;

    [Header("Триггер")]
    [Tooltip("Тег машины игрока")]
    public string playerTag = "Player";

    // Данные ошибки — задаются через Initialize()
    private DrivingError _error;
    private bool _triggered = false;
    private Vector3 _baseScale;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        _baseScale = transform.localScale;
        Debug.Log($"[ErrorMarker] Awake — коллайдер: {col.GetType().Name}, isTrigger: {col.isTrigger}");
    }

    public void Initialize(DrivingError error)
    {
        _error = error;
        Debug.Log($"[ErrorMarker] Initialize — ошибка: {error.TypeName}, позиция: {transform.position}");

        if (TryGetComponent<Renderer>(out var rend))
        {
            rend.material.color = error.type switch
            {
                DrivingError.ErrorType.Speeding => new Color(1f, 0.5f, 0f),
                DrivingError.ErrorType.RedLight => Color.red,
                DrivingError.ErrorType.WrongLane => Color.yellow,
                DrivingError.ErrorType.WrongManeuver => Color.magenta,
                _ => Color.white
            };
        }
    }

    private void Update()
    {
        if (!pulseAnimation) return;
        float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseScale;
        transform.localScale = _baseScale * scale;
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[ErrorMarker] OnTriggerEnter: объект='{other.gameObject.name}' тег='{other.tag}'");

        if (_triggered)
        {
            Debug.Log("[ErrorMarker] Пропуск — триггер уже срабатывал");
            return;
        }

        if (!other.CompareTag(playerTag))
        {
            Debug.Log($"[ErrorMarker] Пропуск — тег '{other.tag}' не совпадает с playerTag='{playerTag}'");
            return;
        }

        if (_error == null)
        {
            Debug.LogWarning("[ErrorMarker] _error == null — маркер не был инициализирован через Initialize()!");
            return;
        }

        _triggered = true;
        Debug.Log($"[ErrorMarker] ✅ Триггер сработал! Ошибка: {_error.TypeName}");

        if (ErrorReviewUI.Instance != null)
        {
            Debug.Log("[ErrorMarker] Открываю ErrorReviewUI...");
            ErrorReviewUI.Instance.Show(_error);
        }
        else
        {
            Debug.LogError("[ErrorMarker] ❌ ErrorReviewUI.Instance == null — UIManager не найден на сцене!");
        }
    }

    public void DestroyMarker()
    {
        Destroy(gameObject, 0.3f);
    }
}