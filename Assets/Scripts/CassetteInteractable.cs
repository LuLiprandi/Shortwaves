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
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentDay > availableUntilDay)
            gameObject.SetActive(false);
    }

    public void Interact()
    {
        if (HeldItemSystem.Instance.HasItem) return;

        HeldItemSystem.Instance.PickUp(cassetteModelPrefab);

        gameObject.SetActive(false);
    }
}
