using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Централизованная система логирования для симулятора вождения
/// Записывает события в файл с временными метками
/// </summary>
public class GameLogger : MonoBehaviour
{
    private static GameLogger instance;
    public static GameLogger Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("GameLogger");
                instance = go.AddComponent<GameLogger>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    [Header("Настройки")]
    [SerializeField] private bool enableLogging = true;
    [SerializeField] private bool logToConsole = true;
    [SerializeField] private bool logToFile = true;
    [SerializeField] private int maxLogFileSize = 5; // МБ

    private string logFilePath;
    private string sessionId;
    private DateTime sessionStartTime;
    private StreamWriter logWriter;

    // Категории логов
    public enum LogCategory
    {
        Game,           // Игровые события
        System,         // Системные события
        Network,        // Сетевые события
        Performance,    // Производительность
        Error,          // Ошибки
        Analytics       // Аналитика
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeLogger();
    }

    private void InitializeLogger()
    {
        sessionId = Guid.NewGuid().ToString().Substring(0, 8);
        sessionStartTime = DateTime.Now;

        // Создаем папку для логов
        string logFolder = Path.Combine(Application.dataPath, "Logs");
        if (!Directory.Exists(logFolder))
        {
            Directory.CreateDirectory(logFolder);
        }

        // Имя файла с датой и ID сессии
        string fileName = $"Log_{sessionStartTime:yyyy-MM-dd_HH-mm-ss}_{sessionId}.txt";
        logFilePath = Path.Combine(logFolder, fileName);

        // Очищаем старые логи (оставляем только последние 10)
        CleanOldLogs(logFolder, 10);

        if (logToFile)
        {
            try
            {
                logWriter = new StreamWriter(logFilePath, true);
                logWriter.AutoFlush = true;

                WriteHeader();
            }
            catch (Exception e)
            {
                Debug.LogError($"Не удалось создать файл лога: {e.Message}");
                logToFile = false;
            }
        }

        Log(LogCategory.System, "Логгер инициализирован");
    }

    private void WriteHeader()
    {
        if (logWriter == null) return;

        logWriter.WriteLine("=".PadRight(80, '='));
        logWriter.WriteLine($"  DRIVING SIMULATOR - LOG FILE");
        logWriter.WriteLine($"  Session ID: {sessionId}");
        logWriter.WriteLine($"  Start Time: {sessionStartTime:yyyy-MM-dd HH:mm:ss}");
        logWriter.WriteLine($"  Unity Version: {Application.unityVersion}");
        logWriter.WriteLine($"  Platform: {Application.platform}");
        logWriter.WriteLine("=".PadRight(80, '='));
        logWriter.WriteLine();
    }

    /// <summary>
    /// Основной метод логирования
    /// </summary>
    public static void Log(LogCategory category, string message)
    {
        Instance.LogInternal(category, message, LogType.Log);
    }

    /// <summary>
    /// Логирование предупреждений
    /// </summary>
    public static void LogWarning(LogCategory category, string message)
    {
        Instance.LogInternal(category, message, LogType.Warning);
    }

    /// <summary>
    /// Логирование ошибок
    /// </summary>
    public static void LogError(LogCategory category, string message)
    {
        Instance.LogInternal(category, message, LogType.Error);
    }

    /// <summary>
    /// Логирование игрового события с дополнительными данными
    /// </summary>
    public static void LogEvent(string eventName, params object[] data)
    {
        string message = $"EVENT: {eventName}";
        if (data != null && data.Length > 0)
        {
            message += " | Data: " + string.Join(", ", data);
        }
        Instance.LogInternal(LogCategory.Game, message, LogType.Log);
    }

    private void LogInternal(LogCategory category, string message, LogType logType)
    {
        if (!enableLogging) return;

        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        string categoryStr = category.ToString().ToUpper().PadRight(12);
        string typeStr = logType.ToString().ToUpper().PadRight(8);
        string logMessage = $"[{timestamp}] [{categoryStr}] [{typeStr}] {message}";

        // Вывод в консоль Unity
        if (logToConsole)
        {
            switch (logType)
            {
                case LogType.Error:
                    Debug.LogError(logMessage);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(logMessage);
                    break;
                default:
                    Debug.Log(logMessage);
                    break;
            }
        }

        // Запись в файл
        if (logToFile && logWriter != null)
        {
            try
            {
                // ПРОВЕРКА: файл еще открыт?
                if (logWriter.BaseStream != null && logWriter.BaseStream.CanWrite)
                {
                    logWriter.WriteLine(logMessage);
                    // Проверка размера файла
                    CheckFileSize();
                }
            }
            catch (ObjectDisposedException)
            {
                // Файл уже закрыт, ничего не делаем
                logToFile = false;
            }
            catch (Exception e)
            {
                Debug.LogError($"Ошибка записи в лог: {e.Message}");
                logToFile = false;
            }
        }
    }

    private void CheckFileSize()
    {
        if (!File.Exists(logFilePath)) return;

        FileInfo fileInfo = new FileInfo(logFilePath);
        long fileSizeMB = fileInfo.Length / (1024 * 1024);

        if (fileSizeMB >= maxLogFileSize)
        {
            logWriter?.Close();

            // Создаем новый файл
            string newFileName = $"Log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_{sessionId}_continued.txt";
            logFilePath = Path.Combine(Path.GetDirectoryName(logFilePath), newFileName);

            logWriter = new StreamWriter(logFilePath, true);
            logWriter.AutoFlush = true;

            Log(LogCategory.System, "Создан новый файл лога (превышен размер)");
        }
    }

    private void CleanOldLogs(string logFolder, int keepCount)
    {
        try
        {
            DirectoryInfo dirInfo = new DirectoryInfo(logFolder);
            FileInfo[] files = dirInfo.GetFiles("Log_*.txt");

            if (files.Length <= keepCount) return;

            // Сортируем по дате создания
            Array.Sort(files, (a, b) => a.CreationTime.CompareTo(b.CreationTime));

            // Удаляем старые
            int toDelete = files.Length - keepCount;
            for (int i = 0; i < toDelete; i++)
            {
                files[i].Delete();
            }

            Debug.Log($"Удалено старых логов: {toDelete}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Не удалось очистить старые логи: {e.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        TimeSpan sessionDuration = DateTime.Now - sessionStartTime;
        Log(LogCategory.System, $"Приложение закрывается. Длительность сессии: {sessionDuration:hh\\:mm\\:ss}");

        // Отключаем логирование в файл
        logToFile = false;

        // Закрываем файл
        try
        {
            if (logWriter != null)
            {
                logWriter.Flush();
                logWriter.Close();
                logWriter = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Ошибка при закрытии лога: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        // Отключаем логирование в файл
        logToFile = false;

        try
        {
            if (logWriter != null)
            {
                logWriter.Flush();
                logWriter.Close();
                logWriter = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Ошибка при закрытии лога: {e.Message}");
        }
    }

    // Публичные методы для удобства

    public static void LogCollision(string objectName, float velocity)
    {
        LogEvent("Collision", $"Object: {objectName}", $"Velocity: {velocity:F2}");
    }

    public static void LogSceneLoad(string sceneName)
    {
        LogEvent("SceneLoad", $"Scene: {sceneName}");
    }

    public static void LogNetworkEvent(string eventType, string details)
    {
        Log(LogCategory.Network, $"{eventType}: {details}");
    }

    public static void LogPerformance(string metric, float value)
    {
        Log(LogCategory.Performance, $"{metric}: {value:F2}");
    }

    public static string GetCurrentLogPath()
    {
        return Instance.logFilePath;
    }

    public static void OpenLogFolder()
    {
        string folder = Path.GetDirectoryName(Instance.logFilePath);
        if (Directory.Exists(folder))
        {
            System.Diagnostics.Process.Start(folder);
        }
    }
}