using UnityEngine;

// Downed players get back up on their own while this is loaded, e.g. in the Lobby.
public class AutoReviveZone : MonoBehaviour
{
    void OnEnable()
    {
        Player.AutoReviveActive = true;
    }

    void OnDisable()
    {
        Player.AutoReviveActive = false;
    }
}
