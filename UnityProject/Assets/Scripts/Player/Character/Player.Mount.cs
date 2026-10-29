using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public partial class Player
{
    [Header("Mount / Revive")]
    [SerializeField] private float mountDurationSeconds = 20f;
    [SerializeField] private GameObject mountInteractRoot;
    [SerializeField] private Vector3 mountedOffset = new Vector3(0f, 0.25f, 0f);
    [SerializeField] private int mountedSortingOrderBump = 5;
    [SerializeField] private Image reviveProgress;

    private bool isMounted;
    private Player mountedPig;
    private float mountElapsed;
    private Player collisionIgnoredWith;
    private float riderSince = -1f;

    // 0..1 while an ally rides this downed player, -1 otherwise; local per peer like the world revive bar.
    public float ReviveFraction { get; private set; } = -1f;

    // Wired to the MountInteract zone's onInteract (a child of the downed player); starts mounting whichever local player interacted.
    public void RequestMountByLocalPlayer()
    {
        if (!IsDead)
            return;

        GameObject localPlayerObj = LocalPlayer.Get();
        Player localPlayer = localPlayerObj != null ? localPlayerObj.GetComponent<Player>() : null;
        if (localPlayer == null || localPlayer == this || localPlayer.IsDead)
            return;

        localPlayer.StartMounting(this);
    }

    // The server parents the rider to the pig; LateUpdateMount applies the offset once that parenting replicates.
    private void StartMounting(Player pig)
    {
        if (!this.IsLocallyControlled() || isMounted || pig == null || !pig.IsDead)
            return;

        isMounted = true;
        mountedPig = pig;
        mountElapsed = 0f;

        if (rb != null)
            rb.bodyType = RigidbodyType2D.Kinematic;

        if (NetworkObject.IsSpawned)
        {
            RequestMountParentServerRpc(pig.NetworkObject);
        }
        else
        {
            transform.SetParent(pig.transform, worldPositionStays: false);
            transform.localPosition = mountedOffset;
            transform.localRotation = Quaternion.identity;
        }
    }

    private void StopMounting()
    {
        if (!isMounted)
            return;

        if (rb != null)
            rb.bodyType = RigidbodyType2D.Dynamic;

        if (NetworkObject.IsSpawned)
            RequestUnmountParentServerRpc();
        else
            transform.SetParent(null, worldPositionStays: true);

        isMounted = false;
        mountedPig = null;
        mountElapsed = 0f;
    }

    // worldPositionStays true, else the rider teleports for a frame before LateUpdateMount corrects it.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestMountParentServerRpc(NetworkObjectReference pigRef)
    {
        if (pigRef.TryGet(out NetworkObject pigNetworkObject))
            NetworkObject.TrySetParent(pigNetworkObject, worldPositionStays: true);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestUnmountParentServerRpc()
    {
        NetworkObject.TrySetParent((Transform)null, worldPositionStays: true);
    }

    // Runs on every client (each simulates physics locally); reads the replicated transform.parent, not the local isMounted flag.
    private void UpdateMountCollisionIgnore()
    {
        Player currentParent = transform.parent != null ? transform.parent.GetComponent<Player>() : null;
        if (currentParent == collisionIgnoredWith)
            return;

        if (collisionIgnoredWith != null && bodyCollider != null && collisionIgnoredWith.bodyCollider != null)
            Physics2D.IgnoreCollision(bodyCollider, collisionIgnoredWith.bodyCollider, false);

        collisionIgnoredWith = currentParent;

        if (collisionIgnoredWith != null && bodyCollider != null && collisionIgnoredWith.bodyCollider != null)
            Physics2D.IgnoreCollision(bodyCollider, collisionIgnoredWith.bodyCollider, true);
    }

    // Unparents before reviving, or observers could see the revived player drag the rider along.
    private void UpdateMount()
    {
        if (!isMounted || !this.IsLocallyControlled())
            return;

        if (IsDead || mountedPig == null || !mountedPig.IsDead)
        {
            StopMounting();
            return;
        }

        mountElapsed += Time.deltaTime;
        if (mountElapsed >= mountDurationSeconds)
        {
            Player pigToRevive = mountedPig;
            StopMounting();
            pigToRevive.RequestRevive();
        }
    }

    // Re-offsets once the server-driven parenting request has actually landed on this client; no-op once already in place.
    private void LateUpdateMount()
    {
        if (!isMounted || !this.IsLocallyControlled() || mountedPig == null)
            return;

        if (transform.parent == mountedPig.transform)
        {
            transform.localPosition = mountedOffset;
            transform.localRotation = Quaternion.identity;
        }
    }

    // Hides the revive prompt while someone rides, read from the replicated transform.parent.
    private void UpdateMountInteractVisibility()
    {
        if (mountInteractRoot == null || !IsDead)
            return;

        bool shouldShow = !HasRider();
        if (mountInteractRoot.activeSelf != shouldShow)
            mountInteractRoot.SetActive(shouldShow);
    }

    // Revive bar filled over mountDurationSeconds while someone rides, timed locally from the replicated parenting.
    private void UpdateReviveProgress()
    {
        bool riding = IsDead && HasRider();
        if (!riding)
            riderSince = -1f;
        else if (riderSince < 0f)
            riderSince = Time.time;
        ReviveFraction = riding ? Mathf.Clamp01((Time.time - riderSince) / mountDurationSeconds) : -1f;

        if (reviveProgress == null)
            return;

        Canvas canvas = reviveProgress.GetComponentInParent<Canvas>(true);
        GameObject root = canvas != null ? canvas.gameObject : reviveProgress.gameObject;
        if (root.activeSelf != riding)
            root.SetActive(riding);
        if (nameTag != null)
            nameTag.SetHidden(riding);
        if (riding)
            BrushFill.Set(reviveProgress, ReviveFraction);
    }

    private bool HasRider()
    {
        foreach (Player candidate in AllActiveInstances)
        {
            if (candidate != this && candidate.transform.parent == transform)
                return true;
        }
        return false;
    }

    // Local rendering only, runs for every observer; the rider pins itself in front of the pig and the pig right under it (after the pig's own Update).
    private void LateUpdateMountSorting()
    {
        Player parentPlayer = transform.parent != null ? transform.parent.GetComponent<Player>() : null;

        if (sortingLayerAuto != null)
            sortingLayerAuto.enabled = parentPlayer == null;

        if (parentPlayer == null || spriteRenderer == null || parentPlayer.spriteRenderer == null)
            return;

        int pigSortingOrder = parentPlayer.sortingLayerAuto != null
            ? parentPlayer.sortingLayerAuto.CurrentSortingOrder
            : parentPlayer.spriteRenderer.sortingOrder;
        spriteRenderer.sortingOrder = pigSortingOrder + mountedSortingOrderBump;

        if (parentPlayer.pigRenderer != null)
        {
            parentPlayer.pigRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            parentPlayer.pigRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
        }
    }
}
