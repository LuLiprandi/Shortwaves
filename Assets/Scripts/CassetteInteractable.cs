using UnityEngine;

public class CassetteInteractable : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string promptText = "Appuyer sur [E] pour prendre la cassette";
    [SerializeField] private GameObject cassetteModelPrefab;

    [Header("Disponibilité")]
    [Tooltip("Jour à partir duquel la cassette disparaît (elle a déjà été insérée).")]
    [SerializeField] private int availableUntilDay = 1;

    public string PromptMessage => promptText;

    private void Start()
    {
        // La cassette n'existe qu'au Jour 1 — au Jour 2+ elle a déjà été utilisée.
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentDay > availableUntilDay)
            gameObject.SetActive(false);
    }

    /// <summary>Picks up the cassette and hides the world object.</summary>
    public void Interact()
    {
        if (HeldItemSystem.Instance.HasItem) return;

        HeldItemSystem.Instance.PickUp(cassetteModelPrefab);

        gameObject.SetActive(false);
    }
}
