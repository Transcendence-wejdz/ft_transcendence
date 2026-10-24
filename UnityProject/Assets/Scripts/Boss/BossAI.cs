using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public enum BossPattern : byte { None, Charge, Slam, Summon }

public enum BossPhase : byte { Windup, Active, Recovery }

// Server-authoritative boss brain: runs attack combos (melee charge into slam, chained charges ending in a summon), strafes while waiting and enrages at low health; visuals replay on clients.
public class BossAI : NetworkBehaviour
{
    private const float ContactMargin = 0.5f;
    private const float HoldRangeFraction = 0.8f;
    private const float RetargetHysteresis = 0.7f;
    private const float VelocitySmoothing = 0.2f;
    private const float StrafeSpeedFraction = 0.6f;
    private const float ChainWindupFraction = 0.6f;
    private const float ChargeOvershoot = 3f;
    private const float MinChargeLength = 4f;

    private Boss boss;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Vector2 bodyOffset;
    private float bodyRadius;
    private WaveSpawner waveSpawner;

    private Player target;
    private Vector2 lastTargetPosition;
    private Vector2 targetVelocity;
    private float nextRetargetTime;
    private float nextSteerTime;
    private Vector2 steerDirection;
    private float strafeSign = 1f;
    private float nextStrafeFlipTime;

    private readonly Queue<BossPattern> combo = new Queue<BossPattern>();
    private BossPattern pattern;
    private BossPhase phase;
    private float phaseEndTime;
    private bool chained;
    private float chargeLength;
    private Vector2 patternDirection;
    private bool comboUsedCharge;
    private bool comboUsedSlam;
    private bool comboUsedSummon;
    private bool openingDone;
    private float nextPatternTime;
    private float nextSlamTime;
    private float nextChargeTime;
    private float nextSummonTime;
    private readonly HashSet<Player> chargeHits = new HashSet<Player>();
    private float localChargeEndTime;
    private bool localChargeHit;
    private readonly List<Player> victims = new List<Player>();

    private Vector2 lastPosition;
    private float lastMovedTime;
    private float heldStateEndTime;
    private int heldStateHash;
    private int currentStateHash;

    private readonly NetworkVariable<bool> facingLeft = new OfflineSafeNetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private bool offlineFacingLeft;

    private BossKit Kit => boss.Kit;

    // The collider sits above the pivot; range checks and telegraphs start from the real body.
    public Vector2 BodyCenter => (Vector2)transform.position + bodyOffset;

    // Above 1 once enraged: windups, recoveries and cooldowns shrink, walking speeds up.
    private float Tempo => boss.Stats.MaxHealth > 0f && boss.Stats.CurrentHealth > 0f
        && boss.Stats.CurrentHealth <= boss.Stats.MaxHealth * Kit.EnrageHealthFraction ? Kit.EnrageTempo : 1f;

    // Lane width actually hit: never narrower than the body plus a contact margin, since colliders keep players outside the body.
    private float ChargeHitWidth => Mathf.Max(Kit.ChargeWidth, 2f * (bodyRadius + ContactMargin));

    private bool FacingLeft => IsSpawned ? facingLeft.Value : offlineFacingLeft;

    private bool SummonReady => Kit.SummonKits != null && Kit.SummonKits.Length > 0 && Kit.SummonCount > 0 && Time.time >= nextSummonTime;

    void Awake()
    {
        boss = GetComponent<Boss>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        animator = GetComponentInChildren<Animator>();

        CircleCollider2D circle = GetComponent<CircleCollider2D>();
        bodyOffset = circle != null ? circle.offset * (Vector2)transform.lossyScale : Vector2.zero;
        bodyRadius = circle != null ? circle.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y) : 0.5f;
    }

    void Start()
    {
        lastPosition = transform.position;
        nextPatternTime = Time.time + Kit.PatternCooldown;
        nextSummonTime = Time.time + Kit.SummonInterval * 0.5f;
    }

    // Runs on every peer; facing is server-replicated, Idle/Walk come from real movement.
    void Update()
    {
        UpdateVisuals();
        UpdateLocalCharge();
    }

    // Server-only loop: a running combo owns movement, otherwise start a combo if one fits, else reposition.
    void FixedUpdate()
    {
        if (!this.HasServerAuthority())
            return;

        EnemyFlowFieldManager manager = EnemyFlowFieldManager.Instance;
        if (pattern == BossPattern.None && (target == null || target.IsDead || Time.time >= nextRetargetTime))
            ChooseTarget(manager);
        if (target != null)
            TrackTargetVelocity();

        if (pattern != BossPattern.None)
        {
            TickPattern();
            return;
        }

        if (target == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toTarget = EnemyAI.TargetCenter(target) - BodyCenter;
        float distance = toTarget.magnitude;
        if (Time.time >= nextPatternTime && TryStartCombo(distance))
            return;

        SetFacing(toTarget.x);
        float holdRange = Kit.SlamMaxRange * HoldRangeFraction;
        if (distance <= holdRange)
        {
            rb.linearVelocity = Strafe(toTarget, distance, holdRange);
            return;
        }

        if (Time.time >= nextSteerTime)
        {
            nextSteerTime = Time.time + Kit.SteerInterval;
            steerDirection = manager.TryGetDirection(target, BodyCenter, out Vector2 fieldDirection) ? fieldDirection : toTarget.normalized;
        }

        rb.linearVelocity = steerDirection * Kit.MoveSpeed * Tempo;
    }

    // Opens with a summon; then melee: charge through the target then slam (or slam alone); far: chained charges finished by a summon, or a single charge.
    private bool TryStartCombo(float distance)
    {
        bool chargeReady = Time.time >= nextChargeTime;
        if (!openingDone && Kit.SummonKits != null && Kit.SummonKits.Length > 0 && Kit.SummonCount > 0)
            combo.Enqueue(BossPattern.Summon);
        else if (distance <= Kit.SlamMaxRange)
        {
            if (chargeReady)
                combo.Enqueue(BossPattern.Charge);
            if (chargeReady || Time.time >= nextSlamTime)
                combo.Enqueue(BossPattern.Slam);
        }
        else if (distance >= Kit.ChargeMinRange && chargeReady)
        {
            bool finishWithSummon = SummonReady;
            int charges = finishWithSummon ? Mathf.Max(1, Kit.ChargeChainCount) : 1;
            for (int i = 0; i < charges; i++)
                combo.Enqueue(BossPattern.Charge);
            if (finishWithSummon)
                combo.Enqueue(BossPattern.Summon);
        }

        openingDone = true;
        if (combo.Count == 0)
            return false;

        comboUsedCharge = comboUsedSlam = comboUsedSummon = false;
        StartNextPattern(false);
        return true;
    }

    // Re-aims every step at the current target, so each chained charge tracks where the player went.
    private void StartNextPattern(bool isChained)
    {
        pattern = combo.Dequeue();
        chained = isChained;
        if (target == null || target.IsDead)
            ChooseTarget(EnemyFlowFieldManager.Instance);

        Vector2 toTarget = target != null ? EnemyAI.TargetCenter(target) - BodyCenter : patternDirection;
        if (pattern == BossPattern.Charge)
        {
            comboUsedCharge = true;
            Vector2 aim = toTarget + targetVelocity * Kit.ChargeLeadSeconds;
            patternDirection = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.right;
            chargeLength = Mathf.Clamp(aim.magnitude + ChargeOvershoot, MinChargeLength, Mathf.Max(MinChargeLength, Kit.ChargeDistance));
        }
        else
        {
            comboUsedSlam |= pattern == BossPattern.Slam;
            comboUsedSummon |= pattern == BossPattern.Summon;
            if (toTarget.sqrMagnitude > 0.0001f)
                patternDirection = toTarget.normalized;
        }

        SetFacing(patternDirection.x);
        EnterPhase(BossPhase.Windup);
    }

    // Steps inside a combo skip their recovery; only the last one recovers before the boss moves again.
    private void TickPattern()
    {
        bool charging = pattern == BossPattern.Charge && phase == BossPhase.Active;
        rb.linearVelocity = charging ? patternDirection * Kit.ChargeSpeed : Vector2.zero;
        if (charging)
            HitChargeVictims();

        if (Time.time < phaseEndTime)
            return;

        if (phase == BossPhase.Windup)
            EnterPhase(BossPhase.Active);
        else if (phase == BossPhase.Active && combo.Count > 0)
            StartNextPattern(true);
        else if (phase == BossPhase.Active)
            EnterPhase(BossPhase.Recovery);
        else
            FinishCombo();
    }

    // Applies the phase's gameplay effect here (server), then replays its visuals on every peer with the same duration and lane length.
    private void EnterPhase(BossPhase next)
    {
        phase = next;
        float duration = PhaseDuration(pattern, next);
        phaseEndTime = Time.time + duration;

        if (next == BossPhase.Active)
        {
            if (pattern == BossPattern.Charge)
                chargeHits.Clear();
            else if (pattern == BossPattern.Slam)
                ResolveSlam();
            else if (pattern == BossPattern.Summon)
                Summon();
        }

        PlayPhaseVisual(pattern, next, patternDirection, duration, chargeLength);
        if (IsSpawned)
            PhaseRpc(pattern, next, patternDirection, duration, chargeLength);
    }

    // Cooldowns start once the whole combo is over; only a short pause separates two combos.
    private void FinishCombo()
    {
        float tempo = Tempo;
        if (comboUsedSlam)
            nextSlamTime = Time.time + Kit.SlamCooldown / tempo;
        if (comboUsedCharge)
            nextChargeTime = Time.time + Kit.ChargeCooldown / tempo;
        if (comboUsedSummon)
            nextSummonTime = Time.time + Kit.SummonInterval / tempo;

        pattern = BossPattern.None;
        nextPatternTime = Time.time + Kit.PatternCooldown / tempo;
        nextRetargetTime = 0f;
        rb.linearVelocity = Vector2.zero;
    }

    // Charge travel time follows its length and speed; everything else speeds up with the enrage tempo, chained windups are shorter.
    private float PhaseDuration(BossPattern p, BossPhase ph)
    {
        if (p == BossPattern.Charge && ph == BossPhase.Active)
            return chargeLength / Mathf.Max(0.01f, Kit.ChargeSpeed);

        float baseDuration;
        if (ph == BossPhase.Recovery)
            baseDuration = Kit.RecoveryDuration;
        else if (p == BossPattern.Charge)
            baseDuration = Kit.ChargeWindup;
        else if (p == BossPattern.Slam)
            baseDuration = ph == BossPhase.Windup ? Kit.SlamWindup : Kit.SlamActiveDuration;
        else
            baseDuration = ph == BossPhase.Windup ? Kit.SummonWindup : Kit.SummonActiveDuration;

        if (ph == BossPhase.Windup && chained)
            baseDuration *= ChainWindupFraction;
        return baseDuration / Tempo;
    }

    // Waiting out cooldowns in melee: circles the target just inside hold range, flipping direction now and then.
    private Vector2 Strafe(Vector2 toTarget, float distance, float holdRange)
    {
        Vector2 toward = distance > 0.01f ? toTarget / distance : Vector2.right;
        if (Time.time >= nextStrafeFlipTime)
        {
            strafeSign = -strafeSign;
            nextStrafeFlipTime = Time.time + Random.Range(1.5f, 3f);
        }

        float holdDistance = holdRange * 0.85f;
        Vector2 tangent = new Vector2(-toward.y, toward.x) * strafeSign;
        Vector2 radial = toward * Mathf.Clamp(distance - holdDistance, -1f, 1f);
        return (tangent + radial).normalized * Kit.MoveSpeed * StrafeSpeedFraction * Tempo;
    }

    // Keeps the current target unless another living player is clearly closer, so the boss doesn't dither between two.
    private void ChooseTarget(EnemyFlowFieldManager manager)
    {
        nextRetargetTime = Time.time + Kit.RetargetInterval;
        Player nearest = manager.FindNearestLivingPlayer(BodyCenter);
        if (nearest == null || target == null || target.IsDead || nearest == target)
        {
            if (nearest != target && nearest != null)
                ResetTargetTracking(nearest);
            target = nearest;
            return;
        }

        float currentDistance = (EnemyAI.TargetCenter(target) - BodyCenter).sqrMagnitude;
        float nearestDistance = (EnemyAI.TargetCenter(nearest) - BodyCenter).sqrMagnitude;
        if (nearestDistance < currentDistance * RetargetHysteresis * RetargetHysteresis)
        {
            target = nearest;
            ResetTargetTracking(nearest);
        }
    }

    private void ResetTargetTracking(Player newTarget)
    {
        lastTargetPosition = EnemyAI.TargetCenter(newTarget);
        targetVelocity = Vector2.zero;
    }

    // Estimated from position deltas, since remote players' rigidbodies don't carry velocity on the server.
    private void TrackTargetVelocity()
    {
        Vector2 position = EnemyAI.TargetCenter(target);
        Vector2 sample = (position - lastTargetPosition) / Time.fixedDeltaTime;
        lastTargetPosition = position;
        targetVelocity = Vector2.Lerp(targetVelocity, sample, VelocitySmoothing);
    }

    // Each living player the boss body passes over is hit at most once per charge.
    private void HitChargeVictims()
    {
        float reach = ChargeHitWidth * 0.5f;
        CollectVictims(reach);
        foreach (Player player in victims)
        {
            if (chargeHits.Add(player))
                HitPlayer(player, Kit.ChargeDamage);
        }
    }

    private void ResolveSlam()
    {
        CollectVictims(Kit.SlamRadius);
        foreach (Player player in victims)
            HitPlayer(player, Kit.SlamDamage);
    }

    // Snapshot first: a killing blow removes the player from LivingPlayers mid-loop (Player.OnPlayerDied), which would break the enumeration.
    private void CollectVictims(float radius)
    {
        victims.Clear();
        foreach (Player player in EnemyFlowFieldManager.Instance.LivingPlayers)
        {
            if ((EnemyAI.TargetCenter(player) - BodyCenter).sqrMagnitude <= radius * radius)
                victims.Add(player);
        }
    }

    // The server only hits its own player; remote players judge slams and charges on their own machine (see JudgeLocalHit).
    private void HitPlayer(Player player, float damage)
    {
        if (IsSpawned && player.OwnerClientId != NetworkManager.LocalClientId)
            return;
        if (!Player.TryAbsorbWithShield(BodyCenter, EnemyAI.TargetCenter(player), damage))
            player.RequestDamage(damage);
    }

    // Client side: hits this machine's own player if it stands inside the radius around the boss as displayed here.
    private bool JudgeLocalHit(float radius, float damage)
    {
        Player local = LocalPlayer.GetPlayer();
        if (local == null || local.IsDead || (EnemyAI.TargetCenter(local) - BodyCenter).sqrMagnitude > radius * radius)
            return false;

        if (!Player.TryAbsorbWithShield(BodyCenter, EnemyAI.TargetCenter(local), damage))
            local.TakeDamage(damage);
        return true;
    }

    // Remote clients check charge contact every frame of the charge they see, at most once per charge.
    private void UpdateLocalCharge()
    {
        if (IsServer || localChargeHit || Time.time >= localChargeEndTime)
            return;
        localChargeHit = JudgeLocalHit(ChargeHitWidth * 0.5f, Kit.ChargeDamage);
    }

    // Each minion rolls its own kit from the boss's summon list; spawn spots are server-picked, so they travel to clients for the dust FX.
    private void Summon()
    {
        if (waveSpawner == null)
            waveSpawner = FindAnyObjectByType<WaveSpawner>();
        if (waveSpawner == null)
            return;

        Vector2[] spots = new Vector2[Kit.SummonCount];
        for (int i = 0; i < spots.Length; i++)
            spots[i] = waveSpawner.SpawnEnemyNear(Kit.SummonKits[Random.Range(0, Kit.SummonKits.Length)], BodyCenter, Kit.SummonRadius);

        PlaySummonFx(spots);
        if (IsSpawned)
            SummonFxRpc(spots);
    }

    [Rpc(SendTo.NotServer)]
    private void SummonFxRpc(Vector2[] spots)
    {
        PlaySummonFx(spots);
    }

    // Visual only: one random dust puff at each minion's feet.
    private void PlaySummonFx(Vector2[] spots)
    {
        GameObject[] fx = Kit.SummonFxPrefabs;
        if (fx == null || fx.Length == 0)
            return;

        foreach (Vector2 spot in spots)
        {
            GameObject prefab = fx[Random.Range(0, fx.Length)];
            if (prefab != null)
                Instantiate(prefab, spot + Kit.SummonFxOffset, Quaternion.identity);
        }
    }

    // Server/offline only; ignores tiny horizontal offsets so the boss doesn't flicker when a target is straight above or below.
    private void SetFacing(float directionX)
    {
        if (Mathf.Abs(directionX) < 0.1f)
            return;

        bool left = directionX < 0f;
        if (!IsSpawned)
            offlineFacingLeft = left;
        else if (facingLeft.Value != left)
            facingLeft.Value = left;
    }

    [Rpc(SendTo.NotServer)]
    private void PhaseRpc(BossPattern p, BossPhase ph, Vector2 direction, float duration, float laneLength)
    {
        PlayPhaseVisual(p, ph, direction, duration, laneLength);
        if (ph != BossPhase.Active)
            return;

        if (p == BossPattern.Slam)
        {
            JudgeLocalHit(Kit.SlamRadius, Kit.SlamDamage);
        }
        else if (p == BossPattern.Charge)
        {
            localChargeEndTime = Time.time + duration;
            localChargeHit = false;
        }
    }

    // Telegraphs and animations only; positions come from this peer's own transform.
    private void PlayPhaseVisual(BossPattern p, BossPhase ph, Vector2 direction, float duration, float laneLength)
    {
        if (ph == BossPhase.Windup)
        {
            HoldState(Kit.WindupState, duration, FitSpeed(Kit.WindupState, duration));
            if (p == BossPattern.Charge)
                AttackTelegraph.ShowLane(transform, BodyCenter, direction, laneLength, ChargeHitWidth, duration);
            else if (p == BossPattern.Slam)
                AttackTelegraph.Show(transform, BodyCenter, Vector2.right, 360f, Kit.SlamRadius, duration);
        }
        else if (ph == BossPhase.Active)
        {
            if (p == BossPattern.Charge)
                HoldState(Kit.WalkState, duration, Kit.ChargeAnimationSpeed);
            else
                HoldState(Kit.AttackState, duration, FitSpeed(Kit.AttackState, duration));
        }
        else
        {
            HoldState(Kit.RecoveryState, duration, FitSpeed(Kit.RecoveryState, duration));
        }
    }

    // Playback speed that makes the clip play exactly once over duration.
    private float FitSpeed(string clipName, float duration)
    {
        if (animator == null || animator.runtimeAnimatorController == null || duration <= 0f)
            return 1f;

        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == clipName)
                return clip.length / duration;
        }
        return 1f;
    }

    private void HoldState(string state, float duration, float speed)
    {
        if (animator == null)
            return;

        heldStateEndTime = Time.time + duration;
        heldStateHash = Animator.StringToHash(state);
        currentStateHash = heldStateHash;
        animator.speed = speed;
        animator.Play(heldStateHash, 0, 0f);
    }

    // Applies the replicated facing and falls back to Walk/Idle once no pattern state is held.
    private void UpdateVisuals()
    {
        if (spriteRenderer != null && spriteRenderer.flipX != FacingLeft)
            spriteRenderer.flipX = FacingLeft;

        Vector2 position = transform.position;
        Vector2 delta = position - lastPosition;
        lastPosition = position;

        if (animator == null || Time.time < heldStateEndTime)
            return;

        float minStep = 0.3f * Time.deltaTime;
        if (delta.sqrMagnitude > minStep * minStep)
            lastMovedTime = Time.time;

        int desired = Animator.StringToHash(Time.time - lastMovedTime < 0.15f ? Kit.WalkState : Kit.IdleState);
        if (desired == currentStateHash)
            return;

        currentStateHash = desired;
        animator.speed = 1f;
        animator.Play(desired, 0, 0f);
    }
}
