using System;
using UnityEngine;

/// <summary>
/// Singleton — source de vérité de l'état global du jeu.
/// Tous les autres systèmes lisent cet état et s'abonnent à ses événements ;
/// ils ne se cherchent jamais entre eux via FindObjectOfType au runtime.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────

    public static GameStateManager Instance { get; private set; }

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Dev")]
    [Tooltip("Décocher pendant le développement pour toujours repartir au Jour 1 sans persistance.")]
    [SerializeField] private bool persistState = true;

    // ── État public (lecture seule) ───────────────────────────────────────────

    public int  CurrentDay       { get; private set; } = 1;
    public bool IsCutsceneActive { get; private set; } = false;
    public bool IsPostAnomaly    { get; private set; } = false;

    /// <summary>
    /// Vrai si au moins un système a ouvert une UI bloquante (journal, décodeur, options…).
    /// Utilise un compteur interne pour gérer plusieurs ouvertures simultanées.
    /// </summary>
    public bool IsBlockingUIOpen => _blockingUICount > 0;

    // ── Événements ────────────────────────────────────────────────────────────

    public event Action        OnCutsceneStarted;
    public event Action        OnCutsceneEnded;
    public event Action<int>   OnDayChanged;
    public event Action        OnAnomalyTriggered;
    public event Action        OnBlockingUIOpened;
    public event Action        OnBlockingUIClosed;

    // ── Privé ─────────────────────────────────────────────────────────────────

    private const string PrefDay          = "gsm_day";
    private const string PrefPostAnomaly  = "gsm_post";

    private int _blockingUICount = 0;

    // ── Unity lifecycle ───────────────────────────────────────────────────────

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

    // ── API publique — cutscènes ──────────────────────────────────────────────

    /// <summary>Verrouille les entrées joueur et signale le début d'une cutscène.</summary>
    public void StartCutscene()
    {
        IsCutsceneActive = true;
        OnCutsceneStarted?.Invoke();
    }

    /// <summary>Déverrouille les entrées joueur et signale la fin d'une cutscène.</summary>
    public void EndCutscene()
    {
        IsCutsceneActive = false;
        OnCutsceneEnded?.Invoke();
    }

    // ── API publique — progression ────────────────────────────────────────────

    /// <summary>Déclenche l'anomalie du jour en cours. Sans effet si déjà en post-anomalie.</summary>
    public void TriggerAnomaly()
    {
        if (IsPostAnomaly) return;
        IsPostAnomaly = true;
        OnAnomalyTriggered?.Invoke();
    }

    /// <summary>Passe au jour suivant et remet à zéro les états journaliers.</summary>
    public void NextDay()
    {
        CurrentDay++;
        IsPostAnomaly = false;
        OnDayChanged?.Invoke(CurrentDay);
    }

    // ── API publique — UI bloquante ───────────────────────────────────────────

    /// <summary>
    /// Signale qu'une UI bloquante vient de s'ouvrir.
    /// Chaque appel doit être compensé par un CloseBlockingUI().
    /// </summary>
    public void OpenBlockingUI()
    {
        _blockingUICount++;
        if (_blockingUICount == 1)
            OnBlockingUIOpened?.Invoke();
    }

    /// <summary>
    /// Signale qu'une UI bloquante vient de se fermer.
    /// N'envoie OnBlockingUIClosed que quand toutes les UIs sont fermées.
    /// </summary>
    public void CloseBlockingUI()
    {
        _blockingUICount = Mathf.Max(0, _blockingUICount - 1);
        if (_blockingUICount == 0)
            OnBlockingUIClosed?.Invoke();
    }

    // ── Persistance ───────────────────────────────────────────────────────────

    private void SaveState()
    {
        PlayerPrefs.SetInt(PrefDay,         CurrentDay);
        PlayerPrefs.SetInt(PrefPostAnomaly, IsPostAnomaly ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        CurrentDay    = PlayerPrefs.GetInt(PrefDay,         1);
        IsPostAnomaly = PlayerPrefs.GetInt(PrefPostAnomaly, 0) == 1;
    }

#if UNITY_EDITOR
    // ── Debug GUI (éditeur uniquement) ────────────────────────────────────────

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
