using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFocusController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float focusTransitionDuration = 0.5f;
    [SerializeField] private float focusFOV = 40f;

    private Camera playerCamera;
    private Transform cameraTransform;
    private FirstPersonController playerController;
    private InteractionSystem interactionSystem;

    private bool isFocused = false;
    private bool isTransitioning = false;

    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;
    private float originalFOV;

    private Transform focusTarget;
    private Vector3 focusTargetPosition;
    private Quaternion focusTargetRotation;

    public bool IsFocused => isFocused;
    public bool IsTransitioning => isTransitioning;

    /// <summary>Fired when the camera finishes exiting focus.</summary>
    public event System.Action OnFocusExited;

    private void Awake()
    {
        playerCamera = Camera.main;
        cameraTransform = playerCamera.transform;
        playerController = GetComponent<FirstPersonController>();
        interactionSystem = GetComponent<InteractionSystem>();

        originalFOV = playerCamera.fieldOfView;
    }

    private void Update()
    {
        if (isFocused && !GameStateManager.Instance.IsCutsceneActive && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            DocumentInteractable.ConsumeEscape();
            ExitFocus();
        }
    }

    public void EnterFocus(Transform target, Vector3 targetPosition, Quaternion targetRotation)
    {
        if (isTransitioning || isFocused) return;

        focusTarget = target;
        focusTargetPosition = targetPosition;
        focusTargetRotation = targetRotation;

        originalCameraLocalPosition = cameraTransform.localPosition;
        originalCameraLocalRotation = cameraTransform.localRotation;

        StartCoroutine(FocusTransition(true));
    }

    public void ExitFocus()
    {
        if (isTransitioning || !isFocused) return;

        StartCoroutine(FocusTransition(false));
    }

    private IEnumerator FocusTransition(bool entering)
    {
        isTransitioning = true;

        if (entering)
        {
            playerController.CanMove = false;
            playerController.CanLook = false;
            if (interactionSystem != null)
                interactionSystem.enabled = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        float startFOV = playerCamera.fieldOfView;
        float targetFOV = entering ? focusFOV : originalFOV;

        Vector3 startPos = cameraTransform.position;
        Quaternion startRot = cameraTransform.rotation;

        Vector3 targetPos;
        Quaternion targetRot;

        if (entering)
        {
            targetPos = focusTargetPosition;
            targetRot = focusTargetRotation;
        }
        else
        {
            targetPos = transform.position + transform.TransformDirection(originalCameraLocalPosition);
            targetRot = transform.rotation * originalCameraLocalRotation;
        }

        float elapsedTime = 0f;
        while (elapsedTime < focusTransitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsedTime / focusTransitionDuration);

            cameraTransform.position = Vector3.Lerp(startPos, targetPos, t);
            cameraTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            playerCamera.fieldOfView = Mathf.Lerp(startFOV, targetFOV, t);

            yield return null;
        }

        cameraTransform.position = targetPos;
        cameraTransform.rotation = targetRot;
        playerCamera.fieldOfView = targetFOV;

        isFocused = entering;
        isTransitioning = false;

        if (!entering)
        {
            playerController.CanMove = true;
            playerController.CanLook = true;
            if (interactionSystem != null)
                interactionSystem.enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            OnFocusExited?.Invoke();
        }
    }
}
