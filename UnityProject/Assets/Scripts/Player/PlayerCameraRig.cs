using UnityEngine;

// Camera child lookups by name, in one place.
public static class PlayerCameraRig
{
    private const string MainCameraName = "Main Camera";
    private const string CinemachineCameraName = "CinemachineCamera";

    private static AudioListener FindOwnListener(Transform player)
    {
        Transform mainCamera = player.Find(MainCameraName);

        if (mainCamera == null)
            return null;
        return mainCamera.GetComponent<AudioListener>();
    }

    public static void SetActive(Transform player, bool active)
    {
        Transform mainCamera = player.Find(MainCameraName);
        Transform cinemachineCamera = player.Find(CinemachineCameraName);

        if (mainCamera != null)
            mainCamera.gameObject.SetActive(active);
        if (cinemachineCamera != null)
            cinemachineCamera.gameObject.SetActive(active);
    }

    // Makes this player's camera the only enabled AudioListener (prefab listeners start disabled, joining rigs and remote copies would add more).
    public static void ClaimAudioListener(Transform player)
    {
        AudioListener own = FindOwnListener(player);

        if (own == null)
            return;
        foreach (AudioListener listener in Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude))
        {
            if (listener != own)
                listener.enabled = false;
        }
        own.enabled = true;
    }

    // Reparents the player's cameras so they survive its deactivation.
    public static GameObject Detach(Transform player)
    {
        Transform mainCamera = player.Find(MainCameraName);
        Transform cinemachineCamera = player.Find(CinemachineCameraName);
        GameObject holder;

        if (mainCamera == null && cinemachineCamera == null)
            return null;
        holder = new GameObject("DetachedPlayerCameraRig");
        if (mainCamera != null)
            mainCamera.SetParent(holder.transform, worldPositionStays: true);
        if (cinemachineCamera != null)
            cinemachineCamera.SetParent(holder.transform, worldPositionStays: true);
        return holder;
    }
}
