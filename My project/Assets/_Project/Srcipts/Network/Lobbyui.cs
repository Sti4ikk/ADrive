using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Mirror;

/// <summary>
/// Прикрепи на MultiplayerRoomPanel.
/// Хост: Создать комнату → сразу грузится на карту.
/// Клиент: вводит IP → Присоединиться → грузится на карту.
/// </summary>
public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance { get; private set; }

    [Header("Панели")]
    [SerializeField] private GameObject multiplayerButtonsPanel;  // MultiplayerButtons
    [SerializeField] private GameObject createRoomPanel;          // CreateRoomPanel
    [SerializeField] private GameObject joinRoomPanel;            // JoinRoomPanel

    [Header("MultiplayerButtons")]
    [SerializeField] private Button createRoomButton;             // CreateRoomButton
    [SerializeField] private Button joinRoomButton;               // JoinRoomButton
    [SerializeField] private Button backButton;                   // back_button

    [Header("CreateRoomPanel")]
    [SerializeField] private Button createRoomAndGoButton;        // CreateRoomAndGoButton
    [SerializeField] private TMP_Text createErrorText;            // ErrorText

    [Header("JoinRoomPanel")]
    [SerializeField] private TMP_InputField joinIPInput;          // RoomNameEntry
    [SerializeField] private Button joinRoomAndGoButton;          // JoinRoomAndGoButton
    [SerializeField] private TMP_Text joinErrorText;              // ErrorText

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        ShowPanel(multiplayerButtonsPanel);

        createRoomButton?.onClick.AddListener(OnCreateRoomClicked);
        joinRoomButton?.onClick.AddListener(OnJoinRoomClicked);
        backButton?.onClick.AddListener(OnBackClicked);
        createRoomAndGoButton?.onClick.AddListener(OnCreateRoomAndGoClicked);
        joinRoomAndGoButton?.onClick.AddListener(OnJoinRoomAndGoClicked);
    }

    // -------------------------------------------------------
    // Кнопка "Создать комнату"
    // -------------------------------------------------------
    private void OnCreateRoomClicked()
    {
        ShowPanel(createRoomPanel);
        SetCreateError("");
    }

    // -------------------------------------------------------
    // Кнопка "Присоединиться"
    // -------------------------------------------------------
    private void OnJoinRoomClicked()
    {
        ShowPanel(joinRoomPanel);
        SetJoinError("");
        if (joinIPInput != null)
        {
            joinIPInput.text = "";
            var placeholder = joinIPInput.placeholder.GetComponent<TMP_Text>();
            if (placeholder != null) placeholder.text = "Введите IP хоста...";
        }
    }

    // -------------------------------------------------------
    // Кнопка "Запустить карту" (хост)
    // -------------------------------------------------------
    public void OnCreateRoomAndGoClicked()
    {
        SetCreateError("Запускаем...");
        NetworkManagerLobby.Instance.maxConnections = 2;
        NetworkManagerLobby.Instance.StartHost();
        // NetworkManagerLobby.OnStartHost() сам загрузит сцену
    }

    // -------------------------------------------------------
    // Кнопка "Войти" (клиент)
    // -------------------------------------------------------
    private void OnJoinRoomAndGoClicked()
    {
        string ip = joinIPInput != null ? joinIPInput.text.Trim() : "";
        if (string.IsNullOrEmpty(ip))
        {
            SetJoinError("Введите IP адрес хоста!");
            return;
        }

        SetJoinError($"Подключаемся к {ip}...");
        NetworkManagerLobby.Instance.networkAddress = ip;
        NetworkManagerLobby.Instance.StartClient();
        // Mirror сам загрузит сцену когда подключится к хосту
    }

    // -------------------------------------------------------
    // Кнопка "Назад"
    // -------------------------------------------------------
    private void OnBackClicked()
    {
        if (createRoomPanel != null && createRoomPanel.activeSelf)
        {
            ShowPanel(multiplayerButtonsPanel);
            return;
        }
        if (joinRoomPanel != null && joinRoomPanel.activeSelf)
        {
            ShowPanel(multiplayerButtonsPanel);
            return;
        }
        // Закрываем MultiplayerRoomPanel
        gameObject.SetActive(false);
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------
    private void ShowPanel(GameObject panel)
    {
        if (multiplayerButtonsPanel != null) multiplayerButtonsPanel.SetActive(false);
        if (createRoomPanel != null) createRoomPanel.SetActive(false);
        if (joinRoomPanel != null) joinRoomPanel.SetActive(false);
        if (panel != null) panel.SetActive(true);
    }

    private void SetCreateError(string msg)
    {
        if (createErrorText != null) createErrorText.text = msg;
    }

    private void SetJoinError(string msg)
    {
        if (joinErrorText != null) joinErrorText.text = msg;
    }
}