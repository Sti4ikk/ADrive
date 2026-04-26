using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Простой селектор языка для Dropdown
/// Специально адаптирован под существующий UI
/// </summary>
public class LanguageSelector : MonoBehaviour
{
    [Header("Dropdown для выбора языка")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    private void Start()
    {
        SetupDropdown();
    }

    /// <summary>
    /// Настройка Dropdown с языками
    /// </summary>
    private void SetupDropdown()
    {
        if (languageDropdown == null)
        {
            Debug.LogError("Language Dropdown не назначен!");
            return;
        }

        // Очищаем существующие опции
        languageDropdown.ClearOptions();

        // Создаем список языков
        List<string> languageNames = new List<string>
        {
            "Русский",
            "English",
            "Español"
        };

        // Добавляем опции в Dropdown
        languageDropdown.AddOptions(languageNames);

        // Устанавливаем текущий язык
        LocalizationManagr.Language currentLang = LocalizationManagr.GetCurrentLanguage();
        languageDropdown.value = (int)currentLang;
        languageDropdown.RefreshShownValue();

        // Подписываемся на изменение
        languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        Debug.Log($"Dropdown настроен. Текущий язык: {currentLang}");
    }

    /// <summary>
    /// Обработчик изменения языка в Dropdown
    /// </summary>
    private void OnLanguageChanged(int index)
    {
        LocalizationManagr.Language selectedLanguage = (LocalizationManagr.Language)index;
        LocalizationManagr.SetLanguage(selectedLanguage);

        Debug.Log($"Язык изменен на: {selectedLanguage}");

        // Логируем изменение
        GameLogger.Log(GameLogger.LogCategory.System,
            $"Пользователь изменил язык на: {selectedLanguage}");
    }

    private void OnDestroy()
    {
        // Отписываемся от события
        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.RemoveListener(OnLanguageChanged);
        }
    }
}