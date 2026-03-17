using UnityEngine;

public class CinematicRadioActivation : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Light radioLight;
    [SerializeField] private GameObject radioGlowObject;

    /// <summary>Activates the radio visual feedback at the end of the intro cutscene.</summary>
    public void Activate()
    {
        if (radioLight != null)
            radioLight.enabled = true;

        if (radioGlowObject != null)
            radioGlowObject.SetActive(true);
    }
}
