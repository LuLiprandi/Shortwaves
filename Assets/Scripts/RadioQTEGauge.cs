using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RadioQTEGauge : MonoBehaviour
{
    [Header("Aiguille")]
    [Tooltip("RectTransform de l'aiguille")]
    [SerializeField] private RectTransform needle;
    [Tooltip("Largeur totale de la jauge en pixels")]
    [SerializeField] private float gaugeHalfWidth = 200f;

    [Header("Zone de succès")]
    [Tooltip("Largeur de la zone verte en normalized (-1 à 1)")]
    [SerializeField] private float successZoneHalfSize = 0.18f;
    [SerializeField] private Image successZoneImage;
    [SerializeField] private Color successZoneColor = new Color(0.2f, 0.9f, 0.3f, 0.4f);
    [SerializeField] private Color dangerZoneColor = new Color(0.9f, 0.2f, 0.2f, 0.4f);

    [Header("Physique de l'aiguille")]
    [SerializeField] private float driftAcceleration = 1.0f;
    [SerializeField] private float maxDriftSpeed = 0.75f;
    [SerializeField] private float playerPushForce = 2.2f;
    [Tooltip("Damping exponentiel par seconde (indépendant du framerate). 0.2 = perd 80% de vitesse/s")]
    [SerializeField] private float damping = 0.2f;
    [SerializeField] private float randomImpulseInterval = 2.5f;
    [SerializeField] private float randomImpulseStrength = 0.35f;

    [Header("Progression")]
    [SerializeField] private Image progressBar;

    [Header("Indice contrôles")]
    [Tooltip("Label indiquant les touches au joueur pendant le QTE")]
    [SerializeField] private TMPro.TextMeshProUGUI controlsHintLabel;
    private const string ControlsHintText = "<size=150%>← →</size>  Maintenir dans la zone verte";

    [Header("Root UI")]
    [SerializeField] private GameObject gaugeRoot;

    public event Action OnSuccess;
    public event Action OnFail;

    private float needlePosition;
    private float needleVelocity;
    private float successDuration;
    private float timeInSuccessZone;
    private float nextImpulseTime;
    private bool isRunning;

    private const float FailZoneHalfSize = 0.95f;

    public void SetVisible(bool visible)
    {
        if (gaugeRoot != null)
            gaugeRoot.SetActive(visible);
    }

    public void StartQTE(float duration)
    {
        successDuration = duration;
        needlePosition = 0f;
        needleVelocity = UnityEngine.Random.Range(-0.3f, 0.3f);
        timeInSuccessZone = 0f;
        nextImpulseTime = Time.time + randomImpulseInterval;
        isRunning = true;

        if (controlsHintLabel != null)
            controlsHintLabel.text = ControlsHintText;

        UpdateNeedleVisual();
        UpdateProgressBar(0f);
    }

    public void StopQTE()
    {
        isRunning = false;
        needlePosition = 0f;
        needleVelocity = 0f;
        timeInSuccessZone = 0f;
        UpdateNeedleVisual();
        UpdateProgressBar(0f);
    }

    private void Update()
    {
        if (!isRunning) return;

        HandlePlayerInput();
        ApplyDrift();
        ApplyRandomImpulse();
        ClampNeedle();
        UpdateNeedleVisual();
        CheckZones();
    }

    public void PushInput(float direction)
    {
        if (!isRunning) return;
        needleVelocity += direction * playerPushForce * Time.deltaTime;
    }

    private void HandlePlayerInput()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.leftArrowKey.isPressed)
            needleVelocity -= playerPushForce * Time.deltaTime;

        if (Keyboard.current.rightArrowKey.isPressed)
            needleVelocity += playerPushForce * Time.deltaTime;
    }

    private void ApplyDrift()
    {
        // Needle always has a tendency to drift away from center
        float driftDirection = needlePosition >= 0f ? 1f : -1f;
        needleVelocity += driftDirection * driftAcceleration * Time.deltaTime;

        // Frame-rate independent exponential damping via Mathf.Pow
        needleVelocity *= Mathf.Pow(damping, Time.deltaTime);
        needleVelocity = Mathf.Clamp(needleVelocity, -maxDriftSpeed, maxDriftSpeed);
        needlePosition += needleVelocity * Time.deltaTime;
    }

    private void ApplyRandomImpulse()
    {
        if (Time.time < nextImpulseTime) return;

        float impulse = UnityEngine.Random.Range(-randomImpulseStrength, randomImpulseStrength);
        needleVelocity += impulse;
        nextImpulseTime = Time.time + UnityEngine.Random.Range(randomImpulseInterval * 0.7f, randomImpulseInterval * 1.3f);
    }

    private void ClampNeedle()
    {
        needlePosition = Mathf.Clamp(needlePosition, -1f, 1f);
    }

    private void CheckZones()
    {
        bool inSuccess = Mathf.Abs(needlePosition) <= successZoneHalfSize;
        bool inFail = Mathf.Abs(needlePosition) >= FailZoneHalfSize;

        if (successZoneImage != null)
            successZoneImage.color = inSuccess ? successZoneColor : dangerZoneColor;

        if (inFail)
        {
            isRunning = false;
            OnFail?.Invoke();
            return;
        }

        if (inSuccess)
        {
            timeInSuccessZone += Time.deltaTime;
            UpdateProgressBar(timeInSuccessZone / successDuration);

            if (timeInSuccessZone >= successDuration)
            {
                isRunning = false;
                OnSuccess?.Invoke();
            }
        }
        else
        {
            timeInSuccessZone = Mathf.MoveTowards(timeInSuccessZone, 0f, Time.deltaTime * 0.5f);
            UpdateProgressBar(timeInSuccessZone / successDuration);
        }
    }

    private void UpdateNeedleVisual()
    {
        if (needle == null) return;
        float xPos = needlePosition * gaugeHalfWidth;
        Vector2 pos = needle.anchoredPosition;
        needle.anchoredPosition = new Vector2(xPos, pos.y);
    }

    private void UpdateProgressBar(float t)
    {
        if (progressBar != null)
            progressBar.fillAmount = t;
    }
}
