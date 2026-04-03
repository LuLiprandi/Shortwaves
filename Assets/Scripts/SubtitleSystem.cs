using System;
using System.Collections;
using TMPro;
using UnityEngine;

[Serializable]
public class SubtitleEntry
{
    public float startTime;
    public float duration;
    [TextArea] public string text;
}

public class SubtitleSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI subtitleText;
    [SerializeField] private SubtitleEntry[] subtitles;

    private Coroutine activeRoutine;

    private void Awake()
    {
        SetText(string.Empty);
    }

    /// <summary>Starts displaying subtitles synced to the given AudioSource playback time, using the Inspector-defined entries.</summary>
    public void Play(AudioSource audioSource)
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(SubtitleRoutine(audioSource, subtitles));
    }

    /// <summary>Starts displaying subtitles synced to the given AudioSource, using the provided entries instead of the Inspector ones.</summary>
    public void Play(AudioSource audioSource, SubtitleEntry[] entries)
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(SubtitleRoutine(audioSource, entries));
    }

    /// <summary>Stops all subtitles and clears the text.</summary>
    public void Stop()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        SetText(string.Empty);
    }

    private IEnumerator SubtitleRoutine(AudioSource audioSource, SubtitleEntry[] entries)
    {
        SetText(string.Empty);

        foreach (SubtitleEntry entry in entries)
        {
            yield return new WaitUntil(() => audioSource.time >= entry.startTime);

            SetText(entry.text);

            yield return new WaitForSeconds(entry.duration);

            SetText(string.Empty);
        }

        activeRoutine = null;
    }

    private void SetText(string message)
    {
        if (subtitleText == null) return;
        subtitleText.text = message;
    }
}
