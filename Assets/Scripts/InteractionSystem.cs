using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class InteractionSystem : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private LayerMask interactionLayer;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI interactionPrompt;

    private Camera playerCamera;
    private PlayerInputActions inputActions;
    private IInteractable currentInteractable;
    private RaycastHit hitInfo;
    private Transform cameraTransform;
    private bool wasShowingPrompt;

    private void Awake()
    {
        playerCamera = Camera.main;
        cameraTransform = playerCamera.transform;
        inputActions = new PlayerInputActions();

        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Interact.performed += OnInteract;
    }

    private void OnDisable()
    {
        inputActions.Player.Interact.performed -= OnInteract;
        inputActions.Player.Disable();
    }

    private void Update()
    {
        CheckForInteractable();
    }

    private void CheckForInteractable()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out hitInfo, interactionDistance, interactionLayer))
        {
            IInteractable interactable = hitInfo.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    UpdatePrompt(true, interactable.PromptMessage);
                }
                return;
            }
        }

        if (currentInteractable != null)
        {
            currentInteractable = null;
            UpdatePrompt(false, string.Empty);
        }
    }

    private void UpdatePrompt(bool show, string message)
    {
        if (interactionPrompt == null) return;

        if (show != wasShowingPrompt)
        {
            interactionPrompt.gameObject.SetActive(show);
            wasShowingPrompt = show;
        }

        if (show)
        {
            interactionPrompt.text = message;
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (currentInteractable != null)
        {
            currentInteractable.Interact();
        }
    }
}
