using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PopcornMinigame : Minigame
{
    public enum MovementMode { Falling, Bouncing }

    [Header("Testing & Design")]
    [SerializeField] private MovementMode movementMode = MovementMode.Falling;

    [Tooltip("Enable to spawn occasional burnt kernels that penalize the player.")]
    [SerializeField] private bool enableHazards = true;

    [Tooltip("The maximum number of burnt kernels allowed on the screen at the same time.")]
    [SerializeField] private int maxBurntKernelsOnScreen = 2;

    [Header("Spawning Settings")]
    [SerializeField] private Kernel kernelPrefab;
    [SerializeField] private RectTransform boundsArea;
    [SerializeField] private int totalKernelsOnScreen = 8;
    [SerializeField] private int targetPopsToWin = 15;

    [Header("UI & Audio")]
    [SerializeField] private TextMeshProUGUI counterText;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip popSound;
    [SerializeField] private AudioClip errorSound;

    private int remainingPops;
    private int currentBurntKernelsCount;
    private List<Kernel> activeKernels = new List<Kernel>();

    public override void StartMinigame()
    {
        base.StartMinigame();

        remainingPops = targetPopsToWin;
        currentBurntKernelsCount = 0;

        UpdateCounterUI();
        ClearKernels();

        for (int i = 0; i < totalKernelsOnScreen; i++)
        {
            SpawnKernel();
        }
    }

    public override void CloseMinigame()
    {
        base.CloseMinigame();
        ClearKernels();
    }

    private void SpawnKernel()
    {
        Kernel newKernel = Instantiate(kernelPrefab, boundsArea);

        float randomX = Random.Range(boundsArea.rect.xMin, boundsArea.rect.xMax);
        newKernel.GetComponent<RectTransform>().anchoredPosition = new Vector2(randomX, boundsArea.rect.yMin);

        // Only allow a burnt kernel if hazards are enabled AND we haven't hit the cap
        bool isBurnt = false;
        if (enableHazards && currentBurntKernelsCount < maxBurntKernelsOnScreen)
        {
            if (Random.value < 0.15f) // 15% chance
            {
                isBurnt = true;
                currentBurntKernelsCount++;
            }
        }

        newKernel.Setup(movementMode, boundsArea, isBurnt);
        newKernel.OnKernelPopped += HandleKernelPopped;

        activeKernels.Add(newKernel);
    }

    private void HandleKernelPopped(Kernel poppedKernel)
    {
        if (!IsActive) return;

        if (poppedKernel.IsBurnt)
        {
            // Free up a slot for a new hazard
            currentBurntKernelsCount--;

            if (sfxSource != null && errorSound != null) sfxSource.PlayOneShot(errorSound);

            // Penalty: Add to the countdown!
            remainingPops++;
        }
        else
        {
            if (sfxSource != null && popSound != null) sfxSource.PlayOneShot(popSound);

            // Success: Deduct from the countdown
            remainingPops--;
        }

        UpdateCounterUI();
        activeKernels.Remove(poppedKernel);

        if (remainingPops <= 0)
        {
            CompleteMinigame();
        }
        else
        {
            SpawnKernel();
        }
    }

    private void UpdateCounterUI()
    {
        if (counterText != null)
        {
            // Just display the remaining number
            counterText.text = remainingPops.ToString();
        }
    }

    private void ClearKernels()
    {
        foreach (var kernel in activeKernels)
        {
            if (kernel != null) Destroy(kernel.gameObject);
        }
        activeKernels.Clear();
        currentBurntKernelsCount = 0; // Reset the cap counter
    }
}