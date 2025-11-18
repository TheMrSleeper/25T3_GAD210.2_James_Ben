using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class EventOptionButton : MonoBehaviour
{
    [SerializeField] private int optionIndex = 0;

    private void Awake()
    {
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnClicked);
        }
    }

    private void OnClicked()
    {
        if (GameManager.Instance == null) return;
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost) return;

        // Ask the server to apply this option for the active player
        GameManager.Instance.SubmitEventOptionServerRpc(optionIndex);
    }
}