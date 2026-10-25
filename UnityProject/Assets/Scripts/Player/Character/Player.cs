using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

// Root player component: movement/actions/stats/customization/feedback/pig-form, split by concern across Player.*.cs files.
public partial class Player : NetworkBehaviour, IDamageable
{
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private PlayerInput playerInput;
    private SortingLayer_Auto sortingLayerAuto;

    private bool IsBlocked => PauseManager.IsPaused;

    // False for stripped instances (e.g. the preview doll) - also checked in OnNetworkSpawn/Despawn, which ignore enabled.
    private bool isValidInstance;
    private bool hasStarted;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        isValidInstance = GetComponent<NetworkObject>() != null && rb != null;
        if (!isValidInstance)
        {
            enabled = false;
            return;
        }

        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<Collider2D>();
        playerInput = GetComponent<PlayerInput>();
        sortingLayerAuto = GetComponent<SortingLayer_Auto>();

        AwakeStats();
        AwakeCustomization();
        AwakeFeedback();
        AwakePigForm();
        AwakeActions();
    }

    // Re-enabling after Start only happens to the offline player coming back from a failed join, so it reclaims the listener.
    void OnEnable()
    {
        OnEnableFeedback();
        if (hasStarted && !IsSpawned)
            PlayerCameraRig.ClaimAudioListener(transform);
    }

    void OnDisable()
    {
        OnDisableFeedback();
    }

    void Start()
    {
        StartMovement();
        StartLevelUp();
        StartStats();
        StartCustomization();
        StartPigForm();
        if (!IsSpawned && this.IsLocallyControlled())
            PlayerCameraRig.ClaimAudioListener(transform);
        hasStarted = true;
    }

    public override void OnNetworkSpawn()
    {
        if (!isValidInstance)
            return;

        SpawnMovement();
        SpawnStats();
        SpawnCustomization();
    }

    public override void OnNetworkDespawn()
    {
        if (!isValidInstance)
            return;

        DespawnMovement();
        DespawnStats();
        DespawnCustomization();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        OnDestroyFeedback();
        UnbindDodgeInput();
    }

    void Update()
    {
        UpdateMovement();
        UpdateSoftPlayerCollision();
        UpdateDodge();
        UpdateDashDust();
        UpdateMountCollisionIgnore();
        UpdateMount();
        UpdateMountInteractVisibility();
        UpdateReviveProgress();
        UpdateStats();
        UpdatePigForm();
        UpdateActions();
        UpdateShield();
        UpdateShieldContactPush();
    }

    void LateUpdate()
    {
        LateUpdateMount();
        LateUpdateRemotePushPrediction();
        LateUpdateMountSorting();
        LateUpdateFlip();
        LateUpdateReveal();
    }
}
