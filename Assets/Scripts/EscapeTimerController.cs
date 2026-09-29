using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EscapeTimerController : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField, Min(1f)] private float duration = 180f;
    [SerializeField, Min(1f)] private float dangerDuration = 30f;

    [Header("Flow")]
    [SerializeField] private GameFlowUIController gameFlow;

    [Header("Hourglass HUD")]
    [SerializeField] private GameObject timerHUD;
    [SerializeField] private Image upperSand;
    [SerializeField] private Image lowerSand;
    [SerializeField] private GameObject sandStream;
    [SerializeField] private TMP_Text timerPausedText;

    [Tooltip("Optional. If empty, a label is created above the hourglass at runtime.")]
    [SerializeField] private TMP_Text timeRemainingText;
    [SerializeField] private Vector2 timeRemainingTextPosition = new Vector2(0f, 104f);
    [SerializeField, Min(1f)] private float timeRemainingFontSize = 22f;

    [Header("Final 30 Seconds")]
    [SerializeField] private GameObject dangerOverlay;
    [SerializeField] private Image[] dangerEdgeImages;
    [SerializeField] private Image dangerCenterHaze;

    private float remainingTime;
    private bool timerRunning;
    private bool timerPaused;
    private bool timeoutTriggered;

    public float RemainingTime => remainingTime;
    public bool TimerRunning => timerRunning;
    public bool TimerPaused => timerPaused;

    void Awake()
    {
        if (gameFlow == null)
            gameFlow = GetComponent<GameFlowUIController>();

        if (gameFlow == null)
            gameFlow = FindFirstObjectByType<GameFlowUIController>();

        ResetVisualState();
    }

    void Update()
    {
        if (!timerRunning)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.pKey.wasPressedThisFrame)
            ToggleTimerPause();

        if (timerPaused)
            return;

        remainingTime = Mathf.Max(0f, remainingTime - Time.unscaledDeltaTime);
        UpdateVisuals();

        if (remainingTime > 0f || timeoutTriggered)
            return;

        timeoutTriggered = true;
        timerRunning = false;
        timerPaused = false;

        if (timerHUD != null)
            timerHUD.SetActive(false);

        if (timerPausedText != null)
            timerPausedText.gameObject.SetActive(false);

        SetDangerProgress(1f);

        if (gameFlow != null)
            gameFlow.ShowTimeoutFailure();
    }

    public void StartTimer()
    {
        remainingTime = duration;
        timerRunning = true;
        timerPaused = false;
        timeoutTriggered = false;

        if (timerHUD != null)
            timerHUD.SetActive(true);

        if (timerPausedText != null)
            timerPausedText.gameObject.SetActive(false);

        if (dangerOverlay != null)
            dangerOverlay.SetActive(false);

        UpdateVisuals();
    }

    public void StopTimer()
    {
        timerRunning = false;
        timerPaused = false;

        if (timerHUD != null)
            timerHUD.SetActive(false);

        if (timerPausedText != null)
            timerPausedText.gameObject.SetActive(false);

        if (dangerOverlay != null)
            dangerOverlay.SetActive(false);
    }

    public void StopTimerForTimeout()
    {
        timerRunning = false;
        timerPaused = false;

        if (timerHUD != null)
            timerHUD.SetActive(false);

        if (timerPausedText != null)
            timerPausedText.gameObject.SetActive(false);

        SetDangerProgress(1f);
    }

    public void ToggleTimerPause()
    {
        if (!timerRunning || timeoutTriggered)
            return;

        timerPaused = !timerPaused;

        if (timerPausedText != null)
            timerPausedText.gameObject.SetActive(timerPaused);

        if (sandStream != null)
            sandStream.SetActive(!timerPaused && remainingTime > 0f);
    }

    void ResetVisualState()
    {
        remainingTime = duration;
        timerRunning = false;
        timerPaused = false;
        timeoutTriggered = false;

        if (timerHUD != null)
            timerHUD.SetActive(false);

        if (timerPausedText != null)
            timerPausedText.gameObject.SetActive(false);

        if (dangerOverlay != null)
            dangerOverlay.SetActive(false);

        EnsureTimeRemainingText();
        UpdateHourglass(1f);
        UpdateTimeRemainingText();
        SetDangerProgress(0f);
    }

    void EnsureTimeRemainingText()
    {
        if (timeRemainingText != null || timerHUD == null)
            return;

        GameObject go = new GameObject("TimeRemainingText", typeof(RectTransform));
        go.transform.SetParent(timerHUD.transform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = timeRemainingTextPosition;
        rect.sizeDelta = new Vector2(160f, 30f);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = timeRemainingFontSize;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;

        // Match the existing HUD text styling.
        if (timerPausedText != null)
        {
            text.font = timerPausedText.font;
            text.color = timerPausedText.color;
        }

        timeRemainingText = text;
    }

    void UpdateTimeRemainingText()
    {
        if (timeRemainingText == null)
            return;

        int totalSeconds = Mathf.CeilToInt(remainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timeRemainingText.text = $"{minutes}:{seconds:00}";
    }

    void UpdateVisuals()
    {
        float normalizedTime = duration > 0f
            ? Mathf.Clamp01(remainingTime / duration)
            : 0f;

        UpdateHourglass(normalizedTime);
        UpdateTimeRemainingText();

        float dangerProgress = remainingTime <= dangerDuration
            ? 1f - Mathf.Clamp01(remainingTime / dangerDuration)
            : 0f;

        SetDangerProgress(dangerProgress);
    }

    void UpdateHourglass(float normalizedTime)
    {
        if (upperSand != null)
            upperSand.fillAmount = normalizedTime;

        if (lowerSand != null)
            lowerSand.fillAmount = 1f - normalizedTime;

        if (sandStream != null)
            sandStream.SetActive(
                timerRunning &&
                !timerPaused &&
                remainingTime > 0f
            );
    }

    void SetDangerProgress(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (dangerOverlay != null)
            dangerOverlay.SetActive(progress > 0f);

        if (dangerCenterHaze != null)
        {
            float centerProgress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(0.82f, 1f, progress)
            );
            Color centerColor = dangerCenterHaze.color;
            centerColor.a = 0.58f * centerProgress;
            dangerCenterHaze.color = centerColor;
        }

        if (dangerEdgeImages == null)
            return;

        for (int i = 0; i < dangerEdgeImages.Length; i++)
        {
            Image image = dangerEdgeImages[i];

            if (image == null)
                continue;

            int layer = i / 4;
            int side = i % 4;
            float layerStart = layer * 0.2f;
            float layerProgress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.InverseLerp(layerStart, layerStart + 0.45f, progress)
            );

            float targetDepth = Mathf.Lerp(0.16f, 0.5f, layer / 2f);
            float depth = targetDepth * layerProgress;
            SetEdgeAnchors(image.rectTransform, side, depth);

            Color color = image.color;
            float maximumAlpha = Mathf.Lerp(0.38f, 0.62f, layer / 2f);
            float pulse = 0.96f + Mathf.Sin(Time.unscaledTime * 3.2f + i) * 0.04f;
            color.a = maximumAlpha * layerProgress * pulse;
            image.color = color;
        }
    }

    static void SetEdgeAnchors(RectTransform edge, int side, float depth)
    {
        depth = Mathf.Clamp01(depth);

        switch (side)
        {
            case 0:
                edge.anchorMin = Vector2.zero;
                edge.anchorMax = new Vector2(depth, 1f);
                break;
            case 1:
                edge.anchorMin = new Vector2(1f - depth, 0f);
                edge.anchorMax = Vector2.one;
                break;
            case 2:
                edge.anchorMin = Vector2.zero;
                edge.anchorMax = new Vector2(1f, depth);
                break;
            default:
                edge.anchorMin = new Vector2(0f, 1f - depth);
                edge.anchorMax = Vector2.one;
                break;
        }

        edge.offsetMin = Vector2.zero;
        edge.offsetMax = Vector2.zero;
    }
}
