using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

/// <summary>
/// Заменяет OpenRouterClient.cs — теперь запросы идут через локальный Flask сервер.
/// Получает текст разбора + автоматически скачивает и воспроизводит аудио.
///
/// Настройка:
/// 1. Добавьте на пустой GameObject в сцене
/// 2. Назначьте AudioSource (создайте отдельный GameObject с AudioSource)
/// 3. Убедитесь что server.py запущен на ПК
/// </summary>
public class AIInstructorClient : MonoBehaviour
{
    public static AIInstructorClient Instance { get; private set; }

    [Header("Сервер")]
    public string serverUrl = "http://localhost:5000";

    [Header("Аудио")]
    [Tooltip("AudioSource для воспроизведения голоса инструктора")]
    public AudioSource instructorAudioSource;

    [Header("UI (необязательно)")]
    [Tooltip("Текстовое поле для отображения ответа ИИ")]
    public TextMeshProUGUI aiResponseText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        // Создаём AudioSource если не назначен
        if (instructorAudioSource == null)
            instructorAudioSource = gameObject.AddComponent<AudioSource>();
    }

    /// <summary>
    /// Отправить ошибку на сервер — получить разбор текстом + голосом
    /// </summary>
    public void AnalyzeError(DrivingError error,
                              Action<string> onTextReady = null,
                              Action onError = null)
    {
        StartCoroutine(SendAnalyzeRequest(error, onTextReady, onError));
    }

    private IEnumerator SendAnalyzeRequest(DrivingError error,
                                            Action<string> onTextReady,
                                            Action onError)
    {
        // Формируем JSON запрос
        string json = $"{{" +
                      $"\"errorType\":\"{EscapeJson(error.TypeName)}\"," +
                      $"\"description\":\"{EscapeJson(error.description)}\"," +
                      $"\"speed\":{error.speedAtMoment}," +
                      $"\"speedLimit\":{error.speedLimit}" +
                      $"}}";

        using var request = new UnityWebRequest($"{serverUrl}/analyze", "POST");
        request.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 30;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[AIInstructor] Ошибка запроса: {request.error}");
            if (aiResponseText) aiResponseText.text = "Не удалось подключиться к серверу.";
            onError?.Invoke();
            yield break;
        }

        // Парсим ответ
        string responseJson = request.downloadHandler.text;
        string aiText       = ParseField(responseJson, "text");
        string audioUrl     = ParseField(responseJson, "audio_url");

        Debug.Log($"[AIInstructor] Ответ: {aiText}");

        // Показываем текст в UI
        if (aiResponseText) aiResponseText.text = aiText;
        onTextReady?.Invoke(aiText);

        // Скачиваем и воспроизводим аудио
        if (!string.IsNullOrEmpty(audioUrl) && audioUrl != "null")
            yield return DownloadAndPlayAudio(audioUrl);
        else
            Debug.LogWarning("[AIInstructor] audio_url пустой — аудио не будет воспроизведено.");
    }

    private IEnumerator DownloadAndPlayAudio(string url)
    {
        Debug.Log($"[AIInstructor] Загружаю аудио: {url}");

        using var audioRequest = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.WAV);
        yield return audioRequest.SendWebRequest();

        if (audioRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[AIInstructor] Ошибка загрузки аудио: {audioRequest.error}");
            yield break;
        }

        AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
        if (clip == null)
        {
            Debug.LogError("[AIInstructor] AudioClip пустой");
            yield break;
        }

        instructorAudioSource.clip = clip;
        instructorAudioSource.Play();
        Debug.Log("[AIInstructor] 🔊 Воспроизвожу голос инструктора");
    }

    // ── Утилиты ──────────────────────────────────────────────────────────────

    /// <summary>Простой парсинг JSON поля без Newtonsoft</summary>
    private string ParseField(string json, string field)
    {
        string marker = $"\"{field}\":\"";
        int start = json.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0) return "";
        start += marker.Length;
        int end = json.IndexOf("\"", start);
        if (end < 0) return "";
        return json.Substring(start, end - start)
                   .Replace("\\n", "\n")
                   .Replace("\\\"", "\"")
                   .Replace("\\\\", "\\");
    }

    private string EscapeJson(string s) =>
        s?.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n") ?? "";
}
