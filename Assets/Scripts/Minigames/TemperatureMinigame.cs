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
    [SerializeField] private float initialSpeed = 0.2f;
    [SerializeField] private float acceleration = 0.8f;
    [SerializeField] private float perfectZoneMin = 0.65f;
    [SerializeField] private float perfectZoneMax = 0.8f;
    [SerializeField] private float resultDelay = 1.25f;

    [Header("Audio Settings")]
    [Tooltip("The AudioSource used to play minigame sound effects.")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip successClip;
    [SerializeField] private AudioClip failureClip;

    private bool isEvaluating = false;
    private float currentSpeed;

    public override void StartMinigame()
    {
        base.StartMinigame();

        PositionTargetLine(minTargetLine, perfectZoneMin);
        PositionTargetLine(maxTargetLine, perfectZoneMax);

        ResetRound();
    }

    private void Update()
    {
        if (!IsActive || isEvaluating) return;

        // Exponential movement
        currentSpeed += acceleration * Time.deltaTime;
        temperatureSlider.value += currentSpeed * Time.deltaTime;

        // Auto-fail if the bar reaches the top
        if (temperatureSlider.value >= 1f && !isEvaluating)
        {
            temperatureSlider.value = 1f;
            StartCoroutine(ShowResultCoroutine(burntSprite, false));
        }
    }

    public void OnDoorButtonClicked()
    {
        if (!IsActive || isEvaluating) return;

        float stopValue = temperatureSlider.value;

        if (stopValue >= perfectZoneMin && stopValue <= perfectZoneMax)
        {
            StartCoroutine(ShowResultCoroutine(perfectSprite, true));
        }
        else if (stopValue < perfectZoneMin)
        {
            StartCoroutine(ShowResultCoroutine(frozenSprite, false));
        }
        else
        {
            StartCoroutine(ShowResultCoroutine(burntSprite, false));
        }
    }

    private IEnumerator ShowResultCoroutine(Sprite resultSprite, bool isWin)
    {
        isEvaluating = true; // Instantly stops the bar from moving

        // 1. Immediately show the result visual (door opens)
        if (microwaveDisplayImage != null) microwaveDisplayImage.sprite = resultSprite;

        // 2. Play the click sound and wait for it to finish
        if (sfxSource != null && buttonClickClip != null)
        {
            sfxSource.PlayOneShot(buttonClickClip);
            yield return new WaitForSeconds(buttonClickClip.length);
        }

        // 3. Now play the success or failure sound
        if (sfxSource != null)
        {
            AudioClip clipToPlay = isWin ? successClip : failureClip;
            if (clipToPlay != null)
            {
                sfxSource.PlayOneShot(clipToPlay);
            }
        }

        // 4. Wait for the player to process the result before resetting/winning
        yield return new WaitForSeconds(resultDelay);

        if (isWin)
        {
            CompleteMinigame();
        }
        else
        {
            ResetRound();
        }
    }

    private void ResetRound()
    {
        isEvaluating = false;
        temperatureSlider.value = 0f;
        currentSpeed = initialSpeed;

        if (microwaveDisplayImage != null) microwaveDisplayImage.sprite = cookingSprite;
    }

    private void PositionTargetLine(RectTransform lineRect, float normalizedPosition)
    {
        if (lineRect == null) return;

        lineRect.anchorMin = new Vector2(0, normalizedPosition);
        lineRect.anchorMax = new Vector2(1, normalizedPosition);
        lineRect.anchoredPosition = Vector2.zero;
        lineRect.sizeDelta = new Vector2(0, 2f);
    }
}