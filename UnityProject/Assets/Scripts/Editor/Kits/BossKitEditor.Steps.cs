using System.Collections.Generic;
using UnityEngine;

// Simulated combos: the patterns each preview plays and their timed steps, sequenced like Boss.Combo.
public partial class BossKitEditor
{
    private const float TailSeconds = 0.4f;
    private const float ChainWindupFraction = 0.6f;
    private const float ChargeOvershoot = 3f;
    private const float MinChargeLength = 4f;
    private const float MinTempo = 0.01f;
    private const float MinChargeSpeed = 0.01f;
    private const float AimSqrEpsilon = 0.0001f;

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

    private float Tempo
    {
        get
        {
            if (enraged)
                return Mathf.Max(Kit.EnrageTempo, MinTempo);
            return 1f;
        }
    }

    // Where the simulated target stands: inside slam range for melee, past the charge range otherwise.
    private Vector2 TargetFor(Preview preview)
    {
        if (preview == Preview.Slam || (preview == Preview.Combo && combo == Combo.Melee))
            return Vector2.right * Mathf.Max(0.5f, Kit.SlamMaxRange * 0.6f);
        return Vector2.right * Mathf.Max(Kit.ChargeMinRange + 2f, 3f);
    }

    private void AddComboPatterns(List<BossPattern> patterns)
    {
        if (combo == Combo.Opening)
        {
            patterns.Add(BossPattern.Summon);
            return;
        }
        if (combo == Combo.Melee)
        {
            patterns.Add(BossPattern.Charge);
            patterns.Add(BossPattern.Slam);
            return;
        }
        for (int i = 0; i < Mathf.Max(1, Kit.ChargeChainCount); i++)
            patterns.Add(BossPattern.Charge);
        patterns.Add(BossPattern.Summon);
    }

    private List<BossPattern> Patterns(Preview preview)
    {
        List<BossPattern> patterns = new List<BossPattern>();

        if (preview == Preview.Charge)
            patterns.Add(BossPattern.Charge);
        else if (preview == Preview.Slam)
            patterns.Add(BossPattern.Slam);
        else if (preview == Preview.Summon)
            patterns.Add(BossPattern.Summon);
        else
            AddComboPatterns(patterns);
        return patterns;
    }

    private float WindupOf(BossPattern pattern)
    {
        if (pattern == BossPattern.Charge)
            return Kit.ChargeWindup;
        if (pattern == BossPattern.Slam)
            return Kit.SlamWindup;
        return Kit.SummonWindup;
    }

    private static Vector2 AimDirection(Vector2 aim)
    {
        if (aim.sqrMagnitude > AimSqrEpsilon)
            return aim.normalized;
        return Vector2.right;
    }

    // Charges travel past the target (clamped to the kit's distance); other patterns stay put.
    private void SetActive(ref Step step, float aimDistance)
    {
        if (step.Pattern == BossPattern.Charge)
        {
            step.Length = Mathf.Clamp(aimDistance + ChargeOvershoot, MinChargeLength, Mathf.Max(MinChargeLength, Kit.ChargeDistance));
            step.Active = step.Length / Mathf.Max(MinChargeSpeed, Kit.ChargeSpeed);
        }
        else if (step.Pattern == BossPattern.Slam)
            step.Active = Kit.SlamActiveDuration / Tempo;
        else
            step.Active = Kit.SummonActiveDuration / Tempo;
    }

    // Same sequencing as Boss.Combo: every step re-aims at the target, chained windups are shortened, only the last step recovers, all scaled by the tempo.
    private List<Step> BuildSteps(Preview preview)
    {
        List<Step> steps = new List<Step>();
        List<BossPattern> patterns = Patterns(preview);
        Vector2 target = TargetFor(preview);
        Vector2 position = Vector2.zero;
        float time = 0f;
        float windup;
        Vector2 aim;
        Step step;

        for (int i = 0; i < patterns.Count; i++)
        {
            step = new Step { Pattern = patterns[i], Start = time, From = position };
            aim = target - position;
            step.Direction = AimDirection(aim);
            windup = WindupOf(patterns[i]);
            if (i > 0)
                windup *= ChainWindupFraction;
            step.Windup = windup / Tempo;
            SetActive(ref step, aim.magnitude);
            position += step.Direction * step.Length;
            if (i == patterns.Count - 1)
                step.Recovery = Kit.RecoveryDuration / Tempo;
            time = step.End;
            steps.Add(step);
        }
        return steps;
    }

    private static float TimelineLength(List<Step> steps)
    {
        if (steps.Count == 0)
            return TailSeconds;
        return steps[steps.Count - 1].End + TailSeconds;
    }

    private static float ChargeProgress(Step step, float time)
    {
        if (step.Active <= 0f)
            return 1f;
        return Mathf.Clamp01((time - step.Start - step.Windup) / step.Active);
    }

    // Body center at time t: still between steps, sliding along the lane during charge actives.
    private static Vector2 BodyAt(List<Step> steps, float time)
    {
        Vector2 position = Vector2.zero;
        float k;

        foreach (Step step in steps)
        {
            if (step.Pattern != BossPattern.Charge)
            {
                if (time < step.End)
                    return step.From;
                continue;
            }
            if (time <= step.Start + step.Windup)
                return step.From;
            k = ChargeProgress(step, time);
            position = step.From + step.Direction * step.Length * k;
            if (k < 1f)
                return position;
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
        if (steps.Count == 0)
            return default;
        return steps[steps.Count - 1];
    }
}
