using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Net;
using System.Net.Sockets;

public class NetworkBootstrap : MonoBehaviour
{
    public static NetworkBootstrap Instance { get; private set; }

    public static string LocalIpAddress { get; private set; } = "127.0.0.1";

    [Header("Transport Settings")]
    [SerializeField] private string listenAddress = "0.0.0.0";
    [SerializeField] private ushort listenPort = 7777;

    private void Awake()
    {
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

        LocalIpAddress = GetLocalIPv4() ?? "127.0.0.1";
        Debug.Log($"[NetworkBootstrap] Local IP detected: {LocalIpAddress}");
    }

    private string GetLocalIPv4()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[NetworkBootstrap] Failed to get local IP: {e.Message}");
        }

        return null;
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