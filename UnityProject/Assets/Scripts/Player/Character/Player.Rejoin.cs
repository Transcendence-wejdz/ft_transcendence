using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Run state a player gets back after a drop: the host keeps it by services id, since a rejoin gets a new client id and player object.
public partial class Player
{
    private const int RejoinRestoreDelayFrames = 2;
    private const string StaleBodyReason = "Rejoined from another connection";
    private const float RideResumeTimeout = 5f;
    private const float SpawnTeleportBlockSeconds = 5f;

    private struct RejoinSnapshot
    {
        public LevelUpBonusStacks stacks;
        public float health;
        public Vector3 position;
        public ulong ridingPigId;
        public ulong riderId;
        public float rideFraction;
    }

    private float spawnTeleportBlockedUntil;

    private static readonly Dictionary<string, RejoinSnapshot> RejoinSnapshots = new Dictionary<string, RejoinSnapshot>();

    public static event System.Action<string> OnServicesIdClaimed;

    // Server: set for a kick or a voluntary leave, so the run neither waits for this player nor keeps their state.
    public bool LeftOnPurpose { get; private set; }

    public void MarkLeftOnPurpose()
    {
        LeftOnPurpose = true;
    }

    // Owner, just before leaving; a real drop never gets to send this.
    public void AnnounceLeaving()
    {
        if (IsOwner && IsSpawned)
            AnnounceLeavingServerRpc();
    }

    [ServerRpc]
    private void AnnounceLeavingServerRpc()
    {
        MarkLeftOnPurpose();
    }

    // Server, when a fresh run starts: nothing from an older run can be handed back.
    public static void ClearRejoinSnapshots()
    {
        RejoinSnapshots.Clear();
    }

    // Lobby stats don't belong to any run, so only a player leaving a run is remembered.
    private void SaveRejoinSnapshot()
    {
        if (!IsServer || IsOwner || LeftOnPurpose || ActiveSession.InLobby || string.IsNullOrEmpty(ServicesId))
            return;
        RejoinSnapshots[ServicesId] = CaptureRide(new RejoinSnapshot { stacks = levelUpBonusStacks.Value, health = currentHealth.Value, position = transform.position });
    }

    // A revive in progress survives the drop of either side: the rider or the pig.
    private RejoinSnapshot CaptureRide(RejoinSnapshot snapshot)
    {
        Player pig = ParentPlayer();

        if (pig != null)
        {
            snapshot.ridingPigId = pig.NetworkObjectId;
            snapshot.rideFraction = pig.ReviveFraction;
        }
        foreach (Player candidate in AllActiveInstances)
        {
            if (candidate != this && candidate.transform.parent == transform)
            {
                snapshot.riderId = candidate.NetworkObjectId;
                snapshot.rideFraction = ReviveFraction;
            }
        }
        return snapshot;
    }

    private Player FindSpawnedPlayer(ulong networkObjectId)
    {
        NetworkObject found;

        if (networkObjectId == 0 || !NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out found))
            return null;
        return found.GetComponent<Player>();
    }

    // Server: puts the rider back on the pig where the ride stopped, whichever of the two has just come back.
    private void ResumeRide(Player rider, Player pig, float fraction)
    {
        if (rider == null || pig == null)
            return;
        pig.ResumeRideProgressClientRpc(fraction);
        rider.ResumeMountClientRpc(pig.NetworkObject, fraction, rider.OwnerOnlyParams());
    }

    // Every observer times the revive bar locally, so each one is told how far the ride already went.
    [ClientRpc]
    private void ResumeRideProgressClientRpc(float fraction)
    {
        float offset = Mathf.Max(0f, fraction) * mountDurationSeconds;

        if (riderSince >= 0f)
            riderSince = Time.time - offset;
        else
            pendingRideOffset = offset;
    }

    [ClientRpc]
    private void ResumeMountClientRpc(NetworkObjectReference pigRef, float fraction, ClientRpcParams rpcParams = default)
    {
        NetworkObject pigObject;

        if (pigRef.TryGet(out pigObject))
            StartCoroutine(ResumeMountWhenReady(pigObject.GetComponent<Player>(), fraction));
    }

    private static bool IsHealthKnown(Player player)
    {
        return player != null && !player.IsHealthPending;
    }

    // Both sides may have just spawned at 0 health, so it waits for their real state: a living rider and a pig really dead.
    private IEnumerator ResumeMountWhenReady(Player pig, float fraction)
    {
        float deadline = Time.unscaledTime + RideResumeTimeout;

        while (Time.unscaledTime < deadline)
        {
            if (pig == null)
                yield break;
            if (IsHealthKnown(this) && IsHealthKnown(pig) && !IsDead && pig.IsDead)
                break;
            yield return null;
        }
        if (pig == null || IsDead || !pig.IsDead)
            yield break;
        StartMounting(pig);
        if (isMounted)
            mountElapsed = Mathf.Max(0f, fraction) * mountDurationSeconds;
    }

    // Server: the host only notices a drop after a timeout, so the player can be back while their old body is still here.
    private void ReleaseStaleBody()
    {
        ulong staleClient = OwnerClientId;

        SaveRejoinSnapshot();
        servicesId.Value = default;
        NetworkManager.DisconnectClient(staleClient, StaleBodyReason);
    }

    // Server, once the player has proven who they are; each snapshot is handed back only once.
    private void RestoreRejoinSnapshot(string id)
    {
        RejoinSnapshot snapshot;

        if (ActiveSession.InLobby || !RejoinSnapshots.TryGetValue(id, out snapshot))
            return;
        RejoinSnapshots.Remove(id);
        RestoreRunStateClientRpc(snapshot.stacks, snapshot.health, snapshot.position, snapshot.ridingPigId == 0, OwnerOnlyParams());
        ResumeRide(this, FindSpawnedPlayer(snapshot.ridingPigId), snapshot.rideFraction);
        ResumeRide(FindSpawnedPlayer(snapshot.riderId), this, snapshot.rideFraction);
    }

    [ClientRpc]
    private void RestoreRunStateClientRpc(LevelUpBonusStacks stacks, float health, Vector3 position, bool restorePosition, ClientRpcParams rpcParams = default)
    {
        spawnTeleportBlockedUntil = Time.unscaledTime + SpawnTeleportBlockSeconds;
        StartCoroutine(ApplyRestoredRunState(stacks, health, position, restorePosition));
    }

    // Waits out InitializeHealthNextFrame, which would otherwise heal the player back to full; a rider's place comes from the pig, not the old position.
    private IEnumerator ApplyRestoredRunState(LevelUpBonusStacks stacks, float health, Vector3 position, bool restorePosition)
    {
        for (int i = 0; i < RejoinRestoreDelayFrames; i++)
            yield return null;
        levelUpBonusStacks.Value = stacks;
        currentHealth.Value = Mathf.Min(health, MaxHealth);
        if (!restorePosition)
            yield break;
        rb.position = position;
        rb.linearVelocity = Vector2.zero;
        transform.position = position;
    }
}
