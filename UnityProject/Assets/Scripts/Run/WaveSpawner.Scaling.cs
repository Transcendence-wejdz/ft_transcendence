using UnityEngine;

// Keeps big fights affordable: a cap on the enemies alive, with stats that grow with the party instead of the headcount.
public partial class WaveSpawner
{
    [Tooltip("Enemies alive at once from waves; the rest wait in the queue until room frees up. Boss summons ignore it.")]
    [SerializeField] private int maxAliveEnemies = 300;
    [SerializeField] private float healthPerExtraPlayer = 0.25f;
    [SerializeField] private float damagePerExtraPlayer = 0.1f;

    private int RoomForEnemies()
    {
        return Mathf.Max(0, maxAliveEnemies - Enemy.ActiveCount);
    }

    private float StatScale(float perExtraPlayer)
    {
        int extraPlayers = Mathf.Max(0, Player.AllActiveInstances.Count - 1);

        return 1f + extraPlayers * perExtraPlayer;
    }

    private void ApplyStatScale(Enemy enemy)
    {
        enemy.SetStatScale(StatScale(healthPerExtraPlayer), StatScale(damagePerExtraPlayer));
    }
}
