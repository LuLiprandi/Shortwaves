using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChairInteractable : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string sitPrompt = "Appuyer sur [E] pour s'asseoir";
    [SerializeField] private float sitDuration = 0.6f;
    [SerializeField] private Vector3 exitOffset = new Vector3(0.8f, 0f, 0f);

    [Header("Seated Anchor")]
    [Tooltip("Positionner au sol devant le bureau, orienté face au bureau. La caméra sera 1.4 unités au-dessus.")]
    [SerializeField] private Transform seatedAnchor;

    private FirstPersonController playerController;
    private CharacterController playerCharacterController;
    private Transform playerRoot;

    private Vector3 originalRootPosition;
    private Quaternion originalRootRotation;

    private bool isSitting = false;
    private bool isAnimating = false;

    public bool IsSitting => isSitting;
    public string PromptMessage => isSitting ? "" : sitPrompt;

    private void Start()
    {
        ResolvePlayerReferences();
    }

    private void Update()
    {
        if (isSitting && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            StandUp();
    }

    /// <summary>Moves the player root to the seated anchor and enables seated mode.</summary>
    public void Interact()
    {
        if (isAnimating || isSitting) return;

        StartCoroutine(SitDownRoutine());
    }

    private void StandUp()
    {
        if (playerController == null) return;

        playerRoot.position = originalRootPosition;
        playerRoot.rotation = originalRootRotation;

        if (playerCharacterController != null)
            playerCharacterController.enabled = true;

        playerController.SetSeatedMode(false);
        playerController.CanMove = true;
        isSitting = false;
    }

    private IEnumerator SitDownRoutine()
    {
        if (seatedAnchor == null)
        {
            Debug.LogWarning("ChairInteractable: SeatedAnchor non assigné.", this);
            yield break;
        }

        isAnimating = true;
        playerController.CanMove = false;

        if (playerCharacterController != null)
            playerCharacterController.enabled = false;

        originalRootPosition = playerRoot.position;
        originalRootRotation = playerRoot.rotation;

        Vector3 targetPosition = seatedAnchor.position;
        Quaternion targetRotation = Quaternion.Euler(0f, seatedAnchor.eulerAngles.y, 0f);

        float elapsed = 0f;

        while (elapsed < sitDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / sitDuration);

            playerRoot.position = Vector3.Lerp(originalRootPosition, targetPosition, t);
            playerRoot.rotation = Quaternion.Slerp(originalRootRotation, targetRotation, t);

            yield return null;
        }

        playerRoot.position = targetPosition;
        playerRoot.rotation = targetRotation;

        // CharacterController reste désactivé pendant toute la session assise
        playerController.SetSeatedMode(true);
        isSitting = true;
        isAnimating = false;
    }

    private void ResolvePlayerReferences()
    {
        FirstPersonController found = FindFirstObjectByType<FirstPersonController>();
        if (found == null) return;

        playerController = found;
        playerCharacterController = found.GetComponent<CharacterController>();
        playerRoot = found.transform.root;
    }
}
