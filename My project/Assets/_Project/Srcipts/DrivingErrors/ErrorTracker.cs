using UnityEngine;

/// <summary>
/// Вешается на машину игрока.
/// Следит за нарушениями и спавнит маркеры ошибок на дороге.
/// </summary>
public class ErrorTracker : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Разрешённая скорость по умолчанию (км/ч)")]
    public float defaultSpeedLimit = 60f;

    [Tooltip("Задержка перед появлением маркера после ошибки (сек)")]
    public float markerSpawnDelay = 2f;

    [Tooltip("Prefab маркера (3D объект — конус, знак и т.д.)")]
    public GameObject errorMarkerPrefab;

    [Tooltip("На сколько метров ВПЕРЁД от машины появится конус")]
    public float markerAheadDistance = 15f;

    [Tooltip("Смещение вправо (на обочину), чтобы не мешал движению")]
    public float markerSideOffset = 2.5f;

    // Текущий лимит скорости (можно менять через SpeedZone триггеры)
    [HideInInspector] public float currentSpeedLimit;

    private Rigidbody _rb;
    private bool _speedingActive = false;  // идёт ли сейчас нарушение скорости
    private float _speedingStartTime;
    private Vector3 _speedingStartPos;
    private float _maxSpeedDuringViolation;

    // Задержка после красного — чтобы не спавнить несколько маркеров подряд
    private float _redLightCooldown = 0f;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        currentSpeedLimit = defaultSpeedLimit;
    }

    private void Update()
    {
        float speed = GetSpeedKmh();

        // ── Контроль превышения скорости ─────────────────────────────────────
        if (speed > currentSpeedLimit + 5f)   // +5 км/ч допуск
        {
            if (!_speedingActive)
            {
                _speedingActive = true;
                _speedingStartTime = Time.time;
                _speedingStartPos = transform.position;
                _maxSpeedDuringViolation = speed;
                Debug.Log($"[ErrorTracker] 🚨 Начало превышения: {speed:F0} км/ч (лимит {currentSpeedLimit})");
            }
            else
            {
                _maxSpeedDuringViolation = Mathf.Max(_maxSpeedDuringViolation, speed);
            }
        }
        else if (_speedingActive)
        {
            _speedingActive = false;
            Debug.Log($"[ErrorTracker] ✅ Превышение закончилось. Макс: {_maxSpeedDuringViolation:F0} км/ч. Спавню маркер через {markerSpawnDelay} сек...");
            RegisterError(
                DrivingError.ErrorType.Speeding,
                $"Превышение скорости: {_maxSpeedDuringViolation:F0} км/ч при лимите {currentSpeedLimit} км/ч",
                _maxSpeedDuringViolation,
                currentSpeedLimit,
                _speedingStartPos
            );
        }

        if (_redLightCooldown > 0f)
            _redLightCooldown -= Time.deltaTime;
    }

    // ── Публичные методы вызова из других скриптов ───────────────────────────

    /// <summary>Вызывается из триггера светофора</summary>
    public void RegisterRedLight()
    {
        if (_redLightCooldown > 0f) return;
        _redLightCooldown = 5f;

        RegisterError(
            DrivingError.ErrorType.RedLight,
            "Проезд на запрещающий сигнал светофора",
            GetSpeedKmh(),
            0f,
            transform.position
        );
    }

    /// <summary>Вызывается из триггеров разметки / других зон</summary>
    public void RegisterCustomError(string description, DrivingError.ErrorType type = DrivingError.ErrorType.Custom)
    {
        RegisterError(type, description, GetSpeedKmh(), currentSpeedLimit, transform.position);
    }

    // ── Внутренняя логика ────────────────────────────────────────────────────

    private void RegisterError(DrivingError.ErrorType type, string description,
                                float speed, float limit, Vector3 position)
    {
        var error = new DrivingError(type, description, speed, limit, position);
        Debug.Log($"[ErrorTracker] Зафиксирована ошибка: {error.TypeName} | {description}");

        // Спавним маркер с задержкой
        StartCoroutine(SpawnMarkerDelayed(error));
    }

    private System.Collections.IEnumerator SpawnMarkerDelayed(DrivingError error)
    {
        Debug.Log($"[ErrorTracker] Ожидаю {markerSpawnDelay} сек перед спавном маркера...");
        yield return new WaitForSeconds(markerSpawnDelay);

        if (errorMarkerPrefab == null)
        {
            Debug.LogError("[ErrorTracker] ❌ errorMarkerPrefab не назначен в Inspector!");
            yield break;
        }

        Vector3 spawnPos = transform.position
                         + transform.forward * markerAheadDistance
                         + transform.right * markerSideOffset;

        if (Physics.Raycast(spawnPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f))
        {
            spawnPos = hit.point;
            Debug.Log($"[ErrorTracker] Raycast попал в: {hit.collider.gameObject.name}");
        }
        else
        {
            Debug.LogWarning("[ErrorTracker] ⚠ Raycast не попал в поверхность — маркер появится в воздухе!");
        }

        Debug.Log($"[ErrorTracker] Спавню маркер на позиции {spawnPos}");
        GameObject markerGO = Instantiate(errorMarkerPrefab, spawnPos, Quaternion.identity);
        markerGO.name = $"ErrorMarker_{error.TypeName}";

        if (markerGO.TryGetComponent<ErrorMarker>(out var marker))
        {
            marker.Initialize(error);
            Debug.Log($"[ErrorTracker] ✅ Маркер создан и инициализирован");
        }
        else
        {
            Debug.LogError("[ErrorTracker] ❌ На Prefab маркера нет компонента ErrorMarker!");
        }
    }

    public float GetSpeedKmh()
    {
        if (_rb != null) return _rb.linearVelocity.magnitude * 3.6f;
        // Запасной вариант для WheelCollider / CharacterController
        return 0f;
    }
}