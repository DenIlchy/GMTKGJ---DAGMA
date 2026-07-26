using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TemperatureMinigame : Minigame
{
    [Header("UI References")]
    [Tooltip("The Unity Slider representing the temperature bar.")]
    [SerializeField] private Slider temperatureSlider;

    [Tooltip("The main image showing the microwave door/food 2D animation.")]
    [SerializeField] private Image microwaveDisplayImage;

    [Tooltip("The bottom white line marking the start of the perfect zone.")]
    [SerializeField] private RectTransform minTargetLine;

    [Tooltip("The top white line marking the end of the perfect zone.")]
    [SerializeField] private RectTransform maxTargetLine;

    [Header("2D Animation Sequence")]
    [Tooltip("Array of 2D animation frames (0000.png through 0047.png).")]
    [SerializeField] private Sprite[] animationFrames;

    [Header("Gameplay Settings")]
    [SerializeField] private float initialSpeed = 0.2f;
    [SerializeField] private float acceleration = 0.8f;

    [Tooltip("Start of 'Just Right' zone (normalized: 0.511 = Frame 25).")]
    [SerializeField] private float perfectZoneMin = 0.511f;

    [Tooltip("End of 'Just Right' zone (normalized: 0.733 = Frame 34).")]
    [SerializeField] private float perfectZoneMax = 0.733f;

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

        if (microwaveDisplayImage != null)
        {
            microwaveDisplayImage.preserveAspect = true;
        }

        ResetRound();
    }

    private void Update()
    {
        if (!IsActive || isEvaluating) return;

        // Move slider forward
        currentSpeed += acceleration * Time.deltaTime;
        temperatureSlider.value += currentSpeed * Time.deltaTime;

        // Synchronize 2D animation frame with current slider position
        UpdateAnimationFrame();

        // Auto-fail if the bar reaches the top (100% / Overcooked)
        if (temperatureSlider.value >= 1f && !isEvaluating)
        {
            temperatureSlider.value = 1f;
            UpdateAnimationFrame();
            StartCoroutine(ShowResultCoroutine(false));
        }
    }

    private void UpdateAnimationFrame()
    {
        if (microwaveDisplayImage == null || animationFrames == null || animationFrames.Length == 0) return;

        float val = Mathf.Clamp01(temperatureSlider != null ? temperatureSlider.value : 0f);
        int frameIndex = Mathf.Clamp(Mathf.FloorToInt(val * (animationFrames.Length - 1)), 0, animationFrames.Length - 1);

        if (animationFrames[frameIndex] != null)
        {
            microwaveDisplayImage.sprite = animationFrames[frameIndex];
        }
    }

    public void OnDoorButtonClicked()
    {
        if (!IsActive || isEvaluating) return;

        float stopValue = temperatureSlider != null ? temperatureSlider.value : 0f;

        bool isWin = (stopValue >= perfectZoneMin && stopValue <= perfectZoneMax);
        StartCoroutine(ShowResultCoroutine(isWin));
    }

    private IEnumerator ShowResultCoroutine(bool isWin)
    {
        isEvaluating = true; // Instantly stops the slider and freezes animation frame

        // 1. Play the click sound and wait for it to finish
        if (sfxSource != null && buttonClickClip != null)
        {
            sfxSource.PlayOneShot(buttonClickClip);
            yield return new WaitForSeconds(buttonClickClip.length);
        }

        // 2. Play success or failure sound
        if (sfxSource != null)
        {
            AudioClip clipToPlay = isWin ? successClip : failureClip;
            if (clipToPlay != null)
            {
                sfxSource.PlayOneShot(clipToPlay);
            }
        }

        // 3. Wait for the player to process the result before resetting/winning
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
        if (temperatureSlider != null)
        {
            temperatureSlider.value = 0f;
        }
        currentSpeed = initialSpeed;
        UpdateAnimationFrame();
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