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
    private OutlineEffect currentOutline;
    private RaycastHit hitInfo;
    private Transform cameraTransform;
    private bool wasShowingPrompt;

    private void Awake()
    {
        playerCamera = Camera.main;
        cameraTransform = playerCamera.transform;
        inputActions = new PlayerInputActions();

        if (interactionPrompt != null)
            interactionPrompt.gameObject.SetActive(false);
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

        DisableCurrentOutline();
        currentInteractable = null;
        wasShowingPrompt = false;

        if (interactionPrompt != null)
            interactionPrompt.gameObject.SetActive(false);
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
                    DisableCurrentOutline();
                    currentInteractable = interactable;

                    OutlineEffect outline = hitInfo.collider.GetComponent<OutlineEffect>();
                    if (outline != null)
                    {
                        outline.EnableOutline();
                        currentOutline = outline;
                    }
                }

                UpdatePrompt(currentInteractable.PromptMessage);
                return;
            }
        }

        if (currentInteractable != null)
        {
            DisableCurrentOutline();
            currentInteractable = null;
            UpdatePrompt(string.Empty);
        }
    }

    private void DisableCurrentOutline()
    {
        if (currentOutline != null)
        {
            currentOutline.DisableOutline();
            currentOutline = null;
        }
    }

    /// <summary>Shows or hides the interaction prompt based on whether the message is non-empty.</summary>
    private void UpdatePrompt(string message)
    {
        if (interactionPrompt == null) return;

        bool shouldShow = !string.IsNullOrEmpty(message);

        if (shouldShow != wasShowingPrompt)
        {
            interactionPrompt.gameObject.SetActive(shouldShow);
            wasShowingPrompt = shouldShow;
        }

        if (shouldShow)
            interactionPrompt.text = message;
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (currentInteractable != null)
            currentInteractable.Interact();
    }
}
