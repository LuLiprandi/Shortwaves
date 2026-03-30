using UnityEngine;
using UnityEngine.InputSystem;

public class RotatableKnob : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Degrés par cran de molette — pas fixe indépendant de la valeur brute de la souris")]
    [SerializeField] private float scrollStepDegrees = 5f;
    [Tooltip("Degrés par seconde quand une flèche est maintenue")]
    [SerializeField] private float keyTuneSpeed = 120f;
    [SerializeField] private float minRotation = 0f;
    [SerializeField] private float maxRotation = 270f;
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;

    [Header("Audio")]
    [SerializeField] private AudioClip rotateSound;
    [SerializeField] private float soundInterval = 15f;

    private float currentRotation = 0f;
    private AudioSource audioSource;
    private float lastSoundRotation = 0f;
    private CameraFocusController focusController;
    private RadioSystem radioSystem;

    private void Awake()
    {
        focusController = FindFirstObjectByType<CameraFocusController>();
        radioSystem = FindFirstObjectByType<RadioSystem>();

        if (rotateSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = rotateSound;
            audioSource.playOnAwake = false;
        }
    }

    private void Update()
    {
        if (focusController == null || !focusController.IsFocused) return;

        // Seulement pendant le tuning — le QTE gère lui-même les flèches
        bool isTuning = radioSystem == null || radioSystem.State == RadioState.Tuning;
        if (!isTuning) return;

        float rotationDelta = 0f;

        // Molette — pas fixe par cran, indépendant de la valeur brute de la souris
        float scrollRaw = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scrollRaw) > 0.01f)
            rotationDelta += Mathf.Sign(scrollRaw) * scrollStepDegrees;

        // Flèches gauche/droite — rotation continue en maintenant
        if (Keyboard.current != null)
        {
            if (Keyboard.current.leftArrowKey.isPressed)
                rotationDelta -= keyTuneSpeed * Time.deltaTime;
            if (Keyboard.current.rightArrowKey.isPressed)
                rotationDelta += keyTuneSpeed * Time.deltaTime;
        }

        if (Mathf.Abs(rotationDelta) < 0.001f) return;

        float newRotation = Mathf.Clamp(currentRotation + rotationDelta, minRotation, maxRotation);
        if (Mathf.Abs(newRotation - currentRotation) > 0.001f)
        {
            currentRotation = newRotation;
            ApplyRotation();
            PlaySoundIfNeeded();
        }
    }

    private void ApplyRotation()
    {
        transform.localRotation = Quaternion.AngleAxis(currentRotation, rotationAxis);
    }

    private void PlaySoundIfNeeded()
    {
        if (audioSource == null || rotateSound == null) return;
        if (Mathf.Abs(currentRotation - lastSoundRotation) >= soundInterval)
        {
            audioSource.PlayOneShot(rotateSound);
            lastSoundRotation = currentRotation;
        }
    }

    /// <summary>Returns the knob's current rotation as a normalized value between 0 and 1.</summary>
    public float NormalizedValue => maxRotation > 0f ? currentRotation / maxRotation : 0f;
}
