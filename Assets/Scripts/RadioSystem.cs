using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum RadioState { Idle, Tuning, QTE, Decoded }

/// <summary>Main radio controller: maps knob rotation to frequency, detects stations, and drives the QTE.</summary>
public class RadioSystem : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private float frequencyMin = 88f;
    [SerializeField] private float frequencyMax = 108f;
    [SerializeField] private RadioStationData[] stations;

    [Header("Références")]
    [SerializeField] private RotatableKnob knob;
    [SerializeField] private AudioSource decodingAudioSource;
    [SerializeField] private RadioFrequencyVisualizer frequencyVisualizer;
    [SerializeField] private RadioQTEGauge qteGauge;
    [SerializeField] private RadioDecoderPanel decoderPanel;
    [SerializeField] private SubtitleSystem subtitleSystem;
    [SerializeField] private CameraFocusController focusController;

    [Header("Audio")]
    [SerializeField] private float signalFadeSpeed = 2f;

    public RadioState State { get; private set; } = RadioState.Idle;
    public float CurrentFrequency { get; private set; }
    public bool IsNearStation { get; private set; }

    public event Action<RadioStationData> OnStationDecoded;

    /// <summary>
    /// Fired when the anomaly broadcast sequence completes (voice clip finished playing).
    /// </summary>
    public event Action OnAnomalyBroadcastComplete;

    private bool isActive;
    private RadioStationData activeStation;

    /// <summary>Activates or deactivates the radio interaction system.</summary>
    public void SetActive(bool active)
    {
        isActive = active;

        if (!active)
        {
            StopDecoding();
            if (State == RadioState.QTE) ExitQTE();
            decoderPanel?.Hide();
            subtitleSystem?.Stop();
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

        // En état Decoded : on garde les barres animées mais on ne fait rien d'autre
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
    }

    private void ExitQTE()
    {
        State = RadioState.Tuning;
        qteGauge.SetVisible(false);
        qteGauge.StopQTE();
        qteGauge.OnSuccess -= HandleQTESuccess;
        qteGauge.OnFail -= HandleQTEFail;
        frequencyVisualizer.HideQTEAlert();
    }

    private void HandleQTESuccess()
    {
        State = RadioState.Decoded;
        qteGauge.SetVisible(false);
        frequencyVisualizer.HideQTEAlert();

        // Arrêter le son de proximité, jouer le message vocal une seule fois
        StopDecoding();
        if (activeStation.VoiceClip != null)
        {
            decodingAudioSource.clip   = activeStation.VoiceClip;
            decodingAudioSource.loop   = false;
            decodingAudioSource.volume = 1f;
            decodingAudioSource.Play();

            if (subtitleSystem != null && activeStation.Subtitles.Length > 0)
                subtitleSystem.Play(decodingAudioSource, activeStation.Subtitles);
        }

        // Ouvrir le décodeur à slots — le joueur entre le code après avoir écouté
        decoderPanel?.Initialize(activeStation.SolutionCode);
        OnStationDecoded?.Invoke(activeStation);

        // Déverrouille l'onglet décodage du journal pour le jour en cours
        JournalManager.Instance?.GetJournalPanel()?.UnlockDecoder();
    }

    private void HandleQTEFail()
    {
        ExitQTE();
    }

    /// <summary>
    /// Forces the radio to broadcast an anomaly clip at the given frequency target (normalized 0-1).
    /// No QTE — plays once then returns the radio to static.
    /// </summary>
    public void TriggerAnomalyBroadcast(AudioClip voiceClip, float targetNormalized,
        SubtitleEntry[] subtitles = null)
    {
        StartCoroutine(AnomalyBroadcastRoutine(voiceClip, targetNormalized, subtitles));
    }

    private System.Collections.IEnumerator AnomalyBroadcastRoutine(AudioClip voiceClip,
        float targetNormalized, SubtitleEntry[] subtitles)
    {
        // Force radio state and move the knob to the target frequency
        if (State == RadioState.QTE) ExitQTE();
        State = RadioState.Decoded;

        if (knob != null)
            knob.SetNormalizedValue(targetNormalized);

        // Short buzz before the clear signal
        StopDecoding();
        yield return new WaitForSeconds(0.4f);

        // Play the anomaly voice clip once
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

        // Return to static / tuning
        State = RadioState.Tuning;
        StopDecoding();

        OnAnomalyBroadcastComplete?.Invoke();
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
