using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


// Класс-обработчик действий главного меню
public class MainMenuManager : MonoBehaviour
{
    [Header("Сцены")]
    [SerializeField] private int singlePlayerSceneIndex = 1;
    [SerializeField] private int windowLoadingSceneIndex = 5;
    [SerializeField] private int meltiPlayerSceneIndex = 4;
    [SerializeField] private int multiplayersceneindex = 3;
    

    [Header("Панели")]
    [SerializeField] private GameObject mainMenuButtons;       // объект Buttons
    [SerializeField] private GameObject aboutAuthorPanel;      // панель "Об авторе"
    [SerializeField] private GameObject multiplayerRoomPanel;  // панель MultiplayerRoomPanel
    [SerializeField] private GameObject ButtonsMultiPlayerRoomPanel;  // кнопки MultiplayerButtons
    [SerializeField] private GameObject createRoomPanel;       // панель CreateRoomPanel
    [SerializeField] private GameObject joinRoomPanel;         // панель JoinRoomPanel
    [SerializeField] private GameObject settingsPanel;         // панель настроек

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
    [SerializeField] private Color activeTabColor = new Color(1f, 1f, 1f, 1f);      // Белый для активной
    [SerializeField] private Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Серый для неактивной

    [Header("Валидация")]
    [SerializeField] private RoomValidator validator;          // Валидатор для проверки названий комнат
    [SerializeField] private Text errorMessageText;            // Текст для отображения ошибок (опционально)

    private Button[] menuButtons; // храним все кнопки главного меню
    public InputField create;
    public InputField join;

    private void Start()
    {
        CloseAllPanels(); // на старте только главное меню видно
        // Получаем все кнопки в mainMenuButtons
        menuButtons = mainMenuButtons.GetComponentsInChildren<Button>();

        // Если валидатор не назначен, ищем или создаем
        if (validator == null)
        {
            validator = UnityEngine.Object.FindObjectOfType<RoomValidator>();
            if (validator == null)
            {
                GameObject go = new GameObject("RoomValidator");
                validator = go.AddComponent<RoomValidator>();
            }
        }

        // Скрываем текст ошибки, если он есть
        if (errorMessageText != null)
            errorMessageText.gameObject.SetActive(false);

        // ЛОГИРОВАНИЕ ЗАПУСКА
        GameLogger.Log(GameLogger.LogCategory.System, "Главное меню загружено");
    }

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Приоритет: сначала проверяем самые глубокие уровни
            if (createRoomPanel != null && createRoomPanel.activeSelf)
            {
                CloseCreateRoom();
            }
            else if (joinRoomPanel != null && joinRoomPanel.activeSelf)
            {
                CloseJoinRoom();
            }
            else if (multiplayerRoomPanel != null && multiplayerRoomPanel.activeSelf)
            {
                CloseMultiplayerRoom();
            }
            else if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else if (aboutAuthorPanel.activeSelf)
            {
                CloseAboutAuthor();
            }
        }
    }

    // ============= ГЛАВНОЕ МЕНЮ =============
    public void PlaySinglePlayer()
    {
        GameLogger.LogSceneLoad("SinglePlayer");
        GameLogger.LogEvent("GameModeSelected", "Mode: SinglePlayer");

        SceneManager.LoadScene(windowLoadingSceneIndex);
    }

    public void PlayMultiplayer()
    {
        GameLogger.LogEvent("GameModeSelected", "Mode: Multiplayer");
        GameLogger.LogNetworkEvent("ConnectAttempt", "Connecting to Photon...");

        //OpenMultiplayerRoom();

        SceneManager.LoadScene(multiplayersceneindex);
    }

    public void QuitGame()
    {
        GameLogger.Log(GameLogger.LogCategory.System, "Выход из игры");
        CleanupResources();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ============= ОБ АВТОРЕ =============
    public void OpenAboutAuthor()
    {
        SetButtonsInteractable(false);
        aboutAuthorPanel.SetActive(true);

        if (blurImage != null)
            blurImage.SetActive(true);
    }

    public void CloseAboutAuthor()
    {
        aboutAuthorPanel.SetActive(false);
        SetButtonsInteractable(true);
        DeselectAllButtons(); // Снимаем выделение

        if (blurImage != null)
            blurImage.SetActive(false);
    }

    // ============= НАСТРОЙКИ =============
    public void OpenSettings()
    {
        SetButtonsInteractable(false);

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
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        SetButtonsInteractable(true);
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

    // ============= МУЛЬТИПЛЕЕР ПАНЕЛИ =============
    public void OpenMultiplayerRoom()
    {
        mainMenuButtons.SetActive(false);

        if (multiplayerRoomPanel != null)
        {
            multiplayerRoomPanel.SetActive(true);
            ButtonsMultiPlayerRoomPanel.SetActive(true);
        }

        // Убеждаемся что другие панели закрыты
        if (createRoomPanel != null)
            createRoomPanel.SetActive(false);

        if (joinRoomPanel != null)
            joinRoomPanel.SetActive(false);

        if (blurImage != null)
            blurImage.SetActive(true);
    }

    public void CloseMultiplayerRoom()
    {
        if (multiplayerRoomPanel != null)
        {
            multiplayerRoomPanel.SetActive(false);
        }

        if (ButtonsMultiPlayerRoomPanel != null)
            ButtonsMultiPlayerRoomPanel.SetActive(false);

        if (createRoomPanel != null)
            createRoomPanel.SetActive(false);

        if (joinRoomPanel != null)
            joinRoomPanel.SetActive(false);

        mainMenuButtons.SetActive(true);
        DeselectAllButtons(); // Снимаем выделение со всех кнопок

        if (blurImage != null)
            blurImage.SetActive(false);
    }

    // ============= СОЗДАНИЕ КОМНАТЫ =============
    public void OpenCreateRoom()
    {
        // Скрываем кнопки мультиплеера
        if (ButtonsMultiPlayerRoomPanel != null)
            ButtonsMultiPlayerRoomPanel.SetActive(false);

        // Показываем панель создания комнаты
        if (createRoomPanel != null)
            createRoomPanel.SetActive(true);
    }

    public void CloseCreateRoom()
    {
        // Закрываем панель создания комнаты
        if (createRoomPanel != null)
            createRoomPanel.SetActive(false);

        // Возвращаем кнопки мультиплеера
        if (ButtonsMultiPlayerRoomPanel != null)
            ButtonsMultiPlayerRoomPanel.SetActive(true);
    }

    // ============= ПРИСОЕДИНЕНИЕ К КОМНАТЕ =============
    public void OpenJoinRoom()
    {
        // Скрываем кнопки мультиплеера
        if (ButtonsMultiPlayerRoomPanel != null)
            ButtonsMultiPlayerRoomPanel.SetActive(false);

        // Показываем панель присоединения
        if (joinRoomPanel != null)
            joinRoomPanel.SetActive(true);
    }

    public void CloseJoinRoom()
    {
        // Закрываем панель присоединения
        if (joinRoomPanel != null)
            joinRoomPanel.SetActive(false);

        // Возвращаем кнопки мультиплеера
        if (ButtonsMultiPlayerRoomPanel != null)
            ButtonsMultiPlayerRoomPanel.SetActive(true);
    }

    // ============= ДЕЙСТВИЯ С КОМНАТАМИ С ВАЛИДАЦИЕЙ =============
    public void CreateRoomAndGo()
    {
        string roomName = create.text;

        // ВАЛИДАЦИЯ НАЗВАНИЯ КОМНАТЫ
        var result = validator.ValidateRoomName(roomName);

        if (!result.isValid)
        {
            // Показываем ошибку пользователю
            ShowErrorMessage($"Ошибка: {result.errorMessage}");

            // Логируем попытку создания комнаты с невалидным названием
            GameLogger.LogWarning(GameLogger.LogCategory.Network,
                $"Попытка создать комнату с невалидным названием: '{roomName}' - {result.errorMessage}");
            return;
        }

        // Используем очищенное название
        string cleanRoomName = result.sanitizedValue;





        //RoomOptions roomOptions = new RoomOptions();
        //roomOptions.MaxPlayers = 4;
        //PhotonNetwork.CreateRoom(cleanRoomName, roomOptions);
    }

    public void JoinRoomAndGo()
    {
        //PhotonNetwork.JoinRoom(join.text);
    }

    public void OnJoinedRoom()
    {
        //GameLogger.LogNetworkEvent("RoomJoined",
           // $"Room: {PhotonNetwork.CurrentRoom.Name}, Players: {PhotonNetwork.CurrentRoom.PlayerCount}");
        
        //PhotonNetwork.LoadLevel("MultiplayScene");
    }

    public void OnCreateRoomFailed(short returnCode, string message)
    {
        ShowErrorMessage($"Не удалось создать комнату: {message}");
        GameLogger.LogError(GameLogger.LogCategory.Network,
            $"CreateRoom failed: Code {returnCode}, Message: {message}");
    }

    public void OnJoinRoomFailed(short returnCode, string message)
    {
        ShowErrorMessage($"Не удалось присоединиться: {message}");
        GameLogger.LogError(GameLogger.LogCategory.Network,
            $"JoinRoom failed: Code {returnCode}, Message: {message}");
    }

    // ============= ПОКАЗ СООБЩЕНИЙ ОБ ОШИБКАХ =============
    private void ShowErrorMessage(string message)
    {
        // Если есть UI элемент для ошибок - показываем
        if (errorMessageText != null)
        {
            errorMessageText.text = message;
            errorMessageText.gameObject.SetActive(true);

            // Автоматически скрываем через 3 секунды




            //CancelInvoke(nameof(HideErrorMessage));
            //Invoke(nameof(HideErrorMessage), 3f);




        }
        else
        {
            // Если UI нет - выводим в консоль
            UnityEngine.Debug.LogWarning(message);
        }
    }

    private void HideErrorMessage()
    {
        if (errorMessageText != null)
            errorMessageText.gameObject.SetActive(false);
    }

    // ============= ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =============
    private void SetButtonsInteractable(bool interactable)
    {
        foreach (var button in menuButtons)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }

    // НОВЫЙ МЕТОД: Снимает выделение со всех кнопок
    private void DeselectAllButtons()
    {
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void CloseAllPanels()
    {
        mainMenuButtons.SetActive(true);
        aboutAuthorPanel.SetActive(false);

        if (multiplayerRoomPanel != null)
            multiplayerRoomPanel.SetActive(false);

        if (ButtonsMultiPlayerRoomPanel != null)
            ButtonsMultiPlayerRoomPanel.SetActive(false);

        if (createRoomPanel != null)
            createRoomPanel.SetActive(false);

        if (joinRoomPanel != null)
            joinRoomPanel.SetActive(false);

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (blurImage != null)
            blurImage.SetActive(false);
    }



    private void CleanupResources()
    {
        // Останавливаем все Invoke
        //CancelInvoke();



        //// Отключаемся от Photon, если подключены
        //if (PhotonNetwork.IsConnected)
        //{
        //    PhotonNetwork.Disconnect();
        //}





        // Очищаем массив кнопок
        if (menuButtons != null)
        {
            menuButtons = null;
        }

        // Очищаем InputFields
        if (create != null)
        {
            create.text = "";
            create = null;
        }

        if (join != null)
        {
            join.text = "";
            join = null;
        }

        // Очищаем ссылки на панели
        mainMenuButtons = null;
        aboutAuthorPanel = null;
        multiplayerRoomPanel = null;
        ButtonsMultiPlayerRoomPanel = null;
        createRoomPanel = null;
        joinRoomPanel = null;
        settingsPanel = null;
        blurImage = null;

        // Очищаем ссылки на меню настроек
        graphicsMenu = null;
        soundMenu = null;
        languageMenu = null;
        graphicsButton = null;
        soundButton = null;
        languageButton = null;

        // Очищаем ссылки на валидатор и текст ошибок
        validator = null;
        errorMessageText = null;

        // Принудительная сборка мусора
        System.GC.Collect();
        Resources.UnloadUnusedAssets();
    }
}