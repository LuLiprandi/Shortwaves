using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Controls the main menu: frequency bar animation, panel toggling, and navigation.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Références UI")]
    [SerializeField] private RectTransform[] frequencyBars;
    [SerializeField] private TextMeshProUGUI subtitleLabel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject sauvegardePanel;

    [Header("Barres de fréquence")]
    [SerializeField] private float minBarHeight = 3f;
    [SerializeField] private float maxBarHeight = 36f;
    [SerializeField] private float noiseSpeed = 1.4f;
    [SerializeField] private float noiseScale = 0.35f;
    [SerializeField] private float barLerpSpeed = 7f;

    [Header("Scène de jeu")]
    [SerializeField] private string gameSceneName = "SampleScene";

    private static readonly string[] SubtitleLines =
    {
        "SIGNAL DÉTECTÉ",
        "FRÉQUENCE AUDIO",
        "TRANSMISSION EN COURS",
        "CANAL CRYPTÉ"
    };

    private float[] noiseOffsets;
    private float[] currentHeights;
    private float subtitleTimer;
    private int subtitleIndex;

    private const float SubtitleInterval = 3.5f;

    private void Awake()
    {
        int count = frequencyBars != null ? frequencyBars.Length : 0;
        noiseOffsets    = new float[count];
        currentHeights  = new float[count];
        for (int i = 0; i < count; i++)
            noiseOffsets[i] = Random.Range(0f, 100f);

        optionsPanel?.SetActive(false);
        sauvegardePanel?.SetActive(false);
    }

    private void Update()
    {
        AnimateBars();
        RotateSubtitle();
    }

    private void AnimateBars()
    {
        if (frequencyBars == null) return;
        for (int i = 0; i < frequencyBars.Length; i++)
        {
            if (frequencyBars[i] == null) continue;
            float noise       = Mathf.PerlinNoise(noiseOffsets[i] + Time.time * noiseSpeed, i * noiseScale);
            float targetHeight = Mathf.Lerp(minBarHeight, maxBarHeight, noise);
            currentHeights[i]  = Mathf.Lerp(currentHeights[i], targetHeight, Time.deltaTime * barLerpSpeed);

            Vector2 size = frequencyBars[i].sizeDelta;
            frequencyBars[i].sizeDelta = new Vector2(size.x, currentHeights[i]);
        }
    }

    private void RotateSubtitle()
    {
        if (subtitleLabel == null) return;
        subtitleTimer += Time.deltaTime;
        if (subtitleTimer < SubtitleInterval) return;

        subtitleTimer = 0f;
        subtitleIndex = (subtitleIndex + 1) % SubtitleLines.Length;
        subtitleLabel.text = SubtitleLines[subtitleIndex];
    }

    // ── Boutons ──────────────────────────────────────────────────────────────

    /// <summary>Charge la scène de jeu.</summary>
    public void OnCommencer() => SceneManager.LoadScene(gameSceneName);

    /// <summary>Bascule le panneau Options.</summary>
    public void OnOptions()
    {
        bool open = optionsPanel != null && optionsPanel.activeSelf;
        optionsPanel?.SetActive(!open);
        sauvegardePanel?.SetActive(false);
    }

    /// <summary>Bascule le panneau Sauvegarde.</summary>
    public void OnSauvegarde()
    {
        bool open = sauvegardePanel != null && sauvegardePanel.activeSelf;
        sauvegardePanel?.SetActive(!open);
        optionsPanel?.SetActive(false);
    }

    /// <summary>Quitte l'application.</summary>
    public void OnQuitter()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
