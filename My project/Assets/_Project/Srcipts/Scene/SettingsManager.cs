using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


// Класс-обработчик действий главного менюS
public class SettingsManager: MonoBehaviour
{
    [SerializeField] private GameObject settingsPanel;         // панель настроек
    [SerializeField] private GameObject menuPanel;             // панель главного меню

    [Header("BLUR IMAGE С ШЕЙДЕРОМ")]
    [SerializeField] private GameObject blurImage;             // Image с шейдером blur

    [Header("Меню настроек")]
    [SerializeField] private GameObject graphicsMenu;          // ScrollViewGraphics
    [SerializeField] private GameObject soundMenu;             // ScrollViewSounds
    [SerializeField] private GameObject languageMenu;          // ScrollViewLanguage
    [SerializeField] private Button graphicsButton;            // Кнопка "Графика"
    [SerializeField] private Button soundButton;               // Кнопка "Звук"
    [SerializeField] private Button languageButton;            // Кнопка "Язык"

    [Header("Цвета кнопок настроек")]
    [SerializeField] private Color activeTabColor = new Color(1f, 0f, 0f, 1f);      // Красный для активной
    [SerializeField] private Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Серый для неактивной

    [Header("Менеджеры настроек")]
    [SerializeField] private GameObject graphicsSettings;  // Менеджер графики

    [Header("РЕСПАВН МАШИНЫ")]
    [SerializeField] private Vector3 respawnPosition = new Vector3(0f, 1f, 0f);  // Точка респавна
    [SerializeField] private Vector3 respawnRotation = new Vector3(0f, 0f, 0f);  // Поворот при респавне
    [SerializeField] private Key respawnKey = Key.L;                     // Клавиша респавна

    private GameObject playerCar;
    private Rigidbody carRigidbody;


    private void Start()
    {
        CloseAllPanels(); // на старте только главное меню видно

        // ЛОГИРОВАНИЕ ЗАПУСКА
        GameLogger.Log(GameLogger.LogCategory.System, "Главное меню загружено");

        // Находим машину игрока
        //FindPlayerCar();
    }

    private void Update()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (settingsPanel != null && settingsPanel.activeSelf)
                {
                    CloseSettings();
                }
            }

            //if (keyboard[respawnKey].wasPressedThisFrame)
            //{
            //    RespawnCar();
            //}
        }
    }


    // ============= НАСТРОЙКИ =============
    public void OpenSettings()
    {
        // ЗАКРЫВАЕМ главное меню
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // ОТКРЫВАЕМ настройки
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);

            // По умолчанию открываем вкладку "Графика"
            ShowGraphicsMenu();
        }

        if (blurImage != null)
            blurImage.SetActive(true);

        GameLogger.LogEvent("SettingsOpened", "Settings panel opened");
    }

    public void CloseSettings()
    {
        // ЗАКРЫВАЕМ настройки
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        DeselectAllButtons();

        if (blurImage != null)
            blurImage.SetActive(false);

        GameLogger.LogEvent("SettingsClosed", "Settings panel closed");
    }

    // Переключение на вкладку "Графика"
    public void ShowGraphicsMenu()
    {
        if (graphicsMenu != null)
            graphicsMenu.SetActive(true);

        if (soundMenu != null)
            soundMenu.SetActive(false);

        if (languageMenu != null)
            languageMenu.SetActive(false);

        // Загружаем настройки графики при открытии вкладки
        //if (graphicsSettings != null)
        //graphicsSettings.LoadSettings();

        // Визуальная индикация активной кнопки
        UpdateTabButtonsState(graphicsButton);

        GameLogger.LogEvent("SettingsTab", "Tab: Graphics");
    }

    // Переключение на вкладку "Звук"
    public void ShowSoundMenu()
    {
        if (graphicsMenu != null)
            graphicsMenu.SetActive(false);

        if (soundMenu != null)
            soundMenu.SetActive(true);

        if (languageMenu != null)
            languageMenu.SetActive(false);

        // Визуальная индикация активной кнопки
        UpdateTabButtonsState(soundButton);

        GameLogger.LogEvent("SettingsTab", "Tab: Sound");
    }

    // Переключение на вкладку "Язык"
    public void ShowLanguageMenu()
    {
        if (graphicsMenu != null)
            graphicsMenu.SetActive(false);

        if (soundMenu != null)
            soundMenu.SetActive(false);

        if (languageMenu != null)
            languageMenu.SetActive(true);

        // Визуальная индикация активной кнопки
        UpdateTabButtonsState(languageButton);

        GameLogger.LogEvent("SettingsTab", "Tab: Language");
    }

    // Обновление состояния кнопок вкладок (визуальная индикация)
    private void UpdateTabButtonsState(Button activeButton)
    {
        // Обновляем кнопку "Графика"
        if (graphicsButton != null)
        {
            graphicsButton.interactable = (graphicsButton != activeButton);
            var colors = graphicsButton.colors;
            colors.normalColor = (graphicsButton == activeButton) ? activeTabColor : inactiveTabColor;
            colors.disabledColor = activeTabColor; // Цвет для выбранной (неактивной) кнопки
            graphicsButton.colors = colors;
        }

        // Обновляем кнопку "Звук"
        if (soundButton != null)
        {
            soundButton.interactable = (soundButton != activeButton);
            var colors = soundButton.colors;
            colors.normalColor = (soundButton == activeButton) ? activeTabColor : inactiveTabColor;
            colors.disabledColor = activeTabColor;
            soundButton.colors = colors;
        }

        // Обновляем кнопку "Язык"
        if (languageButton != null)
        {
            languageButton.interactable = (languageButton != activeButton);
            var colors = languageButton.colors;
            colors.normalColor = (languageButton == activeButton) ? activeTabColor : inactiveTabColor;
            colors.disabledColor = activeTabColor;
            languageButton.colors = colors;
        }
    }

    // НОВЫЙ МЕТОД: Снимает выделение со всех кнопок
    private void DeselectAllButtons()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void CloseAllPanels()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (menuPanel != null)
            menuPanel.SetActive(false);

        if (blurImage != null)
            blurImage.SetActive(false);
    }

    //// ============= РЕСПАВН МАШИНЫ =============

    ///// <summary>
    ///// Найти машину игрока в сцене
    ///// </summary>
    //private void FindPlayerCar()
    //{
    //    // Вариант 1: Поиск по тегу
    //    playerCar = GameObject.FindGameObjectWithTag("Car");

    //    // Вариант 2: Поиск через Photon (для мультиплеера)
    //    if (playerCar == null)
    //    {
    //        PhotonView[] photonViews = FindObjectsOfType<PhotonView>();
    //        foreach (var pv in photonViews)
    //        {
    //            if (pv.IsMine)
    //            {
    //                playerCar = pv.gameObject;
    //                break;
    //            }
    //        }
    //    }

    //    if (playerCar != null)
    //    {
    //        carRigidbody = playerCar.GetComponent<Rigidbody>();
    //        GameLogger.Log(GameLogger.LogCategory.Game,
    //            "Машина игрока найдена для системы респавна");
    //    }
    //    else
    //    {
    //        UnityEngine.Debug.LogWarning("Машина игрока не найдена! Проверьте тег 'Player'");
    //    }
    //}

    ///// <summary>
    ///// Респавн машины (публичный метод для кнопки)
    ///// </summary>
    //public void RespawnCar()
    //{
    //    if (playerCar == null)
    //    {
    //        FindPlayerCar();
    //    }

    //    if (playerCar == null)
    //    {
    //        UnityEngine.Debug.LogError("Не удалось найти машину для респавна!");
    //        return;
    //    }

    //    // Телепортируем машину на заданную позицию
    //    playerCar.transform.position = respawnPosition;
    //    playerCar.transform.rotation = Quaternion.Euler(respawnRotation);

    //    // Сбрасываем физику
    //    if (carRigidbody != null)
    //    {
    //        carRigidbody.linearVelocity = Vector3.zero;
    //        carRigidbody.angularVelocity = Vector3.zero;
    //    }

    //    GameLogger.Log(GameLogger.LogCategory.Game,
    //        $"Машина респавнулась: позиция={respawnPosition}, поворот={respawnRotation}");

    //    UnityEngine.Debug.Log($"Респавн машины: {respawnPosition}");
    //}

    /// <summary>
    /// Установить точку респавна из кода (опционально)
    /// </summary>
    public void SetRespawnPoint(Vector3 position, Vector3 rotation)
    {
        respawnPosition = position;
        respawnRotation = rotation;

        GameLogger.Log(GameLogger.LogCategory.System,
            $"Точка респавна изменена: {position}");
    }

    /// <summary>
    /// Визуализация точки респавна в редакторе
    /// </summary>
    private void OnDrawGizmos()
    {
        // Рисуем точку респавна в редакторе
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(respawnPosition, 1f);

        // Рисуем направление (вперед)
        Gizmos.color = Color.blue;
        Vector3 forward = Quaternion.Euler(respawnRotation) * Vector3.forward;
        Gizmos.DrawRay(respawnPosition, forward * 3f);
    }
}