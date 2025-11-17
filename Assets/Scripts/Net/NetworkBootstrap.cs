using UnityEngine;
using Unity.Netcode;

public class NetworkBootstrap : MonoBehaviour
{
    [SerializeField] private GameObject networkManagerPrefab;

    private void Awake()
    {
        if (NetworkManager.Singleton == null)
        {
            // No NetworkManager exists yet, so create one
            var nm = Instantiate(networkManagerPrefab);
            nm.name = "NetworkManager";
            DontDestroyOnLoad(nm);
        }
    }
}
