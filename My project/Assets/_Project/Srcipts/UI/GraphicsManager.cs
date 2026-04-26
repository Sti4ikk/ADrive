using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GraphicsManager : MonoBehaviour
{
    [Header("UI Элементы")]
    [SerializeField] private TMP_Dropdown qualityDropdown;  // Quality → Dropdown
    [SerializeField] private InputField fpsInputField;      // Fps → InputField
    [SerializeField] private Text fpsPlaceholder;           // Fps → Placeholder
    [SerializeField] private Toggle vsyncToggle;            // VSYNC → Toggle
    [SerializeField] private Slider gammaSlider;            // Gamma → Slider
    [SerializeField] private Text gammaValueText;           // Gamma → Text (для отображения значения)

    [Header("Кнопки")]
    [SerializeField] private Button saveButton;             // Кнопка "Сохранить"
    [SerializeField] private Button resetButton;            // Кнопка "Сбросить" (опционально)

    // Ключи для сохранения
    private const string QUALITY_KEY = "Graphics_Quality";
    private const string FPS_LIMIT_KEY = "Graphics_FPSLimit";
    private const string VSYNC_KEY = "Graphics_VSync";
    private const string GAMMA_KEY = "Graphics_Gamma";

    // Флаг изменений
    private bool hasUnappliedChanges = false;
    private bool isLoading = false;

    private void Start()
    {
        InitializeUI();
        LoadSettings();

        GameLogger.Log(GameLogger.LogCategory.System, "Graphics settings initialized");
    }

    // ============= ИНИЦИАЛИЗАЦИЯ =============

    private void InitializeUI()
    {
        // Инициализация Quality Dropdown
        if (qualityDropdown != null)
        {
            qualityDropdown.ClearOptions();
            List<string> qualityNames = new List<string>(QualitySettings.names);
            qualityDropdown.AddOptions(qualityNames);
            qualityDropdown.onValueChanged.AddListener(OnSettingChanged);
        }

        // Инициализация FPS InputField
        if (fpsInputField != null)
        {
            fpsInputField.contentType = InputField.ContentType.IntegerNumber;
            fpsInputField.onValueChanged.AddListener(delegate { OnSettingChanged(0); });
            fpsInputField.onEndEdit.AddListener(ValidateFPS);

            // Устанавливаем placeholder
            if (fpsPlaceholder != null)
            {
                fpsPlaceholder.text = "60";
            }
        }

        // Инициализация VSync Toggle
        if (vsyncToggle != null)
        {
            vsyncToggle.onValueChanged.AddListener(delegate { OnSettingChanged(0); });
        }

        // Инициализация Gamma Slider
        if (gammaSlider != null)
        {
            gammaSlider.minValue = 0.5f;  // Темнее
            gammaSlider.maxValue = 2.5f;  // Светлее
            gammaSlider.value = 1.0f;     // Стандарт
            gammaSlider.onValueChanged.AddListener(OnGammaChanged);
        }

        // Кнопка "Сохранить"
        if (saveButton != null)
        {
            saveButton.onClick.AddListener(SaveSettings);
            saveButton.interactable = false; // Изначально неактивна
        }

        // Кнопка "Сбросить"
        if (resetButton != null)
        {
            resetButton.onClick.AddListener(ResetToDefaults);
        }
    }

    // ============= ОБРАБОТЧИКИ ИЗМЕНЕНИЙ =============

    private void OnSettingChanged(int value)
    {
        if (isLoading) return;

        hasUnappliedChanges = true;

        if (saveButton != null)
            saveButton.interactable = true;

        Debug.Log("⚠ Настройки изменены. Нажмите 'Сохранить' для применения.");
    }

    private void OnGammaChanged(float value)
    {
        if (isLoading) return;

        UpdateGammaText(value);
        hasUnappliedChanges = true;

        if (saveButton != null)
            saveButton.interactable = true;

        Debug.Log("⚠ Гамма изменена. Нажмите 'Сохранить' для применения.");
    }

    private void ValidateFPS(string value)
    {
        if (isLoading) return;

        if (string.IsNullOrEmpty(value))
        {
            fpsInputField.text = "60";
        }
        else
        {
            // Валидация: минимум 30, максимум 300
            int fps = int.Parse(value);
            fps = Mathf.Clamp(fps, 30, 300);
            fpsInputField.text = fps.ToString();
        }
    }

    // Обновление текста значения гаммы
    private void UpdateGammaText(float value)
    {
        if (gammaValueText != null)
        {
            gammaValueText.text = value.ToString("F2");
        }
    }

    // ============= СОХРАНЕНИЕ НАСТРОЕК =============

    public void SaveSettings()
    {
        if (!hasUnappliedChanges)
        {
            Debug.Log("✓ Нет изменений для сохранения");
            return;
        }

        // Quality
        if (qualityDropdown != null)
        {
            int qualityLevel = qualityDropdown.value;
            QualitySettings.SetQualityLevel(qualityLevel);
            PlayerPrefs.SetInt(QUALITY_KEY, qualityLevel);

            GameLogger.LogEvent("GraphicsApplied", $"Quality: {QualitySettings.names[qualityLevel]}");
            Debug.Log($"✓ Качество графики: {QualitySettings.names[qualityLevel]}");
        }

        // FPS Limit
        if (fpsInputField != null && !string.IsNullOrEmpty(fpsInputField.text))
        {
            int fpsLimit = int.Parse(fpsInputField.text);
            Application.targetFrameRate = fpsLimit;
            PlayerPrefs.SetInt(FPS_LIMIT_KEY, fpsLimit);

            GameLogger.LogEvent("GraphicsApplied", $"FPS Limit: {fpsLimit}");
            Debug.Log($"✓ FPS лимит: {fpsLimit}");
        }

        // VSync
        if (vsyncToggle != null)
        {
            bool vsyncEnabled = vsyncToggle.isOn;
            QualitySettings.vSyncCount = vsyncEnabled ? 1 : 0;
            PlayerPrefs.SetInt(VSYNC_KEY, vsyncEnabled ? 1 : 0);

            GameLogger.LogEvent("GraphicsApplied", $"VSync: {vsyncEnabled}");
            Debug.Log($"✓ VSync: {(vsyncEnabled ? "Включен" : "Выключен")}");
        }

        // Gamma
        if (gammaSlider != null)
        {
            float gamma = gammaSlider.value;
            RenderSettings.ambientIntensity = gamma;
            PlayerPrefs.SetFloat(GAMMA_KEY, gamma);

            GameLogger.LogEvent("GraphicsApplied", $"Gamma: {gamma:F2}");
            Debug.Log($"✓ Гамма: {gamma:F2}");
        }

        // Сохраняем все изменения в PlayerPrefs
        PlayerPrefs.Save();

        // Сбрасываем флаг изменений
        hasUnappliedChanges = false;

        if (saveButton != null)
            saveButton.interactable = false;

        GameLogger.Log(GameLogger.LogCategory.System, "Graphics settings saved");
        Debug.Log("✅ Настройки графики сохранены!");
    }

    // ============= ЗАГРУЗКА НАСТРОЕК =============

    public void LoadSettings()
    {
        isLoading = true; // Отключаем обработчики изменений

        // Quality
        if (qualityDropdown != null)
        {
            int quality = PlayerPrefs.GetInt(QUALITY_KEY, QualitySettings.GetQualityLevel());
            qualityDropdown.value = quality;
            QualitySettings.SetQualityLevel(quality);
        }

        // FPS Limit
        if (fpsInputField != null)
        {
            int fpsLimit = PlayerPrefs.GetInt(FPS_LIMIT_KEY, 60);
            fpsInputField.text = fpsLimit.ToString();
            Application.targetFrameRate = fpsLimit;
        }

        // VSync
        if (vsyncToggle != null)
        {
            bool vsync = PlayerPrefs.GetInt(VSYNC_KEY, 1) == 1;
            vsyncToggle.isOn = vsync;
            QualitySettings.vSyncCount = vsync ? 1 : 0;
        }

        // Gamma
        if (gammaSlider != null)
        {
            float gamma = PlayerPrefs.GetFloat(GAMMA_KEY, 1.0f);
            gammaSlider.value = gamma;
            UpdateGammaText(gamma);
            RenderSettings.ambientIntensity = gamma;
        }

        hasUnappliedChanges = false;

        if (saveButton != null)
            saveButton.interactable = false;

        isLoading = false; // Включаем обработчики обратно

        GameLogger.Log(GameLogger.LogCategory.System, "Graphics settings loaded");
        Debug.Log("✓ Настройки графики загружены");
    }

    // ============= СБРОС К НАСТРОЙКАМ ПО УМОЛЧАНИЮ =============

    public void ResetToDefaults()
    {
        isLoading = true;

        // Quality - максимальное качество
        if (qualityDropdown != null)
        {
            qualityDropdown.value = QualitySettings.names.Length - 1;
        }

        // FPS - 60
        if (fpsInputField != null)
        {
            fpsInputField.text = "60";
        }

        // VSync - включен
        if (vsyncToggle != null)
        {
            vsyncToggle.isOn = true;
        }

        // Gamma - 1.0 (стандарт)
        if (gammaSlider != null)
        {
            gammaSlider.value = 1.0f;
            UpdateGammaText(1.0f);
        }

        isLoading = false;

        hasUnappliedChanges = true;

        if (saveButton != null)
            saveButton.interactable = true;

        GameLogger.LogEvent("GraphicsReset", "Graphics settings reset to defaults");
        Debug.Log("⚠ Настройки сброшены. Нажмите 'Сохранить' для применения.");
    }

    // ============= ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =============

    // Проверка наличия несохраненных изменений
    public bool HasUnappliedChanges()
    {
        return hasUnappliedChanges;
    }

    // Отмена изменений (перезагрузка настроек)
    public void CancelChanges()
    {
        LoadSettings();

        if (saveButton != null)
            saveButton.interactable = false;

        Debug.Log("✓ Изменения отменены");
    }

    // Получить текущий FPS лимит
    public int GetCurrentFPSLimit()
    {
        if (fpsInputField != null && !string.IsNullOrEmpty(fpsInputField.text))
        {
            return int.Parse(fpsInputField.text);
        }
        return 60;
    }

    // Получить текущее значение гаммы
    public float GetCurrentGamma()
    {
        return gammaSlider != null ? gammaSlider.value : 1.0f;
    }
}