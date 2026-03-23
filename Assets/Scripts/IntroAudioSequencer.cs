using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class IntroAudioSequencer : MonoBehaviour
{
    [Header("Intro")]
    [SerializeField] private AudioClip introClip;

    [Header("Lofi Music")]
    [SerializeField] private AudioClip lofiMusicClip;
    [SerializeField] private float lofiMusicFadeInDuration = 3f;
    [SerializeField] private float lofiMusicFadeOutDuration = 2f;
    [SerializeField][Range(0f, 1f)] private float lofiMusicVolume = 1f;

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

    /// <summary>Plays the intro clip, then starts the lofi music and unlocks the player simultaneously.</summary>
    public void PlaySequence()
    {
        if (introClip == null)
        {
            Debug.LogWarning("IntroAudioSequencer: aucun clip assigné.", this);
            OnSequenceComplete?.Invoke();
            return;
        }

        StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        introSource.Play();
        yield return new WaitForSeconds(introClip.length);

        if (lofiMusicClip != null)
            StartCoroutine(MusicRoutine());

        OnSequenceComplete?.Invoke();
    }

    private IEnumerator MusicRoutine()
    {
        musicSource.clip = lofiMusicClip;
        musicSource.Play();

        // Fade in
        float elapsed = 0f;
        while (elapsed < lofiMusicFadeInDuration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, lofiMusicVolume, elapsed / lofiMusicFadeInDuration);
            yield return null;
        }

        musicSource.volume = lofiMusicVolume;

        // Attendre la fin du clip (moins le fade out)
        float remainingTime = lofiMusicClip.length - lofiMusicFadeInDuration - lofiMusicFadeOutDuration;
        if (remainingTime > 0f)
            yield return new WaitForSeconds(remainingTime);

        // Fade out
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
