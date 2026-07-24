using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TemperatureMinigame : Minigame
{
    [Header("UI References")]
    [Tooltip("The Unity Slider representing the temperature bar.")]
    [SerializeField] private Slider temperatureSlider;

    [Tooltip("The main image showing the microwave door/food.")]
    [SerializeField] private Image microwaveDisplayImage;

    [Tooltip("The bottom white line marking the start of the perfect zone.")]
    [SerializeField] private RectTransform minTargetLine;

    [Tooltip("The top white line marking the end of the perfect zone.")]
    [SerializeField] private RectTransform maxTargetLine;

    [Header("Visual States")]
    [SerializeField] private Sprite cookingSprite; // Door closed
    [SerializeField] private Sprite frozenSprite;  // Door open, ice block
    [SerializeField] private Sprite perfectSprite; // Door open, perfect food
    [SerializeField] private Sprite burntSprite;   // Door open, ash pile

    [Header("Gameplay Settings")]
    [Tooltip("Starting speed of the slider.")]
    [SerializeField] private float initialSpeed = 0.2f;

    [Tooltip("How quickly the slider accelerates over time.")]
    [SerializeField] private float acceleration = 0.8f;

    [Tooltip("The start of the winning zone (0.0 to 1.0)")]
    [SerializeField] private float perfectZoneMin = 0.65f;
    [Tooltip("The end of the winning zone (0.0 to 1.0)")]
    [SerializeField] private float perfectZoneMax = 0.8f;

    [Tooltip("How long to show the food result before resetting or winning.")]
    [SerializeField] private float resultDelay = 1.25f;

    private bool isEvaluating = false;
    private float currentSpeed;

    public override void StartMinigame()
    {
        base.StartMinigame();

        // Automatically position the target lines based on our min/max variables
        PositionTargetLine(minTargetLine, perfectZoneMin);
        PositionTargetLine(maxTargetLine, perfectZoneMax);

        ResetRound();
    }

    private void Update()
    {
        if (!IsActive || isEvaluating) return;

        // Exponential movement logic: increase speed over time, then apply speed to slider
        currentSpeed += acceleration * Time.deltaTime;
        temperatureSlider.value += currentSpeed * Time.deltaTime;

        // Auto-fail if the bar reaches the absolute maximum before they click
        if (temperatureSlider.value >= 1f && !isEvaluating)
        {
            temperatureSlider.value = 1f;
            StartCoroutine(ShowResultCoroutine(burntSprite, false));
        }
    }

    /// <summary>
    /// This method will be triggered by your UI Button OnClick event.
    /// </summary>
    public void OnDoorButtonClicked()
    {
        // Prevent double-clicking
        if (!IsActive || isEvaluating) return;

        float stopValue = temperatureSlider.value;

        if (stopValue >= perfectZoneMin && stopValue <= perfectZoneMax)
        {
            // Win condition
            StartCoroutine(ShowResultCoroutine(perfectSprite, true));
        }
        else if (stopValue < perfectZoneMin)
        {
            // Clicked too early
            StartCoroutine(ShowResultCoroutine(frozenSprite, false));
        }
        else
        {
            // Clicked too late
            StartCoroutine(ShowResultCoroutine(burntSprite, false));
        }
    }

    private IEnumerator ShowResultCoroutine(Sprite resultSprite, bool isWin)
    {
        isEvaluating = true;

        // Show the result
        if (microwaveDisplayImage != null) microwaveDisplayImage.sprite = resultSprite;

        // Pause so the player can see what happened
        yield return new WaitForSeconds(resultDelay);

        if (isWin)
        {
            CompleteMinigame(); // From base class, triggers success
        }
        else
        {
            ResetRound(); // Try again
        }
    }

    private void ResetRound()
    {
        isEvaluating = false;
        temperatureSlider.value = 0f;
        currentSpeed = initialSpeed; // Reset our acceleration

        if (microwaveDisplayImage != null) microwaveDisplayImage.sprite = cookingSprite;
    }

    private void PositionTargetLine(RectTransform lineRect, float normalizedPosition)
    {
        if (lineRect == null) return;

        // Set the Y anchors to the exact percentage on the slider
        lineRect.anchorMin = new Vector2(0, normalizedPosition);
        lineRect.anchorMax = new Vector2(1, normalizedPosition);

        // Reset offsets so it sits exactly on the anchor
        lineRect.anchoredPosition = Vector2.zero;
        lineRect.sizeDelta = new Vector2(0, 2f); // Keep it full width, 2 pixels tall
    }
}