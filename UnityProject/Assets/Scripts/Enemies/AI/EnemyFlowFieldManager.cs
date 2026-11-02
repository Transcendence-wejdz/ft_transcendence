using System.Collections.Generic;
using UnityEngine;

// Server hub: living players, one flow field per chased player, and the ring places of Encircle enemies.
public partial class EnemyFlowFieldManager : MonoBehaviour
{
    private const float PlayerRefreshInterval = 0.25f;
    private const float RebuildInterval = 0.5f;
    private const float ForcedRebuildInterval = 2f;
    private const float UnusedFieldLifetime = 2f;
    private const float SlotAssignInterval = 0.3f;

    private static EnemyFlowFieldManager instance;

    private readonly List<Player> livingPlayers = new List<Player>();
    private readonly List<Player> scratchPlayers = new List<Player>();
    private readonly Dictionary<Player, EnemyFlowField> fields = new Dictionary<Player, EnemyFlowField>();
    private readonly List<Player> staleKeys = new List<Player>();
    private readonly HashSet<Enemy> encirclers = new HashSet<Enemy>();
    private readonly Dictionary<Player, List<Enemy>> ringsByTarget = new Dictionary<Player, List<Enemy>>();
    private readonly List<Enemy> ringScratch = new List<Enemy>();
    private readonly Dictionary<Player, float> ringBaseAngles = new Dictionary<Player, float>();
    private readonly Dictionary<Player, int> ringSizes = new Dictionary<Player, int>();
    private float nextPlayerRefreshTime;
    private float nextSlotAssignTime;

    public int PlayersVersion { get; private set; }
    public IReadOnlyList<Player> LivingPlayers => livingPlayers;

    public static Vector2 BodyOffset { get; set; }
    public static float BodyRadius { get; set; } = 0.35f;

    public static EnemyFlowFieldManager Instance
    {
        get
        {
            if (instance == null)
                instance = new GameObject("EnemyFlowFieldManager").AddComponent<EnemyFlowFieldManager>();
            return instance;
        }
    }

    void OnEnable()
    {
        Player.OnPlayerDied += HandlePlayerDied;
    }

    void OnDisable()
    {
        Player.OnPlayerDied -= HandlePlayerDied;
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void FixedUpdate()
    {
        if (Time.time >= nextPlayerRefreshTime)
            RefreshPlayers();

        UpdateFields();
        if (Time.time >= nextSlotAssignTime)
            AssignRingSlots();
    }

    public Player FindNearestLivingPlayer(Vector2 position)
    {
        Player nearest = null;
        float nearestDistSq = float.MaxValue;
        float distSq;

        foreach (Player player in livingPlayers)
        {
            distSq = ((Vector2)player.transform.position - position).sqrMagnitude;
            if (distSq < nearestDistSq)
            {
                nearestDistSq = distSq;
                nearest = player;
            }
        }
        return nearest;
    }

    // Samples the target's field; false if not built yet or out of range.
    public bool TryGetDirection(Player target, Vector2 position, out Vector2 direction)
    {
        EnemyFlowField field;

        direction = Vector2.zero;
        if (!fields.TryGetValue(target, out field))
        {
            field = new EnemyFlowField();
            fields.Add(target, field);
        }
        field.LastRequestedAt = Time.time;
        return field.IsBuilt && field.TryGetDirection(position, out direction);
    }

    private void HandlePlayerDied(Player dead)
    {
        RefreshPlayers();
    }

    // Bumps PlayersVersion when the living set changes; public so spawn placement can force a refresh early.
    public void RefreshPlayers()
    {
        nextPlayerRefreshTime = Time.time + PlayerRefreshInterval;
        scratchPlayers.Clear();
        foreach (Player player in Player.ActivePlayers)
        {
            if (!player.IsDead)
                scratchPlayers.Add(player);
        }

        if (SameContents(scratchPlayers, livingPlayers))
            return;
        livingPlayers.Clear();
        livingPlayers.AddRange(scratchPlayers);
        PlayersVersion++;
    }

    private static bool SameContents(List<Player> a, List<Player> b)
    {
        if (a.Count != b.Count)
            return false;
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i] != b[i])
                return false;
        }
        return true;
    }

    private bool IsFieldStale(Player player, EnemyFlowField field, float now)
    {
        if (player == null || player.IsDead)
            return true;
        return now - field.LastRequestedAt > UnusedFieldLifetime;
    }

    // Due when never built, when the player moved to another cell, or anyway past the forced interval.
    private bool IsFieldDue(Player player, EnemyFlowField field, float age)
    {
        Vector2Int playerCell;

        if (!field.IsBuilt || age >= ForcedRebuildInterval)
            return true;
        if (age < RebuildInterval)
            return false;
        playerCell = EnemyFlowField.ToCell((Vector2)player.transform.position + BodyOffset);
        return playerCell != field.GoalCell;
    }

    // Drops stale fields, then rebuilds the most out-of-date within the budget.
    private void UpdateFields()
    {
        float now = Time.time;
        Player mostDue = null;
        float mostDueAge = 0f;
        float age;

        staleKeys.Clear();
        foreach (KeyValuePair<Player, EnemyFlowField> pair in fields)
        {
            if (IsFieldStale(pair.Key, pair.Value, now))
            {
                staleKeys.Add(pair.Key);
                continue;
            }
            age = now - pair.Value.BuiltAt;
            if (!IsFieldDue(pair.Key, pair.Value, age) || (mostDue != null && age <= mostDueAge))
                continue;
            mostDue = pair.Key;
            mostDueAge = float.MaxValue;
            if (pair.Value.IsBuilt)
                mostDueAge = age;
        }
        foreach (Player key in staleKeys)
            fields.Remove(key);
        if (mostDue != null)
            fields[mostDue].Rebuild((Vector2)mostDue.transform.position + BodyOffset);
    }
}
