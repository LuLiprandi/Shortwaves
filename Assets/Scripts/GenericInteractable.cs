using UnityEngine;
using UnityEngine.Events;

public class GenericInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction Settings")]
    [SerializeField] private string promptText = "Appuyer sur [E] pour interagir";

    [Header("Actions")]
    [SerializeField] private UnityEvent onInteract;

    public string PromptMessage => promptText;

    public void Interact()
    {
        onInteract?.Invoke();
    }
}
