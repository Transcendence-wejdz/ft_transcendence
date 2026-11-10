using System.Collections;
using Unity.Netcode;
using UnityEngine;

// Arrival on the spawner: a joining player stays hidden until their arrival plays, then fades in.
public partial class Player
{
    private const float SpawnFadeSeconds = 0.8f;
    private const float ArrivalHoldTimeout = 5f;

    private Coroutine spawnFade;
    private Coroutine arrivalHold;
    private bool arrivalHealthPending;

    public bool IsArriving => spawnFade != null || arrivalHold != null;

    // Just spawned here with no health written yet, which isn't a death.
    public bool IsArrivalHealthPending => arrivalHealthPending;

    private void SetSpriteAlpha(float alpha)
    {
        Color color = spriteRenderer.color;

        color.a = alpha;
        spriteRenderer.color = color;
    }

    private IEnumerator SpawnFadeRoutine()
    {
        for (float t = 0f; t < SpawnFadeSeconds; t += Time.deltaTime)
        {
            SetSpriteAlpha(t / SpawnFadeSeconds);
            yield return null;
        }
        SetSpriteAlpha(1f);
        spawnFade = null;
    }

    // Safety net if the arrival never comes: the player still shows up, without the FX.
    private IEnumerator ArrivalHoldRoutine()
    {
        yield return new WaitForSecondsRealtime(ArrivalHoldTimeout);
        arrivalHold = null;
        arrivalHealthPending = false;
        BeginSpawnFade();
    }

    // A copy at 0 health in the Lobby was just created: anyone already there has their real health.
    private void SpawnArrivalHold()
    {
        if (IsOwner || !ActiveSession.InLobby || currentHealth.Value > 0f)
            return;
        arrivalHealthPending = true;
        SetSpriteAlpha(0f);
        arrivalHold = StartCoroutine(ArrivalHoldRoutine());
    }

    private void ReleaseArrivalHold()
    {
        if (arrivalHold == null)
            return;
        StopCoroutine(arrivalHold);
        arrivalHold = null;
    }

    // A disabled player's coroutine never finishes, so it must not stay see-through.
    private void StopSpawnFade()
    {
        ReleaseArrivalHold();
        if (spawnFade != null)
            StopCoroutine(spawnFade);
        spawnFade = null;
        if (spriteRenderer != null)
            SetSpriteAlpha(1f);
    }

    [Rpc(SendTo.Everyone)]
    private void JoinArrivalRpc(int colorIndex)
    {
        PlayerSpawner.PlayJoinArrival(this, colorIndex);
    }

    // Owner, after a join: only a client entering the Lobby arrives on the altar.
    public void AnnounceJoinArrival()
    {
        if (IsOwner && IsSpawned && !IsServer && ActiveSession.InLobby)
            JoinArrivalRpc(ColorIndex);
    }

    public void BeginSpawnFade()
    {
        StopSpawnFade();
        SetSpriteAlpha(0f);
        spawnFade = StartCoroutine(SpawnFadeRoutine());
    }
}
