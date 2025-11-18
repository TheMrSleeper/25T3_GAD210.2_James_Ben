using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerState : NetworkBehaviour
{
    // 0–100 integrity
    public NetworkVariable<int> Integrity = new NetworkVariable<int>(
        100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Supplies as integer
    public NetworkVariable<int> Supplies = new NetworkVariable<int>(
        10, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // 0–100 survival chance
    public NetworkVariable<int> SurvivalChance = new NetworkVariable<int>(
        100, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool _registeredWithGameManager = false;

    public override void OnNetworkSpawn()
    {
        if (IsServer && !_registeredWithGameManager)
        {
            // Register with GameManager once it exists (handles host & clients)
            StartCoroutine(RegisterWithGameManagerWhenReady());
        }

        if (IsOwner)
        {
            // Subscribe to stat changes to update local HUD
            Integrity.OnValueChanged += OnStatsChanged;
            Supplies.OnValueChanged += OnStatsChanged;
            SurvivalChance.OnValueChanged += OnStatsChanged;

            // Initial HUD update
            OnStatsChanged(Integrity.Value, Integrity.Value);
        }
    }

    private IEnumerator RegisterWithGameManagerWhenReady()
    {
        // Wait until GameManager.Instance is available on the server
        while (GameManager.Instance == null)
        {
            yield return null;
        }

        if (!_registeredWithGameManager)
        {
            GameManager.Instance.RegisterPlayer(this);
            _registeredWithGameManager = true;
            Debug.Log($"[PlayerState] Registered with GameManager on server for client {OwnerClientId}");
        }
    }

    private void OnDestroy()
    {
        if (IsOwner)
        {
            Integrity.OnValueChanged -= OnStatsChanged;
            Supplies.OnValueChanged -= OnStatsChanged;
            SurvivalChance.OnValueChanged -= OnStatsChanged;
        }
    }

    private void OnStatsChanged(int oldValue, int newValue)
    {
        if (!IsOwner) return;

        int sc = SurvivalChance.Value;
        string survivalText = "UNKNOWN";

        if (sc >= 80) survivalText = "HIGH";
        else if (sc >= 50) survivalText = "MEDIUM";
        else if (sc >= 20) survivalText = "LOW";
        else if (sc > 0) survivalText = "CRITICAL";
        else survivalText = "NONE";

        GameUIController.Instance?.UpdateLocalStats(
            Integrity.Value,
            Supplies.Value,
            survivalText
        );
    }
}