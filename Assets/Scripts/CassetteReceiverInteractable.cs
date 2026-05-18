using UnityEngine;

public class CassetteReceiverInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string promptWithCassette = "Appuyer sur [E] pour ins�rer la cassette";
    [SerializeField] private string promptWithoutCassette = "";
    [SerializeField] private CutsceneController cutsceneController;

    private bool cassetteInserted = false;

    public string PromptMessage
    {
        get
        {
            if (cassetteInserted) return "";
            return HeldItemSystem.Instance.HasItem ? promptWithCassette : promptWithoutCassette;
        }
    }

    public void Interact()
    {
        if (cassetteInserted || !HeldItemSystem.Instance.HasItem) return;

        cassetteInserted = true;
        HeldItemSystem.Instance.Drop();
        cutsceneController.TriggerCutscene();
    }
}
