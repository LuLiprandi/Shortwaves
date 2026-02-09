using UnityEngine;

public class RadioInspectable : MonoBehaviour, IInteractable
{
    [Header("Inspection Settings")]
    [SerializeField] private string promptText = "Appuyer sur [E] pour inspecter";
    [SerializeField] private Transform inspectionCameraPosition;
    [SerializeField] private float inspectionDistance = 0.5f;

    private CameraFocusController focusController;

    public string PromptMessage => promptText;

    private void Start()
    {
        focusController = FindFirstObjectByType<CameraFocusController>();

        if (inspectionCameraPosition == null)
        {
            GameObject camPos = new GameObject("InspectionCameraPosition");
            camPos.transform.SetParent(transform);
            camPos.transform.localPosition = new Vector3(0, 0, -inspectionDistance);
            camPos.transform.localRotation = Quaternion.identity;
            inspectionCameraPosition = camPos.transform;
        }
    }

    public void Interact()
    {
        if (focusController != null && !focusController.IsFocused)
        {
            focusController.EnterFocus(
                transform,
                inspectionCameraPosition.position,
                inspectionCameraPosition.rotation
            );
        }
    }
}
