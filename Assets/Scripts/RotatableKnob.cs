using UnityEngine;
using UnityEngine.InputSystem;

public class RotatableKnob : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private Vector3 rotationAxis = Vector3.forward;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float minRotation = 0f;
    [SerializeField] private float maxRotation = 270f;

    [Header("Audio")]
    [SerializeField] private AudioClip rotateSound;
    [SerializeField] private float soundInterval = 15f;

    private Camera mainCamera;
    private bool isDragging = false;
    private float currentRotation = 0f;
    private Vector2 lastMousePosition;
    private AudioSource audioSource;
    private float lastSoundRotation = 0f;
    private CameraFocusController focusController;

    private void Awake()
    {
        mainCamera = Camera.main;
        focusController = FindFirstObjectByType<CameraFocusController>();

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

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryStartDrag();
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            isDragging = false;
        }

        if (isDragging)
        {
            HandleDrag();
        }
    }

    private void TryStartDrag()
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                isDragging = true;
                lastMousePosition = Mouse.current.position.ReadValue();
            }
        }
    }

    private void HandleDrag()
    {
        Vector2 currentMousePosition = Mouse.current.position.ReadValue();
        Vector2 mouseDelta = currentMousePosition - lastMousePosition;
        lastMousePosition = currentMousePosition;

        float rotationDelta = mouseDelta.x * rotationSpeed * Time.deltaTime;

        float newRotation = Mathf.Clamp(currentRotation + rotationDelta, minRotation, maxRotation);

        if (Mathf.Abs(newRotation - currentRotation) > 0.01f)
        {
            currentRotation = newRotation;
            ApplyRotation();

            if (audioSource != null && Mathf.Abs(currentRotation - lastSoundRotation) >= soundInterval)
            {
                audioSource.PlayOneShot(rotateSound);
                lastSoundRotation = currentRotation;
            }
        }
    }

    private void ApplyRotation()
    {
        transform.localRotation = Quaternion.AngleAxis(currentRotation, rotationAxis);
    }
}
