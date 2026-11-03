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
    private readonly List<Player> currentlyLiving = new List<Player>();
    private readonly Dictionary<(Player player, bool large), EnemyFlowField> fields = new Dictionary<(Player player, bool large), EnemyFlowField>();
    private readonly List<(Player player, bool large)> keysToRemove = new List<(Player player, bool large)>();
    private readonly HashSet<Enemy> encirclers = new HashSet<Enemy>();
    private readonly Dictionary<Player, List<Enemy>> ringsByTarget = new Dictionary<Player, List<Enemy>>();
    private readonly List<Enemy> ringSortedByAngle = new List<Enemy>();
    private readonly Dictionary<Player, float> ringBaseAngles = new Dictionary<Player, float>();
    private readonly Dictionary<Player, int> ringSizes = new Dictionary<Player, int>();
    private float nextPlayerRefreshTime;
    private float nextSlotAssignTime;

    public int PlayersVersion { get; private set; }
    public IReadOnlyList<Player> LivingPlayers => livingPlayers;

    public static Vector2 BodyOffset { get; set; }

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
            RefreshLivingPlayers();

        RemoveUnusedFieldsAndRebuildOne();
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
    public bool TryGetPathDirection(Player target, Vector2 position, float bodyRadius, out Vector2 direction)
    {
        bool large = EnemyFlowField.IsLargeBody(bodyRadius);
        EnemyFlowField field;

        direction = Vector2.zero;
        if (!fields.TryGetValue((target, large), out field))
        {
            field = new EnemyFlowField(large);
            fields.Add((target, large), field);
        }
        field.LastRequestedAt = Time.time;
        return field.IsBuilt && field.TryGetPathDirection(position, out direction);
    }

    private void HandlePlayerDied(Player dead)
    {
        RefreshLivingPlayers();
    }

    // Bumps PlayersVersion when the living set changes; public so spawn placement can force a refresh early.
    public void RefreshLivingPlayers()
    {
        nextPlayerRefreshTime = Time.time + PlayerRefreshInterval;
        currentlyLiving.Clear();
        foreach (Player player in Player.ActivePlayers)
        {
            if (!player.IsDead)
                currentlyLiving.Add(player);
        }

        if (SameContents(currentlyLiving, livingPlayers))
            return;
        livingPlayers.Clear();
        livingPlayers.AddRange(currentlyLiving);
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

    private bool IsFieldUnused(Player player, EnemyFlowField field, float now)
    {
        if (player == null || player.IsDead)
            return true;
        return now - field.LastRequestedAt > UnusedFieldLifetime;
    }

    // True when never built, when the player moved to another cell, or anyway past the forced interval.
    private bool NeedsRebuild(Player player, EnemyFlowField field, float age)
    {
        Vector2Int playerCell;

        if (!field.IsBuilt || age >= ForcedRebuildInterval)
            return true;
        if (age < RebuildInterval)
            return false;
        playerCell = EnemyFlowField.WorldToCell((Vector2)player.transform.position + BodyOffset);
        return playerCell != field.GoalCell;
    }

    // Drops fields no enemy reads anymore, then rebuilds at most one per tick, the most out of date.
    private void RemoveUnusedFieldsAndRebuildOne()
    {
        float now = Time.time;
        (Player player, bool large) fieldToRebuild = (null, false);
        float fieldToRebuildAge = 0f;
        float age;

        keysToRemove.Clear();
        foreach (KeyValuePair<(Player player, bool large), EnemyFlowField> pair in fields)
        {
            if (IsFieldUnused(pair.Key.player, pair.Value, now))
            {
                keysToRemove.Add(pair.Key);
                continue;
            }
            age = now - pair.Value.BuiltAt;
            if (!NeedsRebuild(pair.Key.player, pair.Value, age) || (fieldToRebuild.player != null && age <= fieldToRebuildAge))
                continue;
            fieldToRebuild = pair.Key;
            fieldToRebuildAge = float.MaxValue;
            if (pair.Value.IsBuilt)
                fieldToRebuildAge = age;
        }
        foreach ((Player player, bool large) key in keysToRemove)
            fields.Remove(key);
        if (fieldToRebuild.player != null)
            fields[fieldToRebuild].Rebuild((Vector2)fieldToRebuild.player.transform.position + BodyOffset);
    }
}
