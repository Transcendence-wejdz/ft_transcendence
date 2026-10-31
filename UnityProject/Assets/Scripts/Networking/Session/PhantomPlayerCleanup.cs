using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.SceneManagement;

// Removes ghost offline players auto-spawned after each scene load.
public class PhantomPlayerCleanup : NetworkBehaviour
{
    private static bool IsPhantom(NetworkObject networkObject)
    {
        return networkObject != null && networkObject.IsSpawned && !networkObject.IsPlayerObject;
    }

    // Snapshot first: despawning removes the player from AllActiveInstances mid-loop.
    private System.Collections.IEnumerator CleanupPhantomsNextFrame()
    {
        List<Player> players;
        NetworkObject networkObject;

        yield return null;
        players = new List<Player>(Player.AllActiveInstances);
        foreach (Player player in players)
        {
            networkObject = player.GetComponent<NetworkObject>();
            if (!IsPhantom(networkObject))
                continue;
            networkObject.Despawn(false);
            networkObject.gameObject.SetActive(false);
        }
    }

    private void HandleSceneLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (IsServer)
            StartCoroutine(CleanupPhantomsNextFrame());
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted += HandleSceneLoadEventCompleted;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted -= HandleSceneLoadEventCompleted;
    }
}
