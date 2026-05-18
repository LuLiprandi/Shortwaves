using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum RadioState { Idle, Tuning, QTE, Decoded }

public class RadioSystem : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float frequencyMin = 88f;
    [SerializeField] private float frequencyMax = 108f;

    [Tooltip("Station unique utilisée si stationsPerDay est vide (rétro-compatibilité).")]
    [SerializeField] private RadioStationData[] stations;

    [Header("Stations par jour (index 0 = Jour 1, index 1 = Jour 2, …)")]
    [Tooltip("Une RadioStationData par jour. Au démarrage, seule la station du jour courant est active. " +
             "Si ce tableau est rempli, il remplace le tableau 'stations' ci-dessus.")]
    [SerializeField] private RadioStationData[] stationsPerDay;

    [Header("Références")]
    [SerializeField] private RotatableKnob knob;
    [SerializeField] private AudioSource decodingAudioSource;
    [SerializeField] private RadioFrequencyVisualizer frequencyVisualizer;
    [SerializeField] private RadioQTEGauge qteGauge;
    [SerializeField] private RadioDecoderPanel decoderPanel;
    [SerializeField] private SubtitleSystem subtitleSystem;
    [SerializeField] private CameraFocusController focusController;
    [SerializeField] private RadioInspectable radioInspectable;

    [Header("Audio")]
    [SerializeField] private float signalFadeSpeed = 2f;

    public RadioState State { get; private set; } = RadioState.Idle;
    public float CurrentFrequency { get; private set; }
    public bool IsNearStation { get; private set; }

    public event Action<RadioStationData> OnStationDecoded;
    public event Action OnAnomalyBroadcastComplete;

    private bool isActive;
    private RadioStationData activeStation;

    private void Start()
    {
        ApplyDayStation();
    }

    private void ApplyDayStation()
    {
        if (stationsPerDay == null || stationsPerDay.Length == 0) return;

        int dayIndex = (GameStateManager.Instance?.CurrentDay ?? 1) - 1;
        dayIndex = Mathf.Clamp(dayIndex, 0, stationsPerDay.Length - 1);

        RadioStationData dayStation = stationsPerDay[dayIndex];
        stations = dayStation != null
            ? new RadioStationData[] { dayStation }
            : System.Array.Empty<RadioStationData>();
    }

    public void SetActive(bool active)
    {
        isActive = active;

        if (!active)
        {
            if (State != RadioState.Decoded)
            {
                StopDecoding();
                subtitleSystem?.Stop();
            }

            if (State == RadioState.QTE) ExitQTE();
            decoderPanel?.Hide();
            State = RadioState.Idle;
        }
        else if (State == RadioState.Idle)
        {
            State = RadioState.Tuning;
        }

        frequencyVisualizer.SetVisible(active);
        qteGauge.SetVisible(false);
    }

    private void Update()
    {
        if (!isActive) return;

        CurrentFrequency = Mathf.Lerp(frequencyMin, frequencyMax, knob.NormalizedValue);

        if (State == RadioState.Decoded)
        {
            frequencyVisualizer.UpdateVisualizer(CurrentFrequency, 0f, knob.NormalizedValue);
            return;
        }

        RadioStationData nearest = FindNearestStation(out float distance);
        IsNearStation = nearest != null && distance <= nearest.ProximityRangeMHz;

        UpdateDecodingAudio(nearest, distance);

        float signalStrength = IsNearStation && nearest != null
            ? 1f - Mathf.Clamp01(distance / nearest.ProximityRangeMHz)
            : 0f;

        switch (State)
        {
            case RadioState.Tuning:
                if (IsNearStation && nearest != null && distance <= nearest.LockRangeMHz)
                {
                    activeStation = nearest;
                    EnterQTE();
                }
                break;

            case RadioState.QTE:
                bool stillLocked = IsNearStation && nearest == activeStation && distance <= activeStation.LockRangeMHz * 1.5f;
                if (!stillLocked)
                    ExitQTE();
                else
                    HandleQTEInput();
                break;
        }

        frequencyVisualizer.UpdateVisualizer(CurrentFrequency, signalStrength, knob.NormalizedValue);
    }

    private void UpdateDecodingAudio(RadioStationData nearest, float distance)
    {
        if (IsNearStation && nearest != null && nearest.ProximityClip != null)
        {
            float t = 1f - Mathf.Clamp01(distance / nearest.ProximityRangeMHz);
            float targetVolume = Mathf.Lerp(0f, 1f, t);

            if (!decodingAudioSource.isPlaying || decodingAudioSource.clip != nearest.ProximityClip)
            {
                decodingAudioSource.clip = nearest.ProximityClip;
                decodingAudioSource.loop = true;
                decodingAudioSource.Play();
            }

            decodingAudioSource.volume = Mathf.MoveTowards(decodingAudioSource.volume, targetVolume, Time.deltaTime * signalFadeSpeed);
        }
        else
        {
            decodingAudioSource.volume = Mathf.MoveTowards(decodingAudioSource.volume, 0f, Time.deltaTime * signalFadeSpeed);
            if (decodingAudioSource.volume <= 0f && decodingAudioSource.isPlaying)
                StopDecoding();
        }
    }

    private void HandleQTEInput()
    {
        if (focusController == null || !focusController.IsFocused) return;
        if (Keyboard.current == null) return;

        float direction = 0f;
        if (Keyboard.current.leftArrowKey.isPressed)  direction -= 1f;
        if (Keyboard.current.rightArrowKey.isPressed) direction += 1f;

        if (direction != 0f)
            qteGauge.PushInput(direction);
    }

    private void EnterQTE()
    {
        State = RadioState.QTE;
        qteGauge.SetVisible(true);
        qteGauge.StartQTE(activeStation.QTESuccessDuration);
        qteGauge.OnSuccess += HandleQTESuccess;
        qteGauge.OnFail += HandleQTEFail;
        frequencyVisualizer.ShowQTEAlert();

        if (focusController != null)
            focusController.EscapeInterceptor = () => { ExitQTE(); return true; };
    }

    private void ExitQTE()
    {
        State = RadioState.Tuning;
        qteGauge.SetVisible(false);
        qteGauge.StopQTE();
        qteGauge.OnSuccess -= HandleQTESuccess;
        qteGauge.OnFail -= HandleQTEFail;
        frequencyVisualizer.HideQTEAlert();

        if (focusController != null)
            focusController.EscapeInterceptor = null;
    }

    private void HandleQTESuccess()
    {
        State = RadioState.Decoded;
        qteGauge.SetVisible(false);
        frequencyVisualizer.HideQTEAlert();

        StopDecoding();

        AudioClip       voiceClip = activeStation?.VoiceClip;
        SubtitleEntry[] subs      = activeStation?.Subtitles ?? System.Array.Empty<SubtitleEntry>();

        if (voiceClip != null)
        {
            decodingAudioSource.clip   = voiceClip;
            decodingAudioSource.loop   = false;
            decodingAudioSource.volume = 1f;
            decodingAudioSource.Play();

            if (subtitleSystem != null && subs != null && subs.Length > 0)
                subtitleSystem.Play(decodingAudioSource, subs);
        }

        decoderPanel?.Initialize(activeStation?.SolutionCode ?? "");
        OnStationDecoded?.Invoke(activeStation);

        JournalManager.Instance?.GetJournalPanel()?.UnlockDecoder();

        if (focusController != null)
            focusController.LockEscape = true;

        JournalManager.Instance?.OpenOnDecoderTab();
    }

    public void HideFrequencyVisualizer()
    {
        frequencyVisualizer?.SetVisible(false);
    }

    public void ReleaseAfterAnomaly()
    {
        if (focusController != null)
            focusController.LockEscape = false;

        radioInspectable?.ResetFocusState();

        ExitFocusAndDeactivate();
    }

    public void LockInteraction()
    {
        SetActive(false);
        radioInspectable?.Lock();
    }

    private void ExitFocusAndDeactivate()
    {
        SetActive(false);
        focusController?.ExitFocus();
    }

    private void HandleQTEFail()
    {
        ExitQTE();
    }

    public void TriggerAnomalyBroadcast(AudioClip voiceClip, float targetNormalized,
        SubtitleEntry[] subtitles = null)
    {
        StartCoroutine(AnomalyBroadcastRoutine(voiceClip, targetNormalized, subtitles));
    }

    private System.Collections.IEnumerator AnomalyBroadcastRoutine(AudioClip voiceClip,
        float targetNormalized, SubtitleEntry[] subtitles)
    {
        if (State == RadioState.QTE) ExitQTE();
        State = RadioState.Decoded;

        if (knob != null)
            knob.SetNormalizedValue(targetNormalized);

        frequencyVisualizer?.SetVisible(true);

        float targetFrequency = Mathf.Lerp(frequencyMin, frequencyMax, targetNormalized);
        frequencyVisualizer?.UpdateVisualizer(targetFrequency, 0f, targetNormalized);

        StopDecoding();
        yield return new WaitForSeconds(0.4f);

        if (voiceClip != null)
        {
            decodingAudioSource.clip   = voiceClip;
            decodingAudioSource.loop   = false;
            decodingAudioSource.volume = 1f;
            decodingAudioSource.Play();

            if (subtitleSystem != null && subtitles != null && subtitles.Length > 0)
                subtitleSystem.Play(decodingAudioSource, subtitles);

            yield return new WaitUntil(() => !decodingAudioSource.isPlaying);

            if (subtitleSystem != null)
                subtitleSystem.Stop();
        }

        State = RadioState.Tuning;
        StopDecoding();

        if (!isActive)
            frequencyVisualizer?.SetVisible(false);

        OnAnomalyBroadcastComplete?.Invoke();
    }

    public void PlayStaticBurst(float duration, AudioClip overrideClip = null)
    {
        StartCoroutine(StaticBurstRoutine(duration, overrideClip));
    }

    private System.Collections.IEnumerator StaticBurstRoutine(float duration, AudioClip overrideClip)
    {
        AudioClip clip = overrideClip;
        if (clip == null && activeStation != null) clip = activeStation.ProximityClip;
        if (clip == null) yield break;

        decodingAudioSource.clip   = clip;
        decodingAudioSource.loop   = true;
        decodingAudioSource.volume = 1f;
        decodingAudioSource.Play();

        yield return new WaitForSeconds(duration);

        decodingAudioSource.Stop();
        decodingAudioSource.volume = 0f;
    }

    private void StopDecoding()
    {
        decodingAudioSource.Stop();
        decodingAudioSource.volume = 0f;
    }

    private RadioStationData FindNearestStation(out float distance)
    {
        distance = float.MaxValue;
        RadioStationData nearest = null;

        foreach (RadioStationData station in stations)
        {
            float d = Mathf.Abs(CurrentFrequency - station.FrequencyMHz);
            if (d < distance)
            {
                distance = d;
                nearest = station;
            }
        }

        return nearest;
    }
}
