using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUIController : MonoBehaviour
{
    public static GameUIController Instance { get; private set; }

    [Header("Top HUD - You")]
    [SerializeField] private TextMeshProUGUI textYouIntegrity;
    [SerializeField] private TextMeshProUGUI textYouSupplies;

    [Header("Top HUD - Survival")]
    [SerializeField] private TextMeshProUGUI textSurvivalValue;

    [Header("Top HUD - Other Crew Estimate")]
    [SerializeField] private TextMeshProUGUI textOtherIntegrity;
    [SerializeField] private TextMeshProUGUI textOtherSupplies;

    [Header("Bottom HUD")]
    [SerializeField] private GameObject panelBottomBar;
    [SerializeField] private GameObject buttonReturnToMenu;
    [SerializeField] private GameObject buttonPlayAgain;

    [Header("Center Panels")]
    [SerializeField] private GameObject panelEvent;
    [SerializeField] private GameObject panelEventOptions;
    [SerializeField] private GameObject panelWaiting;
    [SerializeField] private GameObject panelMessage;

    [Header("Event UI")]
    [SerializeField] private TextMeshProUGUI textEventTitle;
    [SerializeField] private TextMeshProUGUI textEventDescription;
    [SerializeField] private TextMeshProUGUI textLastMessageFromOther;
    [SerializeField] private TextMeshProUGUI textOption1;
    [SerializeField] private TextMeshProUGUI textOption2;
    [SerializeField] private TextMeshProUGUI textOption3;

    [Header("Message UI")]
    [SerializeField] private TMP_InputField inputMessage;
    [SerializeField] private TextMeshProUGUI textMessageRemoteStatus;
    [SerializeField] private GameObject panelMessageLocal;
    [SerializeField] private GameObject panelMessageRemote;

    [Header("Wait For Players UI")]
    [SerializeField] private GameObject panelWaitForPlayers;
    [SerializeField] private TextMeshProUGUI textConnectedStatus;
    [SerializeField] private TextMeshProUGUI textHostIpHint;

    [Header("Game Over UI")]
    [SerializeField] private GameObject panelGameOver;
    [SerializeField] private TextMeshProUGUI textGameOverTitle;
    [SerializeField] private TextMeshProUGUI textGameOverLocal;
    [SerializeField] private TextMeshProUGUI textGameOverOther;
    [SerializeField] private TextMeshProUGUI textGameOverOverall;

    private string _lastMessageFromOther = "";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    #region HUD Updates

    public void UpdateLocalStats(int integrityPercent, int supplies, string survivalText)
    {
        if (textYouIntegrity != null)
            textYouIntegrity.text = $"INTEGRITY: {integrityPercent}%";

        if (textYouSupplies != null)
            textYouSupplies.text = $"SUPPLIES: {supplies}";

        if (textSurvivalValue != null)
            textSurvivalValue.text = survivalText.ToUpperInvariant();
    }

    public void UpdateOtherEstimate(string integrityEstimate, string suppliesEstimate)
    {
        if (textOtherIntegrity != null)
            textOtherIntegrity.text = $"INTEGRITY: {integrityEstimate.ToUpperInvariant()}";

        if (textOtherSupplies != null)
            textOtherSupplies.text = $"SUPPLIES: {suppliesEstimate.ToUpperInvariant()}";
    }

    public void SetBottomBarDefaultMode()
    {
        if (panelBottomBar != null)
            panelBottomBar.SetActive(true);

        if (buttonReturnToMenu != null)
            buttonReturnToMenu.SetActive(true);

        if (buttonPlayAgain != null)
            buttonPlayAgain.SetActive(false);
    }

    public void SetBottomBarGameOverMode()
    {
        if (panelBottomBar != null)
            panelBottomBar.SetActive(true);

        if (buttonReturnToMenu != null)
            buttonReturnToMenu.SetActive(true);

        if (buttonPlayAgain != null)
            buttonPlayAgain.SetActive(true);
    }
    #endregion

    #region Center Panel Modes

    public void ShowEventPanel(bool isLocalActive)
    {
        // Turn central panels on/off
        if (panelEvent != null) panelEvent.SetActive(true);
        if (panelWaiting != null) panelWaiting.SetActive(false);
        if (panelMessage != null) panelMessage.SetActive(false);
        if (panelWaitForPlayers != null) panelWaitForPlayers.SetActive(false);
        if (panelGameOver != null) panelGameOver.SetActive(false);

        if (panelEventOptions != null)
            panelEventOptions.SetActive(isLocalActive);
    }

    public void ShowWaitingPanel()
    {
        ShowWaitingForTurnPanel();
    }

    public void ShowMessagePanel(bool isLocalActive)
    {
        if (panelEvent != null) panelEvent.SetActive(false);
        if (panelWaiting != null) panelWaiting.SetActive(false);
        if (panelWaitForPlayers != null) panelWaitForPlayers.SetActive(false);
        if (panelMessage != null) panelMessage.SetActive(true);

        if (panelMessageLocal != null)
            panelMessageLocal.SetActive(isLocalActive);
        if (panelMessageRemote != null)
            panelMessageRemote.SetActive(!isLocalActive);

        if (inputMessage != null)
        {
            inputMessage.interactable = isLocalActive;
            inputMessage.text = "";
            if (isLocalActive)
                inputMessage.ActivateInputField();
        }

        if (!isLocalActive)
        {
            SetRemoteMessageStatus("INCOMING TRANSMISSION...");
        }
    }

    #endregion

    #region Event Setup

    public void SetEventText(string title, string description,
                             string option1, string option2, string option3 = null)
    {
        if (textEventTitle != null) textEventTitle.text = title;
        if (textEventDescription != null) textEventDescription.text = description;

        if (textOption1 != null) textOption1.text = option1;
        if (textOption2 != null) textOption2.text = option2;

        if (textOption3 != null)
        {
            GameObject option3Object = textOption3.gameObject;

            option3Object.SetActive(!string.IsNullOrEmpty(option3));
            textOption3.text = option3 ?? "";
        }
    }

    #endregion

    #region Remote Message Display

    public void SetRemoteMessageStatus(string status)
    {
        if (textMessageRemoteStatus != null)
            textMessageRemoteStatus.text = status;
    }

    public void ShowWaitForPlayersPanel()
    {
        if (panelEvent != null) panelEvent.SetActive(false);
        if (panelWaiting != null) panelWaiting.SetActive(false);
        if (panelMessage != null) panelMessage.SetActive(false);
        if (panelWaitForPlayers != null) panelWaitForPlayers.SetActive(true);
    }

    public void ShowWaitingForTurnPanel()
    {
        if (panelEvent != null) panelEvent.SetActive(false);
        if (panelMessage != null) panelMessage.SetActive(false);
        if (panelWaitForPlayers != null) panelWaitForPlayers.SetActive(false);
        if (panelGameOver != null) panelGameOver.SetActive(false);

        if (panelWaiting != null) panelWaiting.SetActive(true);
    }

    #endregion

    public void RecordLastMessageFromOther(string message)
    {
        _lastMessageFromOther = message ?? "";
        UpdateLastMessageLabel();
    }

    public void ClearLastMessageFromOther()
    {
        _lastMessageFromOther = "";
        UpdateLastMessageLabel();
    }

    private void UpdateLastMessageLabel()
    {
        if (textLastMessageFromOther == null) return;

        if (string.IsNullOrWhiteSpace(_lastMessageFromOther))
        {
            textLastMessageFromOther.text = "LAST TRANSMISSION: [NONE]";
        }
        else
        {
            textLastMessageFromOther.text = $"LAST TRANSMISSION: \"{_lastMessageFromOther}\"";
        }
    }

    public void UpdateConnectionStatus(int connected, int total, string hostIp)
    {
        if (textConnectedStatus != null)
        {
            textConnectedStatus.text = $"CONNECTED ENGINEERS: {connected} / {total}";
        }

        if (textHostIpHint != null)
        {
            if (string.IsNullOrWhiteSpace(hostIp))
            {
                textHostIpHint.text = "HOST IP: [DETECTING...]";
            }
            else
            {
                textHostIpHint.text = $"HOST IP: {hostIp}";
            }
        }
    }

    public void ShowGameOver(string localSummary, string otherSummary, string overallSummary)
    {
        // Hide the other panels
        if (panelEvent != null) panelEvent.SetActive(false);
        if (panelWaiting != null) panelWaiting.SetActive(false);
        if (panelMessage != null) panelMessage.SetActive(false);
        if (panelWaitForPlayers != null) panelWaitForPlayers.SetActive(false);

        if (panelGameOver != null) panelGameOver.SetActive(true);

        if (textGameOverTitle != null)
            textGameOverTitle.text = "== [POST-MISSION SUMMARY] ==";

        if (textGameOverLocal != null)
            textGameOverLocal.text = localSummary;

        if (textGameOverOther != null)
            textGameOverOther.text = otherSummary;

        if (textGameOverOverall != null)
            textGameOverOverall.text = overallSummary;
    }

    // Called by the Return to Main Menu button
    public void OnReturnToMenuClicked()
    {
        // Stop networking if active
        if (NetworkManager.Singleton != null)
        {
            if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.Shutdown();
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        // Load main menu locally
        SceneManager.LoadScene("MainMenu");
    }

    // Called by the Play Again button (only host actually triggers a new round)
    public void OnPlayAgainClicked()
    {
        if (NetworkManager.Singleton == null)
            return;

        if (NetworkManager.Singleton.IsHost)
        {
            // Host reloads the Game scene via Netcode scene manager
            // Pulls all connected clients along.
            NetworkManager.Singleton.SceneManager.LoadScene("Game", LoadSceneMode.Single);
        }
        else
        {
            // Add status messsage later
            Debug.Log("[UI] Play Again clicked on client. Waiting for host to restart round.");
        }
    }

    public TMP_InputField GetMessageInputField() => inputMessage;
}