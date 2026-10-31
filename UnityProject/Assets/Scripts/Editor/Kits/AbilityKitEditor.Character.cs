using UnityEditor;
using UnityEngine;

// The previewed character: its action or Idle clip, and the aim indicator ring under it.
public partial class AbilityKitEditor
{
    private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
    private const string IdleState = "Idle_Tree";
    private const float RingRotationOffset = -90f;
    private static readonly Color RingTint = new Color(1f, 1f, 1f, 0.35f);

    private static GameObject PlayerPrefab()
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
    }

    private static MouseDirectionIndicator AimIndicator()
    {
        GameObject player = PlayerPrefab();

        if (player == null)
            return null;
        return player.GetComponentInChildren<MouseDirectionIndicator>(true);
    }

    // Center of the Player prefab's aim indicator ring, relative to its feet.
    private static Vector2 AimIndicatorCenter()
    {
        MouseDirectionIndicator indicator = AimIndicator();

        if (indicator == null)
            return Vector2.zero;
        return indicator.transform.position - PlayerPrefab().transform.position;
    }

    // The aim indicator ring under the character, pointing along the aim like MouseDirectionIndicator.
    private void DrawAimIndicator(PreviewCanvas canvas)
    {
        MouseDirectionIndicator indicator = AimIndicator();
        Vector2 center;
        Vector2 offset;

        if (indicator == null)
            return;
        center = AimIndicatorCenter();
        foreach (SpriteRenderer ring in indicator.GetComponentsInChildren<SpriteRenderer>(true))
        {
            offset = ring.transform.position - indicator.transform.position;
            canvas.Sprite(ring.sprite, center + offset, ring.transform.lossyScale, aimAngle + RingRotationOffset, false, RingTint);
        }
    }

    // Action clip (scaled by its anim speed) until holdUntil (its last frame held past animLength, like ShieldHeld), then Idle; mirrored when aiming left like the in-game flipX.
    private void DrawCharacter(PreviewCanvas canvas, AbilityKit kit, Vector2 direction, float time, float holdUntil, float animLength)
    {
        Sprite sprite;

        if (time < holdUntil)
            sprite = PreviewAnimation.SampleFrames(PreviewAnimation.SpriteKeys(ActionClip(SlotStateName())), Mathf.Min(time, animLength) * kit.AnimSpeed, false);
        else
            sprite = PreviewAnimation.SampleFrames(PreviewAnimation.SpriteKeys(ActionClip(IdleState)), time - holdUntil, true);
        canvas.PrefabSprite(PlayerPrefab(), sprite, Vector2.zero, direction.x < FacingLeftThreshold, Color.white);
    }
}
