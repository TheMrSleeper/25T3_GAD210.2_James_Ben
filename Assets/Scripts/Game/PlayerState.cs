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

    public static PlayerState LocalInstance { get; private set; }

    public override void OnNetworkSpawn()
    {
        if (IsServer && !_registeredWithGameManager)
        {
            StartCoroutine(RegisterWithGameManagerWhenReady());
        }

        if (IsOwner)
        {
            LocalInstance = this;

            // Initial HUD update (uses current values, but only once)
            RefreshLocalHud();
        }
    }

    private IEnumerator RegisterWithGameManagerWhenReady()
    {
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
        if (IsOwner && LocalInstance == this)
        {
            LocalInstance = null;
        }
    }

    public void RefreshLocalHud()
    {
        if (!IsOwner || GameUIController.Instance == null) return;

        int integrity = Integrity.Value;
        int supplies = Supplies.Value;
        int sc = SurvivalChance.Value;

        string survivalText;
        if (sc >= 80) survivalText = "HIGH";
        else if (sc >= 50) survivalText = "MEDIUM";
        else if (sc >= 20) survivalText = "LOW";
        else if (sc > 0) survivalText = "CRITICAL";
        else survivalText = "NONE";

        GameUIController.Instance.UpdateLocalStats(
            integrity,
            supplies,
            survivalText
        );
    }
}