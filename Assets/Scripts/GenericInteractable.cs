using UnityEngine;
using UnityEngine.Events;

public class GenericInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string promptText = "Appuyer sur [E] pour interagir";
    [SerializeField] private UnityEvent onInteract;

    public string PromptMessage => promptText;

    public void Interact()
    {
        onInteract?.Invoke();
    }
}
