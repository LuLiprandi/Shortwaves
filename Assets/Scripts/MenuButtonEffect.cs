using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Retro-style button: glows amber on hover, dims on exit, sinks on click.
/// Attach alongside a Button component.
/// </summary>
[RequireComponent(typeof(Button))]
public class MenuButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image   background;
    [SerializeField] private TextMeshProUGUI label;

    private static readonly Color NormalBg   = new Color(0.10f, 0.09f, 0.07f, 1f);
    private static readonly Color HoverBg    = new Color(0.22f, 0.16f, 0.05f, 1f);
    private static readonly Color PressedBg  = new Color(0.07f, 0.06f, 0.04f, 1f);

    private static readonly Color NormalText  = new Color(0.75f, 0.58f, 0.22f, 1f);
    private static readonly Color HoverText   = new Color(1.00f, 0.80f, 0.30f, 1f);
    private static readonly Color PressedText = new Color(0.55f, 0.42f, 0.15f, 1f);

    private void Awake()
    {
        if (background == null) background = GetComponent<Image>();
        if (label == null)      label      = GetComponentInChildren<TextMeshProUGUI>();
        ApplyColors(NormalBg, NormalText);
    }

    public void OnPointerEnter(PointerEventData _) => ApplyColors(HoverBg, HoverText);
    public void OnPointerExit(PointerEventData _)  => ApplyColors(NormalBg, NormalText);
    public void OnPointerDown(PointerEventData _)  => ApplyColors(PressedBg, PressedText);
    public void OnPointerUp(PointerEventData _)    => ApplyColors(HoverBg, HoverText);

    private void ApplyColors(Color bg, Color text)
    {
        if (background != null) background.color = bg;
        if (label != null)      label.color       = text;
    }
}
