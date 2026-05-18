using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public class JournalPanel : MonoBehaviour
{

    private static readonly Color ColOverlay    = new(0f,    0f,    0f,    0.78f);
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
    private static readonly Color ColSlotFocus  = new(0.78f, 0.72f, 0.56f, 1f);
    private static readonly Color ColSlotLine   = new(0.28f, 0.20f, 0.10f, 1f);
    private static readonly Color ColSlotHidden = new(0.82f, 0.76f, 0.60f, 1f);
    private static readonly Color ColCorrect    = new(0.15f, 0.55f, 0.25f, 1f);
    private static readonly Color ColWrong      = new(0.75f, 0.20f, 0.15f, 1f);
    private static readonly Color ColMuted      = new(0.55f, 0.45f, 0.30f, 1f);
    private static readonly Color ColKeyOverBg  = new(0.96f, 0.93f, 0.84f, 0.97f);

    

    private const float SpineW      = 80f;
    private const float CoverPad    = 18f;
    private const float HeaderH     = 72f;
    private const float TabH        = 48f;
    private const float DivH        = 1.5f;
    private const float ContentTop  = HeaderH + DivH + TabH + DivH;
    private const int   Holes       = 15;
    private const int   RuleLines   = 22;

  

    private const string PrefLettersKey  = "jrn_letters_d";
    private const string PrefDoneKey     = "jrn_done_d";


    [SerializeField] private JournalConfig  journalConfig;

    [Tooltip("Image du tableau de décodage affichée dans l'overlay (touche T).")]
    [SerializeField] private Sprite         decryptionKeyImage;

    

    private TextMeshProUGUI dayTitleTmp;
    private TextMeshProUGUI thoughtsTmp;
    private TextMeshProUGUI journalSectionLabel;
    private TextMeshProUGUI journalTabLabel;
    private TextMeshProUGUI decoderTabLabel;
    private GameObject      journalContent;
    private GameObject      decoderContent;
    private Image           journalTabBg;
    private Image           decoderTabBg;
    private GameObject      decoderTabGO;

    
    private Transform           slotsContainer;
    private TextMeshProUGUI     radioHintTmp;
    private TextMeshProUGUI     feedbackTmp;
    private Button              validateBtn;
    private GameObject          keyOverlay;

    
    private SlotData[]          slots;
    private int                 focusedSlot = -1;

  

    private int            currentDay;
    private DecryptionMode currentMode;
    private bool           radioUnlocked;
    private bool           decodingComplete;

    public event Action OnMessageDecoded;

    

    private class SlotData
    {
        public int             code;
        public bool            isHidden;        // Slot masqué : le joueur doit saisir le chiffre lui-même
        public bool            codeResolved;    // Partial/Full : le code numérique est connu/affiché
        public string          letterInput;     // Lettre(s) saisie(s) par le joueur
        public GameObject      root;
        public TextMeshProUGUI codeText;        // Texte statique (slots visibles)
        public TMP_InputField  codeInputField;  // Champ de saisie numérique (slots masqués)
        public TMP_InputField  letterField;
        public Image           bg;
        public Image           focusOutline;
    }

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        BuildUI();
        gameObject.SetActive(false);
    }

    private void Start()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnDayChanged += HandleDayChanged;
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnDayChanged -= HandleDayChanged;
    }

    private void Update()
    {
        if (decoderContent == null || !decoderContent.activeSelf) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        // Tab navigation only available when radio is unlocked and decoding is ongoing
        if (decodingComplete || !radioUnlocked) return;

        if (kb.tabKey.wasPressedThisFrame)
            MoveFocus(kb.leftShiftKey.isPressed ? -1 : 1);
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.DeleteKey(PrefLettersKey + currentDay);
        PlayerPrefs.DeleteKey(PrefDoneKey    + currentDay);
        PlayerPrefs.Save();
    }

    // ── Day change / reset ────────────────────────────────────────────────────

    private void HandleDayChanged(int newDay)
    {
        ClearDecoderProgress(newDay);

        radioUnlocked    = false;
        decodingComplete = false;
    }

    public void ClearDecoderProgress(int day)
    {
        PlayerPrefs.DeleteKey(PrefLettersKey + day);
        PlayerPrefs.DeleteKey(PrefDoneKey    + day);
        PlayerPrefs.Save();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Show(int day, string thoughts, int[] codeSequence, int[] hiddenSlotIndices, string decodedSolution)
    {
        currentDay = day;
        dayTitleTmp.text         = "Carnet  -  Jour " + day;
        journalSectionLabel.text = "Jour " + day + " :";
        thoughtsTmp.text         = thoughts;

        bool hasDecoder = journalConfig != null ? journalConfig.HasDecoder(day) : day <= 3;
        currentMode = journalConfig != null ? journalConfig.GetMode(day) : DecryptionMode.Guided;

        decoderTabGO.SetActive(hasDecoder);

        if (hasDecoder && codeSequence != null && codeSequence.Length > 0)
            RebuildDecoderSlots(codeSequence, hiddenSlotIndices ?? Array.Empty<int>(), decodedSolution);

        SetTab(true);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        SaveDecoderState();
        gameObject.SetActive(false);
    }

    public void ShowWithThoughts(int day, string thoughts)
    {
        currentDay = day;
        dayTitleTmp.text         = "Carnet  -  Jour " + day;
        journalSectionLabel.text = "Jour " + day + " :";
        thoughtsTmp.text         = thoughts;

        decoderTabGO.SetActive(false);

        SetTab(true);
        gameObject.SetActive(true);
    }

    public void ShowOnDecoderTab(int day, string thoughts, int[] codeSequence, int[] hiddenSlotIndices, string decodedSolution)
    {
        currentDay = day;
        dayTitleTmp.text         = "Carnet  -  Jour " + day;
        journalSectionLabel.text = "Jour " + day + " :";
        thoughtsTmp.text         = thoughts;

        bool hasDecoder = journalConfig != null ? journalConfig.HasDecoder(day) : day <= 3;
        currentMode = journalConfig != null ? journalConfig.GetMode(day) : DecryptionMode.Guided;

        decoderTabGO.SetActive(hasDecoder);

        if (hasDecoder && codeSequence != null && codeSequence.Length > 0)
            RebuildDecoderSlots(codeSequence, hiddenSlotIndices ?? Array.Empty<int>(), decodedSolution);

        SetTab(false); // Ouvre directement sur l'onglet décodage
        gameObject.SetActive(true);
    }

    public void UpdateThoughts(string t) => thoughtsTmp.text = t;

    public void HideSlots(int[] indices)
    {
        if (slots == null || indices == null) return;

        foreach (int i in indices)
        {
            if (i < 0 || i >= slots.Length) continue;
            var sd = slots[i];
            if (sd.codeInputField != null) continue; // déjà converti

            // Masquer le texte statique
            sd.codeText.text  = "";
            sd.codeText.color = Color.clear;
            sd.bg.color       = ColSlotHidden;
            sd.isHidden       = true;

            // Installer un champ de saisie numérique à la place
            InstallCodeInputField(sd, i);

            // Réactiver le champ lettre — le joueur remplit les deux
            if (sd.letterField != null)
                sd.letterField.interactable = !decodingComplete;

            // Le slot est considéré "prêt" pour la validation dès que le joueur l'a rempli
            sd.codeResolved = true;
        }

        // Déplacer le focus sur le premier slot interactable
        if (focusedSlot >= 0 && focusedSlot < (slots?.Length ?? 0)
            && slots[focusedSlot].isHidden)
            SetFocus(FindFirstInputSlot());
    }

    private void InstallCodeInputField(SlotData sd, int index)
    {
        var fieldGO = MakeGO("CodeInput" + index, sd.root.transform);
        var fieldRT = fieldGO.AddComponent<RectTransform>();
        fieldRT.anchorMin        = new Vector2(0f, 1f);
        fieldRT.anchorMax        = new Vector2(1f, 1f);
        fieldRT.pivot            = new Vector2(0.5f, 1f);
        fieldRT.sizeDelta        = new Vector2(-4f, 42f);
        fieldRT.anchoredPosition = Vector2.zero;
        fieldGO.AddComponent<Image>().color = new Color(ColInk.r, ColInk.g, ColInk.b, 0.07f);

        var field = fieldGO.AddComponent<TMP_InputField>();

        // Zone de texte avec masque
        var aGO = MakeGO("A", fieldGO.transform);
        var aRT = aGO.AddComponent<RectTransform>();
        Stretch(aRT); aRT.offsetMin = new Vector2(2f, 1f); aRT.offsetMax = new Vector2(-2f, -1f);
        aGO.AddComponent<RectMask2D>();

        // Placeholder — affiche __ tant que le champ est vide
        var phGO = MakeGO("Placeholder", aGO.transform);
        Stretch(phGO.AddComponent<RectTransform>());
        var phTmp       = phGO.AddComponent<TextMeshProUGUI>();
        phTmp.text      = "__";
        phTmp.fontSize  = 16f;
        phTmp.color     = ColMuted;
        phTmp.fontStyle = FontStyles.Bold;
        phTmp.alignment = TextAlignmentOptions.Center;

        // Texte saisi par le joueur
        var tGO = MakeGO("T", aGO.transform);
        Stretch(tGO.AddComponent<RectTransform>());
        var tmp       = tGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = 16f;
        tmp.color     = ColInk;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        field.textViewport      = aRT;
        field.textComponent     = tmp;
        field.placeholder       = phTmp;
        field.customCaretColor  = true;
        field.caretColor        = ColInk;
        field.caretWidth        = 2;
        field.caretBlinkRate    = 0.85f;
        field.selectionColor    = new Color(ColInk.r, ColInk.g, ColInk.b, 0.22f);
        field.characterLimit    = 3;
        field.contentType       = TMP_InputField.ContentType.IntegerNumber;
        field.lineType          = TMP_InputField.LineType.SingleLine;
        field.interactable      = !decodingComplete;

        var fNav = Navigation.defaultNavigation; fNav.mode = Navigation.Mode.None;
        field.navigation = fNav;

        sd.codeInputField = field;
    }

    public void UnlockDecoder()
    {
        radioUnlocked = true;

        if (slots == null) return;

        // Guided: reveal all codes at once
        if (currentMode == DecryptionMode.Guided)
        {
            foreach (var sd in slots)
                if (!sd.codeResolved) RevealCode(sd);
        }

        RefreshRadioHint();
        if (validateBtn != null) validateBtn.interactable = true;
        SetFocus(FindFirstInputSlot());
    }

    public void ResolveSlotCode(int slotIndex)
    {
        if (slots == null || slotIndex < 0 || slotIndex >= slots.Length) return;
        var sd = slots[slotIndex];
        if (sd.codeResolved) return;
        RevealCode(sd);
        SaveDecoderState();
    }

    // ── Decoder rebuild ───────────────────────────────────────────────────────

    private void RebuildDecoderSlots(int[] codeSequence, int[] hiddenIndices, string decodedSolution)
    {
        // Clear previous slots
        if (slotsContainer != null)
        {
            foreach (Transform child in slotsContainer)
                Destroy(child.gameObject);
        }

        int count        = codeSequence.Length;
        var hiddenSet    = new HashSet<int>(hiddenIndices);
        decodingComplete = PlayerPrefs.GetInt(PrefDoneKey + currentDay, 0) == 1;

        slots = new SlotData[count];
        for (int i = 0; i < count; i++)
        {
            var sd = new SlotData();
            sd.code     = codeSequence[i];
            sd.isHidden = currentMode switch
            {
                DecryptionMode.Guided  => false,
                DecryptionMode.Partial => hiddenSet.Contains(i),
                DecryptionMode.Full    => true,
                _                     => false
            };
            // In Guided mode, codes are visible as soon as radioUnlocked is true
            sd.codeResolved = currentMode == DecryptionMode.Guided
                ? radioUnlocked
                : (!sd.isHidden && radioUnlocked);
            sd.letterInput  = "";
            slots[i] = sd;
            BuildSlotUI(sd, i, slotsContainer);
        }

        // Store solution (spaces stripped, uppercase)
        if (!string.IsNullOrEmpty(decodedSolution))
            PlayerPrefs.SetString(PrefLettersKey + currentDay + "_solution",
                decodedSolution.Replace(" ", "").ToUpperInvariant());

        LoadDecoderState();
        RefreshRadioHint();
        RefreshFeedback(decodedSolution);
        SetFocus(decodingComplete || !radioUnlocked ? -1 : FindFirstInputSlot());
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight  = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();

        Stretch(MakeImg("Overlay", transform, ColOverlay).GetComponent<RectTransform>());

        var shRT = MakeImg("Shadow", transform, ColShadow).GetComponent<RectTransform>();
        shRT.anchorMin = Vector2.zero;  shRT.anchorMax = Vector2.one;
        shRT.offsetMin = new Vector2(-9f, -9f); shRT.offsetMax = new Vector2(9f, 9f);

        var nb   = MakeGO("Notebook", transform);
        var nbRT = nb.AddComponent<RectTransform>();
        nbRT.anchorMin = Vector2.zero; nbRT.anchorMax = Vector2.one;
        nbRT.offsetMin = new Vector2(6f, 6f); nbRT.offsetMax = new Vector2(-6f, -6f);
        nb.AddComponent<Image>().color = ColCover;
        BuildCoverBorder(nb.transform);
        BuildSpine(nb.transform);

        var pg   = MakeGO("Page", nb.transform);
        var pgRT = pg.AddComponent<RectTransform>();
        pgRT.anchorMin = Vector2.zero;  pgRT.anchorMax = Vector2.one;
        pgRT.offsetMin = new Vector2(SpineW + CoverPad, CoverPad);
        pgRT.offsetMax = new Vector2(-CoverPad, -CoverPad);
        pg.AddComponent<Image>().color = ColPage;
        BuildRules(pg.transform);
        BuildPage(pg.transform);
    }

    private static void BuildCoverBorder(Transform p)
    {
        var img = MakeImg("Border", p, ColCoverEdge);
        var rt  = img.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(10f, 10f); rt.offsetMax = new Vector2(-10f, -10f);
        img.color = new Color(ColCoverEdge.r, ColCoverEdge.g, ColCoverEdge.b, 0.35f);
    }

    private static void BuildSpine(Transform p)
    {
        var s    = MakeGO("Spine", p);
        var sRT  = s.AddComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0f, 0f); sRT.anchorMax = new Vector2(0f, 1f);
        sRT.pivot     = new Vector2(0f, 0.5f);
        sRT.sizeDelta = new Vector2(SpineW, 0f);
        sRT.anchoredPosition = Vector2.zero;
        s.AddComponent<Image>().color = ColCoverEdge;

        var eRT = MakeImg("Edge", s.transform, ColSpiral).GetComponent<RectTransform>();
        eRT.anchorMin = new Vector2(1f, 0.01f); eRT.anchorMax = new Vector2(1f, 0.99f);
        eRT.pivot     = new Vector2(1f, 0.5f);
        eRT.sizeDelta = new Vector2(3f, 0f);
        eRT.anchoredPosition = Vector2.zero;

        var knob = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
        for (int i = 0; i < Holes; i++)
            BuildHole(s.transform, knob, i, 0.95f - i / (float)(Holes - 1) * 0.90f);
    }

    private static void BuildHole(Transform p, Sprite k, int idx, float y)
    {
        var r   = MakeImg("R" + idx, p, ColSpiral); r.sprite = k; r.type = Image.Type.Simple;
        var rRT = r.GetComponent<RectTransform>();
        rRT.anchorMin = rRT.anchorMax = new Vector2(0.5f, y);
        rRT.pivot     = Vector2.one * 0.5f;
        rRT.sizeDelta = new Vector2(26f, 26f);
        rRT.anchoredPosition = Vector2.zero;

        var h   = MakeImg("H" + idx, p, ColPage); h.sprite = k; h.type = Image.Type.Simple;
        var hRT = h.GetComponent<RectTransform>();
        hRT.anchorMin = hRT.anchorMax = new Vector2(0.5f, y);
        hRT.pivot     = Vector2.one * 0.5f;
        hRT.sizeDelta = new Vector2(14f, 14f);
        hRT.anchoredPosition = Vector2.zero;
    }

    private static void BuildRules(Transform p)
    {
        const float PageRefH = 720f - CoverPad * 2f;
        float topFrac   = (ContentTop + 14f) / PageRefH;
        float botFrac   = 24f / PageRefH;
        float available = 1f - topFrac - botFrac;

        for (int i = 0; i < RuleLines; i++)
        {
            float yAnchor = 1f - topFrac - available * (i / (float)(RuleLines - 1));
            var rt = MakeImg("Rl" + i, p, ColRule).GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, yAnchor);
            rt.anchorMax        = new Vector2(1f, yAnchor);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.sizeDelta        = new Vector2(0f, 1f);
            rt.anchoredPosition = Vector2.zero;
        }
    }

    private void BuildPage(Transform p)
    {
        // Header
        var h   = MakeGO("Hdr", p);
        var hRT = h.AddComponent<RectTransform>();
        hRT.anchorMin = new Vector2(0f, 1f); hRT.anchorMax = new Vector2(1f, 1f);
        hRT.pivot     = new Vector2(0.5f, 1f);
        hRT.sizeDelta = new Vector2(0f, HeaderH);
        hRT.anchoredPosition = Vector2.zero;

        dayTitleTmp = MakeTMP("Title", h.transform, "Carnet  -  Jour 1", 26f, ColInk, FontStyles.Bold);
        var tRT = dayTitleTmp.GetComponent<RectTransform>();
        Stretch(tRT); tRT.offsetMin = new Vector2(36f, 0f); tRT.offsetMax = new Vector2(-170f, 0f);
        dayTitleTmp.alignment       = TextAlignmentOptions.MidlineLeft;
        dayTitleTmp.characterSpacing = 1.5f;

        var mRT = MakeImg("Margin", h.transform, new Color(0.75f, 0.25f, 0.15f, 0.35f)).GetComponent<RectTransform>();
        mRT.anchorMin = new Vector2(0f, 0f); mRT.anchorMax = new Vector2(0f, 1f);
        mRT.pivot     = new Vector2(0f, 0.5f);
        mRT.sizeDelta = new Vector2(2f, 0f);
        mRT.anchoredPosition = new Vector2(28f, 0f);

        // TABLE button — toggles the key panel
        var tblGO = MakeGO("TableBtn", h.transform);
        var tblRT = tblGO.AddComponent<RectTransform>();
        tblRT.anchorMin = new Vector2(1f, 0f); tblRT.anchorMax = Vector2.one;
        tblRT.pivot     = new Vector2(1f, 0.5f);
        tblRT.sizeDelta = new Vector2(88f, 0f);
        tblRT.anchoredPosition = new Vector2(-62f, 0f);
        var tblBg  = tblGO.AddComponent<Image>(); tblBg.color = new Color(ColInkDim.r, ColInkDim.g, ColInkDim.b, 0.15f);
        var tblBtn = tblGO.AddComponent<Button>(); tblBtn.targetGraphic = tblBg;
        var tblNav = Navigation.defaultNavigation; tblNav.mode = Navigation.Mode.None; tblBtn.navigation = tblNav;
        tblBtn.onClick.AddListener(ToggleKeyOverlay);
        var tblLbl = MakeTMP("L", tblGO.transform, "CODE", 11f, ColInkDim, FontStyles.Bold);
        tblLbl.alignment = TextAlignmentOptions.Center; tblLbl.characterSpacing = 1f;
        Stretch(tblLbl.GetComponent<RectTransform>());        // Close button
        var cGO = MakeGO("Close", h.transform);
        var cRT = cGO.AddComponent<RectTransform>();
        cRT.anchorMin = new Vector2(1f, 0f); cRT.anchorMax = Vector2.one;
        cRT.pivot     = new Vector2(1f, 0.5f);
        cRT.sizeDelta = new Vector2(58f, 0f);
        var cBg  = cGO.AddComponent<Image>(); cBg.color = Color.clear;
        var cBtn = cGO.AddComponent<Button>(); cBtn.targetGraphic = cBg;
        var noNav = Navigation.defaultNavigation; noNav.mode = Navigation.Mode.None;
        cBtn.navigation = noNav;
        cBtn.onClick.AddListener(() => JournalManager.Instance?.Close());
        var xl = MakeTMP("X", cGO.transform, "x", 26f, ColInkDim);
        xl.alignment = TextAlignmentOptions.Center;
        Stretch(xl.GetComponent<RectTransform>());

        HRule("HR1", p, HeaderH);

        // Tabs
        var tb   = MakeGO("Tabs", p);
        var tbRT = tb.AddComponent<RectTransform>();
        tbRT.anchorMin = new Vector2(0f, 1f); tbRT.anchorMax = new Vector2(1f, 1f);
        tbRT.pivot     = new Vector2(0.5f, 1f);
        tbRT.sizeDelta = new Vector2(0f, TabH);
        tbRT.anchoredPosition = new Vector2(0f, -(HeaderH + DivH));

        var hlg = tb.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(32, 32, 0, 0); hlg.spacing = 8f;
        hlg.childControlWidth     = false; hlg.childControlHeight     = true;
        hlg.childForceExpandWidth = false; hlg.childForceExpandHeight = true;

        MakeTab("TabJ", tb.transform, "JOURNAL",  true,  out journalTabBg, out journalTabLabel, out _);
        MakeTab("TabD", tb.transform, "DECODAGE", false, out decoderTabBg, out decoderTabLabel, out decoderTabGO);

        HRule("HR2", p, ContentTop - DivH);
        journalContent = BuildJournalTab(p);
        decoderContent = BuildDecoderTab(p);

        // Key panel — enfant du decoderContent, rendu par-dessus les slots
        keyOverlay = BuildKeyOverlay(decoderContent.transform);
        keyOverlay.SetActive(false);
    }

    private void MakeTab(string name, Transform p, string label, bool isJ,
        out Image bg, out TextMeshProUGUI lbl, out GameObject tabGO)
    {
        var go = MakeGO(name, p);
        go.AddComponent<RectTransform>();
        tabGO = go;
        bg    = go.AddComponent<Image>(); bg.color = ColTabInact;
        go.AddComponent<LayoutElement>().preferredWidth = 200f;

        var btn = go.AddComponent<Button>();
        var nav = Navigation.defaultNavigation; nav.mode = Navigation.Mode.None;
        btn.navigation = nav; btn.targetGraphic = bg;
        btn.onClick.AddListener(() => SetTab(isJ));

        lbl = MakeTMP("L", go.transform, label, 13f, ColInkDim, FontStyles.Bold);
        lbl.alignment        = TextAlignmentOptions.Center;
        lbl.characterSpacing = 2.5f;
        Stretch(lbl.GetComponent<RectTransform>());
    }

    private GameObject BuildJournalTab(Transform p)
    {
        var root = MakeGO("JournalContent", p);
        var rRT  = root.AddComponent<RectTransform>();
        rRT.anchorMin = Vector2.zero; rRT.anchorMax = Vector2.one;
        rRT.offsetMin = Vector2.zero; rRT.offsetMax = new Vector2(0f, -ContentTop);

        var vlg = root.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(52, 52, 40, 40); vlg.spacing = 12f;
        vlg.childControlWidth     = true;  vlg.childControlHeight     = true;
        vlg.childForceExpandWidth = true;  vlg.childForceExpandHeight = false;

        journalSectionLabel = MakeTMP("Sec", root.transform, "Jour 1 :", 14f, ColInkDim,
            FontStyles.Bold | FontStyles.Italic);
        journalSectionLabel.alignment        = TextAlignmentOptions.TopLeft;
        journalSectionLabel.characterSpacing = 1f;
        journalSectionLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 28f;

        var sepGO = MakeGO("Sep", root.transform);
        sepGO.AddComponent<RectTransform>().sizeDelta = new Vector2(0f, 1.5f);
        sepGO.AddComponent<Image>().color = ColRule;
        sepGO.AddComponent<LayoutElement>().preferredHeight = 1.5f;

        thoughtsTmp = MakeTMP("Thoughts", root.transform, "", 22f, ColInk, FontStyles.Italic);
        thoughtsTmp.alignment        = TextAlignmentOptions.TopLeft;
        thoughtsTmp.textWrappingMode = TextWrappingModes.Normal;
        thoughtsTmp.lineSpacing      = 4f;
        thoughtsTmp.characterSpacing = 0.3f;

        return root;
    }

    private GameObject BuildDecoderTab(Transform p)
    {
        var root = MakeGO("DecoderContent", p);
        var rRT  = root.AddComponent<RectTransform>();
        rRT.anchorMin = Vector2.zero; rRT.anchorMax = Vector2.one;
        rRT.offsetMin = Vector2.zero; rRT.offsetMax = new Vector2(0f, -ContentTop);

        // Outer scroll for long sequences
        var sr   = root.AddComponent<ScrollRect>();
        sr.horizontal     = false;
        sr.vertical       = true;
        sr.movementType   = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 30f;

        var vpGO = MakeGO("Viewport", root.transform);
        var vpRT = vpGO.AddComponent<RectTransform>();
        Stretch(vpRT);
        vpGO.AddComponent<RectMask2D>();
        sr.viewport = vpRT;

        var contentGO = MakeGO("Content", vpGO.transform);
        var cntRT     = contentGO.AddComponent<RectTransform>();
        cntRT.anchorMin = new Vector2(0f, 1f); cntRT.anchorMax = new Vector2(1f, 1f);
        cntRT.pivot     = new Vector2(0.5f, 1f);
        cntRT.sizeDelta = Vector2.zero;
        sr.content = cntRT;

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(52, 52, 36, 36); vlg.spacing = 16f;
        vlg.childControlWidth     = true;  vlg.childControlHeight     = true;
        vlg.childForceExpandWidth = true;  vlg.childForceExpandHeight = false;

        contentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Radio hint — hidden once radio is unlocked
        radioHintTmp = MakeTMP("RadioHint", contentGO.transform,
            "⚠  Syntonisez la radio pour recevoir le message avant de pouvoir décoder.",
            13f, ColInkDim, FontStyles.Italic);
        radioHintTmp.alignment        = TextAlignmentOptions.TopLeft;
        radioHintTmp.textWrappingMode = TextWrappingModes.Normal;
        radioHintTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 40f;

        // Section: codes
        SecLabel("Codes", contentGO.transform);

        // Slots container — WrapGrid for long sequences
        var scGO = MakeGO("SlotsContainer", contentGO.transform);
        scGO.AddComponent<RectTransform>();
        var grid = scGO.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(72f, 90f);
        grid.spacing         = new Vector2(10f, 10f);
        grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment  = TextAnchor.UpperLeft;
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 10;
        scGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scGO.AddComponent<LayoutElement>().minHeight = 100f;
        slotsContainer = scGO.transform;

        Spacer(contentGO.transform, 8f);

        // Section: feedback + validate
        SecLabel("Résultat", contentGO.transform);

        feedbackTmp = MakeTMP("Feedback", contentGO.transform,
            "Remplissez les slots de lettres puis validez.", 13f, ColInkDim, FontStyles.Italic);
        feedbackTmp.alignment        = TextAlignmentOptions.TopLeft;
        feedbackTmp.textWrappingMode = TextWrappingModes.Normal;
        feedbackTmp.gameObject.AddComponent<LayoutElement>().preferredHeight = 36f;

        // Validate button
        var btnGO = MakeGO("ValidateBtn", contentGO.transform);
        var btnRT = btnGO.AddComponent<RectTransform>();
        btnGO.AddComponent<LayoutElement>().preferredHeight = 44f;
        var btnBg = btnGO.AddComponent<Image>(); btnBg.color = ColSlotLine;
        validateBtn = btnGO.AddComponent<Button>();
        var btnNav = Navigation.defaultNavigation; btnNav.mode = Navigation.Mode.None;
        validateBtn.navigation    = btnNav;
        validateBtn.targetGraphic = btnBg;
        validateBtn.onClick.AddListener(ValidateDecoding);
        var btnLbl = MakeTMP("BtnLbl", btnGO.transform, "VALIDER LE DÉCODAGE", 13f, ColPage, FontStyles.Bold);
        btnLbl.alignment = TextAlignmentOptions.Center;
        Stretch(btnLbl.GetComponent<RectTransform>());

        return root;
    }

    // ── Slot UI construction ──────────────────────────────────────────────────

    private void BuildSlotUI(SlotData sd, int index, Transform parent)
    {
        var root = MakeGO("Slot" + index, parent);
        root.AddComponent<RectTransform>();
        sd.root = root;

        // Background — Grid controls the root size (72×90), children use absolute positions
        sd.bg = root.AddComponent<Image>();
        sd.bg.color = sd.codeResolved ? ColSlotBg : ColSlotHidden;

        // Focus outline (slightly outside)
        var outlineGO = MakeGO("Outline", root.transform);
        var outRT     = outlineGO.AddComponent<RectTransform>();
        Stretch(outRT);
        outRT.offsetMin = new Vector2(-2f, -2f); outRT.offsetMax = new Vector2(2f, 2f);
        sd.focusOutline = outlineGO.AddComponent<Image>();
        sd.focusOutline.color = Color.clear;

        // Bottom underline
        var lnRT = MakeImg("Line", root.transform, ColSlotLine).GetComponent<RectTransform>();
        lnRT.anchorMin       = new Vector2(0f, 0f); lnRT.anchorMax = new Vector2(1f, 0f);
        lnRT.pivot           = new Vector2(0.5f, 0f);
        lnRT.sizeDelta       = new Vector2(0f, 3f);
        lnRT.anchoredPosition = Vector2.zero;

        // Code number — anchored from top, absolute height 42px
        sd.codeText = MakeTMP("Code", root.transform, GetCodeDisplay(sd), 16f, ColInk, FontStyles.Bold);
        sd.codeText.alignment = TextAlignmentOptions.Center;
        var ctRT = sd.codeText.GetComponent<RectTransform>();
        ctRT.anchorMin       = new Vector2(0f, 1f); ctRT.anchorMax = new Vector2(1f, 1f);
        ctRT.pivot           = new Vector2(0.5f, 1f);
        ctRT.sizeDelta       = new Vector2(0f, 42f);
        ctRT.anchoredPosition = Vector2.zero;
        if (!sd.codeResolved) sd.codeText.color = ColMuted;

        // Letter input — anchored from bottom, absolute height 40px
        var fieldGO = MakeGO("LetterField" + index, root.transform);
        var fieldRT = fieldGO.AddComponent<RectTransform>();
        fieldRT.anchorMin       = new Vector2(0f, 0f); fieldRT.anchorMax = new Vector2(1f, 0f);
        fieldRT.pivot           = new Vector2(0.5f, 0f);
        fieldRT.sizeDelta       = new Vector2(-8f, 40f);
        fieldRT.anchoredPosition = new Vector2(0f, 5f);
        fieldGO.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);

        var field = fieldGO.AddComponent<TMP_InputField>();
        var aGO   = MakeGO("A", fieldGO.transform);
        var aRT   = aGO.AddComponent<RectTransform>();
        Stretch(aRT); aRT.offsetMin = new Vector2(4f, 2f); aRT.offsetMax = new Vector2(-4f, -2f);
        aGO.AddComponent<RectMask2D>();

        var tGO = MakeGO("T", aGO.transform);
        Stretch(tGO.AddComponent<RectTransform>());
        var tmp = tGO.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 18f; tmp.color = ColInk; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;

        field.textViewport   = aRT;
        field.textComponent  = tmp;
        field.caretColor     = ColInk;
        field.selectionColor = new Color(ColInk.r, ColInk.g, ColInk.b, 0.22f);
        field.characterLimit = 3;
        field.lineType       = TMP_InputField.LineType.SingleLine;
        field.contentType    = TMP_InputField.ContentType.Standard;
        field.interactable   = sd.codeResolved && !decodingComplete;
        var fNav = Navigation.defaultNavigation; fNav.mode = Navigation.Mode.None;
        field.navigation = fNav;
        sd.letterField = field;

        int captured = index;
        field.onValueChanged.AddListener(_ => OnLetterChanged(captured));
        field.onSelect.AddListener(     _ => SetFocus(captured));
    }

    // ── Code display ──────────────────────────────────────────────────────────

    private string GetCodeDisplay(SlotData sd) => sd.codeResolved ? sd.code.ToString() : "__";

    private void RevealCode(SlotData sd)
    {
        sd.codeResolved          = true;
        sd.codeText.text         = sd.code.ToString();
        sd.codeText.color        = ColInk;
        sd.bg.color              = ColSlotBg;
        sd.letterField.interactable = !decodingComplete;
    }

    // ── Decoder logic ─────────────────────────────────────────────────────────

    private void OnLetterChanged(int index)
    {
        if (slots == null || index >= slots.Length) return;
        slots[index].letterInput = slots[index].letterField.text;
    }

    private void ValidateDecoding()
    {
        if (slots == null || decodingComplete) return;

        if (!radioUnlocked)
        {
            SetFeedback("Syntonisez d'abord la radio pour recevoir le message.", ColWrong);
            return;
        }

        foreach (var sd in slots)
        {
            // Un slot masqué en mode self-fill est toujours considéré prêt (codeResolved = true)
            if (!sd.codeResolved)
            {
                SetFeedback("Des codes radio manquent encore — continuez d'écouter.", ColWrong);
                return;
            }
        }

        var sb = new System.Text.StringBuilder();
        foreach (var sd in slots)
            sb.Append(sd.letterField != null ? sd.letterField.text.Trim() : "");

        string playerAnswer  = sb.ToString().ToUpperInvariant();
        string savedSolution = PlayerPrefs.GetString(PrefLettersKey + currentDay + "_solution", "");

        if (string.IsNullOrEmpty(savedSolution))
        {
            MarkDecodingComplete();   // no solution configured: sandbox mode
            return;
        }

        if (playerAnswer == savedSolution)
            MarkDecodingComplete();
        else
            SetFeedback("Message incorrect — vérifiez votre traduction.", ColWrong);
    }

    private void MarkDecodingComplete()
    {
        decodingComplete = true;
        SetFeedback("✓  MESSAGE DÉCODÉ — Anomalie déclenchée.", ColCorrect);

        if (validateBtn != null) validateBtn.interactable = false;
        foreach (var sd in slots)
        {
            if (sd.letterField    != null) sd.letterField.interactable    = false;
            if (sd.codeInputField != null) sd.codeInputField.interactable = false;
        }

        SaveDecoderState();
        OnMessageDecoded?.Invoke();
    }

    private void SetFeedback(string msg, Color col)
    {
        if (feedbackTmp == null) return;
        feedbackTmp.text      = msg;
        feedbackTmp.color     = col;
        feedbackTmp.fontStyle = col == ColCorrect ? FontStyles.Bold : FontStyles.Italic;
    }

    private void RefreshRadioHint()
    {
        if (radioHintTmp != null)
            radioHintTmp.gameObject.SetActive(!radioUnlocked);
        if (validateBtn != null)
            validateBtn.interactable = radioUnlocked && !decodingComplete;
    }

    private void RefreshFeedback(string decodedSolution)
    {
        if (decodingComplete)
        {
            SetFeedback("✓  MESSAGE DÉCODÉ — Anomalie déclenchée.", ColCorrect);
            if (validateBtn != null) validateBtn.interactable = false;
            return;
        }

        if (!radioUnlocked)
        {
            SetFeedback("Syntonisez la radio pour débloquer le décodage.", ColInkDim);
            return;
        }

        string hint = currentMode switch
        {
            DecryptionMode.Guided  => "Traduisez chaque code en lettre, puis validez.",
            DecryptionMode.Partial => "Complétez les codes manquants via la radio, puis traduisez et validez.",
            DecryptionMode.Full    => "Transcrivez tous les codes depuis la radio, puis traduisez et validez.",
            _                     => "Remplissez les slots puis validez."
        };
        SetFeedback(hint, ColInkDim);
    }

    // ── Key overlay ───────────────────────────────────────────────────────────

    private GameObject BuildKeyOverlay(Transform parent)
    {
        const float PanelW = 220f;
        const float PanelH = 200f;
        const float BtnSz  = 26f;
        const float Pad    = 8f;

        var ov   = MakeGO("KeyPanel", parent);
        var ovRT = ov.AddComponent<RectTransform>();
        // Anchor top-right of the decoder content area
        ovRT.anchorMin        = new Vector2(1f, 1f);
        ovRT.anchorMax        = new Vector2(1f, 1f);
        ovRT.pivot            = new Vector2(1f, 1f);
        ovRT.anchoredPosition = new Vector2(-8f, -8f);
        ovRT.sizeDelta        = new Vector2(PanelW, PanelH);

        var bg = ov.AddComponent<Image>();
        bg.color = ColKeyOverBg;

        // Image du tableau — remplit le panneau avec marges
        var imgGO = MakeGO("KeyImage", ov.transform);
        var imgRT = imgGO.AddComponent<RectTransform>();
        imgRT.anchorMin        = Vector2.zero;
        imgRT.anchorMax        = Vector2.one;
        imgRT.offsetMin        = new Vector2(Pad, Pad);
        imgRT.offsetMax        = new Vector2(-Pad, -Pad);
        var img = imgGO.AddComponent<Image>();
        img.sprite         = decryptionKeyImage;
        img.preserveAspect = true;

        // Bouton ✕ en haut à droite du panneau
        var closeBtn   = MakeGO("CloseBtn", ov.transform);
        var closeBtnRT = closeBtn.AddComponent<RectTransform>();
        closeBtnRT.anchorMin        = new Vector2(1f, 1f);
        closeBtnRT.anchorMax        = new Vector2(1f, 1f);
        closeBtnRT.pivot            = new Vector2(1f, 1f);
        closeBtnRT.anchoredPosition = Vector2.zero;
        closeBtnRT.sizeDelta        = new Vector2(BtnSz, BtnSz);
        var closeBtnImg  = closeBtn.AddComponent<Image>(); closeBtnImg.color = ColInk;
        var closeBtnComp = closeBtn.AddComponent<Button>();
        closeBtnComp.targetGraphic = closeBtnImg;
        closeBtnComp.onClick.AddListener(ToggleKeyOverlay);
        var closeLbl   = MakeTMP("Lbl", closeBtn.transform, "x", 13f, ColPage, FontStyles.Bold);
        closeLbl.alignment = TextAlignmentOptions.Center;
        var closeLblRT = closeLbl.GetComponent<RectTransform>();
        closeLblRT.anchorMin = Vector2.zero; closeLblRT.anchorMax = Vector2.one;
        closeLblRT.offsetMin = Vector2.zero; closeLblRT.offsetMax = Vector2.zero;

        return ov;
    }

    private void ToggleKeyOverlay()
    {
        if (keyOverlay == null) return;
        keyOverlay.SetActive(!keyOverlay.activeSelf);
    }

    // ── Focus management ──────────────────────────────────────────────────────

    private void SetFocus(int index)
    {
        // Clear previous focus
        if (focusedSlot >= 0 && focusedSlot < (slots?.Length ?? 0))
        {
            var prev = slots[focusedSlot];
            prev.bg.color           = prev.codeResolved ? ColSlotBg : ColSlotHidden;
            prev.focusOutline.color = Color.clear;
        }

        focusedSlot = index;
        if (index < 0 || slots == null || index >= slots.Length) return;

        var sd = slots[index];
        if (!sd.codeResolved) { focusedSlot = -1; return; }

        sd.bg.color           = ColSlotFocus;
        sd.focusOutline.color = new Color(ColInk.r, ColInk.g, ColInk.b, 0.4f);
        sd.letterField.Select();
    }

    private void MoveFocus(int delta)
    {
        if (slots == null) return;
        int next = focusedSlot + delta;
        while (next >= 0 && next < slots.Length)
        {
            if (slots[next].codeResolved) { SetFocus(next); return; }
            next += delta;
        }
    }

    private int FindFirstInputSlot()
    {
        if (slots == null) return -1;
        for (int i = 0; i < slots.Length; i++)
            if (slots[i].codeResolved) return i;
        return -1;
    }

    // ── Mode label ────────────────────────────────────────────────────────────

    // (removed — mode is not shown to the player)

    // ── Persistence ───────────────────────────────────────────────────────────

    private void SaveDecoderState()
    {
        if (slots == null) return;

        var letters = new string[slots.Length];
        for (int i = 0; i < slots.Length; i++)
            letters[i] = slots[i].letterField != null ? slots[i].letterField.text : "";

        PlayerPrefs.SetString(PrefLettersKey + currentDay, string.Join("|", letters));
        PlayerPrefs.SetInt   (PrefDoneKey    + currentDay, decodingComplete ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadDecoderState()
    {
        if (slots == null) return;

        string lettersRaw = PlayerPrefs.GetString(PrefLettersKey + currentDay, "");
        string[] letters  = string.IsNullOrEmpty(lettersRaw) ? Array.Empty<string>() : lettersRaw.Split('|');

        for (int i = 0; i < slots.Length; i++)
        {
            var sd = slots[i];

            // Sync visuals for slots already marked resolved during RebuildDecoderSlots
            if (sd.codeResolved)
            {
                sd.codeText.text  = sd.code.ToString();
                sd.codeText.color = ColInk;
                sd.bg.color       = ColSlotBg;
            }

            // Restore letter input from previous session
            if (sd.codeResolved && i < letters.Length && !string.IsNullOrEmpty(letters[i]))
            {
                sd.letterInput      = letters[i];
                sd.letterField.text = letters[i];
            }

            // Apply interactability
            if (sd.letterField != null)
                sd.letterField.interactable = sd.codeResolved && !decodingComplete;
        }

        if (validateBtn != null)
            validateBtn.interactable = radioUnlocked && !decodingComplete;
    }

    // ── Tab switching ─────────────────────────────────────────────────────────

    private void SetTab(bool isJ)
    {
        journalContent.SetActive(isJ);
        decoderContent.SetActive(!isJ);
        journalTabBg.color    = isJ ? ColPage     : ColTabInact;
        decoderTabBg.color    = isJ ? ColTabInact : ColPage;
        journalTabLabel.color = isJ ? ColInk      : ColInkDim;
        decoderTabLabel.color = isJ ? ColInkDim   : ColInk;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void SecLabel(string label, Transform p)
    {
        var lbl = MakeTMP("L" + label, p, label, 13f, ColInkDim, FontStyles.Bold | FontStyles.Italic);
        lbl.alignment        = TextAlignmentOptions.TopLeft;
        lbl.characterSpacing = 1f;
        lbl.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

        var ln   = MakeGO("Ln" + label, p);
        var lnRT = ln.AddComponent<RectTransform>();
        lnRT.sizeDelta = new Vector2(0f, 1f);
        ln.AddComponent<Image>().color = ColRule;
        ln.AddComponent<LayoutElement>().preferredHeight = 1f;
    }

    private static void Spacer(Transform parent, float h)
    {
        var go = MakeGO("Sp", parent);
        go.AddComponent<RectTransform>();
        go.AddComponent<LayoutElement>().preferredHeight = h;
    }

    private static GameObject MakeGO(string n, Transform parent)
    {
        var go = new GameObject(n);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Image MakeImg(string n, Transform parent, Color col)
    {
        var go  = MakeGO(n, parent);
        go.AddComponent<RectTransform>();
        var img = go.AddComponent<Image>();
        img.color = col;
        return img;
    }

    private static TextMeshProUGUI MakeTMP(string n, Transform parent, string text,
        float size, Color col, FontStyles style = FontStyles.Normal)
    {
        var go = MakeGO(n, parent);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = size; t.color = col; t.fontStyle = style;
        return t;
    }

    private static void HRule(string n, Transform parent, float fromTop)
    {
        var rt = MakeImg(n, parent, ColRule).GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.01f, 1f);
        rt.anchorMax        = new Vector2(0.99f, 1f);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.sizeDelta        = new Vector2(0f, DivH);
        rt.anchoredPosition = new Vector2(0f, -fromTop);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
