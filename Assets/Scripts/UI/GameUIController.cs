using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI turnStatusText;
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button quitButton;

    private ulong localClientId;

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("No NetworkManager in Game scene.");
            return;
        }

        localClientId = NetworkManager.Singleton.LocalClientId;

        if (endTurnButton != null)
            endTurnButton.onClick.AddListener(OnEndTurnClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void Update()
    {
        if (TurnManager.Instance == null || NetworkManager.Singleton == null)
            return;

        int currentTurn = TurnManager.Instance.GetCurrentTurn();

        // For now: host is "player index 0", first client is "player index 1"
        int myIndex = NetworkManager.Singleton.IsServer ? 0 : 1;

        bool myTurn = (currentTurn == myIndex);

        if (turnStatusText != null)
        {
            turnStatusText.text = myTurn
                ? $"YOUR TURN (Player {myIndex + 1})"
                : $"OTHER PLAYER'S TURN (Player {(currentTurn + 1)})";
        }

        if (messageText != null)
        {
            if (myTurn)
            {
                messageText.text = "You may end your turn.";
            }
            else
            {
                messageText.text = $"Player {currentTurn + 1} is taking their turn...";
            }
        }

        if (endTurnButton != null)
        {
            endTurnButton.interactable = myTurn;
        }
    }

    private void OnEndTurnClicked()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogWarning("No NetworkManager when trying to end turn.");
            return;
        }

        if (TurnManager.Instance == null)
        {
            Debug.LogWarning("No TurnManager.Instance when trying to end turn.");
            return;
        }

        if (!TurnManager.Instance.IsSpawned)
        {
            Debug.LogWarning("TurnManager is not network-spawned yet.");
            return;
        }

        TurnManager.Instance.EndTurnServerRpc();
    }

    private void OnQuitClicked()
    {
        // Disconnect from network
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Back to main menu
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}