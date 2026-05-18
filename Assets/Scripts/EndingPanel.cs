using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class EndingPanel : MonoBehaviour
{
    public static EndingPanel Instance { get; private set; }

    private Canvas      canvas;
    private Image       endingImage;
    private CanvasGroup endingGroup;
    private AudioSource audioSource;

    private bool   clickable;
    private Action onClickCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void Update()
    {
        if (!clickable) return;
        if (Input.anyKeyDown)
        {
            clickable = false;
            onClickCallback?.Invoke();
        }
    }

    public void ShowImage(Texture2D texture, AudioClip sfx, float fadeInDuration, Action onClicked)
    {
        if (texture == null)
        {
            onClicked?.Invoke();
            return;
        }

        onClickCallback = onClicked;

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);

        endingImage.sprite         = sprite;
        endingImage.preserveAspect = true;

        endingGroup.alpha          = 0f;
        endingGroup.blocksRaycasts = true;
        endingGroup.interactable   = false;
        canvas.enabled             = true;

        if (sfx != null)
        {
            audioSource.clip   = sfx;
            audioSource.loop   = true;
            audioSource.volume = 1f;
            audioSource.Play();
        }

        StartCoroutine(FadeInRoutine(fadeInDuration));
    }

    public void Hide()
    {
        clickable          = false;
        canvas.enabled     = false;
        endingGroup.alpha  = 0f;
        if (audioSource.isPlaying) audioSource.Stop();
    }

    private IEnumerator FadeInRoutine(float duration)
    {
        yield return new WaitForSeconds(0.3f);
        clickable = true;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed           += Time.deltaTime;
            endingGroup.alpha  = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }
        endingGroup.alpha = 1f;
    }

    private void BuildUI()
    {
        canvas              = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvas.enabled      = false;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        gameObject.AddComponent<GraphicRaycaster>();

        var bgGO = new GameObject("Background");
        bgGO.transform.SetParent(transform, false);
        var bgRT       = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
        var bgImg      = bgGO.AddComponent<Image>();
        bgImg.color         = Color.black;
        bgImg.raycastTarget = true;

        var imgGO = new GameObject("EndingImage");
        imgGO.transform.SetParent(transform, false);
        var imgRT       = imgGO.AddComponent<RectTransform>();
        imgRT.anchorMin = Vector2.zero;
        imgRT.anchorMax = Vector2.one;
        imgRT.offsetMin = imgRT.offsetMax = Vector2.zero;
        endingImage     = imgGO.AddComponent<Image>();
        endingImage.raycastTarget = false;

        endingGroup                = imgGO.AddComponent<CanvasGroup>();
        endingGroup.alpha          = 0f;
        endingGroup.interactable   = false;
        endingGroup.blocksRaycasts = false;

        audioSource             = gameObject.AddComponent<AudioSource>();
        audioSource.loop        = true;
        audioSource.volume      = 1f;
        audioSource.playOnAwake = false;
    }
}
