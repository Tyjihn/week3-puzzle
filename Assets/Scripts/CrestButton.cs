using UnityEngine;

[DisallowMultipleComponent]
public class CrestButton : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("Human-readable name used in logs, such as Lion or Stag.")]
    public string crestName;

    [Tooltip("Lion=0, Serpent=1, Raven=2, Stag=3")]
    public int crestIndex;

    [Header("Puzzle")]
    [Tooltip("The CrestPuzzleManager on TapestryStation.")]
    public CrestPuzzleManager puzzleManager;

    [Header("Visual Feedback")]
    [Min(1f)]
    public float selectedScaleMultiplier = 1.08f;

    [Range(0.5f, 1f)]
    public float wrongScaleMultiplier = 0.94f;

    [Header("Glow Effects")]
    [Tooltip("Outline and light color when a crest is selected.")]
    public Color selectedColor = new Color(1f, 0.78f, 0.25f);

    [Tooltip("Outline and light color when the four-crest order is wrong.")]
    public Color wrongColor = new Color(1f, 0.12f, 0.08f);

    [Tooltip("Optional material for the outline. Leave empty to use an unlit default.")]
    public Material outlineMaterial;

    [Min(0f)]
    public float outlinePadding = 0.06f;

    [Min(0.001f)]
    public float outlineWidth = 0.035f;

    [Min(0f)]
    public float lightIntensity = 1.5f;

    [Min(0f)]
    public float lightRange = 1.2f;

    [Min(0f)]
    [Tooltip("Extra scale added briefly when a crest is clicked.")]
    public float popAmount = 0.12f;

    [Min(0f)]
    [Tooltip("Local-space distance the crest shakes on a wrong answer.")]
    public float shakeAmount = 0.03f;

    public bool IsSelected { get; private set; }

    private enum GlowState { None, Selected, Wrong }

    private Vector3 originalLocalScale;
    private Vector3 originalLocalPosition;
    private bool originalStateCaptured;

    private GlowState glowState;
    private float stateStartTime;
    private float targetScaleMultiplier = 1f;

    private LineRenderer outline;
    private Light glowLight;

    void Awake()
    {
        CaptureOriginalState();
    }

    void Start()
    {
        if (puzzleManager == null)
            puzzleManager = GetComponentInParent<CrestPuzzleManager>();

        if (string.IsNullOrWhiteSpace(crestName))
            crestName = name.Replace("Shield_", string.Empty);
    }

    void Update()
    {
        if (glowState == GlowState.None)
            return;

        float t = Time.time - stateStartTime;

        if (glowState == GlowState.Selected)
        {
            // Quick pop on click, then a gentle breathing glow.
            float pop = popAmount * Mathf.Exp(-t * 10f) * Mathf.Cos(t * 18f);
            transform.localScale = originalLocalScale * (targetScaleMultiplier + pop);
            transform.localPosition = originalLocalPosition;

            float pulse = 0.75f + 0.25f * Mathf.Sin(t * 4f);
            ApplyGlow(selectedColor, pulse);
        }
        else
        {
            // Shake and fast red flashing.
            float decay = Mathf.Exp(-t * 4f);
            float shake = shakeAmount * decay * Mathf.Sin(t * 60f);
            transform.localPosition = originalLocalPosition + transform.localRotation * Vector3.right * shake;
            transform.localScale = originalLocalScale * targetScaleMultiplier;

            float flash = 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(t * 14f));
            ApplyGlow(wrongColor, flash);
        }
    }

    public void Press()
    {
        if (puzzleManager != null)
            puzzleManager.RegisterPress(this);
    }

    internal void SetSelectedVisual()
    {
        CaptureOriginalState();
        IsSelected = true;
        targetScaleMultiplier = selectedScaleMultiplier;
        SetGlowState(GlowState.Selected);
    }

    internal void SetWrongVisual()
    {
        CaptureOriginalState();
        targetScaleMultiplier = wrongScaleMultiplier;
        SetGlowState(GlowState.Wrong);
    }

    internal void ResetVisual()
    {
        CaptureOriginalState();
        IsSelected = false;
        targetScaleMultiplier = 1f;
        SetGlowState(GlowState.None);
        transform.localScale = originalLocalScale;
        transform.localPosition = originalLocalPosition;
    }

    void SetGlowState(GlowState state)
    {
        glowState = state;
        stateStartTime = Time.time;

        bool visible = state != GlowState.None;

        if (visible)
            EnsureEffects();

        if (outline != null)
            outline.enabled = visible;

        if (glowLight != null)
        {
            glowLight.enabled = visible;

            if (visible)
                PositionGlowLight();
        }

        if (visible)
            Update();
    }

    void ApplyGlow(Color color, float strength)
    {
        if (outline != null)
        {
            Color c = color;
            c.a = strength;
            outline.startColor = c;
            outline.endColor = c;
            outline.widthMultiplier = outlineWidth * (0.8f + 0.4f * strength);
        }

        if (glowLight != null)
        {
            glowLight.color = color;
            glowLight.intensity = lightIntensity * strength;
        }
    }

    void EnsureEffects()
    {
        if (outline == null)
            outline = CreateOutline();

        if (glowLight == null)
            glowLight = CreateGlowLight();
    }

    LineRenderer CreateOutline()
    {
        GameObject go = new GameObject("CrestSelectionOutline");
        go.transform.SetParent(transform, false);

        LineRenderer line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.alignment = LineAlignment.View;
        line.numCornerVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.widthMultiplier = outlineWidth;
        line.sharedMaterial = GetOutlineMaterial();

        GetLocalOutlineBounds(out Vector3 center, out Vector3 size);

        float hx = size.x * 0.5f + outlinePadding;
        float hy = size.y * 0.5f + outlinePadding;
        float z = center.z;

        // Shield-like outline: flat top, straight sides, pointed bottom.
        Vector3[] points =
        {
            new Vector3(center.x - hx, center.y + hy, z),
            new Vector3(center.x + hx, center.y + hy, z),
            new Vector3(center.x + hx, center.y - hy * 0.2f, z),
            new Vector3(center.x + hx * 0.55f, center.y - hy * 0.75f, z),
            new Vector3(center.x, center.y - hy - outlinePadding, z),
            new Vector3(center.x - hx * 0.55f, center.y - hy * 0.75f, z),
            new Vector3(center.x - hx, center.y - hy * 0.2f, z),
        };

        line.positionCount = points.Length;
        line.SetPositions(points);
        line.enabled = false;
        return line;
    }

    Light CreateGlowLight()
    {
        GameObject go = new GameObject("CrestGlowLight");
        go.transform.SetParent(transform, false);

        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = lightRange;
        light.intensity = 0f;
        light.shadows = LightShadows.None;
        light.enabled = false;
        return light;
    }

    void PositionGlowLight()
    {
        GetLocalOutlineBounds(out Vector3 center, out Vector3 size);
        Vector3 worldCenter = transform.TransformPoint(center);

        // Sit slightly in front of the crest (toward the viewer) so it lights the shield and the cloth around it.
        Camera cam = Camera.main;
        Vector3 toViewer = cam != null
            ? (cam.transform.position - worldCenter).normalized
            : -transform.forward;

        glowLight.transform.position = worldCenter + toViewer * 0.25f;
    }

    void GetLocalOutlineBounds(out Vector3 center, out Vector3 size)
    {
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            center = box.center;
            size = box.size;
            return;
        }

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            center = meshFilter.sharedMesh.bounds.center;
            size = meshFilter.sharedMesh.bounds.size;
            return;
        }

        center = Vector3.zero;
        size = Vector3.one * 0.8f;
    }

    static Material sharedOutlineMaterial;

    Material GetOutlineMaterial()
    {
        if (outlineMaterial != null)
            return outlineMaterial;

        if (sharedOutlineMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            sharedOutlineMaterial = new Material(shader) { name = "CrestOutline (Runtime)" };
        }

        return sharedOutlineMaterial;
    }

    void CaptureOriginalState()
    {
        if (originalStateCaptured)
            return;

        originalLocalScale = transform.localScale;
        originalLocalPosition = transform.localPosition;
        originalStateCaptured = true;
    }

}
