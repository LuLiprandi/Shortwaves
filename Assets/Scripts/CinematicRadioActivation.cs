using UnityEngine;

public class CinematicRadioActivation : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Light radioLight;
    [SerializeField] private GameObject radioGlowObject;

    [Header("Interaction")]
    [SerializeField] private RadioInspectable radioInspectable;

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
