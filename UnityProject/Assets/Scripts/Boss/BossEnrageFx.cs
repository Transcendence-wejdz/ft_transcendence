using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Phase 2 look of a boss: pulsing red outline, ground halo, strong red light and glowing eyes, on every peer from the replicated health.
public class BossEnrageFx : MonoBehaviour
{
    [Serializable]
    private struct EyeFrame
    {
        public Sprite sprite;
        public bool visible;
        public Vector2 first;
        public Vector2 second;
    }

    private const int OutlineCopies = 8;
    private const float EyeCoreSize = 0.03f;

    [SerializeField] private SpriteRenderer body;
    [SerializeField] private SortingLayer_Auto sorting;
    [SerializeField] private Light2D enrageLight;
    [SerializeField] private Material flatMaterial;
    [SerializeField] private Material glowMaterial;
    [SerializeField] private Color auraColor = new Color(1f, 0.05f, 0.05f, 1f);
    [SerializeField] private float fadeSeconds = 0.6f;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float innerOutlineThickness = 0.05f;
    [SerializeField] private float outerOutlineThickness = 0.12f;
    [SerializeField] private float outerOutlineAlpha = 0.25f;
    [SerializeField] private float haloDiameter = 7f;
    [SerializeField] private float haloAlpha = 0.2f;
    [SerializeField] private Vector2 haloOffset = new Vector2(0f, 0.8f);
    [SerializeField] private float lightIntensity = 0.6f;
    [SerializeField] private float eyeGlowDiameter = 0.7f;
    [SerializeField] private EyeFrame[] eyeFrames;

    private Boss boss;
    private SpriteRenderer[] innerOutline;
    private SpriteRenderer[] outerOutline;
    private SpriteRenderer halo;
    private readonly SpriteRenderer[] eyeCores = new SpriteRenderer[2];
    private readonly SpriteRenderer[] eyeGlows = new SpriteRenderer[2];
    private bool built;
    private float blend;

    private SpriteRenderer CreateRenderer(string objectName, Transform parent, Material material, Sprite sprite)
    {
        GameObject child = new GameObject(objectName);
        SpriteRenderer spriteRenderer = child.AddComponent<SpriteRenderer>();

        child.transform.SetParent(parent, false);
        spriteRenderer.sharedMaterial = material;
        spriteRenderer.sprite = sprite;
        spriteRenderer.color = Color.clear;
        spriteRenderer.sortingLayerID = body.sortingLayerID;
        return spriteRenderer;
    }

    private SpriteRenderer[] CreateOutline(float thickness)
    {
        SpriteRenderer[] copies = new SpriteRenderer[OutlineCopies];
        float localThickness = thickness / body.transform.lossyScale.x;
        float angle;

        for (int i = 0; i < OutlineCopies; i++)
        {
            angle = i * Mathf.PI * 2f / OutlineCopies;
            copies[i] = CreateRenderer("EnrageOutline", body.transform, flatMaterial, body.sprite);
            copies[i].transform.localPosition = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * localThickness;
        }
        return copies;
    }

    private void Build()
    {
        float bodyScale = body.transform.lossyScale.x;

        built = true;
        innerOutline = CreateOutline(innerOutlineThickness);
        outerOutline = CreateOutline(outerOutlineThickness);
        halo = CreateRenderer("EnrageHalo", transform, glowMaterial, BossFxSprites.Glow);
        halo.transform.localPosition = haloOffset;
        halo.transform.localScale = Vector3.one * (haloDiameter / transform.lossyScale.x);
        for (int i = 0; i < eyeCores.Length; i++)
        {
            eyeCores[i] = CreateRenderer("EnrageEye", body.transform, flatMaterial, BossFxSprites.Square);
            eyeCores[i].transform.localScale = Vector3.one * EyeCoreSize;
            eyeGlows[i] = CreateRenderer("EnrageEyeGlow", body.transform, glowMaterial, BossFxSprites.Glow);
            eyeGlows[i].transform.localScale = Vector3.one * (eyeGlowDiameter / bodyScale);
        }
    }

    private bool TryGetEyes(Sprite sprite, out EyeFrame frame)
    {
        foreach (EyeFrame candidate in eyeFrames)
        {
            if (candidate.sprite == sprite)
            {
                frame = candidate;
                return candidate.visible;
            }
        }
        frame = default;
        return false;
    }

    private Color AuraWithAlpha(float alpha)
    {
        return new Color(auraColor.r, auraColor.g, auraColor.b, alpha);
    }

    private void UpdateOutline(SpriteRenderer[] copies, float alpha, int order)
    {
        foreach (SpriteRenderer copy in copies)
        {
            copy.sprite = body.sprite;
            copy.flipX = body.flipX;
            copy.color = AuraWithAlpha(alpha);
            copy.sortingOrder = order;
        }
    }

    private void PlaceEye(int index, Vector2 position, int order)
    {
        if (body.flipX)
            position.x = -position.x;
        eyeCores[index].transform.localPosition = position;
        eyeGlows[index].transform.localPosition = position;
        eyeCores[index].color = AuraWithAlpha(blend);
        eyeGlows[index].color = AuraWithAlpha(blend);
        eyeCores[index].sortingOrder = order + 1;
        eyeGlows[index].sortingOrder = order + 2;
    }

    private void UpdateEyes(int order)
    {
        EyeFrame frame;
        bool visible = TryGetEyes(body.sprite, out frame);

        for (int i = 0; i < eyeCores.Length; i++)
        {
            eyeCores[i].enabled = visible;
            eyeGlows[i].enabled = visible;
        }
        if (!visible)
            return;
        PlaceEye(0, frame.first, order);
        PlaceEye(1, frame.second, order);
    }

    void Awake()
    {
        boss = GetComponent<Boss>();
    }

    void LateUpdate()
    {
        bool enraged = boss.IsEnraged;
        float target = 0f;
        float pulse;
        int order;

        if (!enraged && blend <= 0f)
            return;
        if (!built)
            Build();
        if (enraged)
            target = 1f;
        blend = Mathf.MoveTowards(blend, target, Time.deltaTime / fadeSeconds);
        pulse = blend * Mathf.Lerp(0.7f, 1f, 0.5f + 0.5f * Mathf.Sin(Time.time * pulseSpeed));
        order = sorting.CurrentSortingOrder;
        UpdateOutline(innerOutline, pulse, order - 1);
        UpdateOutline(outerOutline, pulse * outerOutlineAlpha, order - 2);
        halo.color = AuraWithAlpha(pulse * haloAlpha);
        halo.sortingOrder = order - 3;
        enrageLight.enabled = blend > 0f;
        enrageLight.intensity = pulse * lightIntensity;
        UpdateEyes(order);
    }
}
