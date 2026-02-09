using UnityEngine;

public class OutlineEffect : MonoBehaviour
{
    [SerializeField] private float scaleMultiplier = 1.1f;

    private Vector3 originalScale;
    private bool isHighlighted = false;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void EnableOutline()
    {
        if (isHighlighted) return;
        transform.localScale = originalScale * scaleMultiplier;
        isHighlighted = true;
    }

    public void DisableOutline()
    {
        if (!isHighlighted) return;
        transform.localScale = originalScale;
        isHighlighted = false;
    }
}
