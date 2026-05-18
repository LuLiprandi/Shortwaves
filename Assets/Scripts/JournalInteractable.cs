using UnityEngine;

public class JournalInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string promptText = "Appuyer sur [E] pour ouvrir le carnet";

    public string PromptMessage => promptText;

    public void Interact()
    {
        JournalManager.Instance?.Open();
    }
}
