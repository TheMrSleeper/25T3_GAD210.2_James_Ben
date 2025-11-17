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
        // Set initial selection for keyboard / controller
        if (defaultSelectedButton != null)
        {
            EventSystem.current?.SetSelectedGameObject(defaultSelectedButton);
        }

        // For now, add some dummy test data (remove this once LAN discovery is wired)
        AddDummyServers();
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

        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("No NetworkManager.Singleton found. Ensure NetworkBootstrap is in the scene.");
            return;
        }

        var transport = networkManager.NetworkConfig.NetworkTransport as UnityTransport;
        if (transport != null)
        {
            transport.SetConnectionData("0.0.0.0", 7777);
        }

        if (!networkManager.StartHost())
        {
            Debug.LogError("Failed to start host.");
            return;
        }

        // IMPORTANT: use NGO's SceneManager, NOT UnityEngine.SceneManagement directly
        networkManager.SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }

    // Button: JOIN BY IP
    public void OnJoinByIpClicked()
    {
        var networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            Debug.LogError("No NetworkManager.Singleton found. Ensure NetworkBootstrap is in the scene.");
            return;
        }

        var ip = ipInputField != null ? ipInputField.text : "127.0.0.1";
        if (string.IsNullOrWhiteSpace(ip)) ip = "127.0.0.1";

        Debug.Log($"[LOBBY] Join by IP clicked: {ip}");

        var transport = networkManager.NetworkConfig.NetworkTransport as UnityTransport;
        if (transport != null)
        {
            transport.SetConnectionData(ip, 7777);
        }

        if (!networkManager.StartClient())
        {
            Debug.LogError("Failed to start client.");
            return;
        }

        // DO *NOT* load Game here.
        // Client will automatically follow the host's SceneManager.LoadScene("Game", ...)
    }

    // Button: BACK
    public void OnBackClicked()
    {
        Debug.Log("[LOBBY] Back to main menu.");

        // Shut down network if returning to main
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
    }
}
