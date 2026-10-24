using Unity.Netcode;

// NetworkVariable for offline-first objects: until it belongs to a spawned NetworkBehaviour (solo play, or before spawn) a write only updates the local value and fires OnValueChanged, never marking anything to send.
public class OfflineSafeNetworkVariable<T> : NetworkVariable<T>
{
    public OfflineSafeNetworkVariable(T value = default,
        NetworkVariableReadPermission readPerm = DefaultReadPerm,
        NetworkVariableWritePermission writePerm = DefaultWritePerm)
        : base(value, readPerm, writePerm)
    {
    }

    // Unbound means there is no network session to sync to; the spawn snapshot carries the current value anyway.
    public override void SetDirty(bool isDirty)
    {
        if (GetBehaviour() == null)
            return;

        base.SetDirty(isDirty);
    }
}
