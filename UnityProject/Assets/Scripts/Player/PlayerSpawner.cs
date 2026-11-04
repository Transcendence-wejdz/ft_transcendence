using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Spawn altar: places players on glyphs by slot, plays arrivals and colors each glyph after its owner.
[DefaultExecutionOrder(-100)]
public class PlayerSpawner : MonoBehaviour
{
    private const float GlyphTintInterval = 0.25f;
    private const float SpawnFxWhiteBlend = 0.5f;

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] glyphs;
    [SerializeField] private GameObject spawnFxPrefab;

    private GlyphGlow[] glyphGlows;
    private bool[] glyphHeld;
    private float nextGlyphTint;

    // The glyph of this slot, wrapping past the glyph count.
    private Transform GlyphFor(ulong slot)
    {
        if (glyphs == null || glyphs.Length == 0)
            return null;
        return glyphs[(int)(slot % (ulong)glyphs.Length)];
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

    // A leftover scene copy of the offline player isn't anyone's arrival.
    private static bool IsRealPlayer(Player player)
    {
        return !player.IsSpawned || player.NetworkObject.IsPlayerObject;
    }

    private static Color PlayerColor(Player player)
    {
        return CharacterCatalog.Instance.GetFxLightColor(player.ColorIndex);
    }

    public static void TintSpawnFx(GameObject fx, Color color)
    {
        ParticleSystem.MainModule main;

        foreach (ParticleSystem system in fx.GetComponentsInChildren<ParticleSystem>())
        {
            main = system.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, SpawnFxWhiteBlend));
        }
        foreach (Light2D fxLight in fx.GetComponentsInChildren<Light2D>())
            fxLight.color = color;
    }

    private void PlaySpawnFx(Transform glyph, Color color)
    {
        if (glyph != null && spawnFxPrefab != null)
            TintSpawnFx(Instantiate(spawnFxPrefab, glyph.position, Quaternion.identity), color);
    }

    // Polled, so joins, leaves and color changes all show up without per-player subscriptions.
    private void UpdateGlyphTints()
    {
        int slot;

        if (glyphGlows.Length == 0)
            return;
        System.Array.Clear(glyphHeld, 0, glyphHeld.Length);
        foreach (Player player in Player.ActivePlayers)
        {
            if (!IsRealPlayer(player))
                continue;
            slot = (int)(player.OwnerClientId % (ulong)glyphGlows.Length);
            glyphHeld[slot] = true;
            glyphGlows[slot].SetTint(PlayerColor(player));
        }
        for (int i = 0; i < glyphGlows.Length; i++)
        {
            if (!glyphHeld[i])
                glyphGlows[i].ClearTint();
        }
    }

    // Waits a frame so the new player's Start has loaded the saved color.
    private IEnumerator PlayOfflineArrival()
    {
        Player player;

        yield return null;
        player = LocalPlayer.GetPlayer();
        if (player != null)
            PlaySpawnFx(GlyphFor(0), PlayerColor(player));
    }

    // Fires on every peer once all clients loaded; one FX per occupied glyph.
    private void HandleLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        HashSet<Transform> played = new HashSet<Transform>();
        Transform glyph;

        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
        foreach (Player player in Player.ActivePlayers)
        {
            if (!IsRealPlayer(player))
                continue;
            player.BeginSpawnFade();
            glyph = GlyphFor(player.OwnerClientId);
            if (played.Add(glyph))
                PlaySpawnFx(glyph, PlayerColor(player));
        }
    }

    // Player position that puts its feet on the glyph for this slot.
    public Vector3 GetSpawnPosition(ulong slot)
    {
        Transform glyph = GlyphFor(slot);
        Vector3 target = transform.position;

        if (glyph != null)
            target = glyph.position;
        return target - (Vector3)FeetOffset();
    }

    // Spawn position for a slot in the loaded scene; false when the scene has no spawner.
    public static bool TryGetSpawnPosition(ulong slot, out Vector3 position)
    {
        PlayerSpawner spawner = FindAnyObjectByType<PlayerSpawner>();

        position = Vector3.zero;
        if (spawner == null)
            return false;
        position = spawner.GetSpawnPosition(slot);
        return true;
    }

    // The color comes with the call, since the replicated one may still be on its way.
    public static void PlayJoinArrival(Player player, int colorIndex)
    {
        PlayerSpawner spawner = FindAnyObjectByType<PlayerSpawner>();

        player.BeginSpawnFade();
        if (spawner != null)
            spawner.PlaySpawnFx(spawner.GlyphFor(player.OwnerClientId), CharacterCatalog.Instance.GetFxLightColor(colorIndex));
    }

    // Subscribed in Awake because a solo host's scene load can complete before Start.
    private void Awake()
    {
        NetworkManager nm = NetworkManager.Singleton;

        glyphGlows = new GlyphGlow[glyphs.Length];
        glyphHeld = new bool[glyphs.Length];
        for (int i = 0; i < glyphs.Length; i++)
            glyphGlows[i] = glyphs[i].GetComponent<GlyphGlow>();
        if (nm != null && nm.IsListening && nm.SceneManager != null)
            nm.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
    }

    private void Start()
    {
        NetworkManager nm = NetworkManager.Singleton;

        if (nm != null && nm.IsListening)
            return;
        StartCoroutine(PlayOfflineArrival());
        if (GameObject.FindWithTag("Player") != null)
            return;
        Instantiate(playerPrefab, GetSpawnPosition(0), transform.rotation).GetComponent<Player>().BeginSpawnFade();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextGlyphTint)
            return;
        nextGlyphTint = Time.unscaledTime + GlyphTintInterval;
        UpdateGlyphTints();
    }

    private void OnDestroy()
    {
        NetworkManager nm = NetworkManager.Singleton;

        if (nm != null && nm.SceneManager != null)
            nm.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
    }
}
