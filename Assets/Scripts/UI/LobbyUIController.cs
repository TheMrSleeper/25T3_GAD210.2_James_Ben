using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class LobbyUIController : MonoBehaviour
{
    [Header("Server List")]
    [SerializeField] private Transform serverListContainer;   // Content_ServerList
    [SerializeField] private GameObject serverListItemPrefab; // Prefab/ServerListItem

    [Header("Bottom Bar")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private GameObject defaultSelectedButton; // e.g., Btn_Host

    [Header("Networking")]
    [SerializeField] private GameObject networkManagerPrefab;

    // Simple local model for discovered servers
    private class DiscoveredServer
    {
        public string DisplayName;
        public string IpAddress;
        public int Port;
    }

    private readonly List<DiscoveredServer> _servers = new();

    private void Start()
    {
        EnsureNetworkManager();

        if (defaultSelectedButton != null)
        {
            EventSystem.current?.SetSelectedGameObject(defaultSelectedButton);
        }

        // For testing server list UI only:
        //AddDummyServers();
    }

    private void EnsureNetworkManager()
    {
        if (NetworkManager.Singleton != null)
            return;

        if (networkManagerPrefab == null)
        {
            Debug.LogError("[LOBBY] NetworkManager prefab is not assigned!");
            return;
        }

        var nm = Instantiate(networkManagerPrefab);
        nm.name = "NetworkManagerRoot";
    }


    private void AddDummyServers()
    {
        OnServerDiscovered("ENGINEERING_A", "192.168.0.10", 7777);
        OnServerDiscovered("ENGINEERING_B", "192.168.0.11", 7777);
    }

    public void ClearServerList()
    {
        foreach (Transform child in serverListContainer)
        {
            Destroy(child.gameObject);
        }
        _servers.Clear();
    }

    // This will be called later by your Network Discovery script
    public void OnServerDiscovered(string displayName, string ip, int port)
    {
        var server = new DiscoveredServer
        {
            DisplayName = displayName,
            IpAddress = ip,
            Port = port
        };
        _servers.Add(server);

        // Create UI entry
        var itemGO = Instantiate(serverListItemPrefab, serverListContainer);
        var item = itemGO.GetComponent<ServerListItem>();
        if (item != null)
        {
            item.Initialize(this, server.DisplayName, server.IpAddress, server.Port);
        }
    }

    // Called by server list items when clicked
    public void OnClickServer(string ip, int port)
    {
        Debug.Log($"[LOBBY] Selected server {ip}:{port}");
        // Later: connect as client to this address:port using Netcode for GameObjects
        // e.g., Set IP on transport, StartClient, then load Game scene
    }

    // Button: HOST GAME
    public void OnHostClicked()
    {
        Debug.Log("[LOBBY] Host button clicked.");

        // Configure host transport; for LAN this can be 0.0.0.0 so others can connect.
        var bootstrap = FindObjectOfType<NetworkBootstrap>();
        if (bootstrap == null)
        {
            Debug.LogError("[LOBBY] No NetworkBootstrap found!");
            return;
        }

        // Start host
        if (!NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartHost();
        }

        // Sync-load the Game scene so clients follow
        var sceneName = "Game";
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }


    // Button: JOIN BY IP
    public void OnJoinByIpClicked()
    {
        var ip = ipInputField != null ? ipInputField.text : "";
        if (string.IsNullOrWhiteSpace(ip))
        {
            Debug.LogWarning("[LOBBY] No IP provided.");
            return;
        }

        Debug.Log($"[LOBBY] Join by IP clicked: {ip}");

        var bootstrap = FindObjectOfType<NetworkBootstrap>();
        if (bootstrap == null)
        {
            Debug.LogError("[LOBBY] No NetworkBootstrap found!");
            return;
        }

        // For LAN, we assume same port as host
        bootstrap.ConfigureClient(ip, 7777);

        if (!NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.StartClient();
        }
        else if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.StartClient();
        }

        // Client will auto-follow when host switches scenes, so we do NOT manually load Game here.
        // The host's NetworkSceneManager.LoadScene call will propagate to all clients.
    }


    // Button: BACK
    public void OnBackClicked()
    {
        Debug.Log("[LOBBY] Back to main menu.");
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}