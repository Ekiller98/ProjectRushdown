using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject lobbyPanel;

    [Header("Player List UI")]
    public Transform playerListContainer;    // PlayerListContainer
    public GameObject playerEntryPrefab;     // PlayerEntry prefab (with NameText + StatusText)

    [Header("Buttons")]
    public Button toBlueButton;
    public Button toRedButton;
    public Button readyButton;
    public Button startMatchButton;
    public Button backButton;

    [Header("Canvases")]
    public GameObject mainMenuCanvasRoot;   //
    public GameObject lobbyCanvasRoot;      //
    public GameObject hudCanvasRoot;        //

    private PlayerLobbyState _localLobbyState;

    private float _refreshTimer = 0f;
    public float refreshInterval = 0.5f;

    private void Start()
    {
        if (lobbyPanel != null)
            lobbyPanel.SetActive(true);

        // Hook buttons
        if (toBlueButton != null)
            toBlueButton.onClick.AddListener(() => ChangeTeamLocal(0));

        if (toRedButton != null)
            toRedButton.onClick.AddListener(() => ChangeTeamLocal(1));

        if (readyButton != null)
            readyButton.onClick.AddListener(ToggleReadyLocal);

        if (startMatchButton != null)
            startMatchButton.onClick.AddListener(OnStartMatchClicked);

        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    private void Update()
    {
        _refreshTimer += Time.deltaTime;
        if (_refreshTimer >= refreshInterval)
        {
            _refreshTimer = 0f;
            RefreshLocalLobbyState();
            RefreshPlayerList();
            RefreshStartMatchButton();
        }

        // 🔥 Auto-hide lobby once the round actually starts
        if (RoundManager.Instance != null && RoundManager.Instance.roundInProgress.Value)
        {
            if (lobbyCanvasRoot != null)
                lobbyCanvasRoot.SetActive(false);

            if (hudCanvasRoot != null)
                hudCanvasRoot.SetActive(true);
        }
    }

    private void RefreshLocalLobbyState()
    {
        if (_localLobbyState != null && _localLobbyState.IsOwner)
            return;

        var allStates = FindObjectsOfType<PlayerLobbyState>();
        foreach (var pls in allStates)
        {
            if (pls.IsOwner)
            {
                _localLobbyState = pls;
                break;
            }
        }
    }

    private void RefreshPlayerList()
    {
        if (playerListContainer == null || playerEntryPrefab == null)
            return;

        // Clear old entries
        foreach (Transform child in playerListContainer)
        {
            Destroy(child.gameObject);
        }

        var allStates = FindObjectsOfType<PlayerLobbyState>();

        foreach (var pls in allStates)
        {
            GameObject row = Instantiate(playerEntryPrefab, playerListContainer);

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 2)
            {
                // simple name: "Player <id>"
                string nameLabel = pls.IsOwner
                    ? $"You ({pls.OwnerClientId})"
                    : $"Player {pls.OwnerClientId}";

                int teamId = pls.GetTeamId();
                string teamLabel = teamId == 0 ? "RED" : "BLUE";

                string readyLabel = pls.IsReady ? "READY" : "NOT READY";

                texts[0].text = nameLabel;
                texts[1].text = $"{teamLabel} - {readyLabel}";
            }
        }
    }

    private void RefreshStartMatchButton()
    {
        if (startMatchButton == null)
            return;

        // Only host can start the match
        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        startMatchButton.gameObject.SetActive(isHost);

        if (!isHost)
            return;

        // Host: only enable if everyone is ready
        var allStates = FindObjectsOfType<PlayerLobbyState>();
        if (allStates.Length == 0)
        {
            startMatchButton.interactable = false;
            return;
        }

        bool allReady = true;
        foreach (var pls in allStates)
        {
            if (!pls.IsReady)
            {
                allReady = false;
                break;
            }
        }

        startMatchButton.interactable = allReady;
    }

    private void OnBackClicked()
    {
        // Leave the network session
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Show main menu again
        if (mainMenuCanvasRoot != null)
            mainMenuCanvasRoot.SetActive(true);

        // Hide lobby
        if (lobbyCanvasRoot != null)
            lobbyCanvasRoot.SetActive(false);

        // Optionally hide HUD if it got turned on somehow
        if (hudCanvasRoot != null)
            hudCanvasRoot.SetActive(false);
    }


    private void ChangeTeamLocal(int teamId)
    {
        if (_localLobbyState == null) return;
        _localLobbyState.ChangeTeamFromLocal(teamId);
    }

    private void ToggleReadyLocal()
    {
        if (_localLobbyState == null) return;
        _localLobbyState.ToggleReadyFromLocal();
    }

    private void OnStartMatchClicked()
    {
        // Only host should ever see this
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.StartMatchFromLobby();
        }
    }
}
