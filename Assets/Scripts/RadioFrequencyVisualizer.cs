using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Animates the frequency bar display, moves the needle, and shows QTE alerts and decoded clues.</summary>
public class RadioFrequencyVisualizer : MonoBehaviour
{
    [Header("Barres UI")]
    [Tooltip("Tableau de RectTransforms représentant les barres verticales")]
    [SerializeField] private RectTransform[] bars;
    [SerializeField] private float minBarHeight = 8f;
    [SerializeField] private float maxBarHeight = 130f;
    [SerializeField] private float noiseSpeed = 2.5f;
    [SerializeField] private float noiseScale = 0.8f;

    [Header("Aiguille fréquence")]
    [Tooltip("RectTransform de l'aiguille qui se déplace horizontalement selon la fréquence")]
    [SerializeField] private RectTransform needle;
    [Tooltip("Demi-largeur de la zone de déplacement de l'aiguille en pixels")]
    [SerializeField] private float needleHalfRange = 210f;

    [Header("Label fréquence")]
    [SerializeField] private TextMeshProUGUI frequencyLabel;

    [Header("Alerte QTE")]
    [SerializeField] private GameObject qteAlertRoot;
    [SerializeField] private TextMeshProUGUI qteAlertLabel;

    [Header("Panel décodé")]
    [Tooltip("Panneau affiché quand la station est décodée, contenant la clé et les notes")]
    [SerializeField] private GameObject decodedPanel;
    [SerializeField] private TextMeshProUGUI decodedText;

    [Header("Root Canvas")]
    [SerializeField] private GameObject visualizerRoot;

    private float[] currentHeights;
    private float[] noiseOffsets;

    private const float HeightLerpSpeed = 10f;

    private void Awake()
    {
        int count = bars != null ? bars.Length : 0;
        currentHeights = new float[count];
        noiseOffsets = new float[count];

        for (int i = 0; i < count; i++)
        {
            noiseOffsets[i] = Random.Range(0f, 100f);
            currentHeights[i] = minBarHeight;
        }
    }

    private void Update()
    {
        // Plus de logique de flash — le bandeau QTE est géré par Show/HideQTEAlert
    }

    /// <summary>Shows or hides the entire visualizer.</summary>
    public void SetVisible(bool visible)
    {
        if (visualizerRoot != null)
            visualizerRoot.SetActive(visible);
    }

    /// <summary>
    /// Updates bars height from signal strength (0-1), moves the needle,
    /// and refreshes the frequency label. Call every frame from RadioSystem.
    /// </summary>
    public void UpdateVisualizer(float currentFrequency, float signalStrength, float normalizedFrequency)
    {
        if (frequencyLabel != null)
            frequencyLabel.text = currentFrequency.ToString("F1") + " MHz";

        // Needle slides proportionally to current frequency
        if (needle != null)
        {
            float targetX = Mathf.Lerp(-needleHalfRange, needleHalfRange, normalizedFrequency);
            Vector2 pos = needle.anchoredPosition;
            needle.anchoredPosition = new Vector2(
                Mathf.Lerp(pos.x, targetX, Time.deltaTime * 12f),
                pos.y
            );
        }

        // Bars — constant idle noise for realism + signal boost when near a station
        for (int i = 0; i < bars.Length; i++)
        {
            if (bars[i] == null) continue;

            float noise = Mathf.PerlinNoise(noiseOffsets[i] + Time.time * noiseSpeed, i * noiseScale);
            // Even at zero signal, bars breathe at low amplitude (idle noise)
            float idleNoise = Mathf.PerlinNoise(noiseOffsets[i] + Time.time * noiseSpeed * 0.4f, i * noiseScale + 50f) * 0.1f;
            float effectiveStrength = Mathf.Max(signalStrength, idleNoise);
            float targetHeight = minBarHeight + (maxBarHeight - minBarHeight) * effectiveStrength * noise;

            currentHeights[i] = Mathf.Lerp(currentHeights[i], targetHeight, Time.deltaTime * HeightLerpSpeed);

            Vector2 size = bars[i].sizeDelta;
            bars[i].sizeDelta = new Vector2(size.x, currentHeights[i]);
        }
    }

    /// <summary>Shows the QTE instruction banner — stays visible until HideQTEAlert() is called.</summary>
    public void ShowQTEAlert()
    {
        if (qteAlertRoot == null) return;
        qteAlertRoot.SetActive(true);
        if (qteAlertLabel != null)
            qteAlertLabel.text = "← →   Garde l'aiguille dans la zone verte";
    }

    /// <summary>Immediately hides the QTE instruction banner.</summary>
    public void HideQTEAlert()
    {
        if (qteAlertRoot != null) qteAlertRoot.SetActive(false);
    }

    /// <summary>Shows the "transmission en cours" panel after QTE success — the player listens to the voice clip.</summary>
    public void ShowTransmission()
    {
        if (decodedPanel != null)
            decodedPanel.SetActive(true);
        if (decodedText != null)
            decodedText.text = "TRANSMISSION EN COURS...\nÉcoute et mémorise le message.";
    }

    /// <summary>Shows the decoded clue panel with the station's clue text.</summary>
    public void ShowDecoded(string clue)
    {
        if (decodedPanel != null)
            decodedPanel.SetActive(true);
        if (decodedText != null)
            decodedText.text = clue;
    }
}
