using System;
using UnityEngine;

// Boss hub: holds the kit and its parts, and tracks the single living boss for UI and the victory check.
public class Boss : MonoBehaviour
{
    [SerializeField] private BossKit kit;
    [SerializeField] private BossAI ai;
    [SerializeField] private BossStats stats;

    public static Boss Active { get; private set; }
    public static event Action<Boss> OnActiveChanged;

    public BossKit Kit => kit;
    public BossAI AI => ai;
    public BossStats Stats => stats;

    void OnEnable()
    {
        Active = this;
        OnActiveChanged?.Invoke(this);
    }

    void OnDisable()
    {
        if (Active != this)
            return;

        Active = null;
        OnActiveChanged?.Invoke(null);
    }

    private void Reset()
    {
        ai = GetComponent<BossAI>();
        stats = GetComponent<BossStats>();
    }
}
