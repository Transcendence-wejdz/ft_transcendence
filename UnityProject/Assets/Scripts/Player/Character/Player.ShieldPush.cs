using UnityEngine;

// Raised shield physics against enemies only: knockback pushes, and enemies kept in front of the wall; players pass through it.
public partial class Player
{
    private const float ShieldContactGap = 0.02f;
    // Extra size for the push query only, since the solid wall keeps enemies touching it, never overlapping.
    private const float PushQueryPadding = 0.8f;

    private static readonly Collider2D[] ShieldPushBuffer = new Collider2D[32];
    private static readonly Collider2D[] WallOverlapBuffer = new Collider2D[32];
    private static int enemyLayerMask = -1;

    private static int EnemyLayerMask
    {
        get
        {
            if (enemyLayerMask < 0)
                enemyLayerMask = LayerMask.GetMask("Enemy");
            return enemyLayerMask;
        }
    }

    private static Vector2 PushQuerySize(Vector2 wallSize)
    {
        return wallSize + new Vector2(PushQueryPadding, PushQueryPadding);
    }

    private static ContactFilter2D SolidFilter(int layerMask)
    {
        ContactFilter2D filter = new ContactFilter2D { useTriggers = false };

        filter.SetLayerMask(layerMask);
        return filter;
    }

    private static Vector2 DirFromAngle(float degrees)
    {
        float radians = degrees * Mathf.Deg2Rad;

        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    // Live collider centre from the transform, since bounds only refresh on the next physics step.
    private static Vector2 BodyCenterNow(Collider2D collider)
    {
        return collider.transform.TransformPoint(collider.offset);
    }

    // True when the collider touches the given wall shape.
    private static bool OverlapsWall(Collider2D target, Vector2 wallCenter, Vector2 wallSize, CapsuleDirection2D direction, float angle)
    {
        int count = Physics2D.OverlapCapsule(wallCenter, wallSize, direction, angle, SolidFilter(1 << target.gameObject.layer), WallOverlapBuffer);

        for (int i = 0; i < count; i++)
        {
            if (WallOverlapBuffer[i] == target)
                return true;
        }
        return false;
    }

    // Push along the facing that puts a body just past the wall's front.
    private static Vector2 WallFrontPushOut(Vector2 bodyCenter, Collider2D collider, Vector2 shieldCenter, Vector2 facing, Vector2 wallSize)
    {
        Vector2 offset = bodyCenter - shieldCenter;
        float along = Vector2.Dot(offset, facing);
        float radius;
        float lateral;
        float needed;

        if (along <= 0f)
            return Vector2.zero;
        radius = InnerRadius(collider);
        lateral = Mathf.Abs(facing.x * offset.y - facing.y * offset.x);
        if (lateral > wallSize.y * 0.5f + radius)
            return Vector2.zero;
        needed = wallSize.x * 0.5f + radius + ShieldContactGap;
        if (along >= needed)
            return Vector2.zero;
        return facing * (needed - along);
    }

    private CapsuleCollider2D ShieldHitboxTemplate()
    {
        if (ShieldKit == null || ShieldKit.FxPrefab == null)
            return null;
        return ShieldKit.FxPrefab.GetComponent<CapsuleCollider2D>();
    }

    // Same scale SpawnLocalFx applies to the FX: prefab scale x shield Size x level-up size bonus.
    private Vector2 ShieldWallSize(CapsuleCollider2D template)
    {
        Vector2 prefabScale = template.transform.lossyScale;
        float size = ShieldKit.Size * SizeBonusMultiplier;

        return new Vector2(template.size.x * Mathf.Abs(prefabScale.x), template.size.y * Mathf.Abs(prefabScale.y)) * size;
    }

    private Vector2 ShieldCenterFrom(Vector2 origin, Vector2 facing)
    {
        return origin + FxOffset(AbilityType.Ultimate, facing);
    }

    private int OverlapShieldEnemies(Vector2 shieldCenter, Vector2 querySize, CapsuleCollider2D template, float facingDegrees)
    {
        return Physics2D.OverlapCapsule(shieldCenter, querySize, template.direction, facingDegrees, SolidFilter(EnemyLayerMask), ShieldPushBuffer);
    }

    private void KnockBack(Enemy enemy, Vector2 direction)
    {
        enemy.ApplyKnockback(direction * ShieldKit.ShieldKnockbackForce, ShieldKit.ShieldKnockbackDuration);
    }

    // In front: pushed straight away; behind but inside the wall at spawn: pushed forward along the facing.
    private void PushEnemyFromShield(Collider2D body, Vector2 shieldCenter, Vector2 facing, Vector2 wallSize, CapsuleCollider2D template, float facingDegrees, bool onSpawn)
    {
        Enemy enemy = body.GetComponent<Enemy>();
        Vector2 toEnemy;
        bool inFront;

        if (enemy == null)
            return;
        toEnemy = enemy.BodyCenter - shieldCenter;
        inFront = Vector2.Dot(toEnemy, facing) > 0f;
        if (!inFront && !(onSpawn && OverlapsWall(body, shieldCenter, wallSize, template.direction, facingDegrees)))
            return;
        if (inFront && toEnemy.sqrMagnitude > MinMoveInputSqr)
            KnockBack(enemy, toEnemy.normalized);
        else
            KnockBack(enemy, facing);
    }

    // Knocks back enemies near the shield's real wall shape on its facing side; on spawn, those caught inside the wall are pushed forward too.
    private void ApplyPush(Vector2 center, float facingDegrees, bool onSpawn)
    {
        CapsuleCollider2D template = ShieldHitboxTemplate();
        Vector2 facing;
        Vector2 shieldCenter;
        Vector2 wallSize;
        int count;

        if (template == null)
            return;
        facing = DirFromAngle(facingDegrees);
        shieldCenter = ShieldCenterFrom(center, facing);
        wallSize = ShieldWallSize(template);
        count = OverlapShieldEnemies(shieldCenter, PushQuerySize(wallSize), template, facingDegrees);
        for (int i = 0; i < count; i++)
            PushEnemyFromShield(ShieldPushBuffer[i], shieldCenter, facing, wallSize, template, facingDegrees, onSpawn);
    }

    [Unity.Netcode.ServerRpc]
    private void PushEnemiesServerRpc(Vector2 center, float facingDegrees, bool onSpawn)
    {
        ApplyPush(center, facingDegrees, onSpawn);
    }

    // Enemies are server-owned, so a networked shield asks the server to push them.
    private void PushNearbyEnemies(bool onSpawn)
    {
        if (NetworkObject.IsSpawned)
            PushEnemiesServerRpc(transform.position, shieldFacingAngle.Value, onSpawn);
        else
            ApplyPush(transform.position, shieldFacingAngle.Value, onSpawn);
    }

    // Server/offline only, every frame of a raised shield: enemies touching the wall slide with it.
    private void UpdateShieldContactPush()
    {
        CapsuleCollider2D template;
        float facingDegrees;
        Vector2 facing;
        Vector2 shieldCenter;
        Vector2 wallSize;
        int count;
        Enemy enemy;
        Vector2 displacement;

        if (!isShieldActive.Value || ShieldKit == null || !this.HasServerAuthority() || Time.deltaTime <= 0f)
            return;
        template = ShieldHitboxTemplate();
        if (template == null)
            return;
        facingDegrees = shieldFacingAngle.Value;
        facing = DirFromAngle(facingDegrees);
        shieldCenter = ShieldCenterFrom(transform.position, facing);
        wallSize = ShieldWallSize(template);
        count = OverlapShieldEnemies(shieldCenter, PushQuerySize(wallSize), template, facingDegrees);
        for (int i = 0; i < count; i++)
        {
            enemy = ShieldPushBuffer[i].GetComponent<Enemy>();
            if (enemy == null)
                continue;
            displacement = WallFrontPushOut(BodyCenterNow(ShieldPushBuffer[i]), ShieldPushBuffer[i], shieldCenter, facing, wallSize);
            if (displacement != Vector2.zero)
                enemy.ApplyKnockback(Vector2.ClampMagnitude(displacement / Time.deltaTime, MaxPushSpeed), Time.fixedDeltaTime);
        }
    }
}
