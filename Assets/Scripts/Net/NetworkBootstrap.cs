using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkBootstrap : MonoBehaviour
{
    public static NetworkBootstrap Instance { get; private set; }

    [Header("Transport Settings")]
    [SerializeField] private string listenAddress = "0.0.0.0";
    [SerializeField] private ushort listenPort = 7777;

    private void Awake()
    {
        // Make sure there is only one NetworkManagerRoot across scenes
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        var transport = GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.ConnectionData.Address = listenAddress;
            transport.ConnectionData.Port = listenPort;
        }
    }

    /// <summary>
    /// Configure transport to connect to a specific IP (for client).
    /// </summary>
    public void ConfigureClient(string ip, ushort port)
    {
        var transport = GetComponent<UnityTransport>();
        if (transport != null)
        {
            transport.ConnectionData.Address = ip;
            transport.ConnectionData.Port = port;
        }
    }
}