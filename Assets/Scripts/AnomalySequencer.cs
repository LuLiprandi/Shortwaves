using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class AnomalySequencer : MonoBehaviour
{
    [Header("Radio")]
    [Tooltip("The RadioSystem to drive for the anomaly broadcast.")]
    [SerializeField] private RadioSystem radioSystem;

    [Tooltip("Target frequency for the anomaly broadcast (MHz). Knob will snap here.")]
    [SerializeField] private float anomalyFrequencyMHz = 103f;

    [Tooltip("Anomaly voice clip played once at the anomaly frequency.")]
    [SerializeField] private AudioClip anomalyVoiceClip;

    [Tooltip("Optional subtitles for the anomaly voice clip.")]
    [SerializeField] private SubtitleEntry[] anomalySubtitles = System.Array.Empty<SubtitleEntry>();

    [Header("Flicker — post-message")]
    [Tooltip("AudioClip for radio crackle during screen flicker. Uses the active station ProximityClip if left empty.")]
    [SerializeField] private AudioClip flickerStaticClip;

    [Tooltip("Number of black screen flashes after the message.")]
    [SerializeField] private int flickerCount = 3;

    [Tooltip("Duration of each black flash (seconds).")]
    [SerializeField] private float flickerOnDuration = 0.12f;

    [Tooltip("Duration of the clear gap between flashes (seconds).")]
    [SerializeField] private float flickerOffDuration = 0.1f;

    [Header("Decoder — post-message")]
    [Tooltip("Indices (0-based) of code slots to hide after the voice clip ends. " +
             "Leave empty to keep all codes visible.")]
    [SerializeField] private int[] slotsToHideAfterMessage = System.Array.Empty<int>();

    [Header("AnomalieJ1 Overlay")]
    [Tooltip("Sprite shown fullscreen after the anomaly broadcast.")]
    [SerializeField] private Sprite anomalieJ1Sprite;

    [Tooltip("Tint for the overlay background.")]
    [SerializeField] private Color overlayBackgroundColor = new Color(0f, 0f, 0f, 0.85f);

    private const float FrequencyMin = 88f;
    private const float FrequencyMax = 108f;

    private bool sequencePlayed;
    private GameObject overlayRoot;

    private FirstPersonController playerController;
    private InteractionSystem     interactionSystem;

    private void Awake()
    {
        playerController  = FindFirstObjectByType<FirstPersonController>();
        interactionSystem = FindFirstObjectByType<InteractionSystem>();
    }

    public void TriggerSequence()
    {
        if (sequencePlayed) return;
        sequencePlayed = true;
        StartCoroutine(AnomalyRoutine());
    }

    private IEnumerator AnomalyRoutine()
    {
        yield return new WaitForSeconds(0.5f);

        float flickerTotalDuration = flickerCount * (flickerOnDuration + flickerOffDuration);
        if (radioSystem != null)
            radioSystem.PlayStaticBurst(flickerTotalDuration, flickerStaticClip);

        yield return StartCoroutine(FlickerRoutine());

        float normalized = Mathf.InverseLerp(FrequencyMin, FrequencyMax, anomalyFrequencyMHz);

        if (radioSystem != null)
        {
            bool broadcastDone = false;
            radioSystem.OnAnomalyBroadcastComplete += () => broadcastDone = true;

            radioSystem.TriggerAnomalyBroadcast(anomalyVoiceClip, normalized,
                anomalySubtitles.Length > 0 ? anomalySubtitles : null);

            yield return new WaitUntil(() => broadcastDone);
        }
        else if (anomalyVoiceClip != null)
        {
            yield return new WaitForSeconds(anomalyVoiceClip.length);
        }

        if (slotsToHideAfterMessage != null && slotsToHideAfterMessage.Length > 0)
        {
            var panel = JournalManager.Instance?.GetJournalPanel();
            panel?.HideSlots(slotsToHideAfterMessage);
        }

        if (radioSystem != null)
            radioSystem.PlayStaticBurst(flickerTotalDuration, flickerStaticClip);

        yield return StartCoroutine(FlickerRoutine());

        ShowOverlay();
        yield return new WaitUntil(() => overlayRoot == null || !overlayRoot.activeSelf);

        if (radioSystem != null)
            radioSystem.ReleaseAfterAnomaly();
        else
            RestorePlayerControl();

        GameStateManager.Instance?.TriggerAnomaly();

        yield return new WaitForSeconds(0.3f);
        JournalManager.Instance?.OpenOnPostAnomalyThoughts();
    }

    private IEnumerator FlickerRoutine()
    {
        var fader = ScreenFader.Instance;
        if (fader == null) yield break;

        for (int i = 0; i < flickerCount; i++)
        {
            fader.SetBlack();
            yield return new WaitForSeconds(flickerOnDuration);
            fader.SetClear();
            yield return new WaitForSeconds(flickerOffDuration);
        }
    }

    private void ShowOverlay()
    {
        overlayRoot = new GameObject("AnomalieJ1_Overlay");

        var canvas          = overlayRoot.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        overlayRoot.AddComponent<CanvasScaler>();
        overlayRoot.AddComponent<GraphicRaycaster>();

        var bgGO  = new GameObject("Background");
        bgGO.transform.SetParent(overlayRoot.transform, false);
        var bgRT  = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        bgGO.AddComponent<Image>().color = overlayBackgroundColor;

        if (anomalieJ1Sprite != null)
        {
            var imgGO  = new GameObject("AnomalieJ1");
            imgGO.transform.SetParent(overlayRoot.transform, false);
            var imgRT  = imgGO.AddComponent<RectTransform>();
            imgRT.anchorMin = new Vector2(0.1f, 0.1f);
            imgRT.anchorMax = new Vector2(0.9f, 0.9f);
            imgRT.offsetMin = imgRT.offsetMax = Vector2.zero;
            var img         = imgGO.AddComponent<Image>();
            img.sprite         = anomalieJ1Sprite;
            img.preserveAspect = true;
        }

        var hintGO  = new GameObject("DismissHint");
        hintGO.transform.SetParent(overlayRoot.transform, false);
        var hintRT              = hintGO.AddComponent<RectTransform>();
        hintRT.anchorMin        = new Vector2(0f, 0f);
        hintRT.anchorMax        = new Vector2(1f, 0f);
        hintRT.pivot            = new Vector2(0.5f, 0f);
        hintRT.anchoredPosition = new Vector2(0f, 24f);
        hintRT.sizeDelta        = new Vector2(0f, 40f);
        var hintTxt             = hintGO.AddComponent<TMPro.TextMeshProUGUI>();
        hintTxt.text      = "[ Échap ] ou cliquer pour continuer";
        hintTxt.fontSize  = 16f;
        hintTxt.color     = new Color(1f, 1f, 1f, 0.65f);
        hintTxt.alignment = TMPro.TextAlignmentOptions.Center;

        var btnGO  = new GameObject("ClickDismiss");
        btnGO.transform.SetParent(overlayRoot.transform, false);
        var btnRT  = btnGO.AddComponent<RectTransform>();
        btnRT.anchorMin = Vector2.zero;
        btnRT.anchorMax = Vector2.one;
        btnRT.offsetMin = btnRT.offsetMax = Vector2.zero;
        var btnImg      = btnGO.AddComponent<Image>();
        btnImg.color    = Color.clear;
        var btn         = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(DismissOverlay);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        StartCoroutine(WaitForEscapeDismiss());
    }

    private IEnumerator WaitForEscapeDismiss()
    {
        while (overlayRoot != null && overlayRoot.activeSelf)
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                DismissOverlay();
                yield break;
            }
            yield return null;
        }
    }

    private void DismissOverlay()
    {
        if (overlayRoot == null) return;
        overlayRoot.SetActive(false);
        Destroy(overlayRoot, 0.1f);
    }

    private void RestorePlayerControl()
    {
        if (playerController  != null) playerController.CanMove  = true;
        if (interactionSystem != null) interactionSystem.enabled  = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }
}
