using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Builds and manages the Options panel UI entirely at runtime.
/// Attach to the OptionsPanel GameObject in the MainMenu scene.
/// </summary>
public class OptionsManager : MonoBehaviour
{
    private const string KeyMasterVolume = "opt_master_volume";
    private const string KeySensitivity  = "opt_sensitivity";
    private const string KeyQuality      = "opt_quality";
    private const string KeyFullscreen   = "opt_fullscreen";
    private const string KeyResolution   = "opt_resolution_idx";

    // Vintage amber theme
    private static readonly Color ColAmber    = new(1.00f, 0.78f, 0.20f, 1f);
    private static readonly Color ColAmberDim = new(0.85f, 0.60f, 0.30f, 1f);
    private static readonly Color ColCardBg   = new(0.06f, 0.04f, 0.02f, 1.00f);
    private static readonly Color ColSliderBg = new(0.15f, 0.11f, 0.06f, 1f);
    private static readonly Color ColBtnDark  = new(0.12f, 0.09f, 0.05f, 0.90f);

    // Layout constants — tweak here to resize everything at once
    private const float HeaderHeight   = 72f;
    private const float FooterHeight   = 72f;
    private const float RowHeight      = 64f;
    private const float SectionHeight  = 36f;
    private const float FontSection    = 15f;
    private const float FontLabel      = 15f;
    private const float FontValue      = 15f;
    private const float LabelWidth     = 280f;
    private const float SliderWidth    = 340f;
    private const float ValueWidth     = 70f;
    private const float ChoiceValWidth = 260f;

    // Runtime UI references
    private Slider          _masterSlider;
    private TextMeshProUGUI _masterValue;
    private Slider          _sensitivitySlider;
    private TextMeshProUGUI _sensitivityValue;

    private int             _qualityIndex;
    private TextMeshProUGUI _qualityValue;

    private bool            _isFullscreen;
    private TextMeshProUGUI _fullscreenValue;

    private Resolution[]    _resolutions;
    private int             _resolutionIndex;
    private TextMeshProUGUI _resolutionValue;

    private void Start()
    {
        // Stretch panel fullscreen inside Canvas
        var rt = GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        BuildUI();
        LoadSettings();
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Applies and persists all current settings.</summary>
    public void Apply()
    {
        AudioListener.volume = _masterSlider.value;
        PlayerPrefs.SetFloat(KeyMasterVolume, _masterSlider.value);

        PlayerPrefs.SetFloat(KeySensitivity, _sensitivitySlider.value);

        QualitySettings.SetQualityLevel(_qualityIndex, true);
        PlayerPrefs.SetInt(KeyQuality, _qualityIndex);

        Screen.fullScreen = _isFullscreen;
        PlayerPrefs.SetInt(KeyFullscreen, _isFullscreen ? 1 : 0);

        if (_resolutions != null && _resolutions.Length > 0)
        {
            var res = _resolutions[_resolutionIndex];
            Screen.SetResolution(res.width, res.height, _isFullscreen);
            PlayerPrefs.SetInt(KeyResolution, _resolutionIndex);
        }

        PlayerPrefs.Save();
    }

    /// <summary>Returns the saved mouse sensitivity so other systems can read it.</summary>
    public static float MouseSensitivity => PlayerPrefs.GetFloat(KeySensitivity, 1f);

    // ── Settings Load ────────────────────────────────────────────────────────

    private void LoadSettings()
    {
        float vol = PlayerPrefs.GetFloat(KeyMasterVolume, 1f);
        _masterSlider.value  = vol;
        _masterValue.text    = Mathf.RoundToInt(vol * 100f) + "%";
        AudioListener.volume = vol;

        float sens = PlayerPrefs.GetFloat(KeySensitivity, 1f);
        _sensitivitySlider.value = sens;
        _sensitivityValue.text   = sens.ToString("F1");

        _qualityIndex      = Mathf.Clamp(PlayerPrefs.GetInt(KeyQuality, QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
        _qualityValue.text = QualitySettings.names[_qualityIndex];

        _isFullscreen         = PlayerPrefs.GetInt(KeyFullscreen, Screen.fullScreen ? 1 : 0) == 1;
        _fullscreenValue.text = _isFullscreen ? "OUI" : "NON";
        Screen.fullScreen     = _isFullscreen;

        if (_resolutions != null && _resolutions.Length > 0)
        {
            _resolutionIndex      = Mathf.Clamp(PlayerPrefs.GetInt(KeyResolution, GetCurrentResolutionIndex()), 0, _resolutions.Length - 1);
            _resolutionValue.text = ResolutionStr(_resolutionIndex);
        }
    }

    // ── Choice Callbacks ─────────────────────────────────────────────────────

    private void StepQuality(int dir)
    {
        _qualityIndex      = (_qualityIndex + dir + QualitySettings.names.Length) % QualitySettings.names.Length;
        _qualityValue.text = QualitySettings.names[_qualityIndex];
    }

    private void ToggleFullscreen()
    {
        _isFullscreen         = !_isFullscreen;
        _fullscreenValue.text = _isFullscreen ? "OUI" : "NON";
    }

    private void StepResolution(int dir)
    {
        if (_resolutions == null || _resolutions.Length == 0) return;
        _resolutionIndex      = Mathf.Clamp(_resolutionIndex + dir, 0, _resolutions.Length - 1);
        _resolutionValue.text = ResolutionStr(_resolutionIndex);
    }

    private int GetCurrentResolutionIndex()
    {
        if (_resolutions == null) return 0;
        for (int i = 0; i < _resolutions.Length; i++)
            if (_resolutions[i].width  == Screen.currentResolution.width &&
                _resolutions[i].height == Screen.currentResolution.height)
                return i;
        return Mathf.Max(0, _resolutions.Length - 1);
    }

    private string ResolutionStr(int idx)
    {
        if (_resolutions == null || idx < 0 || idx >= _resolutions.Length) return "-";
        return $"{_resolutions[idx].width} \u00d7 {_resolutions[idx].height}";
    }

    // ── UI Builder ───────────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Full-screen dark overlay (blocks click-through)
        var overlay = MakeImage("Overlay", transform, new Color(0f, 0f, 0f, 0.85f));
        StretchFull(overlay.GetComponent<RectTransform>());

        // Fullscreen card
        var card   = MakeImage("Card", transform, ColCardBg);
        var cardRT = card.GetComponent<RectTransform>();
        StretchFull(cardRT);

        // ── Header ────────────────────────────────────────────────────────────
        var header   = MakeGO("Header", card.transform);
        var headerRT = header.AddComponent<RectTransform>();
        headerRT.anchorMin        = new Vector2(0, 1);
        headerRT.anchorMax        = new Vector2(1, 1);
        headerRT.pivot            = new Vector2(0.5f, 1);
        headerRT.sizeDelta        = new Vector2(0, HeaderHeight);
        headerRT.anchoredPosition = Vector2.zero;

        var titleTmp = MakeTMP("Title", header.transform, "OPTIONS", 26, ColAmber, FontStyles.Bold);
        var titleRT  = titleTmp.GetComponent<RectTransform>();
        titleRT.anchorMin = Vector2.zero;
        titleRT.anchorMax = Vector2.one;
        titleRT.offsetMin = new Vector2(40, 0);
        titleRT.offsetMax = new Vector2(-70, 0);
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var closeBtn = MakeButton("CloseBtn", header.transform, "X", 20, ColAmber, ColBtnDark);
        var closeBtnRT = closeBtn.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1, 0);
        closeBtnRT.anchorMax = new Vector2(1, 1);
        closeBtnRT.pivot     = new Vector2(1, 0.5f);
        closeBtnRT.sizeDelta = new Vector2(64, 0);
        closeBtn.GetComponent<Button>().onClick.AddListener(
            () => FindFirstObjectByType<MainMenuController>()?.OnOptions());

        MakeDivider("TopLine", card.transform, isTop: true);

        // ── Scrollable content ────────────────────────────────────────────────
        var content   = MakeGO("Content", card.transform);
        var contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(0, FooterHeight);
        contentRT.offsetMax = new Vector2(0, -HeaderHeight);

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.padding             = new RectOffset(60, 60, 20, 20);
        vlg.spacing             = 8;
        vlg.childControlWidth   = true;
        vlg.childControlHeight  = false;
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;

        // AUDIO
        AddSection(">> AUDIO", content.transform);
        _masterSlider = AddSliderRow("Volume général", content.transform, 0f, 1f, 1f, out _masterValue);
        _masterSlider.onValueChanged.AddListener(v =>
        {
            AudioListener.volume = v;
            _masterValue.text = Mathf.RoundToInt(v * 100f) + "%";
        });

        // GRAPHISMES
        AddSection(">> GRAPHISMES", content.transform);
        _qualityValue = AddChoiceRow("Qualité", content.transform, "",
            () => StepQuality(-1), () => StepQuality(1));

        _fullscreenValue = AddChoiceRow("Plein écran", content.transform, "NON",
            () => ToggleFullscreen(), () => ToggleFullscreen());

        _resolutions = Screen.resolutions
            .GroupBy(r => (r.width, r.height))
            .Select(g => g.Last())
            .OrderBy(r => r.width)
            .ToArray();
        _resolutionValue = AddChoiceRow("Résolution", content.transform, "-",
            () => StepResolution(-1), () => StepResolution(1));

        // GAMEPLAY
        AddSection(">> GAMEPLAY", content.transform);
        _sensitivitySlider = AddSliderRow("Sensibilité souris", content.transform, 0.1f, 2f, 1f, out _sensitivityValue);
        _sensitivitySlider.onValueChanged.AddListener(v => _sensitivityValue.text = v.ToString("F1"));

        // ── Footer with Apply button ──────────────────────────────────────────
        MakeDivider("BottomLine", card.transform, isTop: false);

        var footer   = MakeGO("Footer", card.transform);
        var footerRT = footer.AddComponent<RectTransform>();
        footerRT.anchorMin        = new Vector2(0, 0);
        footerRT.anchorMax        = new Vector2(1, 0);
        footerRT.pivot            = new Vector2(0.5f, 0);
        footerRT.sizeDelta        = new Vector2(0, FooterHeight);
        footerRT.anchoredPosition = Vector2.zero;

        var applyBtn   = MakeButton("ApplyBtn", footer.transform, "APPLIQUER", 15, new Color(0.04f, 0.02f, 0.01f, 1f), ColAmber);
        var applyBtnRT = applyBtn.GetComponent<RectTransform>();
        applyBtnRT.anchorMin = applyBtnRT.anchorMax = new Vector2(0.5f, 0.5f);
        applyBtnRT.pivot     = new Vector2(0.5f, 0.5f);
        applyBtnRT.sizeDelta = new Vector2(240, 46);
        applyBtn.GetComponent<Button>().onClick.AddListener(Apply);
    }

    // ── UI Factory Helpers ───────────────────────────────────────────────────

    private void AddSection(string text, Transform parent)
    {
        var tmp = MakeTMP("Sec_" + text, parent, text, FontSection, ColAmber, FontStyles.Bold);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.gameObject.AddComponent<LayoutElement>().preferredHeight = SectionHeight;
    }

    private Slider AddSliderRow(string label, Transform parent, float min, float max, float val,
        out TextMeshProUGUI valueTmp)
    {
        var row = MakeRow("Row_" + label, parent, RowHeight);

        var lbl = MakeTMP("Lbl", row.transform, label, FontLabel, ColAmberDim);
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        lbl.gameObject.AddComponent<LayoutElement>().preferredWidth = LabelWidth;

        // Slider stretches to fill remaining width
        var sliderGO = MakeGO("Slider", row.transform);
        sliderGO.AddComponent<RectTransform>();
        var sliderLE = sliderGO.AddComponent<LayoutElement>();
        sliderLE.flexibleWidth = 1f;
        var slider = BuildSlider(sliderGO);
        slider.minValue = min;
        slider.maxValue = max;
        slider.value    = val;

        valueTmp = MakeTMP("Val", row.transform, "", FontValue, ColAmber, FontStyles.Bold);
        valueTmp.alignment = TextAlignmentOptions.MidlineRight;
        valueTmp.gameObject.AddComponent<LayoutElement>().preferredWidth = ValueWidth;

        return slider;
    }

    private TextMeshProUGUI AddChoiceRow(string label, Transform parent,
        string initial, System.Action onPrev, System.Action onNext)
    {
        var row = MakeRow("Row_" + label, parent, RowHeight);

        var lbl = MakeTMP("Lbl", row.transform, label, FontLabel, ColAmberDim);
        lbl.alignment = TextAlignmentOptions.MidlineLeft;
        lbl.gameObject.AddComponent<LayoutElement>().preferredWidth = LabelWidth;

        var prevBtn = MakeButton("Prev", row.transform, "<", 13, ColAmber, ColBtnDark);
        prevBtn.GetComponent<Button>().onClick.AddListener(() => onPrev());
        prevBtn.AddComponent<LayoutElement>().preferredWidth = 48;

        // Value label stretches to fill remaining width
        var val = MakeTMP("Val", row.transform, initial, FontValue, ColAmber, FontStyles.Bold);
        val.alignment = TextAlignmentOptions.Center;
        val.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        var nextBtn = MakeButton("Next", row.transform, ">", 13, ColAmber, ColBtnDark);
        nextBtn.GetComponent<Button>().onClick.AddListener(() => onNext());
        nextBtn.AddComponent<LayoutElement>().preferredWidth = 48;

        return val;
    }

    private GameObject MakeRow(string name, Transform parent, float height)
    {
        var go  = MakeGO(name, parent);
        go.AddComponent<RectTransform>();
        go.AddComponent<LayoutElement>().preferredHeight = height;
        var hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment         = TextAnchor.MiddleLeft;
        hlg.childControlHeight     = true;
        hlg.childControlWidth      = true;
        hlg.childForceExpandHeight = true;
        hlg.childForceExpandWidth  = false;
        hlg.spacing = 16;
        return go;
    }

    private Slider BuildSlider(GameObject parent)
    {
        var bg   = MakeImage("Bg", parent.transform, ColSliderBg);
        var bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.35f);
        bgRT.anchorMax = new Vector2(1, 0.65f);
        bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;

        var fillArea   = MakeGO("FillArea", parent.transform);
        var fillAreaRT = fillArea.AddComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0, 0.35f);
        fillAreaRT.anchorMax = new Vector2(1, 0.65f);
        fillAreaRT.offsetMin = new Vector2(5, 0);
        fillAreaRT.offsetMax = new Vector2(-10, 0);

        var fill   = MakeImage("Fill", fillArea.transform, ColAmber);
        var fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0, 1);
        fillRT.sizeDelta = Vector2.zero;

        var handleArea   = MakeGO("HandleArea", parent.transform);
        var handleAreaRT = handleArea.AddComponent<RectTransform>();
        handleAreaRT.anchorMin = new Vector2(0, 0);
        handleAreaRT.anchorMax = new Vector2(1, 1);
        handleAreaRT.offsetMin = new Vector2(8, 0);
        handleAreaRT.offsetMax = new Vector2(-8, 0);

        var handle   = MakeImage("Handle", handleArea.transform, Color.white);
        var handleRT = handle.GetComponent<RectTransform>();
        handleRT.anchorMin = handleRT.anchorMax = new Vector2(0, 0.5f);
        handleRT.pivot     = new Vector2(0.5f, 0.5f);
        handleRT.sizeDelta = new Vector2(18, 18);

        var slider = parent.AddComponent<Slider>();
        slider.fillRect   = fillRT;
        slider.handleRect = handleRT;
        slider.direction  = Slider.Direction.LeftToRight;
        return slider;
    }

    private void MakeDivider(string name, Transform parent, bool isTop)
    {
        var div   = MakeImage(name, parent, new Color(ColAmber.r, ColAmber.g, ColAmber.b, 0.25f));
        var divRT = div.GetComponent<RectTransform>();
        if (isTop)
        {
            divRT.anchorMin = new Vector2(0.02f, 1);
            divRT.anchorMax = new Vector2(0.98f, 1);
            divRT.pivot     = new Vector2(0.5f, 1);
            divRT.sizeDelta = new Vector2(0, 1);
            divRT.anchoredPosition = new Vector2(0, -HeaderHeight);
        }
        else
        {
            divRT.anchorMin = new Vector2(0.02f, 0);
            divRT.anchorMax = new Vector2(0.98f, 0);
            divRT.pivot     = new Vector2(0.5f, 0);
            divRT.sizeDelta = new Vector2(0, 1);
            divRT.anchoredPosition = new Vector2(0, FooterHeight);
        }
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static GameObject MakeGO(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject MakeImage(string name, Transform parent, Color color)
    {
        var go  = MakeGO(name, parent);
        go.AddComponent<RectTransform>();
        go.AddComponent<Image>().color = color;
        return go;
    }

    private static TextMeshProUGUI MakeTMP(string name, Transform parent, string text,
        float size, Color color, FontStyles style = FontStyles.Normal)
    {
        var go  = MakeGO(name, parent);
        go.AddComponent<RectTransform>();
        var tmp       = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static GameObject MakeButton(string name, Transform parent,
        string label, float fontSize, Color textColor, Color bgColor)
    {
        var go  = MakeImage(name, parent, bgColor);
        var btn = go.AddComponent<Button>();
        var cb  = btn.colors;
        cb.highlightedColor = new Color(
            Mathf.Min(1f, bgColor.r * 1.35f),
            Mathf.Min(1f, bgColor.g * 1.35f),
            Mathf.Min(1f, bgColor.b * 1.25f), 1f);
        cb.pressedColor = new Color(bgColor.r * 0.75f, bgColor.g * 0.75f, bgColor.b * 0.75f, 1f);
        btn.colors = cb;

        var lbl = MakeTMP("Lbl", go.transform, label, fontSize, textColor, FontStyles.Bold);
        StretchFull(lbl.GetComponent<RectTransform>());
        lbl.alignment = TextAlignmentOptions.Center;
        return go;
    }
}
