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
    [SerializeField] private Transform serverListContainer;
    [SerializeField] private GameObject serverListItemPrefab;

    [Header("Bottom Bar")]
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private GameObject defaultSelectedButton;

    [Header("Networking")]
    [SerializeField] private GameObject networkManagerPrefab;

    [Header("Status UI")]
    [SerializeField] private TextMeshProUGUI textConnectionStatus;

    // Simple local model for discovered servers
    //private class DiscoveredServer
    //{
    //    public string DisplayName;
    //    public string IpAddress;
    //    public int Port;
    //}

    //private readonly List<DiscoveredServer> _servers = new();

    private void Start()
    {
        EnsureNetworkManager();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayLobbyMusic();
            AudioManager.Instance.StopGameAmbience();
        }

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


    //private void AddDummyServers()
    //{
    //    OnServerDiscovered("ENGINEERING_A", "192.168.0.10", 7777);
    //    OnServerDiscovered("ENGINEERING_B", "192.168.0.11", 7777);
    //}

    //public void ClearServerList()
    //{
    //    foreach (Transform child in serverListContainer)
    //    {
    //        Destroy(child.gameObject);
    //    }
    //    _servers.Clear();
    //}

    public void SetStatus(string message)
    {
        if (textConnectionStatus != null)
        {
            textConnectionStatus.text = $"STATUS: {message}";
        }
    }

    // Deprecated; No longer using Network Discovery
    //public void OnServerDiscovered(string displayName, string ip, int port)
    //{
    //    var server = new DiscoveredServer
    //    {
    //        DisplayName = displayName,
    //        IpAddress = ip,
    //        Port = port
    //    };
    //    _servers.Add(server);

    //    // Create UI entry
    //    var itemGO = Instantiate(serverListItemPrefab, serverListContainer);
    //    var item = itemGO.GetComponent<ServerListItem>();
    //    if (item != null)
    //    {
    //        item.Initialize(this, server.DisplayName, server.IpAddress, server.Port);
    //    }
    //}

    // Called by server list items when clicked
    //public void OnClickServer(string ip, int port)
    //{
    //    Debug.Log($"[LOBBY] Selected server {ip}:{port}");
    //}

    // Button: HOST GAME
    public void OnHostClicked()
    {
        Debug.Log("[LOBBY] Host button clicked.");

        // Configure host transport
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
            SetStatus("NO IP PROVIDED");
            return;
        }

        Debug.Log($"[LOBBY] Join by IP clicked: {ip}");
        SetStatus($"CONNECTING TO {ip}:7777 ...");

        var bootstrap = FindObjectOfType<NetworkBootstrap>();
        if (bootstrap == null)
        {
            Debug.LogError("[LOBBY] No NetworkBootstrap found!");
            SetStatus("ERROR: NO NETWORK BOOTSTRAP");
            return;
        }

        // Configure client address/port
        bootstrap.ConfigureClient(ip, 7777);

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[LOBBY] No NetworkManager.Singleton present.");
            SetStatus("ERROR: NO NETWORK MANAGER");
            return;
        }

        // Try to start client
        bool started = NetworkManager.Singleton.StartClient();
        if (!started)
        {
            Debug.LogError("[LOBBY] StartClient() failed immediately.");
            SetStatus("FAILED TO START CLIENT");
            return;
        }

        // Subscribe to connection callbacks (client-side)
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsClient) return;

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.Log($"[LOBBY] Connected to host as client {clientId}.");
            SetStatus("CONNECTED. WAITING FOR HOST SCENE SYNC...");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsClient) return;

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            Debug.LogWarning("[LOBBY] Disconnected from host or failed to connect.");
            SetStatus("DISCONNECTED OR FAILED TO CONNECT");
        }
    }


    // Button: BACK
    public void OnBackClicked()
    {
        Debug.Log("[LOBBY] Back to main menu.");
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}