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
    [Tooltip("The top text display (e.g., 'Kernels left to pop:').")]
    [SerializeField] private TextMeshProUGUI promptDisplay;
    [Tooltip("The bottom text display for the number countdown.")]
    [SerializeField] private TextMeshProUGUI outputDisplay;

    [Space(10)]
    [SerializeField] private AudioSource sfxSource;

    [Tooltip("Add all your pop sound variations here. One will be chosen at random!")]
    [SerializeField] private AudioClip[] popSounds;

    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip winSound;

    private int remainingPops;
    private int currentBurntKernelsCount;
    private List<Kernel> activeKernels = new List<Kernel>();

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
        if (!IsActive || isWinning) return;

        if (poppedKernel.IsBurnt)
        {
            currentBurntKernelsCount--;
            if (sfxSource != null && errorSound != null) sfxSource.PlayOneShot(errorSound);
            remainingPops++;
        }
        else
        {
            // Pick a random pop sound from the array
            if (sfxSource != null && popSounds != null && popSounds.Length > 0)
            {
                AudioClip randomPop = popSounds[Random.Range(0, popSounds.Length)];
                if (randomPop != null) sfxSource.PlayOneShot(randomPop);
            }

            remainingPops--;
        }

        UpdateCounterUI();
        activeKernels.Remove(poppedKernel);

        if (remainingPops <= 0)
        {
            isWinning = true;

            if (sfxSource != null && winSound != null) sfxSource.PlayOneShot(winSound);
            if (outputDisplay != null) outputDisplay.text = "<color=green>POPPED!</color>";

            StartCoroutine(WinDelayRoutine());
        }
        else
        {
            SpawnKernel();
        }
    }

    private IEnumerator WinDelayRoutine()
    {
        yield return new WaitForSeconds(0.8f);
        CompleteMinigame();
    }

    private void UpdateCounterUI()
    {
        if (isWinning) return;

        if (promptDisplay != null)
        {
            promptDisplay.text = "Kernels left to pop:";
        }

        if (outputDisplay != null)
        {
            outputDisplay.text = remainingPops.ToString();
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