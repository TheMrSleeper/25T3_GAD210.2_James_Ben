using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

public enum TurnPhase
{
    WaitingForPlayers,
    EventResolution,
    MessageTyping,
    EndOfTurn,
    GameOver
}

public enum EventOptionTag
{
    Neutral,
    Altruistic,
    Selfish
}

[System.Serializable]
public class EventOption
{
    [TextArea] public string optionLabel;

    // Tag for analytics / debugging
    public EventOptionTag tag = EventOptionTag.Neutral;

    // Effects on the ACTIVE player (the one making the choice)
    public int selfSuppliesDelta;
    public int selfIntegrityDelta;

    // Effects on the OTHER player
    public int otherSuppliesDelta;
    public int otherIntegrityDelta;
}

[System.Serializable]
public class EventDefinition
{
    public string id;
    [TextArea] public string title;
    [TextArea] public string description;
    public EventOption[] options;
}

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Connection Status")]
    public NetworkVariable<int> ConnectedPlayersCount = new NetworkVariable<int>(
       0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<FixedString64Bytes> HostIpHint = new NetworkVariable<FixedString64Bytes>(
        "", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Events (modular, editable in Inspector)")]
    [SerializeField] private EventDefinition[] events;

    [Header("Turns")]
    [SerializeField] private int maxTurnsPerPlayer = 4;

    public NetworkVariable<ulong> ActivePlayerClientId = new NetworkVariable<ulong>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<TurnPhase> CurrentPhase = new NetworkVariable<TurnPhase>(
        TurnPhase.WaitingForPlayers, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> CurrentEventIndex = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly Dictionary<ulong, PlayerState> _players = new();
    private readonly Dictionary<ulong, int> _turnCounts = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // Set host IP hint once, from NetworkBootstrap
            HostIpHint.Value = NetworkBootstrap.LocalIpAddress;
        }
    }

    private void OnDestroy()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[GameManager] Client connected: {clientId}");
        // PlayerState will call RegisterPlayer when it spawns
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[GameManager] Client disconnected: {clientId}");
    }

    /// <summary>
    /// Called by PlayerState (on server) when it spawns.
    /// </summary>
    public void RegisterPlayer(PlayerState player)
    {
        if (!IsServer) return;

        _players[player.OwnerClientId] = player;
        if (!_turnCounts.ContainsKey(player.OwnerClientId))
        {
            _turnCounts[player.OwnerClientId] = 0;
        }

        ConnectedPlayersCount.Value = _players.Count;

        Debug.Log($"[GameManager] Registered player for client {player.OwnerClientId}. Count: {_players.Count}");

        if (_players.Count == 2 && CurrentPhase.Value == TurnPhase.WaitingForPlayers)
        {
            StartFirstTurn();
        }
    }

    private void StartFirstTurn()
    {
        ulong hostId = NetworkManager.Singleton.LocalClientId;
        ActivePlayerClientId.Value = hostId;

        Debug.Log($"[GameManager] Starting first turn. Active player: {hostId}");

        SelectRandomEvent();
        CurrentPhase.Value = TurnPhase.EventResolution;

        // Initial fuzzy estimate for both players
        BroadcastOtherEstimates();
    }

    private void SelectRandomEvent()
    {
        if (events == null || events.Length == 0)
        {
            Debug.LogError("[GameManager] No events configured!");
            CurrentEventIndex.Value = -1;
            return;
        }

        int index = Random.Range(0, events.Length);
        CurrentEventIndex.Value = index;

        Debug.Log($"[GameManager] Selected event index {index} ({events[index].id})");
    }

    public EventDefinition GetCurrentEvent()
    {
        if (events == null || events.Length == 0) return null;
        int idx = CurrentEventIndex.Value;
        if (idx < 0 || idx >= events.Length) return null;
        return events[idx];
    }

    private PlayerState GetPlayer(ulong clientId)
    {
        _players.TryGetValue(clientId, out var player);
        return player;
    }

    private PlayerState GetOtherPlayer(ulong clientId)
    {
        foreach (var kv in _players)
        {
            if (kv.Key != clientId)
                return kv.Value;
        }
        return null;
    }

    private ulong GetOtherClientId(ulong clientId)
    {
        foreach (var kv in _players)
        {
            if (kv.Key != clientId)
                return kv.Key;
        }
        return clientId;
    }

    // --- Event option resolution ---

    [ServerRpc(RequireOwnership = false)]
    public void SubmitEventOptionServerRpc(int optionIndex, ServerRpcParams rpcParams = default)
    {
        // Only handle options in the EventResolution phase
        if (CurrentPhase.Value != TurnPhase.EventResolution)
            return;

        ulong senderClientId = rpcParams.Receive.SenderClientId;

        // Only the active player is allowed to choose
        if (senderClientId != ActivePlayerClientId.Value)
        {
            Debug.LogWarning($"[GameManager] Client {senderClientId} tried to choose event option but is not active.");
            return;
        }

        var evt = GetCurrentEvent();
        if (evt == null || evt.options == null || evt.options.Length == 0)
        {
            Debug.LogError("[GameManager] No current event or no options.");
            return;
        }

        if (optionIndex < 0 || optionIndex >= evt.options.Length)
        {
            Debug.LogWarning($"[GameManager] Invalid option index {optionIndex}.");
            return;
        }

        var option = evt.options[optionIndex];

        var self = GetPlayer(senderClientId);
        var other = GetOtherPlayer(senderClientId);

        if (self == null || other == null)
        {
            Debug.LogError("[GameManager] Cannot resolve event: missing players.");
            return;
        }

        Debug.Log($"[GameManager] Resolving event '{evt.id}', option {optionIndex} ({option.tag}).");

        // --- Apply deltas to ACTIVE player ---
        self.Supplies.Value = Mathf.Max(0, self.Supplies.Value + option.selfSuppliesDelta);
        self.Integrity.Value = Mathf.Clamp(self.Integrity.Value + option.selfIntegrityDelta, 0, 100);

        // --- Apply deltas to OTHER player ---
        other.Supplies.Value = Mathf.Max(0, other.Supplies.Value + option.otherSuppliesDelta);
        other.Integrity.Value = Mathf.Clamp(other.Integrity.Value + option.otherIntegrityDelta, 0, 100);

        // Recompute survival chances for both
        self.SurvivalChance.Value = ComputeSurvivalChance(self.Integrity.Value, self.Supplies.Value);
        other.SurvivalChance.Value = ComputeSurvivalChance(other.Integrity.Value, other.Supplies.Value);

        // Update fuzzy estimates for both clients
        BroadcastOtherEstimates();

        Debug.Log($"[GameManager] After option {optionIndex}: " +
                  $"Self (CID {senderClientId}) -> Supplies {self.Supplies.Value}, Integrity {self.Integrity.Value}, Survival {self.SurvivalChance.Value}% | " +
                  $"Other -> Supplies {other.Supplies.Value}, Integrity {other.Integrity.Value}, Survival {other.SurvivalChance.Value}%");

        // Transition to message-typing phase
        CurrentPhase.Value = TurnPhase.MessageTyping;
    }

    private int ComputeSurvivalChance(int integrity, int supplies)
    {
        if (integrity <= 0)
            return 0;

        // Basic heuristic:
        // - High integrity + some supplies: 100%
        // - Mid integrity: mid-range chance, boosted by supplies
        // - Low integrity: low base, slightly boosted by supplies

        float baseChance;

        if (integrity >= 80)
        {
            baseChance = 90f + supplies * 1.5f;
        }
        else if (integrity >= 60)
        {
            baseChance = 60f + supplies * 2f;
        }
        else if (integrity >= 40)
        {
            baseChance = 40f + supplies * 2.5f;
        }
        else if (integrity >= 20)
        {
            baseChance = 20f + supplies * 3f;
        }
        else // integrity between 1 and 19
        {
            baseChance = 5f + supplies * 2f;
        }

        int result = Mathf.Clamp(Mathf.RoundToInt(baseChance), 0, 100);
        return result;
    }

    private bool RollFinalSurvival(int integrity, int supplies, int survivalChance)
    {
        // Hard rules first
        if (integrity <= 0)
            return false;

        if (survivalChance >= 99)
            return true;

        if (survivalChance <= 0)
            return false;

        // Use SurvivalChance as a percentage
        float roll = Random.Range(0f, 100f);
        return roll < survivalChance;
    }

    // --- Other crew estimate logic ---

    private void ComputeFuzzyEstimate(int trueValue, out string category)
    {
        // Base categorisation
        // 0–30  => LOW
        // 31–70 => MID
        // 71–100 => HIGH
        string baseCat;
        if (trueValue <= 30) baseCat = "LOW";
        else if (trueValue <= 70) baseCat = "MID";
        else baseCat = "HIGH";

        // Add some "sensor noise": 20% chance to wobble to neighbouring category
        float roll = Random.value; // 0–1
        if (roll < 0.2f)
        {
            if (baseCat == "LOW")
            {
                // Sometimes misread LOW as MID
                baseCat = "MID";
            }
            else if (baseCat == "HIGH")
            {
                // Sometimes misread HIGH as MID
                baseCat = "MID";
            }
            else // MID
            {
                // MID can wobble to LOW or HIGH
                baseCat = (Random.value < 0.5f) ? "LOW" : "HIGH";
            }
        }

        category = baseCat;
    }

    private void BroadcastOtherEstimates()
    {
        if (!IsServer || _players.Count < 2) return;

        foreach (var kv in _players)
        {
            ulong viewerId = kv.Key;
            var otherPlayer = GetOtherPlayer(viewerId);
            if (otherPlayer == null) continue;

            // Compute fuzzy categories based on other's real stats
            ComputeFuzzyEstimate(otherPlayer.Integrity.Value, out string integCat);
            ComputeFuzzyEstimate(otherPlayer.Supplies.Value, out string supCat);

            var rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { viewerId }
                }
            };

            UpdateOtherEstimateClientRpc(integCat, supCat, rpcParams);
        }
    }

    [ClientRpc]
    private void UpdateOtherEstimateClientRpc(string integrityEstimate, string suppliesEstimate, ClientRpcParams clientRpcParams = default)
    {
        if (GameUIController.Instance == null) return;

        GameUIController.Instance.UpdateOtherEstimate(integrityEstimate, suppliesEstimate);
    }

    // --- Messaging and turn end ---

    [ServerRpc(RequireOwnership = false)]
    public void SubmitMessageServerRpc(string message, ServerRpcParams rpcParams = default)
    {
        if (CurrentPhase.Value != TurnPhase.MessageTyping)
            return;

        ulong senderClientId = rpcParams.Receive.SenderClientId;
        if (senderClientId != ActivePlayerClientId.Value)
        {
            Debug.LogWarning($"[GameManager] Client {senderClientId} tried to send message but is not active.");
            return;
        }

        if (message == null) message = "";
        message = message.Trim();
        if (message.Length == 0)
            message = "[NO MESSAGE]";

        Debug.Log($"[GameManager] Message from {senderClientId}: {message}");

        // Broadcast to all clients
        ReceiveMessageClientRpc(message, senderClientId);

        // End the turn and switch to the other player (or game over)
        EndTurn(senderClientId);
    }

    [ClientRpc]
    private void ReceiveMessageClientRpc(string message, ulong senderClientId)
    {
        if (GameUIController.Instance == null || NetworkManager.Singleton == null)
            return;

        ulong localId = NetworkManager.Singleton.LocalClientId;

        if (localId == senderClientId)
        {
            // Sent the message
            GameUIController.Instance.SetRemoteMessageStatus("TRANSMISSION SENT.");
            // DO NOT record this as "last message from other"
        }
        else
        {
            // Other engineer receives it
            GameUIController.Instance.SetRemoteMessageStatus($"MESSAGE RECEIVED: \"{message}\"");

            // Store this so the next event, it is seen at the top
            GameUIController.Instance.RecordLastMessageFromOther(message);
        }
    }

    private void EndTurn(ulong justFinishedClientId)
    {
        if (!_turnCounts.ContainsKey(justFinishedClientId))
            _turnCounts[justFinishedClientId] = 0;

        _turnCounts[justFinishedClientId]++;

        // Check if both players have hit max turns
        bool allDone = (_players.Count >= 2);
        if (allDone)
        {
            foreach (var kv in _turnCounts)
            {
                if (kv.Value < maxTurnsPerPlayer)
                {
                    allDone = false;
                    break;
                }
            }
        }

        if (allDone)
        {
            Debug.Log("[GameManager] Max turns reached. Resolving final outcome...");
            ResolveFinalOutcome();
            return;
        }

        // Switch active player to the other
        ulong nextId = GetOtherClientId(justFinishedClientId);
        ActivePlayerClientId.Value = nextId;

        // Select a new event
        SelectRandomEvent();

        CurrentPhase.Value = TurnPhase.EventResolution;
        Debug.Log($"[GameManager] New turn. Active player: {nextId}");
    }

    private void ResolveFinalOutcome()
    {
        if (!IsServer || _players.Count < 1) return;

        // Compute a final survived/dead result for each client ONCE
        var finalSurvival = new Dictionary<ulong, bool>();

        foreach (var kv in _players)
        {
            ulong clientId = kv.Key;
            var ps = kv.Value;

            bool survived = RollFinalSurvival(
                ps.Integrity.Value,
                ps.Supplies.Value,
                ps.SurvivalChance.Value
            );

            finalSurvival[clientId] = survived;
        }

        // For each client, build a local vs other summary and send via ClientRpc
        foreach (var kv in _players)
        {
            ulong viewerId = kv.Key;
            var localPlayer = kv.Value;
            var otherPlayer = GetOtherPlayer(viewerId);

            bool localSurvived = finalSurvival.TryGetValue(viewerId, out var ls) && ls;

            bool otherSurvived = false;
            ulong otherId = viewerId;
            if (otherPlayer != null)
            {
                otherId = otherPlayer.OwnerClientId;
                otherSurvived = finalSurvival.TryGetValue(otherId, out var os) && os;
            }

            // Build local summary block
            string localOutcomeText = localSurvived ? "SURVIVED" : "DECEASED";
            string localSummary =
                "[COMPARTMENT STATUS // YOU]\n" +
                $"Integrity at shutdown: {localPlayer.Integrity.Value}%\n" +
                $"Supplies remaining: {localPlayer.Supplies.Value}\n" +
                $"Final survival diagnostic: {localPlayer.SurvivalChance.Value}%\n" +
                $"Outcome: {localOutcomeText}";

            // Build other summary block
            string otherSummary;
            if (otherPlayer != null)
            {
                string otherOutcomeText = otherSurvived ? "SURVIVED" : "DECEASED";
                otherSummary =
                    "[COMPARTMENT STATUS // OTHER]\n" +
                    $"Integrity at shutdown: {otherPlayer.Integrity.Value}%\n" +
                    $"Supplies remaining: {otherPlayer.Supplies.Value}\n" +
                    $"Final survival diagnostic: {otherPlayer.SurvivalChance.Value}%\n" +
                    $"Outcome: {otherOutcomeText}";
            }
            else
            {
                otherSummary =
                    "[COMPARTMENT STATUS // OTHER]\n" +
                    "No telemetry received.\n" +
                    "Outcome: UNKNOWN";
            }

            // Overall system-level outcome summary
            string overallSummary;
            if (localSurvived && otherSurvived)
            {
                overallSummary =
                    "Both compartments maintained critical thresholds.\n" +
                    "Vessel remains marginally spaceworthy. Debriefing recommended.";
            }
            else if (localSurvived && !otherSurvived)
            {
                overallSummary =
                    "Only your compartment remained within survivable bounds.\n" +
                    "Systemic damage sustained. Rescue beacon deployment pending.";
            }
            else if (!localSurvived && otherSurvived)
            {
                overallSummary =
                    "Your compartment fell below survivable thresholds.\n" +
                    "Other engineer stabilized their section; recovery of remains uncertain.";
            }
            else // neither survived
            {
                overallSummary =
                    "Neither compartment restored functional stability.\n" +
                    "Vessel lost with all remaining crew. Incident logged for archive.";
            }

            var rpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { viewerId }
                }
            };

            ShowGameOverClientRpc(localSummary, otherSummary, overallSummary, rpcParams);
        }

        // Set phase to GameOver
        CurrentPhase.Value = TurnPhase.GameOver;
        Debug.Log("[GameManager] Final outcome resolved. GameOver.");
    }

    [ClientRpc]
    private void ShowGameOverClientRpc(string localSummary, string otherSummary, string overallSummary, ClientRpcParams clientRpcParams = default)
    {
        if (GameUIController.Instance == null) return;

        GameUIController.Instance.ShowGameOver(localSummary, otherSummary, overallSummary);
    }
}