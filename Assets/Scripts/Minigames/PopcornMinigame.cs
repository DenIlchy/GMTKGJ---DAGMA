using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PopcornMinigame : Minigame
{
    public enum MovementMode { Falling, Bouncing }

    [Header("Testing & Design")]
    [SerializeField] private MovementMode movementMode = MovementMode.Falling;
    [SerializeField] private bool enableHazards = true;
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

    // Using a local flag prevents us from wrestling with the base class's IsActive property
    private bool isWinning = false;

    public override void StartMinigame()
    {
        base.StartMinigame();

        isWinning = false;
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

        bool isBurnt = false;
        if (enableHazards && currentBurntKernelsCount < maxBurntKernelsOnScreen)
        {
            if (Random.value < 0.15f)
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
        // Block clicks if the minigame is closed OR if we are waiting for the win animation
        if (!IsActive || isWinning) return;

        if (poppedKernel.IsBurnt)
        {
            currentBurntKernelsCount--;
            if (sfxSource != null && errorSound != null) sfxSource.PlayOneShot(errorSound);
            remainingPops++;
        }
        else
        {
            if (sfxSource != null && popSound != null) sfxSource.PlayOneShot(popSound);
            remainingPops--;
        }

        UpdateCounterUI();
        activeKernels.Remove(poppedKernel);

        if (remainingPops <= 0)
        {
            // Flag that the game is effectively over to block further input
            isWinning = true;
            StartCoroutine(WinDelayRoutine());
        }
        else
        {
            SpawnKernel();
        }
    }

    private IEnumerator WinDelayRoutine()
    {
        // Wait 0.5 seconds for the final popcorn to fly up and fade out
        yield return new WaitForSeconds(0.5f);

        CompleteMinigame();
    }

    private void UpdateCounterUI()
    {
        if (counterText != null)
        {
            counterText.text = remainingPops.ToString();
        }
    }

    private void ClearKernels()
    {
        foreach (var kernel in activeKernels)
        {
            if (kernel != null) kernel.OnKernelPopped -= HandleKernelPopped;
        }
        activeKernels.Clear();

        foreach (Transform child in boundsArea)
        {
            Destroy(child.gameObject);
        }

        currentBurntKernelsCount = 0;
    }
}