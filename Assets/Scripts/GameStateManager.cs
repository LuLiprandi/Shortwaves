using System;
using Shortwaves;
using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("Dev")]
    [Tooltip("Décocher pendant le développement pour toujours repartir au Jour 1 sans persistance.")]
    [SerializeField] private bool persistState = true;

    public int            CurrentDay       { get; private set; } = 1;
    public bool           IsCutsceneActive { get; private set; } = false;
    public bool           IsPostAnomaly    { get; private set; } = false;
    public Day2DoorChoice Day2Choice       { get; private set; } = Day2DoorChoice.None;

    public bool IsBlockingUIOpen => _blockingUICount > 0;

    public event Action        OnCutsceneStarted;
    public event Action        OnCutsceneEnded;
    public event Action<int>   OnDayChanged;
    public event Action        OnAnomalyTriggered;
    public event Action        OnBlockingUIOpened;
    public event Action        OnBlockingUIClosed;

    private const string PrefDay         = "gsm_day";
    private const string PrefPostAnomaly = "gsm_post";
    private const string PrefDay2Choice  = "gsm_day2choice";

    private int _blockingUICount = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (persistState)
            LoadState();
    }

    private void OnApplicationQuit()
    {
        if (persistState)
            SaveState();
    }

    public void StartCutscene()
    {
        IsCutsceneActive = true;
        OnCutsceneStarted?.Invoke();
    }

    public void EndCutscene()
    {
        IsCutsceneActive = false;
        OnCutsceneEnded?.Invoke();
    }

    public void TriggerAnomaly()
    {
        if (IsPostAnomaly) return;
        IsPostAnomaly = true;
        OnAnomalyTriggered?.Invoke();
    }

    public void NextDay()
    {
        CurrentDay++;
        IsPostAnomaly = false;
        OnDayChanged?.Invoke(CurrentDay);
    }

    public void SetDay2Choice(Day2DoorChoice choice)
    {
        Day2Choice = choice;
        PlayerPrefs.SetInt(PrefDay2Choice, (int)choice);
        PlayerPrefs.Save();
    }

    public void OpenBlockingUI()
    {
        _blockingUICount++;
        if (_blockingUICount == 1)
            OnBlockingUIOpened?.Invoke();
    }

    public void CloseBlockingUI()
    {
        _blockingUICount = Mathf.Max(0, _blockingUICount - 1);
        if (_blockingUICount == 0)
            OnBlockingUIClosed?.Invoke();
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(PrefDay, CurrentDay);
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        CurrentDay    = PlayerPrefs.GetInt(PrefDay, 1);
        Day2Choice    = (Day2DoorChoice)PlayerPrefs.GetInt(PrefDay2Choice, 0);
        IsPostAnomaly = false;
    }

    /// <summary>
    /// Reloads the game state from PlayerPrefs and fires OnDayChanged so all
    /// listeners (RadioSystem, etc.) refresh their day-dependent data.
    /// Call this after writing a new save slot via SaveSlotManager.Apply().
    /// </summary>
    public void ReloadState()
    {
        LoadState();
        OnDayChanged?.Invoke(CurrentDay);
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(8, 8, 240, 130));
        GUI.Box(new Rect(0, 0, 240, 130), "GameStateManager");
        GUILayout.Space(20);
        GUILayout.Label($"  Jour          : {CurrentDay}");
        GUILayout.Label($"  Post-anomalie : {IsPostAnomaly}");
        GUILayout.Label($"  Cutscène      : {IsCutsceneActive}");
        GUILayout.Label($"  UI bloquante  : {IsBlockingUIOpen} ({_blockingUICount})");
        GUILayout.Label($"  Persistance   : {persistState}");
        GUILayout.EndArea();
    }
#endif
}
