using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class JournalManager : MonoBehaviour
{
    public static JournalManager Instance { get; private set; }

    [Header("Data — one entry per day, in order")]
    [SerializeField] private JournalDayData[] days;

    [Header("References")]
    [SerializeField] private JournalPanel                       journalPanel;
    [SerializeField] private AnomalySequencer                   anomalySequencer;
    [SerializeField] private Shortwaves.Day2AnomalySequencer    day2AnomalySequencer;
    [SerializeField] private Shortwaves.Day3AnomalySequencer    day3AnomalySequencer;
    [SerializeField] private Shortwaves.Day4EndingSequencer     day4EndingSequencer;

    [Header("Jour 3 — données narratives (branching J2)")]
    [Tooltip("ScriptableObject Day3Data pour les pensées du matin branching selon le choix J2.")]
    [SerializeField] private Shortwaves.Day3Data day3Data;

    private FirstPersonController playerController;
    private InteractionSystem     interactionSystem;
    private bool isOpen;

    public bool IsOpen => isOpen;

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
        {
            GameStateManager.Instance.OnAnomalyTriggered += HandleAnomalyTriggered;
            GameStateManager.Instance.OnDayChanged       += HandleDayChanged;
        }

        if (journalPanel != null)
        {
            journalPanel.OnMessageDecoded += HandleMessageDecoded;

            if (!GameStateManager.Instance.IsPostAnomaly)
                journalPanel.ClearDecoderProgress(GameStateManager.Instance.CurrentDay);
        }

        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentDay == 4)
            StartCoroutine(WaitForStartupThenBeginDay4());
    }

    private void OnDestroy()
    {
        if (GameStateManager.Instance != null)
        {
            GameStateManager.Instance.OnAnomalyTriggered -= HandleAnomalyTriggered;
            GameStateManager.Instance.OnDayChanged       -= HandleDayChanged;
        }

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

    public void Toggle() { if (isOpen) Close(); else Open(); }

    public JournalPanel GetJournalPanel() => journalPanel;

    public AudioClip GetCurrentDayVoiceClip()
    {
        try { return GetCurrentData()?.RadioVoiceClip; }
        catch (UnityEngine.UnassignedReferenceException) { return null; }
    }

    public SubtitleEntry[] GetCurrentDaySubtitles()
    {
        try { return GetCurrentData()?.RadioSubtitles ?? System.Array.Empty<SubtitleEntry>(); }
        catch (UnityEngine.UnassignedReferenceException) { return System.Array.Empty<SubtitleEntry>(); }
    }

    public void OpenOnDecoderTab()
    {
        if (isOpen) return;
        isOpen = true;

        var data     = GetCurrentData();
        var thoughts = GetMorningThoughts();

        int[]  codeSeq         = data?.CodeSequence           ?? System.Array.Empty<int>();
        int[]  hiddenIndices   = data?.HiddenSlotIndices       ?? System.Array.Empty<int>();
        string decodedSolution = data?.OfficialMessageDecoded  ?? "";

        journalPanel.ShowOnDecoderTab(GameStateManager.Instance.CurrentDay, thoughts,
            codeSeq, hiddenIndices, decodedSolution);

        if (hiddenIndices != null && hiddenIndices.Length > 0)
            journalPanel.HideSlots(hiddenIndices);

        GameStateManager.Instance.OpenBlockingUI();

        if (playerController  != null) { playerController.CanMove = false; playerController.CanLook = false; }
        if (interactionSystem != null) interactionSystem.enabled  = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        var data     = GetCurrentData();
        var thoughts = GetMorningThoughts();

        int[]  codeSeq         = data?.CodeSequence           ?? System.Array.Empty<int>();
        int[]  hiddenIndices   = data?.HiddenSlotIndices       ?? System.Array.Empty<int>();
        string decodedSolution = data?.OfficialMessageDecoded  ?? "";

        journalPanel.Show(GameStateManager.Instance.CurrentDay, thoughts,
            codeSeq, hiddenIndices, decodedSolution);

        GameStateManager.Instance.OpenBlockingUI();

        if (playerController  != null) { playerController.CanMove = false; playerController.CanLook = false; }
        if (interactionSystem != null) interactionSystem.enabled  = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;

        journalPanel.Hide();
        GameStateManager.Instance.CloseBlockingUI();

        bool isDay4 = GameStateManager.Instance != null && GameStateManager.Instance.CurrentDay == 4;
        if (!isDay4)
        {
            if (playerController != null)
            {
                playerController.CanMove = true;
                playerController.CanLook = true;
            }
            if (interactionSystem != null) interactionSystem.enabled = true;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    public void OpenWithThoughts(string thoughts)
    {
        if (isOpen) return;
        isOpen = true;

        journalPanel.ShowWithThoughts(GameStateManager.Instance.CurrentDay, thoughts);

        GameStateManager.Instance.OpenBlockingUI();

        if (playerController  != null) { playerController.CanMove = false; playerController.CanLook = false; }
        if (interactionSystem != null) interactionSystem.enabled  = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
    }

    public void OpenOnPostAnomalyThoughts()
    {
        if (isOpen) return;
        var data = GetCurrentData();
        string thoughts = data?.PostAnomalyThoughts ?? "";
        if (string.IsNullOrWhiteSpace(thoughts)) return;
        OpenWithThoughts(thoughts);
    }

    private void HandleAnomalyTriggered()
    {
        if (!isOpen) return;
        var data = GetCurrentData();
        if (data != null) journalPanel.UpdateThoughts(data.PostAnomalyThoughts);
    }

    private void HandleDayChanged(int newDay) { }

    private IEnumerator WaitForStartupThenBeginDay4()
    {
        yield return new WaitUntil(() =>
            ScreenFader.Instance == null || ScreenFader.Instance.IsStartupComplete);

        day4EndingSequencer?.BeginDay4(skipFadeIn: true);
    }

    private void HandleMessageDecoded()
    {
        var radioSystem = FindFirstObjectByType<RadioSystem>();
        radioSystem?.HideFrequencyVisualizer();

        Close();

        int day = GameStateManager.Instance.CurrentDay;
        if (day == 2)
            day2AnomalySequencer?.TriggerSequence();
        else if (day == 3)
            day3AnomalySequencer?.TriggerSequence();
        else
            anomalySequencer?.TriggerSequence();
    }

    private JournalDayData GetCurrentData()
    {
        if (days == null || days.Length == 0) return null;
        int idx = GameStateManager.Instance.CurrentDay - 1;
        return days[Mathf.Clamp(idx, 0, days.Length - 1)];
    }

    private string GetMorningThoughts()
    {
        int day = GameStateManager.Instance.CurrentDay;

        if (day == 3 && day3Data != null)
        {
            return GameStateManager.Instance.Day2Choice == Shortwaves.Day2DoorChoice.Opened
                ? day3Data.MorningThoughts_Opened
                : day3Data.MorningThoughts_Ignored;
        }

        var data = GetCurrentData();
        return GameStateManager.Instance.IsPostAnomaly
            ? data?.PostAnomalyThoughts ?? ""
            : data?.PreAnomalyThoughts  ?? "";
    }
}
