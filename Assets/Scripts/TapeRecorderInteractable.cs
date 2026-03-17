using UnityEngine;

public class TapeRecorderInteractable : MonoBehaviour, IInteractable
{
    [Header("Settings")]
    [SerializeField] private string promptWithCassette = "Appuyer sur [E] pour insérer la cassette";
    [SerializeField] private string promptWithoutCassette = "";
    [SerializeField] private CutsceneController cutsceneController;

    public string PromptMessage => HeldItemSystem.Instance.HasItem
        ? promptWithCassette
        : promptWithoutCassette;

    /// <summary>Inserts the cassette and triggers the intro cutscene.</summary>
    public void Interact()
    {
        if (!HeldItemSystem.Instance.HasItem) return;

        HeldItemSystem.Instance.Drop();
        cutsceneController.TriggerCutscene();
    }
}
