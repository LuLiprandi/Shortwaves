using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuSavePanel : MonoBehaviour
{
    [Header("Slot 0 — Intro Jour 1")]
    [SerializeField] private Button          slot0Button;
    [SerializeField] private Image           slot0Background;
    [SerializeField] private TextMeshProUGUI slot0LoadLabel;

    [Header("Slot 1 — Jour 2")]
    [SerializeField] private Button          slot1Button;
    [SerializeField] private Image           slot1Background;
    [SerializeField] private TextMeshProUGUI slot1LoadLabel;

    [Header("Slot 2 — Jour 3")]
    [SerializeField] private Button          slot2Button;
    [SerializeField] private Image           slot2Background;
    [SerializeField] private TextMeshProUGUI slot2LoadLabel;

    [Header("Slot 3 — Jour 4 Fin A")]
    [SerializeField] private Button          slot3Button;
    [SerializeField] private Image           slot3Background;
    [SerializeField] private TextMeshProUGUI slot3LoadLabel;

    [Header("Slot 4 — Jour 4 Fin B")]
    [SerializeField] private Button          slot4Button;
    [SerializeField] private Image           slot4Background;
    [SerializeField] private TextMeshProUGUI slot4LoadLabel;

    [Header("Reset")]
    [SerializeField] private Button resetButton;

    [Header("Slot container (scroll)")]
    [SerializeField] private Transform slotContainer;

    private static readonly Color ActiveBg   = new Color(0.22f, 0.17f, 0.04f, 1f);
    private static readonly Color InactiveBg = new Color(0.12f, 0.10f, 0.05f, 1f);
    private static readonly Color AccentCol  = new Color(1f, 0.65f, 0f, 1f);
    private static readonly Color DimCol     = new Color(0.7f, 0.55f, 0.25f, 1f);

    [Header("Scène de jeu")]
    [SerializeField] private string gameSceneName = "SampleScene";

    private void OnEnable() => Refresh();

    private void Start()
    {
        slot0Button?.onClick.AddListener(() => LoadSlot(0));
        slot1Button?.onClick.AddListener(() => LoadSlot(1));
        slot2Button?.onClick.AddListener(() => LoadSlot(2));
        slot3Button?.onClick.AddListener(() => LoadSlot(3));
        slot4Button?.onClick.AddListener(() => LoadSlot(4));
        resetButton?.onClick.AddListener(() => { SaveSlotManager.ResetAll(); Refresh(); });
    }

    private void LoadSlot(int index)
    {
        SaveSlotManager.Apply(SaveSlotManager.Slots[index]);
        SceneManager.LoadScene(gameSceneName);
    }

    private void Refresh()
    {
        int currentDay       = SaveSlotManager.GetCurrentDay();
        int currentDay2Choice = PlayerPrefs.GetInt("gsm_day2choice", 0);

        SetSlotState(slot0Background, slot0LoadLabel,
            SaveSlotManager.Slots[0].Day == currentDay);

        SetSlotState(slot1Background, slot1LoadLabel,
            SaveSlotManager.Slots[1].Day == currentDay);

        SetSlotState(slot2Background, slot2LoadLabel,
            SaveSlotManager.Slots[2].Day == currentDay && currentDay == 3);

        SetSlotState(slot3Background, slot3LoadLabel,
            currentDay == 4 && currentDay2Choice == 1);

        SetSlotState(slot4Background, slot4LoadLabel,
            currentDay == 4 && currentDay2Choice == 2);
    }

    private static void SetSlotState(Image bg, TextMeshProUGUI label, bool isActive)
    {
        if (bg    != null) bg.color    = isActive ? ActiveBg : InactiveBg;
        if (label != null)
        {
            label.text  = isActive ? "ACTIF" : "CHARGER";
            label.color = isActive ? AccentCol : DimCol;
        }
    }
}
