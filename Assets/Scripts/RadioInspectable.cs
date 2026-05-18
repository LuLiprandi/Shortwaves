using UnityEngine;

public class RadioInspectable : MonoBehaviour, IInteractable
{
    [Header("Inspection Settings")]
    [SerializeField] private string promptText = "Appuyer sur [E] pour inspecter";
    [SerializeField] private Transform inspectionCameraPosition;
    [SerializeField] private float inspectionDistance = 0.5f;

    [Header("Références")]
    [SerializeField] private RadioSystem radioSystem;

    private CameraFocusController focusController;
    private bool isFocused;
    private bool isUnlocked = false;

    public string PromptMessage => (isUnlocked && !isFocused) ? promptText : "";

    private void Start()
    {
        focusController = FindFirstObjectByType<CameraFocusController>();

        if (inspectionCameraPosition == null)
        {
            GameObject camPos = new GameObject("InspectionCameraPosition");
            camPos.transform.SetParent(transform);
            camPos.transform.localPosition = new Vector3(0f, 0f, -inspectionDistance);
            camPos.transform.localRotation = Quaternion.identity;
            inspectionCameraPosition = camPos.transform;
        }

        if (focusController != null)
            focusController.OnFocusExited += OnFocusExit;

        // La radio est déjà déverrouillée à partir du Jour 2 (cassette insérée au Jour 1).
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentDay > 1)
            Unlock();
    }

    private void OnDestroy()
    {
        if (focusController != null)
            focusController.OnFocusExited -= OnFocusExit;
    }

    public void Interact()
    {
        if (!isUnlocked) return;
        if (focusController == null || focusController.IsFocused) return;

        // Keep the camera at its current position — the player is already facing the radio.
        Camera mainCam = Camera.main;
        focusController.EnterFocus(
            transform,
            mainCam.transform.position,
            mainCam.transform.rotation
        );

        isFocused = true;

        if (radioSystem != null)
            radioSystem.SetActive(true);

        HintDisplay.Instance?.ShowHint("[Echap] Quitter la radio");
    }

    public void OnFocusExit()
    {
        isFocused = false;

        if (radioSystem != null)
            radioSystem.SetActive(false);

        HintDisplay.Instance?.Hide();
    }

    public void ResetFocusState()
    {
        isFocused = false;
    }

    public void Unlock()
    {
        isUnlocked = true;
    }

    public void Lock()
    {
        isUnlocked = false;
        isFocused  = false;
    }
}
