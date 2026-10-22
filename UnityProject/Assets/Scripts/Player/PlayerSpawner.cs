using Unity.Netcode;
using UnityEngine;

// Scene spawn altar: places each player on a glyph by slot and spawns the offline solo player.
[DefaultExecutionOrder(-100)]
public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] glyphs;

    private void Start()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening)
            return;

        if (GameObject.FindWithTag("Player") != null)
            return;

        Instantiate(playerPrefab, GetSpawnPosition(0), transform.rotation);
    }

    // Player position that puts its feet on the glyph for this slot, wrapping past the glyph count.
    public Vector3 GetSpawnPosition(ulong slot)
    {
        Transform glyph = glyphs != null && glyphs.Length > 0 ? glyphs[(int)(slot % (ulong)glyphs.Length)] : null;
        Vector3 target = glyph != null ? glyph.position : transform.position;
        return target - (Vector3)FeetOffset();
    }

    // Feet offset from the player pivot, taken from the player prefab's solid collider.
    private Vector2 FeetOffset()
    {
        if (playerPrefab == null)
            return Vector2.zero;

        foreach (Collider2D col in playerPrefab.GetComponents<Collider2D>())
        {
            if (!col.isTrigger)
                return col.offset * (Vector2)playerPrefab.transform.localScale;
        }
        return Vector2.zero;
    }

    // Spawn position for a slot in the loaded scene; false when the scene has no spawner.
    public static bool TryGetSpawnPosition(ulong slot, out Vector3 position)
    {
        PlayerSpawner spawner = FindAnyObjectByType<PlayerSpawner>();
        position = spawner != null ? spawner.GetSpawnPosition(slot) : Vector3.zero;
        return spawner != null;
    }
}
