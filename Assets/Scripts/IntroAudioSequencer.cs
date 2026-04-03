using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class IntroAudioSequencer : MonoBehaviour
{
    [Header("Intro")]
    [SerializeField] private AudioClip introClip;
    [SerializeField] private SubtitleSystem subtitleSystem;

    [Header("Lofi Music")]
    [SerializeField] private AudioClip lofiMusicClip;
    [SerializeField] private float lofiMusicFadeInDuration = 3f;
    [SerializeField] private float lofiMusicFadeOutDuration = 2f;
    [SerializeField][Range(0f, 1f)] private float lofiMusicVolume = 1f;

    public static bool IsIntroPlaying { get; private set; }
    private AudioSource introSource;
    private AudioSource musicSource;

    public event Action OnSequenceComplete;

    private void Awake()
    {
        introSource = GetComponent<AudioSource>();
        introSource.playOnAwake = false;
        introSource.loop = false;
        introSource.clip = introClip;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = false;
        musicSource.volume = 0f;
    }

    /// <summary>Plays the intro clip with subtitles, then starts the lofi music and unlocks the player.</summary>
    public void PlaySequence()
    {
        if (introClip == null)
        {
            Debug.LogWarning("IntroAudioSequencer: aucun clip assign�.", this);
            OnSequenceComplete?.Invoke();
            return;
        }

        IsIntroPlaying = true;
        StartCoroutine(PlayRoutine());    }

    private IEnumerator PlayRoutine()
    {
        introSource.Play();

        if (subtitleSystem != null)
            subtitleSystem.Play(introSource);

        yield return new WaitForSeconds(introClip.length);

        if (subtitleSystem != null)
            subtitleSystem.Stop();

        if (lofiMusicClip != null)
            StartCoroutine(MusicRoutine());

        OnSequenceComplete?.Invoke();
    }

    private IEnumerator MusicRoutine()
    {
        musicSource.clip = lofiMusicClip;
        musicSource.Play();

        float elapsed = 0f;
        while (elapsed < lofiMusicFadeInDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, lofiMusicVolume, elapsed / lofiMusicFadeInDuration);
            yield return null;
        }

        musicSource.volume = lofiMusicVolume;

        float remainingTime = lofiMusicClip.length - lofiMusicFadeInDuration - lofiMusicFadeOutDuration;
        if (remainingTime > 0f)
            yield return new WaitForSeconds(remainingTime);

        elapsed = 0f;
        while (elapsed < lofiMusicFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(lofiMusicVolume, 0f, elapsed / lofiMusicFadeOutDuration);
            yield return null;
        }

        musicSource.Stop();
        musicSource.volume = 0f;
    }
}
