using UnityEditor;
using UnityEngine;

// Boss and summon sprites in the preview, animated like Boss.PlayPhaseVisual.
public partial class BossKitEditor
{
    private const float GoldenAngle = 2.39996f;
    private const float SummonDotRadius = 0.2f;
    private const float FacingLeftThreshold = -0.01f;
    private static readonly Color SpriteTint = new Color(1f, 1f, 1f, 0.85f);

    private ObjectReferenceKeyframe[] Frames(string state)
    {
        return PreviewAnimation.SpriteKeys(PreviewAnimation.StateClip(BossPrefab(), state, Vector2.right));
    }

    private static SpriteRenderer SummonRenderer(EnemyKit[] kits, int index)
    {
        EnemyKit summon;

        if (kits == null || kits.Length == 0)
            return null;
        summon = kits[index % kits.Length];
        if (summon == null || summon.EnemyPrefab == null)
            return null;
        return summon.EnemyPrefab.GetComponentInChildren<SpriteRenderer>(true);
    }

    private Vector2 SummonSpot(Vector2 center, int index)
    {
        float angle = index * GoldenAngle;
        float distance = Kit.SummonRadius * Mathf.Sqrt((index + 0.5f) / Mathf.Max(1, Kit.SummonCount));

        return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
    }

    // Spawn spots are random in game; a fixed golden-angle spread shows the count and area, with each summoned kit's sprite.
    private void DrawSummons(PreviewCanvas canvas, Vector2 center)
    {
        SpriteRenderer spriteRenderer;
        Vector2 spot;

        for (int i = 0; i < Kit.SummonCount; i++)
        {
            spot = SummonSpot(center, i);
            spriteRenderer = SummonRenderer(Kit.SummonKits, i);
            if (spriteRenderer != null && spriteRenderer.sprite != null)
                canvas.Sprite(spriteRenderer.sprite, spot, spriteRenderer.transform.lossyScale, 0f, false, SpriteTint);
            else
                canvas.Disc(spot, SummonDotRadius, SummonColor);
        }
    }

    // The charge runs Walk at its own speed, slam and summon fit Attack to their active time.
    private Sprite ActiveSprite(Step step, float sinceActive)
    {
        if (step.Pattern == BossPattern.Charge)
            return PreviewAnimation.SampleFrames(Frames(Kit.WalkState), sinceActive * Kit.ChargeAnimationSpeed, true);
        return PreviewAnimation.SampleFitted(Frames(Kit.AttackState), sinceActive, step.Active);
    }

    // State per phase: windup and recovery clips fit their duration, Idle outside a combo.
    private Sprite BossSprite(Step step, float time, bool inCombo)
    {
        float local = time - step.Start;

        if (!inCombo || local < 0f)
            return PreviewAnimation.SampleFrames(Frames(Kit.IdleState), time, true);
        if (local < step.Windup)
            return PreviewAnimation.SampleFitted(Frames(Kit.WindupState), local, step.Windup);
        if (local < step.Windup + step.Active)
            return ActiveSprite(step, local - step.Windup);
        return PreviewAnimation.SampleFitted(Frames(Kit.RecoveryState), local - step.Windup - step.Active, step.Recovery);
    }

    private void DrawBoss(PreviewCanvas canvas, Step step, float time, Vector2 body, bool inCombo)
    {
        GameObject prefab = BossPrefab();
        Sprite sprite = BossSprite(step, time, inCombo);
        Vector2 bodyOffset;

        PreviewShapes.PrefabBody(prefab, FallbackBodyRadius, out bodyOffset, out _);
        canvas.PrefabSprite(prefab, sprite, body - bodyOffset, step.Direction.x < FacingLeftThreshold, Color.white);
    }
}
