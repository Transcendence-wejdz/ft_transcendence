using UnityEngine;

// Drops one puff every few units walked, from a randomly picked particle system.
public class RandomDustTrail : MonoBehaviour
{
    [SerializeField] private ParticleSystem[] puffs;
    [SerializeField] private float spacing = 1f;
    [SerializeField] private float teleportDistance = 2f;

    private Vector3 lastPosition;
    private float travelled;

    void OnEnable()
    {
        lastPosition = transform.position;
        travelled = 0f;
    }

    // Accumulates walked distance and skips teleports so a respawn doesn't draw a line of dust.
    void LateUpdate()
    {
        Vector3 position = transform.position;
        float step = Vector2.Distance(position, lastPosition);
        lastPosition = position;

        if (puffs == null || puffs.Length == 0 || spacing <= 0f || step > teleportDistance)
            return;

        travelled += step;
        while (travelled >= spacing)
        {
            travelled -= spacing;
            ParticleSystem puff = puffs[Random.Range(0, puffs.Length)];
            if (puff != null)
                puff.Emit(1);
        }
    }
}
