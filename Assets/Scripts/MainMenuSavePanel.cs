using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages the save-slot panel in the main menu.
/// All UI elements are pre-built in the scene; this script only handles
/// button clicks and refreshes the active-slot highlight.
/// </summary>
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

    [Header("Reset")]
    [SerializeField] private Button resetButton;

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
        resetButton?.onClick.AddListener(() => { SaveSlotManager.ResetAll(); Refresh(); });
    }

    private void LoadSlot(int index)
    {
        SaveSlotManager.Apply(SaveSlotManager.Slots[index]);
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>Updates button labels and background tints to reflect the currently active slot.</summary>
    private void Refresh()
    {
        int currentDay = SaveSlotManager.GetCurrentDay();

        SetSlotState(slot0Background, slot0LoadLabel, SaveSlotManager.Slots[0].Day == currentDay);
        SetSlotState(slot1Background, slot1LoadLabel, SaveSlotManager.Slots[1].Day == currentDay);
        SetSlotState(slot2Background, slot2LoadLabel, SaveSlotManager.Slots[2].Day == currentDay);
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
