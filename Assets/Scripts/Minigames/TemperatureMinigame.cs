using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TemperatureMinigame : Minigame
{
    [Header("UI References")]
    [Tooltip("The Unity Slider representing the temperature bar.")]
    [SerializeField] private Slider temperatureSlider;

    [Tooltip("The main image showing the microwave door/food.")]
    [SerializeField] private Image microwaveDisplayImage;

    [Header("Visual States")]
    [SerializeField] private Sprite cookingSprite; // Door closed
    [SerializeField] private Sprite frozenSprite;  // Door open, ice block
    [SerializeField] private Sprite perfectSprite; // Door open, perfect food
    [SerializeField] private Sprite burntSprite;   // Door open, ash pile

    [Header("Gameplay Settings")]
    [Tooltip("How fast the slider fills per second. (1 = 1 second to max)")]
    [SerializeField] private float fillSpeed = 1.5f;

    [Tooltip("The start of the winning zone (0.0 to 1.0)")]
    [SerializeField] private float perfectZoneMin = 0.65f;
    [Tooltip("The end of the winning zone (0.0 to 1.0)")]
    [SerializeField] private float perfectZoneMax = 0.8f;

    [Tooltip("How long to show the failed food before trying again.")]
    [SerializeField] private float resetDelay = 1.25f;

    private bool isEvaluating = false;

    public override void StartMinigame()
    {
        base.StartMinigame(); // From your base Minigame class
        ResetRound();
    }

    private void Update()
    {
        // Stop updating if the game isn't active, we are paused to show a result, or no mouse exists
        if (!IsActive || isEvaluating || Mouse.current == null) return;

        // Move the indicator automatically
        temperatureSlider.value += fillSpeed * Time.deltaTime;

        // Detect Left Click anywhere on the screen
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            EvaluateClick();
        }

        // Auto-fail if the bar reaches the absolute maximum before they click
        if (temperatureSlider.value >= 1f && !isEvaluating)
        {
            temperatureSlider.value = 1f;
            StartCoroutine(ShowResultAndReset(burntSprite));
        }
    }

    private void EvaluateClick()
    {
        isEvaluating = true;
        float stopValue = temperatureSlider.value;

        if (stopValue >= perfectZoneMin && stopValue <= perfectZoneMax)
        {
            // Win condition!
            if (microwaveDisplayImage != null) microwaveDisplayImage.sprite = perfectSprite;
            CompleteMinigame(); // Inherited from base class, tells the manager we won
        }
        else if (stopValue < perfectZoneMin)
        {
            // Clicked too early
            StartCoroutine(ShowResultAndReset(frozenSprite));
        }
        else
        {
            // Clicked too late
            StartCoroutine(ShowResultAndReset(burntSprite));
        }
    }

    private IEnumerator ShowResultAndReset(Sprite resultSprite)
    {
        isEvaluating = true;

        // Show the result (Open door with food state)
        if (microwaveDisplayImage != null) microwaveDisplayImage.sprite = resultSprite;

        // Pause to let the player process what happened
        yield return new WaitForSeconds(resetDelay);

        // Reset and try again
        ResetRound();
    }

    private void ResetRound()
    {
        isEvaluating = false;
        temperatureSlider.value = 0f;
        if (microwaveDisplayImage != null) microwaveDisplayImage.sprite = cookingSprite;
    }
}