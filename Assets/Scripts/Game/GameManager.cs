using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public enum TurnPhase
{
    WaitingForPlayers,
    EventResolution,
    MessageTyping,
    EndOfTurn,
    GameOver
}

[System.Serializable]
public class EventOption
{
    [TextArea] public string optionLabel;

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

        Debug.Log($"[GameManager] Registered player for client {player.OwnerClientId}. Count: {_players.Count}");

        if (_players.Count == 2 && CurrentPhase.Value == TurnPhase.WaitingForPlayers)
        {
            StartFirstTurn();
        }
    }

    private void StartFirstTurn()
    {
        // For simplicity, start with the host
        ulong hostId = NetworkManager.Singleton.LocalClientId;
        ActivePlayerClientId.Value = hostId;

        Debug.Log($"[GameManager] Starting first turn. Active player: {hostId}");

        SelectRandomEvent();
        CurrentPhase.Value = TurnPhase.EventResolution;
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
        if (CurrentPhase.Value != TurnPhase.EventResolution)
            return;

        ulong senderClientId = rpcParams.Receive.SenderClientId;
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

        // Apply deltas
        self.Supplies.Value += option.selfSuppliesDelta;
        self.Integrity.Value = Mathf.Clamp(self.Integrity.Value + option.selfIntegrityDelta, 0, 100);

        other.Supplies.Value += option.otherSuppliesDelta;
        other.Integrity.Value = Mathf.Clamp(other.Integrity.Value + option.otherIntegrityDelta, 0, 100);

        // Recompute survival chances for both
        self.SurvivalChance.Value = ComputeSurvivalChance(self.Integrity.Value, self.Supplies.Value);
        other.SurvivalChance.Value = ComputeSurvivalChance(other.Integrity.Value, other.Supplies.Value);

        Debug.Log($"[GameManager] Option {optionIndex} applied. Self: (Supplies {self.Supplies.Value}, Integrity {self.Integrity.Value}), Other: (Supplies {other.Supplies.Value}, Integrity {other.Integrity.Value})");

        // Move to message typing phase
        CurrentPhase.Value = TurnPhase.MessageTyping;
    }

    private int ComputeSurvivalChance(int integrity, int supplies)
    {
        if (integrity <= 0)
            return 0;

        // Very simple, tweakable heuristic:
        // - High integrity + supplies => ~100%
        // - Lower integrity => lower base, improved slightly by supplies
        float baseChance;
        if (integrity >= 80 && supplies >= 10)
            return 100;
        else if (integrity >= 60)
            baseChance = 60f;
        else if (integrity >= 40)
            baseChance = 40f;
        else if (integrity >= 20)
            baseChance = 20f;
        else
            baseChance = 5f; // very low integrity

        baseChance += supplies * 3f; // each supply adds ~3%

        int result = Mathf.Clamp(Mathf.RoundToInt(baseChance), 0, 100);
        return result;
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
            // We sent the message
            GameUIController.Instance.SetRemoteMessageStatus("TRANSMISSION SENT.");
            // We DO NOT record this as "last message from other"
        }
        else
        {
            // Other engineer receives it
            GameUIController.Instance.SetRemoteMessageStatus($"MESSAGE RECEIVED: \"{message}\"");

            // Store this so the next time WE get an event, we see it at the top
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
            Debug.Log("[GameManager] Max turns reached. Game over.");
            CurrentPhase.Value = TurnPhase.GameOver;
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
}