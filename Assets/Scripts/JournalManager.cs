using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton that owns the journal panel lifecycle.
/// Press J to open/close. Listens to GameStateManager.OnAnomalyTriggered to refresh thoughts live.
/// </summary>
public class JournalManager : MonoBehaviour
{
    public static JournalManager Instance { get; private set; }

    [Header("Data — one entry per day, in order")]
    [SerializeField] private JournalDayData[] days;

    [Header("References")]
    [SerializeField] private JournalPanel      journalPanel;
    [SerializeField] private AnomalySequencer  anomalySequencer;

    private FirstPersonController playerController;
    private InteractionSystem     interactionSystem;
    private bool isOpen;

    public bool IsOpen => isOpen;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        playerController  = FindFirstObjectByType<FirstPersonController>();
        interactionSystem = FindFirstObjectByType<InteractionSystem>();

        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnAnomalyTriggered += HandleAnomalyTriggered;

        if (journalPanel != null)
        {
            journalPanel.OnMessageDecoded += HandleMessageDecoded;

            // If persistence is disabled (dev mode) or CurrentDay is 1 with no saved anomaly,
            // wipe any stale decoder progress so the player always starts clean.
            if (!GameStateManager.Instance.IsPostAnomaly)
                journalPanel.ClearDecoderProgress(GameStateManager.Instance.CurrentDay);
        }
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.OnAnomalyTriggered -= HandleAnomalyTriggered;

        if (journalPanel != null)
            journalPanel.OnMessageDecoded -= HandleMessageDecoded;
    }

    private void Update()
    {
        if (Keyboard.current == null) return;
        if (GameStateManager.Instance.IsCutsceneActive) return;

        if (isOpen && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Close();
            return;
        }

        if (Keyboard.current.jKey.wasPressedThisFrame)
            Toggle();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void Toggle() { if (isOpen) Close(); else Open(); }

    /// <summary>Exposes the journal panel for external systems (e.g., RadioSystem).</summary>
    public JournalPanel GetJournalPanel() => journalPanel;

    /// <summary>
    /// Opens the journal directly on the decoder tab after the radio clip ends.
    /// Locks player input the same way as Open().
    /// </summary>
    public void OpenOnDecoderTab()
    {
        if (isOpen) return;
        isOpen = true;

        var data     = GetCurrentData();
        var thoughts = GameStateManager.Instance.IsPostAnomaly
            ? data?.PostAnomalyThoughts ?? ""
            : data?.PreAnomalyThoughts  ?? "";

        int[]  codeSeq         = data?.CodeSequence        ?? System.Array.Empty<int>();
        int[]  hiddenIndices   = data?.HiddenSlotIndices    ?? System.Array.Empty<int>();
        string decodedSolution = data?.OfficialMessageDecoded ?? "";

        journalPanel.ShowOnDecoderTab(GameStateManager.Instance.CurrentDay, thoughts,
            codeSeq, hiddenIndices, decodedSolution);

        GameStateManager.Instance.OpenBlockingUI();

        if (playerController  != null) playerController.CanMove  = false;
        if (interactionSystem != null) interactionSystem.enabled  = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    /// <summary>Opens the journal and locks player input.</summary>
    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        var data     = GetCurrentData();
        var thoughts = GameStateManager.Instance.IsPostAnomaly
            ? data?.PostAnomalyThoughts ?? ""
            : data?.PreAnomalyThoughts  ?? "";

        int[]  codeSeq        = data?.CodeSequence        ?? System.Array.Empty<int>();
        int[]  hiddenIndices  = data?.HiddenSlotIndices    ?? System.Array.Empty<int>();
        string decodedSolution = data?.OfficialMessageDecoded ?? "";

        journalPanel.Show(GameStateManager.Instance.CurrentDay, thoughts,
            codeSeq, hiddenIndices, decodedSolution);

        GameStateManager.Instance.OpenBlockingUI();

        if (playerController  != null) playerController.CanMove  = false;
        if (interactionSystem != null) interactionSystem.enabled  = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    /// <summary>Closes the journal and restores player input.</summary>
    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        journalPanel.Hide();
        GameStateManager.Instance.CloseBlockingUI();

        if (playerController  != null) playerController.CanMove  = true;
        if (interactionSystem != null) interactionSystem.enabled  = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private void HandleAnomalyTriggered()
    {
        if (!isOpen) return;
        var data = GetCurrentData();
        if (data != null) journalPanel.UpdateThoughts(data.PostAnomalyThoughts);
    }

    /// <summary>Called when the player validates the correct decoded message in the journal.</summary>
    private void HandleMessageDecoded()
    {
        GameStateManager.Instance?.TriggerAnomaly();

        // Close the journal first, then trigger the anomaly sequence
        Close();
        anomalySequencer?.TriggerSequence();
    }

    private JournalDayData GetCurrentData()
    {
        if (days == null || days.Length == 0) return null;
        int idx = GameStateManager.Instance.CurrentDay - 1;
        return days[Mathf.Clamp(idx, 0, days.Length - 1)];
    }
}
