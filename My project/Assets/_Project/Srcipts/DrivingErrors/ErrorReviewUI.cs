using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI окно разбора ошибки.
/// Использует AIInstructorClient → Flask сервер → OpenRouter + TTS голос Дмитрий.
/// Аудио воспроизводится автоматически как только ИИ ответил.
/// </summary>
public class ErrorReviewUI : MonoBehaviour
{
    public static ErrorReviewUI Instance { get; private set; }

    [Header("Панель")]
    public GameObject reviewPanel;

    [Header("Текстовые поля")]
    public TextMeshProUGUI errorTypeText;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI aiResponseText;
    public TextMeshProUGUI loadingText;         // "⏳ Инструктор анализирует..."

    [Header("Кнопки")]
    public Button closeButton;

    private DrivingError _currentError;
    private ErrorMarker  _currentMarker;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        closeButton?.onClick.AddListener(OnClose);
        reviewPanel?.SetActive(false);
    }

    /// <summary>Открывает окно и сразу запрашивает разбор у ИИ</summary>
    public void Show(DrivingError error, ErrorMarker marker = null)
    {
        _currentError  = error;
        _currentMarker = marker;

        if (errorTypeText)
            errorTypeText.text = $"⚠ {error.TypeName}";

        if (speedText)
        {
            speedText.text = error.type == DrivingError.ErrorType.Speeding
                ? $"Скорость: <color=#FF6B35><b>{error.speedAtMoment} км/ч</b></color>  |  Лимит: {error.speedLimit} км/ч"
                : $"Скорость: {error.speedAtMoment} км/ч";
        }

        if (descriptionText)
            descriptionText.text = error.description;

        if (aiResponseText)
            aiResponseText.text = "";

        if (loadingText)
        {
            loadingText.text = "⏳ Инструктор анализирует ошибку...";
            loadingText.gameObject.SetActive(true);
        }

        reviewPanel?.SetActive(true);
        Time.timeScale = 0f;

        // Сразу запрашиваем — без кнопки
        RequestAIAnalysis();
    }

    private void RequestAIAnalysis()
    {
        if (AIInstructorClient.Instance == null)
        {
            ShowError("Сервер не найден. Убедитесь что server.py запущен.");
            return;
        }

        AIInstructorClient.Instance.AnalyzeError(
            _currentError,
            onTextReady: text =>
            {
                if (loadingText) loadingText.gameObject.SetActive(false);
                if (aiResponseText) aiResponseText.text = text;
                // Аудио воспроизводится автоматически внутри AIInstructorClient
            },
            onError: () => ShowError("Не удалось получить разбор.\nПроверьте что server.py запущен.")
        );
    }

    private void ShowError(string message)
    {
        if (loadingText) loadingText.gameObject.SetActive(false);
        if (aiResponseText) aiResponseText.text = $"<color=red>{message}</color>";
    }

    private void OnClose()
    {
        reviewPanel?.SetActive(false);
        Time.timeScale = 1f;
        _currentMarker?.DestroyMarker();
    }
}
