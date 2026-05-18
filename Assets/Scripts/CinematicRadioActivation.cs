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
        if (GameStateManager.Instance != null && GameStateManager.Instance.CurrentDay > 1)
            Activate();
    }

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
