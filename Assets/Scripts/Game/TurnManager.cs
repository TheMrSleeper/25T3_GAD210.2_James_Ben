using UnityEngine;
using Unity.Netcode;

public class TurnManager : NetworkBehaviour
{
    // 0 = host, 1 = first client
    private NetworkVariable<int> currentTurn = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public static TurnManager Instance { get; private set; }

    private void Awake()
    {
        // Basic singleton guard (per scene)
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
            currentTurn.Value = 0; // host (player 1) starts
        }
    }

    public int GetCurrentTurn()
    {
        return currentTurn.Value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void EndTurnServerRpc()
    {
        // toggle between 0 and 1 for two-player test
        currentTurn.Value = (currentTurn.Value == 0) ? 1 : 0;
    }
}