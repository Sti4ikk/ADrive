using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

/// <summary>
/// Простая статистика игрока для сохранения
/// </summary>
[Serializable]
public class PlayerStats
{
    public float totalPlayTimeSeconds;  // Общее время в игре (в секундах)
    public float totalDistanceMeters;   // Общее расстояние (в метрах)

    public PlayerStats()
    {
        totalPlayTimeSeconds = 0f;
        totalDistanceMeters = 0f;
    }

    /// <summary>
    /// Получить время игры в часах
    /// </summary>
    public float GetPlayTimeHours()
    {
        return totalPlayTimeSeconds / 3600f;
    }

    /// <summary>
    /// Получить расстояние в километрах
    /// </summary>
    public float GetDistanceKilometers()
    {
        return totalDistanceMeters / 1000f;
    }
}

/// <summary>
/// Менеджер сохранения и загрузки статистики
/// </summary>
public class SaveLoadManager : MonoBehaviour
{
    private static SaveLoadManager instance;
    public static SaveLoadManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("SaveLoadManager");
                instance = go.AddComponent<SaveLoadManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private PlayerStats stats;
    private string saveFilePath;

    // Для подсчета текущей сессии
    private float sessionStartTime;
    private float sessionDistance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Путь к файлу сохранения
        saveFilePath = Application.persistentDataPath + "/playerdata.save";

        GameLogger.Log(GameLogger.LogCategory.System,
            $"Путь сохранения: {saveFilePath}");
    }

    private void Start()
    {
        LoadData();
        sessionStartTime = Time.time;
    }

    private void OnApplicationQuit()
    {
        // Автосохранение при выходе
        SaveCurrentSession();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        // Автосохранение на мобильных при сворачивании
        if (pauseStatus)
        {
            SaveCurrentSession();
        }
    }

    /// <summary>
    /// Загрузить статистику
    /// </summary>
    public void LoadData()
    {
        if (File.Exists(saveFilePath))
        {
            try
            {
                BinaryFormatter formatter = new BinaryFormatter();

                using (FileStream stream = File.Open(saveFilePath, FileMode.Open))
                {
                    stats = (PlayerStats)formatter.Deserialize(stream);
                }

                GameLogger.Log(GameLogger.LogCategory.System,
                    $"Статистика загружена: Время={stats.GetPlayTimeHours():F2}ч, " +
                    $"Расстояние={stats.GetDistanceKilometers():F2}км");
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка загрузки: {e.Message}");
                stats = new PlayerStats();
            }
        }
        else
        {
            // Первый запуск - создаем новую статистику
            stats = new PlayerStats();
            GameLogger.Log(GameLogger.LogCategory.System,
                "Создана новая статистика");
        }
    }

    /// <summary>
    /// Сохранить статистику
    /// </summary>
    public void SaveData()
    {
        try
        {
            BinaryFormatter formatter = new BinaryFormatter();

            using (FileStream stream = File.Create(saveFilePath))
            {
                formatter.Serialize(stream, stats);
            }

            GameLogger.Log(GameLogger.LogCategory.System,
                $"Статистика сохранена: Время={stats.GetPlayTimeHours():F2}ч, " +
                $"Расстояние={stats.GetDistanceKilometers():F2}км");
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка сохранения: {e.Message}");
        }
    }

    /// <summary>
    /// Сохранить текущую сессию
    /// </summary>
    public void SaveCurrentSession()
    {
        // Добавляем время текущей сессии
        float sessionTime = Time.time - sessionStartTime;
        stats.totalPlayTimeSeconds += sessionTime;

        // Добавляем дистанцию текущей сессии
        stats.totalDistanceMeters += sessionDistance;

        // Сохраняем
        SaveData();

        // Сбрасываем счетчики сессии
        sessionStartTime = Time.time;
        sessionDistance = 0f;
    }

    /// <summary>
    /// Добавить пройденное расстояние (вызывается из скрипта машины)
    /// </summary>
    public void AddDistance(float meters)
    {
        sessionDistance += meters;
    }

    /// <summary>
    /// Получить текущую статистику
    /// </summary>
    public PlayerStats GetStats()
    {
        return stats;
    }

    /// <summary>
    /// Получить общее время игры с текущей сессией
    /// </summary>
    public float GetTotalPlayTimeSeconds()
    {
        float sessionTime = Time.time - sessionStartTime;
        return stats.totalPlayTimeSeconds + sessionTime;
    }

    /// <summary>
    /// Получить общее расстояние с текущей сессией
    /// </summary>
    public float GetTotalDistanceMeters()
    {
        return stats.totalDistanceMeters + sessionDistance;
    }

    /// <summary>
    /// Сброс всей статистики (для тестирования или кнопки "Сброс")
    /// </summary>
    public void ResetStats()
    {
        stats = new PlayerStats();
        sessionDistance = 0f;
        sessionStartTime = Time.time;
        SaveData();

        GameLogger.Log(GameLogger.LogCategory.System,
            "Статистика сброшена");
    }

    /// <summary>
    /// Удалить файл сохранения
    /// </summary>
    public void DeleteSaveFile()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            GameLogger.Log(GameLogger.LogCategory.System,
                "Файл сохранения удален");
        }

        stats = new PlayerStats();
        sessionDistance = 0f;
        sessionStartTime = Time.time;
    }
}

/// <summary>
/// Пример интеграции со скриптом машины
/// Добавь этот код в свой скрипт управления машиной
/// </summary>
public class CarStatsTracker : MonoBehaviour
{
    private Vector3 lastPosition;
    private float distanceThisFrame;

    private void Start()
    {
        lastPosition = transform.position;
    }

    private void Update()
    {
        // Вычисляем пройденное расстояние за кадр
        distanceThisFrame = Vector3.Distance(transform.position, lastPosition);

        // Добавляем в статистику
        if (distanceThisFrame > 0.01f) // Игнорируем микродвижения
        {
            SaveLoadManager.Instance.AddDistance(distanceThisFrame);
        }

        lastPosition = transform.position;
    }
}

/// <summary>
/// UI для отображения статистики
/// Повесь на GameObject с Text компонентами
/// </summary>
public class StatsDisplay : MonoBehaviour
{
    [Header("UI элементы")]
    [SerializeField] private UnityEngine.UI.Text playTimeText;
    [SerializeField] private UnityEngine.UI.Text distanceText;

    [Header("Настройки")]
    [SerializeField] private float updateInterval = 1f; // Обновлять раз в секунду

    private float nextUpdateTime;

    private void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            UpdateDisplay();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    private void UpdateDisplay()
    {
        // Получаем статистику с учетом текущей сессии
        float totalSeconds = SaveLoadManager.Instance.GetTotalPlayTimeSeconds();
        float totalMeters = SaveLoadManager.Instance.GetTotalDistanceMeters();

        // Форматируем время в часы:минуты:секунды
        TimeSpan time = TimeSpan.FromSeconds(totalSeconds);
        string timeString = string.Format("{0:D2}:{1:D2}:{2:D2}",
            (int)time.TotalHours, time.Minutes, time.Seconds);

        // Форматируем расстояние
        float kilometers = totalMeters / 1000f;

        // Обновляем UI
        if (playTimeText != null)
        {
            playTimeText.text = LocalizationManagr.Get("stats.time") + ": " + timeString;
        }

        if (distanceText != null)
        {
            distanceText.text = LocalizationManagr.Get("stats.distance") + ": " +
                kilometers.ToString("F2") + " км";
        }
    }

    /// <summary>
    /// Кнопка для ручного сохранения
    /// </summary>
    public void OnSaveButtonClicked()
    {
        SaveLoadManager.Instance.SaveCurrentSession();
        Debug.Log("Прогресс сохранен вручную!");
    }

    /// <summary>
    /// Кнопка для сброса статистики
    /// </summary>
    public void OnResetButtonClicked()
    {
        SaveLoadManager.Instance.ResetStats();
        UpdateDisplay();
        Debug.Log("Статистика сброшена!");
    }
}