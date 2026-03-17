using UnityEngine;

public class CassetteInteractable : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string promptText = "Appuyer sur [E] pour prendre la cassette";
    [SerializeField] private GameObject cassetteModelPrefab;

    public string PromptMessage => promptText;

    /// <summary>Picks up the cassette and hides the world object.</summary>
    public void Interact()
    {
        if (HeldItemSystem.Instance.HasItem) return;

        HeldItemSystem.Instance.PickUp(cassetteModelPrefab);

        gameObject.SetActive(false);
    }
}
