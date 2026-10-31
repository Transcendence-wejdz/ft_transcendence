using Unity.Netcode;

// Offline-aware authority checks for NetworkBehaviours.
public static class NetworkBehaviourExtensions
{
    // True for the offline solo player or the owner of a spawned one.
    public static bool IsLocallyControlled(this NetworkBehaviour behaviour)
    {
        if (behaviour.NetworkObject == null)
            return false;
        return !behaviour.NetworkObject.IsSpawned || behaviour.IsOwner;
    }

    // True offline or on the server; false for a joined client's spawned copy.
    public static bool HasServerAuthority(this NetworkBehaviour behaviour)
    {
        if (behaviour.NetworkObject == null)
            return false;
        return !behaviour.NetworkObject.IsSpawned || behaviour.IsServer;
    }
}
