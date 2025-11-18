using UnityEngine;
using Unity.Netcode;

public class GameClientController : MonoBehaviour
{
    private ulong _localClientId;
    private bool _subscribed;

    private void Start()
    {
        TrySubscribe();
    }

    private void Update()
    {
        if (!_subscribed)
        {
            TrySubscribe();
        }
    }

    private void TrySubscribe()
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsSpawned)
            return;

        if (NetworkManager.Singleton == null ||
            (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost))
            return;

        _localClientId = NetworkManager.Singleton.LocalClientId;

        var gm = GameManager.Instance;

        // Subscribe with correctly-typed handlers
        gm.ActivePlayerClientId.OnValueChanged += OnActivePlayerChanged;
        gm.CurrentPhase.OnValueChanged += OnPhaseChanged;
        gm.CurrentEventIndex.OnValueChanged += OnEventChanged;

        _subscribed = true;

        // Initial UI setup
        RefreshUI();
        RefreshEventText();
    }

    private void OnDestroy()
    {
        if (_subscribed && GameManager.Instance != null)
        {
            var gm = GameManager.Instance;
            gm.ActivePlayerClientId.OnValueChanged -= OnActivePlayerChanged;
            gm.CurrentPhase.OnValueChanged -= OnPhaseChanged;
            gm.CurrentEventIndex.OnValueChanged -= OnEventChanged;
        }
    }

    // --- Handlers for NetworkVariables ---

    private void OnActivePlayerChanged(ulong oldValue, ulong newValue)
    {
        RefreshUI();
    }

    private void OnPhaseChanged(TurnPhase oldPhase, TurnPhase newPhase)
    {
        RefreshUI();
    }

    private void OnEventChanged(int oldIndex, int newIndex)
    {
        RefreshEventText();
    }

    // --- UI Logic ---

    private void RefreshUI()
    {
        if (GameManager.Instance == null || GameUIController.Instance == null)
            return;

        var gm = GameManager.Instance;
        var phase = gm.CurrentPhase.Value;
        bool isActive = (_localClientId == gm.ActivePlayerClientId.Value);

        switch (phase)
        {
            case TurnPhase.WaitingForPlayers:
                // Host is alone in the game, or only one player registered so far
                GameUIController.Instance.ShowWaitForPlayersPanel();
                break;

            case TurnPhase.EventResolution:
                if (isActive)
                    GameUIController.Instance.ShowEventPanel();
                else
                    GameUIController.Instance.ShowWaitingForTurnPanel();
                break;

            case TurnPhase.MessageTyping:
                GameUIController.Instance.ShowMessagePanel(isActive);
                break;

            case TurnPhase.GameOver:
                // TODO: show an end-of-game summary panel later
                GameUIController.Instance.ShowWaitingForTurnPanel();
                break;

            default:
                // For any transitional states, default to "waiting for turn" to avoid weirdness
                GameUIController.Instance.ShowWaitingForTurnPanel();
                break;
        }
    }

    private void RefreshEventText()
    {
        if (GameManager.Instance == null || GameUIController.Instance == null)
            return;

        var evt = GameManager.Instance.GetCurrentEvent();
        if (evt == null)
        {
            GameUIController.Instance.SetEventText(
                "[NO EVENT]",
                "No event configured.",
                "",
                "",
                ""
            );
            return;
        }

        string option1 = evt.options != null && evt.options.Length > 0 ? evt.options[0].optionLabel : "";
        string option2 = evt.options != null && evt.options.Length > 1 ? evt.options[1].optionLabel : "";
        string option3 = evt.options != null && evt.options.Length > 2 ? evt.options[2].optionLabel : "";

        GameUIController.Instance.SetEventText(
            evt.title,
            evt.description,
            option1,
            option2,
            option3
        );
    }
}