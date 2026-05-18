using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }

    private CameraFocusController focusController;
    private ChairInteractable     chairInteractable;
    private FirstPersonController playerController;
    private InteractionSystem     interactionSystem;

    private bool isPaused;

    private Canvas     canvas;
    private GameObject mainPanel;
    private GameObject savePanel;

    private readonly List<(RectTransform rt, Image img, System.Action action, Color normal, Color hover)> clickables = new();
    private List<(Image bg, TextMeshProUGUI label)> slotElements = new();

    private static readonly Color ColOverlay   = new Color(0f,    0f,    0f,    0.72f);
    private static readonly Color ColPanel     = new Color(0.08f, 0.07f, 0.04f, 0.97f);
    private static readonly Color ColBtn       = new Color(0.14f, 0.12f, 0.06f, 1f);
    private static readonly Color ColBtnHover  = new Color(0.26f, 0.22f, 0.10f, 1f);
    private static readonly Color ColBtnPress  = new Color(0.08f, 0.07f, 0.03f, 1f);
    private static readonly Color ColAccent    = new Color(1f,    0.75f, 0.25f, 1f);
    private static readonly Color ColText      = new Color(0.92f, 0.86f, 0.68f, 1f);
    private static readonly Color ColSlotBg    = new Color(0.12f, 0.10f, 0.05f, 1f);
    private static readonly Color ColSlotActive = new Color(0.22f, 0.17f, 0.04f, 1f);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    private void Start()
    {
        RefreshSceneReferences();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (isPaused) ForceResume();
        RefreshSceneReferences();
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (isPaused)
        {
            HandleManualClicks();

            if (Keyboard.current.escapeKey.wasPressedThisFrame
                && !DocumentInteractable.EscapeConsumedThisFrame)
            {
                DocumentInteractable.ConsumeEscape();
                Resume();
            }
            return;
        }

        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (DocumentInteractable.EscapeConsumedThisFrame) return;

        if (CanOpenPause())
        {
            DocumentInteractable.ConsumeEscape();
            Pause();
        }
    }

    private void HandleManualClicks()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        bool    clicked  = Mouse.current.leftButton.wasPressedThisFrame;
        bool    held     = Mouse.current.leftButton.isPressed;

        System.Action pendingAction = null;

        foreach (var (rt, img, action, normal, hover) in clickables)
        {
            if (!rt.gameObject.activeInHierarchy) { img.color = normal; continue; }

            bool over = RectTransformUtility.RectangleContainsScreenPoint(rt, mousePos);

            if (clicked && over)
            {
                img.color     = ColBtnPress;
                pendingAction = action;
            }
            else if (held && over)
                img.color = ColBtnPress;
            else
                img.color = over ? hover : normal;
        }

        pendingAction?.Invoke();
    }

    private bool CanOpenPause()
    {
        if (GameStateManager.Instance == null)                             return false;
        if (GameStateManager.Instance.IsCutsceneActive)                    return false;
        if (GameStateManager.Instance.IsBlockingUIOpen)                    return false;
        if (focusController   != null && focusController.IsFocused)        return false;
        if (chairInteractable != null && chairInteractable.IsSitting)      return false;
        return true;
    }

    private void Pause()
    {
        isPaused       = true;
        Time.timeScale = 0f;

        ShowMainPanel();
        canvas.enabled = true;

        if (playerController  != null) playerController.CanLook  = false;
        if (playerController  != null) playerController.CanMove  = false;
        if (interactionSystem != null) interactionSystem.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    private void Resume()
    {
        isPaused       = false;
        Time.timeScale = 1f;

        canvas.enabled = false;

        if (playerController  != null) playerController.CanLook  = true;
        if (playerController  != null) playerController.CanMove  = true;
        if (interactionSystem != null) interactionSystem.enabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void ForceResume()
    {
        isPaused       = false;
        Time.timeScale = 1f;
        if (canvas != null) canvas.enabled = false;
    }

    private void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        savePanel.SetActive(false);
    }

    private void ShowSavePanel()
    {
        mainPanel.SetActive(false);
        savePanel.SetActive(true);
        RefreshSavePanel();
    }

    private void RefreshSceneReferences()
    {
        focusController   = Object.FindFirstObjectByType<CameraFocusController>();
        chairInteractable = Object.FindFirstObjectByType<ChairInteractable>();
        playerController  = Object.FindFirstObjectByType<FirstPersonController>();
        interactionSystem = Object.FindFirstObjectByType<InteractionSystem>();
    }

    private void BuildUI()
    {
        canvas              = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        gameObject.AddComponent<GraphicRaycaster>();
        canvas.enabled = false;

        var overlay = MakeRect("Overlay", transform);
        overlay.AddComponent<Image>().color = ColOverlay;

        mainPanel = BuildMainPanel();
        savePanel = BuildSavePanel();
        savePanel.SetActive(false);
    }

    private GameObject BuildMainPanel()
    {
        var panel   = MakeRect("MainPanel", transform);
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.38f, 0.28f);
        panelRT.anchorMax = new Vector2(0.62f, 0.72f);
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;

        panel.AddComponent<Image>().color = ColPanel;

        var title   = MakeRect("Title", panel.transform);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0.78f);
        titleRT.anchorMax = new Vector2(1f, 0.97f);
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

        var titleTMP   = title.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "PAUSE";
        titleTMP.fontSize  = 42f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = ColAccent;
        titleTMP.alignment = TextAlignmentOptions.Center;

        var sep   = MakeRect("Sep", panel.transform);
        var sepRT = sep.GetComponent<RectTransform>();
        sepRT.anchorMin = new Vector2(0.08f, 0.75f);
        sepRT.anchorMax = new Vector2(0.92f, 0.76f);
        sepRT.offsetMin = sepRT.offsetMax = Vector2.zero;
        sep.AddComponent<Image>().color = new Color(ColAccent.r, ColAccent.g, ColAccent.b, 0.4f);

        MakePanelButton(panel.transform, "Continuer",  new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.68f), () => Resume());
        MakePanelButton(panel.transform, "Sauvegarde", new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.46f), () => ShowSavePanel());
        MakePanelButton(panel.transform, "Quitter",    new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.22f), () =>
        {
            ForceResume();
            SceneManager.LoadScene("MainMenu");
        });

        return panel;
    }

    private GameObject BuildSavePanel()
    {
        var panel   = MakeRect("SavePanel", transform);
        var panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.30f, 0.10f);
        panelRT.anchorMax = new Vector2(0.70f, 0.90f);
        panelRT.offsetMin = panelRT.offsetMax = Vector2.zero;

        panel.AddComponent<Image>().color = ColPanel;

        var title   = MakeRect("Title", panel.transform);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0f, 0.88f);
        titleRT.anchorMax = new Vector2(1f, 0.99f);
        titleRT.offsetMin = titleRT.offsetMax = Vector2.zero;

        var titleTMP   = title.AddComponent<TextMeshProUGUI>();
        titleTMP.text      = "SAUVEGARDES DE TEST";
        titleTMP.fontSize  = 24f;
        titleTMP.fontStyle = FontStyles.Bold;
        titleTMP.color     = ColAccent;
        titleTMP.alignment = TextAlignmentOptions.Center;

        slotElements.Clear();
        var   slots  = SaveSlotManager.Slots;
        float slotH  = 0.80f / slots.Length;
        float startY = 0.84f;

        for (int i = 0; i < slots.Length; i++)
        {
            float yMax    = startY - i * slotH;
            float yMin    = yMax - slotH + 0.01f;
            var   snapshot = slots[i];
            int   idx      = i;

            var slotGO = MakeRect($"Slot{i}", panel.transform);
            var slotRT = slotGO.GetComponent<RectTransform>();
            slotRT.anchorMin = new Vector2(0.04f, yMin);
            slotRT.anchorMax = new Vector2(0.96f, yMax);
            slotRT.offsetMin = slotRT.offsetMax = Vector2.zero;

            var slotBg = slotGO.AddComponent<Image>();
            slotBg.color = ColSlotBg;

            clickables.Add((slotRT, slotBg, () =>
            {
                ForceResume();
                SaveSlotManager.Apply(SaveSlotManager.Slots[idx]);
                SceneManager.LoadScene("SampleScene");
            }, ColSlotBg, ColBtnHover));

            var lblGO = MakeRect("Lbl", slotGO.transform);
            var lblRT = lblGO.GetComponent<RectTransform>();
            lblRT.anchorMin = new Vector2(0.03f, 0f);
            lblRT.anchorMax = new Vector2(0.72f, 1f);
            lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;

            var lbl               = lblGO.AddComponent<TextMeshProUGUI>();
            lbl.text              = $"{snapshot.Label}\n<size=11><color=#B38A3F>{snapshot.Description}</color></size>";
            lbl.fontSize          = 14f;
            lbl.color             = ColText;
            lbl.fontStyle         = FontStyles.Bold;
            lbl.alignment         = TextAlignmentOptions.Left;
            lbl.verticalAlignment = VerticalAlignmentOptions.Middle;
            lbl.raycastTarget     = false;

            var statusGO = MakeRect("Status", slotGO.transform);
            var statusRT = statusGO.GetComponent<RectTransform>();
            statusRT.anchorMin = new Vector2(0.74f, 0.15f);
            statusRT.anchorMax = new Vector2(0.98f, 0.85f);
            statusRT.offsetMin = statusRT.offsetMax = Vector2.zero;

            var statusTMP       = statusGO.AddComponent<TextMeshProUGUI>();
            statusTMP.text      = "CHARGER";
            statusTMP.fontSize  = 13f;
            statusTMP.color     = new Color(0.7f, 0.55f, 0.25f, 1f);
            statusTMP.fontStyle = FontStyles.Bold;
            statusTMP.alignment = TextAlignmentOptions.Center;
            statusTMP.raycastTarget = false;

            slotElements.Add((slotBg, statusTMP));
        }

        MakePanelButton(panel.transform, "← Retour",
            new Vector2(0.04f, 0.01f), new Vector2(0.96f, 0.08f),
            () => ShowMainPanel());

        return panel;
    }

    private void RefreshSavePanel()
    {
        int currentDay        = SaveSlotManager.GetCurrentDay();
        int currentDay2Choice = PlayerPrefs.GetInt("gsm_day2choice", 0);

        for (int i = 0; i < slotElements.Count && i < SaveSlotManager.Slots.Length; i++)
        {
            var  s        = SaveSlotManager.Slots[i];
            bool isActive = s.Day == currentDay
                && (s.Day < 4 || s.Day2Choice == currentDay2Choice);

            slotElements[i].bg.color   = isActive ? ColSlotActive : ColSlotBg;
            slotElements[i].label.text = isActive ? "ACTIF" : "CHARGER";
            slotElements[i].label.color = isActive
                ? new Color(1f, 0.65f, 0f, 1f)
                : new Color(0.7f, 0.55f, 0.25f, 1f);
        }
    }

    private static GameObject MakeRect(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }

    private void MakePanelButton(Transform parent, string label,
        Vector2 anchorMin, Vector2 anchorMax, System.Action onClick)
    {
        var go = MakeRect(label, parent);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var img   = go.AddComponent<Image>();
        img.color = ColBtn;

        clickables.Add((rt, img, onClick, ColBtn, ColBtnHover));

        var lblGO = MakeRect("Lbl", go.transform);
        var lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero;
        lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = lblRT.offsetMax = Vector2.zero;

        var tmp           = lblGO.AddComponent<TextMeshProUGUI>();
        tmp.text          = label;
        tmp.fontSize      = 22f;
        tmp.color         = ColText;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
    }
}
