using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Компонент для автоматической локализации UI Text
/// Просто добавь этот компонент к тексту и укажи ключ перевода
/// </summary>
[RequireComponent(typeof(Text))]
public class LocalizedText : MonoBehaviour
{
    [Header("Ключ перевода")]
    [Tooltip("Ключ из системы локализации (например: menu.singleplayer)")]
    [SerializeField] private string translationKey;

    [Header("Параметры форматирования")]
    [Tooltip("Параметры для string.Format (например, для 'Игроков: {0}')")]
    [SerializeField] private string[] formatArgs;

    [Header("Обновление")]
    [Tooltip("Обновлять текст при изменении языка")]
    [SerializeField] private bool updateOnLanguageChange = true;

    private Text textComponent;

    private void Awake()
    {
        textComponent = GetComponent<Text>();
    }

    private void Start()
    {
        UpdateText();

        // Подписываемся на событие смены языка
        if (updateOnLanguageChange)
        {
            LocalizationManagr.OnLanguageChanged += UpdateText;
        }
    }

    private void OnDestroy()
    {
        // Отписываемся от события
        if (updateOnLanguageChange)
        {
            LocalizationManagr.OnLanguageChanged -= UpdateText;
        }
    }

    /// <summary>
    /// Обновить текст
    /// </summary>
    public void UpdateText()
    {
        if (textComponent == null)
            textComponent = GetComponent<Text>();

        if (string.IsNullOrEmpty(translationKey))
        {
            Debug.LogWarning($"Translation key не задан для {gameObject.name}");
            return;
        }

        // Получаем перевод
        string translation = LocalizationManagr.Get(translationKey, (object[])formatArgs);
        textComponent.text = translation;
    }

    /// <summary>
    /// Установить ключ перевода программно
    /// </summary>
    public void SetTranslationKey(string key)
    {
        translationKey = key;
        UpdateText();
    }

    /// <summary>
    /// Установить параметры форматирования программно
    /// </summary>
    public void SetFormatArgs(params string[] args)
    {
        formatArgs = args;
        UpdateText();
    }

    // Для удобства редактирования в Inspector
    private void OnValidate()
    {
        if (Application.isPlaying && textComponent != null)
        {
            UpdateText();
        }
    }
}