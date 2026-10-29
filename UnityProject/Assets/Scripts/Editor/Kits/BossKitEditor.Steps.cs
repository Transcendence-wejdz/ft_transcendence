using System.Collections.Generic;
using UnityEngine;

// Simulated combos: the patterns each preview plays and their timed steps, sequenced like Boss.Combo.
public partial class BossKitEditor
{
    private const float TailSeconds = 0.4f;
    private const float ChainWindupFraction = 0.6f;
    private const float ChargeOvershoot = 3f;
    private const float MinChargeLength = 4f;

    private enum Combo { Opening, Melee, Ranged }
    private static readonly string[] ComboLabels = { "Opening (summon)", "Melee (charge + slam)", "Ranged (chain + summon)" };

    private bool enraged;
    private Combo combo = Combo.Ranged;

    private struct Step
    {
        public BossPattern Pattern;
        public float Start;
        public float Windup;
        public float Active;
        public float Recovery;
        public Vector2 From;
        public Vector2 Direction;
        public float Length;
        public float End => Start + Windup + Active + Recovery;
    }

    private float Tempo => enraged ? Mathf.Max(Kit.EnrageTempo, 0.01f) : 1f;

    // Where the simulated target stands: inside slam range for melee, past the charge range otherwise.
    private Vector2 TargetFor(Preview preview) => preview == Preview.Slam || (preview == Preview.Combo && combo == Combo.Melee)
        ? Vector2.right * Mathf.Max(0.5f, Kit.SlamMaxRange * 0.6f)
        : Vector2.right * Mathf.Max(Kit.ChargeMinRange + 2f, 3f);

    private List<BossPattern> Patterns(Preview preview)
    {
        var patterns = new List<BossPattern>();
        switch (preview)
        {
            case Preview.Charge:
                patterns.Add(BossPattern.Charge);
                break;
            case Preview.Slam:
                patterns.Add(BossPattern.Slam);
                break;
            case Preview.Summon:
                patterns.Add(BossPattern.Summon);
                break;
            default:
                if (combo == Combo.Opening)
                {
                    patterns.Add(BossPattern.Summon);
                }
                else if (combo == Combo.Melee)
                {
                    patterns.Add(BossPattern.Charge);
                    patterns.Add(BossPattern.Slam);
                }
                else
                {
                    for (int i = 0; i < Mathf.Max(1, Kit.ChargeChainCount); i++)
                        patterns.Add(BossPattern.Charge);
                    patterns.Add(BossPattern.Summon);
                }
                break;
        }
        return patterns;
    }

    // Same sequencing as Boss.Combo: every step re-aims at the target, chained windups are shortened, only the last step recovers, all scaled by the tempo.
    private List<Step> BuildSteps(Preview preview)
    {
        var steps = new List<Step>();
        List<BossPattern> patterns = Patterns(preview);
        Vector2 target = TargetFor(preview);
        Vector2 position = Vector2.zero;
        float time = 0f;
        for (int i = 0; i < patterns.Count; i++)
        {
            var step = new Step { Pattern = patterns[i], Start = time, From = position };
            Vector2 aim = target - position;
            step.Direction = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.right;
            float windup = patterns[i] == BossPattern.Charge ? Kit.ChargeWindup : patterns[i] == BossPattern.Slam ? Kit.SlamWindup : Kit.SummonWindup;
            if (i > 0)
                windup *= ChainWindupFraction;
            step.Windup = windup / Tempo;

            if (patterns[i] == BossPattern.Charge)
            {
                step.Length = Mathf.Clamp(aim.magnitude + ChargeOvershoot, MinChargeLength, Mathf.Max(MinChargeLength, Kit.ChargeDistance));
                step.Active = step.Length / Mathf.Max(0.01f, Kit.ChargeSpeed);
                position += step.Direction * step.Length;
            }
            else
            {
                step.Active = (patterns[i] == BossPattern.Slam ? Kit.SlamActiveDuration : Kit.SummonActiveDuration) / Tempo;
            }
            step.Recovery = i == patterns.Count - 1 ? Kit.RecoveryDuration / Tempo : 0f;
            time = step.End;
            steps.Add(step);
        }
        return steps;
    }

    private static float TimelineLength(List<Step> steps) => (steps.Count > 0 ? steps[steps.Count - 1].End : 0f) + TailSeconds;

    // Body center at time t: still between steps, sliding along the lane during charge actives.
    private static Vector2 BodyAt(List<Step> steps, float time)
    {
        Vector2 position = Vector2.zero;
        foreach (Step step in steps)
        {
            float activeStart = step.Start + step.Windup;
            if (step.Pattern == BossPattern.Charge)
            {
                if (time <= activeStart)
                    return step.From;
                float k = step.Active > 0f ? Mathf.Clamp01((time - activeStart) / step.Active) : 1f;
                position = step.From + step.Direction * step.Length * k;
                if (k < 1f)
                    return position;
            }
            else if (time < step.End)
            {
                return step.From;
            }
        }
        return position;
    }

    // The step playing at time, or the last one once the combo is over.
    private static Step StepAt(List<Step> steps, float time)
    {
        foreach (Step step in steps)
        {
            if (time < step.End)
                return step;
        }
        return steps.Count > 0 ? steps[steps.Count - 1] : default;
    }
}
