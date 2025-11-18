using UnityEngine;
using TMPro;

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

    [Header("Center Panels")]
    [SerializeField] private GameObject panelEvent;
    [SerializeField] private GameObject panelWaiting;
    [SerializeField] private GameObject panelMessage;
    [SerializeField] private GameObject panelWaitForPlayers;

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

    #endregion

    #region Center Panel Modes

    public void ShowEventPanel()
    {
        if (panelEvent != null) panelEvent.SetActive(true);
        if (panelWaiting != null) panelWaiting.SetActive(false);
        if (panelMessage != null) panelMessage.SetActive(false);
        if (panelWaitForPlayers != null) panelWaitForPlayers.SetActive(false);

        // Make sure the last-transmission label is always in sync
        UpdateLastMessageLabel();
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
            textOption3.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(option3));
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

    public TMP_InputField GetMessageInputField() => inputMessage;
}