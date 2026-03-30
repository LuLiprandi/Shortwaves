using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// Interactive digit-slot decoder panel. After QTE success, the player listens
/// to the voice clip and enters the corresponding numeric code into the slots.
/// Slots are built dynamically from the SolutionCode format "8/5/11".
/// </summary>
public class RadioDecoderPanel : MonoBehaviour
{
    [Header("Références UI existantes")]
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI feedbackLabel;

    [Header("Apparence des slots")]
    [SerializeField] private Color slotNormalColor   = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    [SerializeField] private Color slotSelectedColor = new Color(0.9f,  0.75f, 0.1f,  1f);
    [SerializeField] private Color slotCorrectColor  = new Color(0.15f, 0.8f,  0.25f, 1f);
    [SerializeField] private Color slotWrongColor    = new Color(0.85f, 0.15f, 0.15f, 1f);

    private const string SolutionSeparator = "/";
    private const string TitleText         = "DÉCHIFFRE LE CODE";
    private const string SubtitleText      = "← → naviguer  •  chiffres: entrer  •  Entrée: valider";

    private string[] solutionParts;
    private string[] playerInput;
    private int currentSlotIndex;
    private TextMeshProUGUI[] slotTexts;
    private Image[] slotBackgrounds;
    private GameObject slotsContainer;
    private bool isValidated;
    private bool isActive;

    public event Action OnSuccess;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    /// <summary>Initializes and shows the decoder with the given solution code. Format: "8/5/11".</summary>
    public void Initialize(string solutionCode)
    {
        if (string.IsNullOrEmpty(solutionCode))
        {
            Debug.LogWarning("[RadioDecoderPanel] SolutionCode vide — assigne la valeur dans RadioStationData.SolutionCode.");
            return;
        }

        solutionParts = solutionCode.Split(SolutionSeparator);
        playerInput   = new string[solutionParts.Length];
        for (int i = 0; i < playerInput.Length; i++) playerInput[i] = "";

        currentSlotIndex = 0;
        isValidated      = false;
        isActive         = true;

        if (titleLabel != null)    titleLabel.text    = TitleText;
        if (feedbackLabel != null) feedbackLabel.text = SubtitleText;

        BuildSlotsUI();
        RefreshSlotVisuals();
        gameObject.SetActive(true);
    }

    /// <summary>Hides the panel and resets state.</summary>
    public void Hide()
    {
        isActive = false;
        if (slotsContainer != null) Destroy(slotsContainer);
        slotsContainer = null;
        gameObject.SetActive(false);
    }

    private void BuildSlotsUI()
    {
        if (slotsContainer != null) Destroy(slotsContainer);

        slotsContainer = new GameObject("SlotsContainer");
        slotsContainer.transform.SetParent(transform, false);

        var rect = slotsContainer.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.35f);
        rect.anchorMax = new Vector2(1f, 0.72f);
        rect.offsetMin = new Vector2(12f, 0f);
        rect.offsetMax = new Vector2(-12f, 0f);

        var hlg                   = slotsContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment        = TextAnchor.MiddleCenter;
        hlg.spacing               = 14f;
        hlg.childControlWidth     = false;
        hlg.childControlHeight    = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight= false;

        slotTexts       = new TextMeshProUGUI[solutionParts.Length];
        slotBackgrounds = new Image[solutionParts.Length];

        for (int i = 0; i < solutionParts.Length; i++)
        {
            if (i > 0) CreateSeparator(slotsContainer.transform);
            CreateSlot(i, slotsContainer.transform);
        }
    }

    private void CreateSeparator(Transform parent)
    {
        var obj  = new GameObject("Sep");
        obj.transform.SetParent(parent, false);
        var r    = obj.AddComponent<RectTransform>();
        r.sizeDelta = new Vector2(22f, 72f);
        var tmp  = obj.AddComponent<TextMeshProUGUI>();
        tmp.text      = "/";
        tmp.fontSize  = 38f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = new Color(1f, 1f, 1f, 0.55f);
        tmp.alignment = TextAlignmentOptions.Center;
    }

    private void CreateSlot(int index, Transform parent)
    {
        int digitCount = Mathf.Max(solutionParts[index].Length, 1);
        float width    = digitCount * 44f + 24f;

        var slotObj  = new GameObject($"Slot_{index}");
        slotObj.transform.SetParent(parent, false);
        var slotRect = slotObj.AddComponent<RectTransform>();
        slotRect.sizeDelta = new Vector2(width, 72f);

        var bg            = slotObj.AddComponent<Image>();
        bg.color          = slotNormalColor;
        slotBackgrounds[index] = bg;

        var textObj  = new GameObject("Text");
        textObj.transform.SetParent(slotObj.transform, false);
        var textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var tmp       = textObj.AddComponent<TextMeshProUGUI>();
        tmp.fontSize  = 42f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        slotTexts[index] = tmp;
    }

    private void Update()
    {
        if (!isActive || isValidated) return;

        HandleNavigation();
        HandleDigitInput();
        HandleBackspace();
        HandleSubmit();
    }

    private void HandleNavigation()
    {
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame && currentSlotIndex > 0)
        { currentSlotIndex--; RefreshSlotVisuals(); }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame && currentSlotIndex < solutionParts.Length - 1)
        { currentSlotIndex++; RefreshSlotVisuals(); }
    }

    private void HandleDigitInput()
    {
        for (int d = 0; d <= 9; d++)
        {
            if (!IsDigitJustPressed(d)) continue;

            int maxLen = solutionParts[currentSlotIndex].Length;
            if (playerInput[currentSlotIndex].Length >= maxLen) continue;

            playerInput[currentSlotIndex] += d.ToString();
            RefreshSlotVisuals();

            // Auto-advance when slot is full
            if (playerInput[currentSlotIndex].Length >= maxLen && currentSlotIndex < solutionParts.Length - 1)
            { currentSlotIndex++; RefreshSlotVisuals(); }
        }
    }

    private void HandleBackspace()
    {
        if (!Keyboard.current.backspaceKey.wasPressedThisFrame) return;

        if (playerInput[currentSlotIndex].Length > 0)
            playerInput[currentSlotIndex] = playerInput[currentSlotIndex][..^1];
        else if (currentSlotIndex > 0)
        {
            currentSlotIndex--;
            if (playerInput[currentSlotIndex].Length > 0)
                playerInput[currentSlotIndex] = playerInput[currentSlotIndex][..^1];
        }
        RefreshSlotVisuals();
    }

    private void HandleSubmit()
    {
        bool enter = Keyboard.current.enterKey.wasPressedThisFrame
                  || Keyboard.current.numpadEnterKey.wasPressedThisFrame;
        if (enter) Validate();
    }

    private void Validate()
    {
        bool allCorrect = true;
        for (int i = 0; i < solutionParts.Length; i++)
            if (playerInput[i] != solutionParts[i]) { allCorrect = false; break; }

        if (allCorrect)
        {
            isValidated = true;
            for (int i = 0; i < slotBackgrounds.Length; i++)
                slotBackgrounds[i].color = slotCorrectColor;
            SetFeedback("CODE CORRECT !", slotCorrectColor);
            OnSuccess?.Invoke();
        }
        else
        {
            StartCoroutine(WrongFeedback());
        }
    }

    private IEnumerator WrongFeedback()
    {
        for (int i = 0; i < slotBackgrounds.Length; i++)
            if (playerInput[i] != solutionParts[i])
                slotBackgrounds[i].color = slotWrongColor;

        SetFeedback("CODE INCORRECT — Réessaie.", slotWrongColor);
        yield return new WaitForSeconds(0.9f);

        for (int i = 0; i < slotBackgrounds.Length; i++)
        {
            if (playerInput[i] != solutionParts[i])
            {
                playerInput[i] = "";
                slotBackgrounds[i].color = i == currentSlotIndex ? slotSelectedColor : slotNormalColor;
            }
        }
        SetFeedback(SubtitleText, Color.white);
        RefreshSlotVisuals();
    }

    private void RefreshSlotVisuals()
    {
        if (slotTexts == null) return;

        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (slotTexts[i] == null) continue;

            int targetLen = solutionParts[i].Length;
            string display = playerInput[i];
            while (display.Length < targetLen) display += "_";
            slotTexts[i].text = display;

            if (!isValidated)
                slotBackgrounds[i].color = i == currentSlotIndex ? slotSelectedColor : slotNormalColor;
        }
    }

    private void SetFeedback(string message, Color color)
    {
        if (feedbackLabel == null) return;
        feedbackLabel.text  = message;
        feedbackLabel.color = color;
    }

    private static bool IsDigitJustPressed(int d) => d switch
    {
        0 => Keyboard.current.digit0Key.wasPressedThisFrame || Keyboard.current.numpad0Key.wasPressedThisFrame,
        1 => Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame,
        2 => Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame,
        3 => Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame,
        4 => Keyboard.current.digit4Key.wasPressedThisFrame || Keyboard.current.numpad4Key.wasPressedThisFrame,
        5 => Keyboard.current.digit5Key.wasPressedThisFrame || Keyboard.current.numpad5Key.wasPressedThisFrame,
        6 => Keyboard.current.digit6Key.wasPressedThisFrame || Keyboard.current.numpad6Key.wasPressedThisFrame,
        7 => Keyboard.current.digit7Key.wasPressedThisFrame || Keyboard.current.numpad7Key.wasPressedThisFrame,
        8 => Keyboard.current.digit8Key.wasPressedThisFrame || Keyboard.current.numpad8Key.wasPressedThisFrame,
        9 => Keyboard.current.digit9Key.wasPressedThisFrame || Keyboard.current.numpad9Key.wasPressedThisFrame,
        _ => false
    };
}
