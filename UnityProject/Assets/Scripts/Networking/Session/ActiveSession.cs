using Unity.Netcode;
using Unity.Services.Multiplayer;

// The session this client is really playing in; the SDK keeps dead sessions listed (still "Connected"), so its list can't be trusted to find it.
public static class ActiveSession
{
    private static ISession current;

    // Null when solo, even if a stale session object is still held.
    public static ISession Current
    {
        get
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return null;
            return current;
        }
    }

    public static void Set(ISession session)
    {
        current = session;
    }

    public static void Clear()
    {
        current = null;
    }
}
