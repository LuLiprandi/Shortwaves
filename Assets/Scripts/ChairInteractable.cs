using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChairInteractable : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string promptText = "Appuyer sur [E] pour s'asseoir";
    [SerializeField] private Transform sitPosition;
    [SerializeField] private float sitHeight = 0.5f;
    [SerializeField] private float sitDuration = 0.5f;
    [SerializeField] private Vector3 exitOffset = new Vector3(0.8f, 0f, 0f);

    private FirstPersonController playerController;
    private CharacterController playerCharacterController;
    private Transform playerTransform;
    private Camera playerCamera;
    private PlayerInputActions inputActions;
    private bool isSitting = false;
    private bool isAnimating = false;

    public string PromptMessage => isSitting ? "Appuyer sur [Échap] pour se lever" : promptText;

    private void Awake()
    {
        if (sitPosition == null)
        {
            GameObject sitPosObj = new GameObject("SitPosition");
            sitPosObj.transform.SetParent(transform);
            sitPosObj.transform.localPosition = new Vector3(0, sitHeight, 0);
            sitPosition = sitPosObj.transform;
        }
    }

    private void Update()
    {
        if (isSitting && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            StandUp();
        }
    }

    public void Interact()
    {
        if (isAnimating) return;
        if (isSitting) return;

        if (playerController == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                player = FindFirstObjectByType<FirstPersonController>()?.gameObject;
            }

            if (player != null)
            {
                playerController = player.GetComponent<FirstPersonController>();
                playerCharacterController = player.GetComponent<CharacterController>();
                playerTransform = player.transform;
                playerCamera = player.GetComponentInChildren<Camera>();
                inputActions = new PlayerInputActions();
            }
        }

        SitDown();
    }

    private void SitDown()
    {
        if (playerController == null) return;

        StartCoroutine(SitDownAnimation());
    }

    private void StandUp()
    {
        if (playerController == null) return;

        if (playerCharacterController != null)
        {
            playerCharacterController.enabled = false;
        }

        Vector3 exitPosition = sitPosition.position + sitPosition.TransformDirection(exitOffset);
        playerTransform.position = exitPosition;

        if (playerCharacterController != null)
        {
            playerCharacterController.enabled = true;
        }

        playerController.CanMove = true;
        isSitting = false;
    }

    private IEnumerator SitDownAnimation()
    {
        isAnimating = true;
        playerController.CanMove = false;

        Vector3 startPosition = playerTransform.position;
        Quaternion startRotation = playerTransform.rotation;
        Quaternion startCameraRotation = playerCamera.transform.localRotation;

        Vector3 targetPosition = sitPosition.position;
        Quaternion targetRotation = sitPosition.rotation;

        float elapsedTime = 0f;

        while (elapsedTime < sitDuration)
        {
            if (playerCharacterController != null && playerCharacterController.enabled)
            {
                playerCharacterController.enabled = false;
            }

            elapsedTime += Time.deltaTime;
            float t = elapsedTime / sitDuration;

            t = Mathf.SmoothStep(0f, 1f, t);

            playerTransform.position = Vector3.Lerp(startPosition, targetPosition, t);
            playerTransform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        playerTransform.position = targetPosition;
        playerTransform.rotation = targetRotation;

        if (playerCharacterController != null)
        {
            playerCharacterController.enabled = true;
        }

        isSitting = true;
        isAnimating = false;
    }

    private void OnDestroy()
    {
        inputActions?.Dispose();
    }
}
