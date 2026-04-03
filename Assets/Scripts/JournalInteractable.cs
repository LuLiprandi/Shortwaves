using UnityEngine;

/// <summary>
/// Marks the physical notebook in the scene as interactable.
/// Interaction opens the journal panel via JournalManager.
/// </summary>
public class JournalInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string promptText = "Appuyer sur [E] pour ouvrir le carnet";

    public string PromptMessage => promptText;

    /// <summary>Opens the journal when the player interacts with the notebook.</summary>
    public void Interact()
    {
        JournalManager.Instance?.Open();
    }
}
