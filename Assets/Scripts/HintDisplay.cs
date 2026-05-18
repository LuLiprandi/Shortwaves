using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HintDisplay : MonoBehaviour
{
    public static HintDisplay Instance { get; private set; }

    private TextMeshProUGUI hintText;
    private CanvasGroup     canvasGroup;
    private Coroutine       hideCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    public void ShowHint(string text)
    {
        if (hideCoroutine != null) { StopCoroutine(hideCoroutine); hideCoroutine = null; }
        hintText.text     = text;
        canvasGroup.alpha = 1f;
    }

    public void ShowHint(string text, float duration)
    {
        ShowHint(text);
        hideCoroutine = StartCoroutine(HideAfter(duration));
    }

    public void Hide()
    {
        if (hideCoroutine != null) { StopCoroutine(hideCoroutine); hideCoroutine = null; }
        canvasGroup.alpha = 0f;
        hintText.text     = string.Empty;
    }

    private IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        Hide();
        hideCoroutine = null;
    }

    private void BuildUI()
    {
        var canvas          = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        gameObject.AddComponent<GraphicRaycaster>();

        canvasGroup                = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha          = 0f;
        canvasGroup.interactable   = false;
        canvasGroup.blocksRaycasts = false;

        var textGO = new GameObject("HintText");
        textGO.transform.SetParent(transform, false);

        var rt       = textGO.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.88f);
        rt.anchorMax = new Vector2(0.9f, 0.97f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        hintText               = textGO.AddComponent<TextMeshProUGUI>();
        hintText.text          = string.Empty;
        hintText.fontSize      = 36f;
        hintText.color         = Color.white;
        hintText.alignment     = TextAlignmentOptions.Center;
        hintText.fontStyle     = FontStyles.Bold;
        hintText.outlineWidth  = 0.3f;
        hintText.outlineColor  = new Color32(0, 0, 0, 200);
        hintText.raycastTarget = false;
    }
}
