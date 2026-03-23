using UnityEngine;

public class DocumentInteractable : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string inspectPrompt = "Appuyer sur [E] pour examiner";
    [SerializeField] private Transform inspectionAnchor;

    private CameraFocusController cameraFocusController;

    public string PromptMessage => inspectPrompt;

    private void Start()
    {
        cameraFocusController = FindFirstObjectByType<CameraFocusController>();
    }

    /// <summary>Zooms the camera onto the document for inspection.</summary>
    public void Interact()
    {
        if (cameraFocusController == null || inspectionAnchor == null) return;
        if (cameraFocusController.IsFocused) return;

        cameraFocusController.EnterFocus(
            inspectionAnchor,
            inspectionAnchor.position,
            inspectionAnchor.rotation
        );
    }
}
