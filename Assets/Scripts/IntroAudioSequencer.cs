using System;
using System.Collections;
using UnityEngine;

public class IntroAudioSequencer : MonoBehaviour
{
    [Header("Audio Clips")]
    [SerializeField] private AudioClip windAmbiance;
    [SerializeField] private AudioClip cassetteClik;
    [SerializeField] private AudioClip militaryVoice;
    [SerializeField] private AudioClip brokenVoice;
    [SerializeField] private AudioClip distortionBurst;
    [SerializeField] private AudioClip lofiMusic;

    [Header("Timings")]
    [SerializeField] private float windDuration = 3f;
    [SerializeField] private float silenceAfterMilitary = 2f;
    [SerializeField] private float lofiMusicFadeInDuration = 3f;

    private AudioSource voiceSource;
    private AudioSource ambianceSource;
    private AudioSource musicSource;

    public event Action OnSequenceComplete;

    private void Awake()
    {
        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;

        ambianceSource = gameObject.AddComponent<AudioSource>();
        ambianceSource.loop = true;
        ambianceSource.playOnAwake = false;

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = 0f;
    }

    /// <summary>Starts the full intro audio sequence.</summary>
    public void PlaySequence()
    {
        StartCoroutine(SequenceRoutine());
    }

    private IEnumerator SequenceRoutine()
    {
        PlayAmbiance(windAmbiance);
        yield return new WaitForSeconds(windDuration);

        PlayVoice(cassetteClik);
        yield return new WaitForSeconds(GetClipLength(cassetteClik));

        PlayVoice(militaryVoice);
        yield return new WaitForSeconds(GetClipLength(militaryVoice));

        yield return new WaitForSeconds(silenceAfterMilitary);

        PlayVoice(brokenVoice);
        yield return new WaitForSeconds(GetClipLength(brokenVoice));

        PlayVoice(distortionBurst);
        yield return new WaitForSeconds(GetClipLength(distortionBurst));

        StopAmbiance();
        StartCoroutine(FadeMusicIn(lofiMusic, lofiMusicFadeInDuration));
        yield return new WaitForSeconds(lofiMusicFadeInDuration);

        OnSequenceComplete?.Invoke();
    }

    private IEnumerator FadeMusicIn(AudioClip clip, float duration)
    {
        musicSource.clip = clip;
        musicSource.Play();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, 1f, elapsed / duration);
            yield return null;
        }

        musicSource.volume = 1f;
    }

    private void PlayVoice(AudioClip clip)
    {
        if (clip == null) return;
        voiceSource.clip = clip;
        voiceSource.Play();
    }

    private void PlayAmbiance(AudioClip clip)
    {
        if (clip == null) return;
        ambianceSource.clip = clip;
        ambianceSource.Play();
    }

    private void StopAmbiance() => ambianceSource.Stop();

    private float GetClipLength(AudioClip clip) => clip != null ? clip.length : 0f;
}
