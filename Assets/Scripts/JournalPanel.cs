using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Immersive notebook UI — leather cover, spiral spine, cream ruled pages, two tabs.</summary>
public class JournalPanel : MonoBehaviour
{
    // Palette
    private static readonly Color ColOverlay    = new(0f, 0f, 0f, 0.78f);
    private static readonly Color ColShadow     = new(0.04f, 0.02f, 0.01f, 0.85f);
    private static readonly Color ColCover      = new(0.14f, 0.10f, 0.06f, 1f);
    private static readonly Color ColCoverEdge  = new(0.09f, 0.06f, 0.03f, 1f);
    private static readonly Color ColSpiral     = new(0.28f, 0.22f, 0.12f, 1f);
    private static readonly Color ColPage       = new(0.97f, 0.94f, 0.86f, 1f);
    private static readonly Color ColRule       = new(0.80f, 0.74f, 0.58f, 0.55f);
    private static readonly Color ColInk        = new(0.10f, 0.07f, 0.03f, 1f);
    private static readonly Color ColInkDim     = new(0.40f, 0.32f, 0.18f, 1f);
    private static readonly Color ColTabInact   = new(0.88f, 0.84f, 0.73f, 1f);
    private static readonly Color ColSlotBg     = new(0.91f, 0.87f, 0.77f, 1f);
    private static readonly Color ColSlotActive = new(0.78f, 0.72f, 0.56f, 1f);
    private static readonly Color ColSlotLine   = new(0.28f, 0.20f, 0.10f, 1f);

    // Layout — notebook fills ~98 % of 1920×1080 reference canvas
    private const float NW = 1880f, NH = 1055f, SpineW = 80f, CoverPad = 18f;
    private const float HeaderH = 72f, TabH = 48f, DivH = 1.5f;
    private const float ContentTop = HeaderH + DivH + TabH + DivH;
    private const int Holes = 15, SlotMaxLen = 3, RuleLines = 22;

    // Prefs — keyed per day so each day keeps its own state
    private const string PrefCodeKey = "jrn_code_d", PrefMsgKey = "jrn_msg_d";

    // Refs
    [SerializeField] private JournalConfig journalConfig;

    private TextMeshProUGUI dayTitleTmp, thoughtsTmp, journalSectionLabel;
    private TextMeshProUGUI journalTabLabel, decoderTabLabel;
    private GameObject journalContent, decoderContent;
    private Image journalTabBg, decoderTabBg;
    private GameObject decoderTabGO;          // the DECODAGE tab button — shown/hidden per day
    private Transform  slotsRowTransform;     // rebuilt when slot count changes

    private int      currentDay;
    private int      activeSlotCount;
    private string[] slotValues = new string[6];
    private TextMeshProUGUI[] slotTexts;
    private Image[]           slotBgs;
    private int activeSlot;
    private TMP_InputField messageField;

    private void Awake() { BuildUI(); gameObject.SetActive(false); }

    private void Update()
    {
        if (decoderContent == null || !decoderContent.activeSelf) return;
        if (messageField != null && messageField.isFocused) return;
        HandleSlotInput();
    }

    /// <summary>Shows the journal for a given day. Decoder tab visibility and slot count come from JournalConfig.</summary>
    public void Show(int day, string thoughts)
    {
        currentDay = day;
        dayTitleTmp.text          = "Carnet  -  Jour " + day;
        journalSectionLabel.text  = "Jour " + day + " :";
        thoughtsTmp.text          = thoughts;

        bool hasDecoder = journalConfig != null ? journalConfig.HasDecoder(day) : day <= 3;
        int  slotCount  = journalConfig != null ? journalConfig.SlotCount(day)  : 6;

        decoderTabGO.SetActive(hasDecoder);

        if (hasDecoder)
        {
            if (slotCount != activeSlotCount)
                RebuildSlots(slotCount);
            LoadDecoder();
        }

        SetTab(true);
        gameObject.SetActive(true);
    }

    /// <summary>Persists decoder state for the current day and hides the panel.</summary>
    public void Hide() { SaveDecoder(); gameObject.SetActive(false); }

    /// <summary>Updates thoughts text while panel is open.</summary>
    public void UpdateThoughts(string t) => thoughtsTmp.text = t;

    // ── Build ─────────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        var c = gameObject.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 20;
        var sc = gameObject.AddComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        sc.referenceResolution = new Vector2(1280f, 720f); sc.matchWidthOrHeight = 0.5f;
        gameObject.AddComponent<GraphicRaycaster>();

        Stretch(MakeImg("Overlay", transform, ColOverlay).GetComponent<RectTransform>());

        // Shadow — stretched behind notebook with slight offset
        var shRT = MakeImg("Shadow", transform, ColShadow).GetComponent<RectTransform>();
        shRT.anchorMin = Vector2.zero; shRT.anchorMax = Vector2.one;
        shRT.offsetMin = new Vector2(-9f, -9f); shRT.offsetMax = new Vector2(9f, 9f);

        // Notebook — fills entire canvas with a small inset margin
        var nb = MakeGO("Notebook", transform);
        var nbRT = nb.AddComponent<RectTransform>();
        nbRT.anchorMin = Vector2.zero; nbRT.anchorMax = Vector2.one;
        nbRT.offsetMin = new Vector2(6f, 6f); nbRT.offsetMax = new Vector2(-6f, -6f);
        nb.AddComponent<Image>().color = ColCover;
        BuildCoverBorder(nb.transform);
        BuildSpine(nb.transform);

        var pg = MakeGO("Page", nb.transform);
        var pgRT = pg.AddComponent<RectTransform>();
        pgRT.anchorMin = Vector2.zero; pgRT.anchorMax = Vector2.one;
        pgRT.offsetMin = new Vector2(SpineW + CoverPad, CoverPad);
        pgRT.offsetMax = new Vector2(-CoverPad, -CoverPad);
        pg.AddComponent<Image>().color = ColPage;
        BuildRules(pg.transform);
        BuildPage(pg.transform);
    }

    private static void BuildCoverBorder(Transform p)
    {
        var img = MakeImg("Border", p, ColCoverEdge);
        var rt = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(10f, 10f); rt.offsetMax = new Vector2(-10f, -10f);
        img.color = new Color(ColCoverEdge.r, ColCoverEdge.g, ColCoverEdge.b, 0.35f);
    }

    private static void BuildSpine(Transform p)
    {
        var s = MakeGO("Spine", p);
        var sRT = s.AddComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 0f); sRT.anchorMax = new Vector2(0f, 1f);
        sRT.pivot = new Vector2(0f, 0.5f); sRT.sizeDelta = new Vector2(SpineW, 0f); sRT.anchoredPosition = Vector2.zero;
        s.AddComponent<Image>().color = ColCoverEdge;

        var eRT = MakeImg("Edge", s.transform, ColSpiral).GetComponent<RectTransform>();
        eRT.anchorMin = new Vector2(1f, 0.01f); eRT.anchorMax = new Vector2(1f, 0.99f);
        eRT.pivot = new Vector2(1f, 0.5f); eRT.sizeDelta = new Vector2(3f, 0f); eRT.anchoredPosition = Vector2.zero;

        var knob = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        for (int i = 0; i < Holes; i++)
            BuildHole(s.transform, knob, i, 0.95f - i / (float)(Holes - 1) * 0.90f);
    }

    private static void BuildHole(Transform p, Sprite k, int idx, float y)
    {
        var r = MakeImg("R" + idx, p, ColSpiral); r.sprite = k; r.type = Image.Type.Simple;
        var rRT = r.GetComponent<RectTransform>();
        rRT.anchorMin = rRT.anchorMax = new Vector2(0.5f, y); rRT.pivot = Vector2.one * 0.5f;
        rRT.sizeDelta = new Vector2(26f, 26f); rRT.anchoredPosition = Vector2.zero;

        var h = MakeImg("H" + idx, p, ColPage); h.sprite = k; h.type = Image.Type.Simple;
        var hRT = h.GetComponent<RectTransform>();
        hRT.anchorMin = hRT.anchorMax = new Vector2(0.5f, y); hRT.pivot = Vector2.one * 0.5f;
        hRT.sizeDelta = new Vector2(14f, 14f); hRT.anchoredPosition = Vector2.zero;
    }

    private static void BuildRules(Transform p)
    {
        // Reference page height at 1280×720: 720 - 2*CoverPad = 684
        const float PageRefH = 720f - CoverPad * 2f;
        float topFrac  = (ContentTop + 14f) / PageRefH;
        float botFrac  = 24f / PageRefH;
        float available = 1f - topFrac - botFrac;
        for (int i = 0; i < RuleLines; i++)
        {
            float yAnchor = 1f - topFrac - available * (i / (float)(RuleLines - 1));
            var rt = MakeImg("Rl" + i, p, ColRule).GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, yAnchor);
            rt.anchorMax = new Vector2(1f, yAnchor);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
        }
    }

    private void BuildPage(Transform p)
    {
        // Header
        var h = MakeGO("Hdr", p); var hRT = h.AddComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0f, 1f); hRT.anchorMax = new Vector2(1f, 1f);
        hRT.pivot = new Vector2(0.5f, 1f); hRT.sizeDelta = new Vector2(0f, HeaderH); hRT.anchoredPosition = Vector2.zero;

        dayTitleTmp = MakeTMP("Title", h.transform, "Carnet  -  Jour 1", 26f, ColInk, FontStyles.Bold);
        var tRT = dayTitleTmp.GetComponent<RectTransform>();
        Stretch(tRT); tRT.offsetMin = new Vector2(36f, 0f); tRT.offsetMax = new Vector2(-64f, 0f);
        dayTitleTmp.alignment = TextAlignmentOptions.MidlineLeft; dayTitleTmp.characterSpacing = 1.5f;

        var mRT = MakeImg("Margin", h.transform, new Color(0.75f, 0.25f, 0.15f, 0.35f)).GetComponent<RectTransform>();
        mRT.anchorMin = new Vector2(0f, 0f); mRT.anchorMax = new Vector2(0f, 1f);
        mRT.pivot = new Vector2(0f, 0.5f); mRT.sizeDelta = new Vector2(2f, 0f); mRT.anchoredPosition = new Vector2(28f, 0f);

        // Close button
        var cGO = MakeGO("Close", h.transform); var cRT = cGO.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(1f, 0f); cRT.anchorMax = Vector2.one;
        cRT.pivot = new Vector2(1f, 0.5f); cRT.sizeDelta = new Vector2(58f, 0f);
        var cBg = cGO.AddComponent<Image>(); cBg.color = Color.clear;
        var cBtn = cGO.AddComponent<Button>(); cBtn.targetGraphic = cBg;
        var noNav = Navigation.defaultNavigation; noNav.mode = Navigation.Mode.None; cBtn.navigation = noNav;
        cBtn.onClick.AddListener(() => JournalManager.Instance?.Close());
        var xl = MakeTMP("X", cGO.transform, "x", 26f, ColInkDim);
        xl.alignment = TextAlignmentOptions.Center; Stretch(xl.GetComponent<RectTransform>());

        HRule("HR1", p, HeaderH);

        // Tabs
        var tb = MakeGO("Tabs", p); var tbRT = tb.AddComponent<RectTransform>();
        tbRT.anchorMin = new Vector2(0f, 1f); tbRT.anchorMax = new Vector2(1f, 1f);
        tbRT.pivot = new Vector2(0.5f, 1f); tbRT.sizeDelta = new Vector2(0f, TabH);
        tbRT.anchoredPosition = new Vector2(0f, -(HeaderH + DivH));
        var hlg = tb.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(32, 32, 0, 0); hlg.spacing = 8f;
        hlg.childControlWidth = false; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

        MakeTab("TabJ", tb.transform, "JOURNAL",  true,  out journalTabBg, out journalTabLabel, out _);
        MakeTab("TabD", tb.transform, "DECODAGE", false, out decoderTabBg, out decoderTabLabel, out decoderTabGO);

        HRule("HR2", p, ContentTop - DivH);
        journalContent = BuildJournalTab(p);
        decoderContent = BuildDecoderTab(p);
    }

    private void MakeTab(string name, Transform p, string label, bool isJ,
        out Image bg, out TextMeshProUGUI lbl, out GameObject tabGO)
    {
        var go = MakeGO(name, p); go.AddComponent<RectTransform>();
        tabGO = go;
        bg = go.AddComponent<Image>(); bg.color = ColTabInact;
        go.AddComponent<LayoutElement>().preferredWidth = 200f;
        var btn = go.AddComponent<Button>();
        var nav = Navigation.defaultNavigation; nav.mode = Navigation.Mode.None;
        btn.navigation = nav; btn.targetGraphic = bg;
        btn.onClick.AddListener(() => SetTab(isJ));
        lbl = MakeTMP("L", go.transform, label, 13f, ColInkDim, FontStyles.Bold);
        lbl.alignment = TextAlignmentOptions.Center; lbl.characterSpacing = 2.5f;
        Stretch(lbl.GetComponent<RectTransform>());
    }

    private GameObject BuildJournalTab(Transform p)
    {
        var root = MakeGO("JournalContent", p);
        var rRT = root.AddComponent<RectTransform>();
        rRT.anchorMin = Vector2.zero; rRT.anchorMax = Vector2.one;
        rRT.offsetMin = Vector2.zero; rRT.offsetMax = new Vector2(0f, -ContentTop);

        // Simple VLG — no ScrollRect, no ContentSizeFitter, no layout conflicts
        var vlg = root.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(52, 52, 40, 40); vlg.spacing = 12f;
        vlg.childControlWidth  = true;  vlg.childControlHeight  = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        journalSectionLabel = MakeTMP("Sec", root.transform, "Jour 1 :", 14f, ColInkDim,
            FontStyles.Bold | FontStyles.Italic);
        journalSectionLabel.alignment = TextAlignmentOptions.TopLeft;
        journalSectionLabel.characterSpacing = 1f;
        journalSectionLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        var sepGO = MakeGO("Sep", root.transform);
        sepGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 1.5f);
        sepGO.AddComponent<Image>().color = ColRule;
        sepGO.AddComponent<LayoutElement>().preferredHeight = 1.5f;

        // Thoughts — preferred height driven by TMP text content, no LayoutElement override
        thoughtsTmp = MakeTMP("Thoughts", root.transform, "", 22f, ColInk, FontStyles.Italic);
        thoughtsTmp.alignment = TextAlignmentOptions.TopLeft;
        thoughtsTmp.textWrappingMode = TextWrappingModes.Normal;
        thoughtsTmp.lineSpacing = 4f;
        thoughtsTmp.characterSpacing = 0.3f;

        return root;
    }

    private GameObject BuildDecoderTab(Transform p)
    {
        var root = MakeGO("DecoderContent", p);
        var rRT = root.AddComponent<RectTransform>();
        rRT.anchorMin = Vector2.zero; rRT.anchorMax = Vector2.one;
        rRT.offsetMin = Vector2.zero; rRT.offsetMax = new Vector2(0f, -ContentTop);

        var vlg = root.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(52, 52, 36, 36); vlg.spacing = 20f;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        SecLabel("Code", root.transform);
        BuildSlots(root.transform, 6); // initial count; RebuildSlots() updates it when Show() changes it

        var hint = MakeTMP("Hint", root.transform,
            "<- ->  naviguer     chiffres: saisir     Backspace: effacer", 11f, ColInkDim, FontStyles.Italic);
        hint.alignment = TextAlignmentOptions.TopLeft;
        hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
        Spacer(root.transform, 4f);

        SecLabel("Message decode", root.transform);
        messageField = BuildMsgField("MsgField", root.transform);
        messageField.gameObject.AddComponent<LayoutElement>().preferredHeight = 180f;
        return root;
    }

    private static void SecLabel(string label, Transform p)
    {
        var lbl = MakeTMP("L" + label, p, label, 13f, ColInkDim, FontStyles.Bold | FontStyles.Italic);
        lbl.alignment = TextAlignmentOptions.TopLeft; lbl.characterSpacing = 1f;
        lbl.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

        // Explicit height — prevents Image from stretching to fill parent
        var ln = MakeGO("Ln" + label, p);
        var lnRT = ln.AddComponent<RectTransform>(); lnRT.sizeDelta = new Vector2(0f, 1f);
        ln.AddComponent<Image>().color = ColRule;
        ln.AddComponent<LayoutElement>().preferredHeight = 1f;
    }

    private void BuildSlots(Transform p, int count = 6)
    {
        var row = MakeGO("Slots", p); row.AddComponent<RectTransform>();
        slotsRowTransform = row.transform;
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true; hlg.spacing = 18f;
        row.AddComponent<LayoutElement>().preferredHeight = 88f;

        slotValues      = new string[count];
        slotTexts       = new TextMeshProUGUI[count];
        slotBgs         = new Image[count];
        activeSlotCount = count;

        for (int i = 0; i < count; i++)
        {
            var s = MakeGO("S" + i, row.transform);
            s.AddComponent<RectTransform>().sizeDelta = new Vector2(96f, 88f);
            slotBgs[i] = s.AddComponent<Image>(); slotBgs[i].color = ColSlotBg;
            slotTexts[i] = MakeTMP("T", s.transform, "_", 30f, ColInk, FontStyles.Bold);
            slotTexts[i].alignment = TextAlignmentOptions.Center;
            Stretch(slotTexts[i].GetComponent<RectTransform>());
            var lRT = MakeImg("L", s.transform, ColSlotLine).GetComponent<RectTransform>();
            lRT.anchorMin = new Vector2(0f, 0f); lRT.anchorMax = new Vector2(1f, 0f);
            lRT.pivot = new Vector2(0.5f, 0f); lRT.sizeDelta = new Vector2(0f, 3f); lRT.anchoredPosition = Vector2.zero;
            slotValues[i] = "";
        }
    }

    /// <summary>Destroys the current slot row and creates a new one with the requested count.</summary>
    private void RebuildSlots(int count)
    {
        if (slotsRowTransform != null) Destroy(slotsRowTransform.gameObject);
        // The decoder VLG parent is decoderContent; insert after its first 2 children (SecLabel = 2 GOs).
        BuildSlots(decoderContent.transform, count);
        slotsRowTransform.SetSiblingIndex(2);
    }

    private static TMP_InputField BuildMsgField(string name, Transform p)
    {
        var go = MakeGO(name, p); go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = ColSlotBg;
        var field = go.AddComponent<TMP_InputField>();

        var aGO = MakeGO("A", go.transform); var aRT = aGO.AddComponent<RectTransform>();
        Stretch(aRT); aRT.offsetMin = new Vector2(16f, 12f); aRT.offsetMax = new Vector2(-16f, -12f);
        aGO.AddComponent<RectMask2D>();

        var tGO = MakeGO("T", aGO.transform); Stretch(tGO.AddComponent<RectTransform>());
        var tmp = tGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 17f; tmp.color = ColInk;
        tmp.alignment = TextAlignmentOptions.TopLeft; tmp.textWrappingMode = TextWrappingModes.Normal;

        var phGO = MakeGO("Ph", aGO.transform); Stretch(phGO.AddComponent<RectTransform>());
        var ph = phGO.AddComponent<TextMeshProUGUI>();
        ph.text = "Saisir le message decode..."; ph.fontSize = 17f;
        ph.color = ColInkDim; ph.fontStyle = FontStyles.Italic;
        ph.alignment = TextAlignmentOptions.TopLeft; ph.textWrappingMode = TextWrappingModes.Normal;

        field.textViewport = aRT; field.textComponent = tmp; field.placeholder = ph;
        field.caretColor = ColInk; field.selectionColor = new Color(ColInk.r, ColInk.g, ColInk.b, 0.22f);
        field.lineType = TMP_InputField.LineType.MultiLineNewline;
        field.contentType = TMP_InputField.ContentType.Standard;
        return field;
    }

    // ── Logic ─────────────────────────────────────────────────────────────────

    private void SetTab(bool isJ)
    {
        journalContent.SetActive(isJ); decoderContent.SetActive(!isJ);
        journalTabBg.color = isJ ? ColPage : ColTabInact;
        decoderTabBg.color = isJ ? ColTabInact : ColPage;
        journalTabLabel.color = isJ ? ColInk : ColInkDim;
        decoderTabLabel.color = isJ ? ColInkDim : ColInk;
        if (!isJ) RefreshSlots();
    }

    private void HandleSlotInput()
    {
        var kb = Keyboard.current; if (kb == null) return;
        if (kb.leftArrowKey.wasPressedThisFrame  && activeSlot > 0)                  { activeSlot--; RefreshSlots(); }
        if (kb.rightArrowKey.wasPressedThisFrame && activeSlot < activeSlotCount - 1) { activeSlot++; RefreshSlots(); }

        for (int d = 0; d <= 9; d++)
        {
            if (!DigitDown(kb, d)) continue;
            if (slotValues[activeSlot].Length >= SlotMaxLen) break;
            slotValues[activeSlot] += d.ToString();
            if (slotValues[activeSlot].Length >= SlotMaxLen && activeSlot < activeSlotCount - 1) activeSlot++;
            RefreshSlots(); break;
        }

        if (kb.backspaceKey.wasPressedThisFrame)
        {
            if (slotValues[activeSlot].Length > 0) slotValues[activeSlot] = slotValues[activeSlot][..^1];
            else if (activeSlot > 0) { activeSlot--; if (slotValues[activeSlot].Length > 0) slotValues[activeSlot] = slotValues[activeSlot][..^1]; }
            RefreshSlots();
        }
    }

    private void RefreshSlots()
    {
        for (int i = 0; i < activeSlotCount; i++)
        {
            if (slotTexts[i] == null) continue;
            slotTexts[i].text = slotValues[i].Length > 0 ? slotValues[i] : "_";
            slotBgs[i].color  = i == activeSlot ? ColSlotActive : ColSlotBg;
        }
    }

    private void SaveDecoder()
    {
        PlayerPrefs.SetString(PrefCodeKey + currentDay, string.Join("/", slotValues));
        PlayerPrefs.SetString(PrefMsgKey  + currentDay, messageField?.text ?? "");
        PlayerPrefs.Save();
    }

    private void LoadDecoder()
    {
        var saved = PlayerPrefs.GetString(PrefCodeKey + currentDay, "");
        if (!string.IsNullOrEmpty(saved))
        { var parts = saved.Split('/'); for (int i = 0; i < activeSlotCount && i < parts.Length; i++) slotValues[i] = parts[i]; }
        else { for (int i = 0; i < activeSlotCount; i++) slotValues[i] = ""; }
        if (messageField != null) messageField.text = PlayerPrefs.GetString(PrefMsgKey + currentDay, "");
        activeSlot = 0; RefreshSlots();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static GameObject MakeGO(string n, Transform parent)
    { var go = new GameObject(n); go.transform.SetParent(parent, false); return go; }

    private static Image MakeImg(string n, Transform parent, Color col)
    { var go = MakeGO(n, parent); go.AddComponent<RectTransform>(); var img = go.AddComponent<Image>(); img.color = col; return img; }

    private static TextMeshProUGUI MakeTMP(string n, Transform parent, string text, float size, Color col, FontStyles style = FontStyles.Normal)
    { var go = MakeGO(n, parent); go.AddComponent<RectTransform>(); var t = go.AddComponent<TextMeshProUGUI>(); t.text = text; t.fontSize = size; t.color = col; t.fontStyle = style; return t; }

    private static void HRule(string n, Transform parent, float fromTop)
    {
        var rt = MakeImg(n, parent, ColRule).GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.01f, 1f); rt.anchorMax = new Vector2(0.99f, 1f);
        rt.pivot = new Vector2(0.5f, 1f); rt.sizeDelta = new Vector2(0f, DivH);
        rt.anchoredPosition = new Vector2(0f, -fromTop);
    }

    private static void Spacer(Transform parent, float h)
    { var go = MakeGO("Sp", parent); go.AddComponent<RectTransform>(); go.AddComponent<LayoutElement>().preferredHeight = h; }

    private static void Stretch(RectTransform rt)
    { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }

    private static void Center(RectTransform rt, float w, float h, Vector2 offset = default)
    { rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.pivot = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(w, h); rt.anchoredPosition = offset; }

    private static bool DigitDown(Keyboard kb, int d) => d switch
    {
        0 => kb.digit0Key.wasPressedThisFrame || kb.numpad0Key.wasPressedThisFrame,
        1 => kb.digit1Key.wasPressedThisFrame || kb.numpad1Key.wasPressedThisFrame,
        2 => kb.digit2Key.wasPressedThisFrame || kb.numpad2Key.wasPressedThisFrame,
        3 => kb.digit3Key.wasPressedThisFrame || kb.numpad3Key.wasPressedThisFrame,
        4 => kb.digit4Key.wasPressedThisFrame || kb.numpad4Key.wasPressedThisFrame,
        5 => kb.digit5Key.wasPressedThisFrame || kb.numpad5Key.wasPressedThisFrame,
        6 => kb.digit6Key.wasPressedThisFrame || kb.numpad6Key.wasPressedThisFrame,
        7 => kb.digit7Key.wasPressedThisFrame || kb.numpad7Key.wasPressedThisFrame,
        8 => kb.digit8Key.wasPressedThisFrame || kb.numpad8Key.wasPressedThisFrame,
        9 => kb.digit9Key.wasPressedThisFrame || kb.numpad9Key.wasPressedThisFrame,
        _ => false
    };
}
