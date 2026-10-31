using Unity.Collections;
using Unity.Netcode;

// Run score split by source, so the Game Over screen can show where the points came from.
public struct ScoreBreakdown : INetworkSerializeByMemcpy
{
    public int kills;
    public int killPoints;
    public int wavePoints;
    public int bossPoints;

    public int Total => killPoints + wavePoints + bossPoints;
}

// One player's own kills, kept by client id so it survives the move to the boss scene.
public struct PlayerKillScore : INetworkSerializeByMemcpy
{
    public ulong clientId;
    public FixedString32Bytes name;
    public int kills;
    public int killPoints;
}

// Fixed four slots (the session cap), since a memcpy NetworkVariable stays safe offline where a NetworkList does not.
public struct PlayerKillTable : INetworkSerializeByMemcpy
{
    public const int Capacity = 4;

    public int count;
    public PlayerKillScore slot0;
    public PlayerKillScore slot1;
    public PlayerKillScore slot2;
    public PlayerKillScore slot3;

    public PlayerKillScore this[int index]
    {
        get
        {
            switch (index)
            {
                case 0: return slot0;
                case 1: return slot1;
                case 2: return slot2;
                default: return slot3;
            }
        }
        set
        {
            switch (index)
            {
                case 0: slot0 = value; break;
                case 1: slot1 = value; break;
                case 2: slot2 = value; break;
                default: slot3 = value; break;
            }
        }
    }

    public int IndexOf(ulong clientId)
    {
        for (int i = 0; i < count; i++)
        {
            if (this[i].clientId == clientId)
                return i;
        }
        return -1;
    }

    // Returns the player's slot, adding it when there is room, or -1 once all four are taken.
    public int GetOrAdd(ulong clientId, string playerName)
    {
        int index = IndexOf(clientId);
        PlayerKillScore entry;

        if (index < 0 && count >= Capacity)
            return -1;
        if (index < 0)
        {
            index = count;
            count++;
        }
        entry = this[index];
        entry.clientId = clientId;
        if (!string.IsNullOrEmpty(playerName))
            entry.name = new FixedString32Bytes(playerName);
        this[index] = entry;
        return index;
    }
}
