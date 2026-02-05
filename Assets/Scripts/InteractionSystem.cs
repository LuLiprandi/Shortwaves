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
    private Ray ray;
    private RaycastHit hitInfo;
    private Transform cameraTransform;
    private bool wasShowingPrompt;

    private void Awake()
    {
        playerCamera = Camera.main;
        cameraTransform = playerCamera.transform;
        inputActions = new PlayerInputActions();

        Debug.Log("InteractionSystem: Initialized");

        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(false);
            Debug.Log("InteractionSystem: Prompt UI found and hidden");
        }
        else
        {
            Debug.LogError("InteractionSystem: Interaction Prompt is NULL!");
        }
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Interact.performed += OnInteract;
        Debug.Log("InteractionSystem: Input actions enabled");
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
        ray.origin = cameraTransform.position;
        ray.direction = cameraTransform.forward;

        Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.green);

        if (Physics.Raycast(ray, out hitInfo, interactionDistance, interactionLayer))
        {
            Debug.Log($"Raycast HIT: {hitInfo.collider.name} on layer '{LayerMask.LayerToName(hitInfo.collider.gameObject.layer)}' at distance {hitInfo.distance}m");

            IInteractable interactable = hitInfo.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                Debug.Log($"Found IInteractable! Message: '{interactable.PromptMessage}'");

                if (currentInteractable != interactable)
                {
                    currentInteractable = interactable;
                    UpdatePrompt(true, interactable.PromptMessage);
                    Debug.Log("Prompt shown");
                }
                return;
            }
            else
            {
                Debug.LogWarning($"Object '{hitInfo.collider.name}' has NO IInteractable component!");
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
        Debug.Log("E key pressed!");

        if (currentInteractable != null)
        {
            Debug.Log($"Interacting with: {currentInteractable.PromptMessage}");
            currentInteractable.Interact();
        }
        else
        {
            Debug.Log("No interactable object in range");
        }
    }
}
