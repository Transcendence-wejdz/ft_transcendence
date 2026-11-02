using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Server-side wave spawner; waves fire on a timer, but end that wait early once the current wave is fully cleared.
public partial class WaveSpawner : NetworkBehaviour
{
    [SerializeField] private EnemyKit[] enemyKits;
    [SerializeField] private int[] tierUnlockWave = { 1, 4 };
    [SerializeField] private int baseEnemiesPerWave = 3;
    [SerializeField] private int extraEnemiesPerWave = 2;
    [SerializeField] private int extraEnemiesPerPlayer = 2;
    [SerializeField] private float spawnRadius = 12f;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private float clearCheckInterval = 0.5f;
    [SerializeField] private int maxSpawnPointAttempts = 30;
    [SerializeField] private float spawnClearance = 1f;

    [SerializeField] private bool spawnWaves = true;
    [Tooltip("Off for scenes that continue a run (the boss scene), so reaching them doesn't open a new gold run.")]
    [SerializeField] private bool startsRun = true;

    private readonly NetworkVariable<int> currentWave = new OfflineSafeNetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private readonly NetworkVariable<double> startServerTime = new OfflineSafeNetworkVariable<double>(
        0d, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int waveNumber;
    private float offlineStartTime;
    private bool wavesStarted;
    private ProceduralMapGenerator cachedMapGenerator;
    private readonly List<int> unlockedKitIndices = new List<int>();

    public int CurrentWave
    {
        get
        {
            if (IsSpawned)
                return currentWave.Value;
            return waveNumber;
        }
    }

    // Fired server/offline side each time a new wave spawns, never for a wave carried over from the previous scene.
    public static event System.Action<int> OnWaveStarted;

    // Seconds since the first wave began, on the shared server clock when networked.
    public float ElapsedSeconds
    {
        get
        {
            if (CurrentWave <= 0)
                return 0f;
            if (IsSpawned)
                return (float)(NetworkManager.ServerTime.Time - startServerTime.Value);
            return Time.time - offlineStartTime;
        }
    }

    private static bool IsNetworked()
    {
        return NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
    }

    private void HandleSceneLoadEventCompleted(string sceneName, UnityEngine.SceneManagement.LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        BeginWaves();
    }

    private void BeginWaves()
    {
        if (wavesStarted)
            return;
        wavesStarted = true;
        StartCoroutine(RunWaves());
    }

    private void SetWave(int wave)
    {
        waveNumber = wave;
        if (IsSpawned)
            currentWave.Value = waveNumber;
    }

    private int EnemiesForWave()
    {
        int playerCount = Mathf.Max(1, Player.AllActiveInstances.Count);

        return baseEnemiesPerWave + (waveNumber - 1) * extraEnemiesPerWave + (playerCount - 1) * extraEnemiesPerPlayer;
    }

    // A scene without waves (the boss scene) only shows the carried-over wave, or wave 1.
    private void HoldCarriedWave(bool carried)
    {
        if (carried)
            SetWave(Mathf.Max(1, RunCarryOver.Wave));
        else
            SetWave(1);
    }

    private void StartNextWave()
    {
        SetWave(waveNumber + 1);
        OnWaveStarted?.Invoke(waveNumber);
        if (waveNumber == 1)
            NotifyRunStarted();
        ReportCheckpoint(waveNumber);
        SpawnWave(EnemiesForWave());
    }

    // The map's water is only known once its seed is, so the first wave waits for it instead of landing in lakes.
    private IEnumerator RunWaves()
    {
        ProceduralMapGenerator mapGenerator = FindAnyObjectByType<ProceduralMapGenerator>();
        bool carried;

        while (mapGenerator != null && !mapGenerator.IsReady)
            yield return null;
        carried = RunCarryOver.IsForActiveScene;
        if (carried)
            MarkStartTime(RunCarryOver.ElapsedSeconds);
        else
            MarkStartTime(0f);
        if (!spawnWaves)
        {
            HoldCarriedWave(carried);
            yield break;
        }
        if (carried)
            waveNumber = RunCarryOver.Wave;
        while (!GameOverCheck.IsRunOver)
        {
            StartNextWave();
            yield return WaitForNextWave();
        }
    }

    private bool IsWaveOver()
    {
        if (GameOverCheck.IsRunOver)
            return true;
        return FindObjectsByType<Enemy>(FindObjectsInactive.Exclude).Length == 0;
    }

    // Waits out timeBetweenWaves, but ends early the moment every enemy from the current wave is dead.
    private IEnumerator WaitForNextWave()
    {
        WaitForSeconds checkDelay = new WaitForSeconds(clearCheckInterval);
        float elapsed = 0f;

        while (elapsed < timeBetweenWaves)
        {
            if (IsWaveOver())
                yield break;
            yield return checkDelay;
            elapsed += clearCheckInterval;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            currentWave.OnValueChanged += HandleWaveChanged;
        if (!IsServer && currentWave.Value > 0)
            HandleRejoinedRun();
        if (IsServer && NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted += HandleSceneLoadEventCompleted;
    }

    public override void OnNetworkDespawn()
    {
        currentWave.OnValueChanged -= HandleWaveChanged;
        if (NetworkManager != null && NetworkManager.SceneManager != null)
            NetworkManager.SceneManager.OnLoadEventCompleted -= HandleSceneLoadEventCompleted;
    }

    void Start()
    {
        if (!IsNetworked())
            BeginWaves();
    }
}
