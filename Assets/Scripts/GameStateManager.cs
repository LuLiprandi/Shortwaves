using System;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public int  CurrentDay        { get; private set; } = 1;
    public bool IsCutsceneActive  { get; private set; } = false;
    public bool IsPostAnomaly     { get; private set; } = false;
    public bool IsBlockingUIOpen  { get; private set; } = false;

    public event Action OnCutsceneStarted;
    public event Action OnCutsceneEnded;
    public event Action<int> OnDayChanged;
    public event Action OnAnomalyTriggered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Locks player control and signals cutscene start.</summary>
    public void StartCutscene()
    {
        IsCutsceneActive = true;
        OnCutsceneStarted?.Invoke();
    }

    /// <summary>Unlocks player control and signals cutscene end.</summary>
    public void EndCutscene()
    {
        IsCutsceneActive = false;
        OnCutsceneEnded?.Invoke();
    }

    /// <summary>Marks the anomaly as triggered for the current day.</summary>
    public void TriggerAnomaly()
    {
        if (IsPostAnomaly) return;

        IsPostAnomaly = true;
        OnAnomalyTriggered?.Invoke();
    }

    /// <summary>Advances to the next day and resets daily states.</summary>
    public void NextDay()
    {
        CurrentDay++;
        IsPostAnomaly = false;
        OnDayChanged?.Invoke(CurrentDay);
    }

    /// <summary>Signals that a full-screen blocking UI (journal, etc.) is open.</summary>
    public void OpenBlockingUI()  => IsBlockingUIOpen = true;

    /// <summary>Signals that the blocking UI has been closed.</summary>
    public void CloseBlockingUI() => IsBlockingUIOpen = false;
}
