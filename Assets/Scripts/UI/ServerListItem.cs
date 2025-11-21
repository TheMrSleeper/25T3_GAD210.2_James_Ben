//using UnityEngine;
//using TMPro;
//using UnityEngine.UI;

//public class ServerListItem : MonoBehaviour
//{
//    [SerializeField] private TextMeshProUGUI serverInfoText;
//    private LobbyUIController _lobby;
//    private string _ip;
//    private int _port;

//    public void Initialize(LobbyUIController lobby, string displayName, string ip, int port)
//    {
//        _lobby = lobby;
//        _ip = ip;
//        _port = port;

//        if (serverInfoText != null)
//        {
//            serverInfoText.text = $"{displayName} @ {_ip}:{_port}";
//        }

//        // Ensure the Button calls OnClick if not wired via inspector
//        var button = GetComponent<Button>();
//        if (button != null)
//        {
//            button.onClick.RemoveAllListeners();
//            button.onClick.AddListener(OnClick);
//        }
//    }

//    public void OnClick()
//    {
//        _lobby?.OnClickServer(_ip, _port);
//    }
//}
