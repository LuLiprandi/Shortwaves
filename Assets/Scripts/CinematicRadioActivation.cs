using UnityEngine;

public class CinematicRadioActivation : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Light radioLight;
    [SerializeField] private GameObject radioGlowObject;

    [Header("Interaction")]
    [SerializeField] private RadioInspectable radioInspectable;

    private void Start()
    {
        // Si on est au Jour 2+, la cutscène d'intro a déjà eu lieu — activer la radio directement.
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentDay > 1)
            Activate();
    }

    /// <summary>Activates the radio visual feedback and unlocks interaction at the end of the intro cutscene.</summary>
    public void Activate()
    {
        if (radioLight != null)
            radioLight.enabled = true;

        if (radioGlowObject != null)
            radioGlowObject.SetActive(true);

        if (radioInspectable != null)
            radioInspectable.Unlock();
    }
}
