using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Spawns the boss prefab on this transform once the scene is ready (after every client loaded it, when networked).
public class BossSpawner : MonoBehaviour
{
    [SerializeField] private GameObject bossPrefab;

    private bool spawned;

    void Start()
    {
        NetworkManager network = NetworkManager.Singleton;
        bool isNetworked = network != null && network.IsListening;
        if (!isNetworked)
            Spawn(false);
        else if (network.IsServer && network.SceneManager != null)
            network.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
    }

    void OnDestroy()
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network != null && network.SceneManager != null)
            network.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
    }

    private void HandleLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
        Spawn(true);
    }

    private void Spawn(bool isNetworked)
    {
        if (spawned || bossPrefab == null)
            return;

        spawned = true;
        GameObject instance = Instantiate(bossPrefab, transform.position, Quaternion.identity);
        if (isNetworked)
            instance.GetComponent<NetworkObject>().Spawn(true);
    }
}
