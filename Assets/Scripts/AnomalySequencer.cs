using System.Collections;
using UnityEngine;

/// <summary>
/// Orchestrates the Day-1 anomaly sequence once the player decodes the journal message:
///   1. Journal hides (handled by JournalManager before this is called).
///   2. Radio buzzes and snaps to 100 MHz.
///   3. Woman's voice plays once, no QTE.
///   4. Radio returns to static.
///   5. Physical carnet tilts on the desk, revealing AnomalieJ1.
/// </summary>
public class AnomalySequencer : MonoBehaviour
{
    [Header("Radio")]
    [Tooltip("The RadioSystem to drive for the anomaly broadcast.")]
    [SerializeField] private RadioSystem radioSystem;

    [Tooltip("Target frequency for the anomaly broadcast (MHz). Knob will snap here.")]
    [SerializeField] private float anomalyFrequencyMHz = 100f;

    [Tooltip("Anomaly voice clip — woman's voice played once.")]
    [SerializeField] private AudioClip anomalyVoiceClip;

    [Tooltip("Optional subtitles for the anomaly voice clip.")]
    [SerializeField] private SubtitleEntry[] anomalySubtitles = System.Array.Empty<SubtitleEntry>();

    [Header("Desk props")]
    [Tooltip("The physical carnet (notebook) GameObject in the scene.")]
    [SerializeField] private Transform carnet;

    [Tooltip("Target local rotation for the tilted carnet (Euler angles).")]
    [SerializeField] private Vector3 carnetTiltEuler = new Vector3(15f, 354.12f, 20f);

    [Tooltip("Duration in seconds for the carnet tilt animation.")]
    [SerializeField] private float carnetTiltDuration = 0.6f;

    [Tooltip("Delay in seconds after the voice clip ends before tilting the carnet.")]
    [SerializeField] private float carnetTiltDelay = 0.8f;

    [Header("AnomalieJ1")]
    [Tooltip("The AnomalieJ1 message object to reveal on the desk.")]
    [SerializeField] private GameObject anomalieJ1;

    // Frequency range must match RadioSystem — default 88-108
    private const float FrequencyMin = 88f;
    private const float FrequencyMax = 108f;

    private bool sequencePlayed;
    private Quaternion carnetOriginalRotation;

    private void Awake()
    {
        // Snapshot the carnet's starting rotation so it can be restored on next launch
        if (carnet != null)
            carnetOriginalRotation = carnet.localRotation;

        // Always start with AnomalieJ1 hidden, regardless of editor/previous-session state
        if (anomalieJ1 != null)
            anomalieJ1.SetActive(false);

        // Always reset the carnet to its original rotation at game start
        if (carnet != null)
            carnet.localRotation = carnetOriginalRotation;
    }

    /// <summary>
    /// Triggers the full anomaly sequence. Safe to call multiple times — runs only once per session.
    /// Called by JournalManager after the journal is closed.
    /// </summary>
    public void TriggerSequence()
    {
        if (sequencePlayed) return;
        sequencePlayed = true;
        StartCoroutine(AnomalyRoutine());
    }

    private IEnumerator AnomalyRoutine()
    {
        // Brief pause after journal close before anything happens
        yield return new WaitForSeconds(0.5f);

        // Compute normalized knob position for 100 MHz
        float normalized = Mathf.InverseLerp(FrequencyMin, FrequencyMax, anomalyFrequencyMHz);

        // Trigger the anomaly broadcast on the radio (buzz → voice → static)
        if (radioSystem != null)
        {
            radioSystem.TriggerAnomalyBroadcast(anomalyVoiceClip, normalized,
                anomalySubtitles.Length > 0 ? anomalySubtitles : null);

            // Wait for the broadcast to complete
            bool broadcastDone = false;
            radioSystem.OnAnomalyBroadcastComplete += () => broadcastDone = true;
            yield return new WaitUntil(() => broadcastDone);
        }
        else if (anomalyVoiceClip != null)
        {
            // Fallback: no radio reference, just wait for clip length
            yield return new WaitForSeconds(anomalyVoiceClip.length);
        }

        // Delay before visual anomaly
        yield return new WaitForSeconds(carnetTiltDelay);

        // Reveal AnomalieJ1
        if (anomalieJ1 != null)
            anomalieJ1.SetActive(true);

        // Tilt the carnet
        if (carnet != null)
            yield return StartCoroutine(TiltCarnet());
    }

    private IEnumerator TiltCarnet()
    {
        Quaternion startRot = carnet.localRotation;
        Quaternion endRot   = Quaternion.Euler(carnetTiltEuler);
        float      elapsed  = 0f;

        while (elapsed < carnetTiltDuration)
        {
            elapsed             += Time.deltaTime;
            float t              = Mathf.SmoothStep(0f, 1f, elapsed / carnetTiltDuration);
            carnet.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        carnet.localRotation = endRot;
    }
}
