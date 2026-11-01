using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// Mount / revive: an ally rides a downed player's pig for mountDurationSeconds to bring them back.
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
    private float pendingRideOffset;

    // 0..1 while an ally rides this downed player, -1 otherwise; local per peer like the world revive bar.
    public float ReviveFraction { get; private set; } = -1f;

    // The player this one rides, read from the replicated transform.parent.
    private Player ParentPlayer()
    {
        if (transform.parent == null)
            return null;
        return transform.parent.GetComponent<Player>();
    }

    private int CurrentSortingOrder()
    {
        if (sortingLayerAuto != null)
            return sortingLayerAuto.CurrentSortingOrder;
        return spriteRenderer.sortingOrder;
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

    private void PlaceOnPig()
    {
        transform.localPosition = mountedOffset;
        transform.localRotation = Quaternion.identity;
    }

    // worldPositionStays true, else the rider teleports for a frame before LateUpdateMount corrects it.
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestMountParentServerRpc(NetworkObjectReference pigRef)
    {
        NetworkObject pigNetworkObject;

        if (pigRef.TryGet(out pigNetworkObject))
            NetworkObject.TrySetParent(pigNetworkObject, worldPositionStays: true);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void RequestUnmountParentServerRpc()
    {
        NetworkObject.TrySetParent((Transform)null, worldPositionStays: true);
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
            return;
        }
        transform.SetParent(pig.transform, worldPositionStays: false);
        PlaceOnPig();
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

    // Interact pressed again while riding: gets off and cancels the revive; false when not riding.
    public bool TryDismount()
    {
        if (!isMounted || !this.IsLocallyControlled())
            return false;
        StopMounting();
        return true;
    }

    private void SetCollisionIgnored(Player other, bool ignored)
    {
        if (other != null && bodyCollider != null && other.bodyCollider != null)
            Physics2D.IgnoreCollision(bodyCollider, other.bodyCollider, ignored);
    }

    // Runs on every client (each simulates physics locally); reads the replicated transform.parent, not the local isMounted flag.
    private void UpdateMountCollisionIgnore()
    {
        Player currentParent = ParentPlayer();

        if (currentParent == collisionIgnoredWith)
            return;
        SetCollisionIgnored(collisionIgnoredWith, false);
        collisionIgnoredWith = currentParent;
        SetCollisionIgnored(collisionIgnoredWith, true);
    }

    // Unparents before reviving, or observers could see the revived player drag the rider along.
    private void FinishRevive()
    {
        Player pigToRevive = mountedPig;

        StopMounting();
        pigToRevive.RequestRevive();
    }

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
            FinishRevive();
    }

    // Re-offsets once the server-driven parenting request has actually landed on this client; no-op once already in place.
    private void LateUpdateMount()
    {
        if (!isMounted || !this.IsLocallyControlled() || mountedPig == null)
            return;
        if (transform.parent == mountedPig.transform)
            PlaceOnPig();
    }

    // Local rendering only, runs for every observer; the rider pins itself in front of the pig and the pig right under it (after the pig's own Update).
    private void LateUpdateMountSorting()
    {
        Player parentPlayer = ParentPlayer();

        if (sortingLayerAuto != null)
            sortingLayerAuto.enabled = parentPlayer == null;
        if (parentPlayer == null || spriteRenderer == null || parentPlayer.spriteRenderer == null)
            return;
        spriteRenderer.sortingOrder = parentPlayer.CurrentSortingOrder() + mountedSortingOrderBump;
        if (parentPlayer.pigRenderer == null)
            return;
        parentPlayer.pigRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
        parentPlayer.pigRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;
    }

    // Wired to the MountInteract zone's onInteract (a child of the downed player); starts mounting whichever local player interacted.
    public void RequestMountByLocalPlayer()
    {
        Player localPlayer;

        if (!IsDead)
            return;
        localPlayer = LocalPlayer.GetPlayer();
        if (localPlayer == null || localPlayer == this || localPlayer.IsDead)
            return;
        localPlayer.StartMounting(this);
    }
}
