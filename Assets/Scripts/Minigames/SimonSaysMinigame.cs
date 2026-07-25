using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SimonSaysMinigame : Minigame
{
    [Header("UI Elements")]
    [Tooltip("The text display that will show the *** round countdown.")]
    [SerializeField] private TextMeshProUGUI roundDisplay;

    [Tooltip("The text display showing the player's input progress (e.g., _ _ _ _).")]
    [SerializeField] private TextMeshProUGUI progressDisplay;

    [Tooltip("Drag all the buttons you want to use (e.g., 0-9) in here.")]
    [SerializeField] private Button[] simonButtons;

    [Header("Game Settings")]
    [SerializeField] private int[] sequenceLengths = new int[] { 5, 4, 3 };
    [SerializeField] private float playbackSpeed = 0.5f;

    [Header("Colors")]
    [SerializeField] private Color highlightColor = new Color(0.5f, 1f, 0.5f, 1f);
    [SerializeField] private Color errorColor = new Color(1f, 0.4f, 0.4f, 1f);

    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip beepSound;
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip roundWinSound;

    private List<int> currentSequence = new List<int>();
    private Color[] originalButtonColors;

    private int currentRound = 0;
    private int playerInputIndex = 0;
    private bool isPlayerTurn = false;
    private bool isWired = false;

    private void Awake()
    {
        WireButtons();
        StoreOriginalColors();
    }

    private void StoreOriginalColors()
    {
        originalButtonColors = new Color[simonButtons.Length];
        for (int i = 0; i < simonButtons.Length; i++)
        {
            if (simonButtons[i] != null)
            {
                originalButtonColors[i] = simonButtons[i].GetComponent<Image>().color;
            }
        }
    }

    private void WireButtons()
    {
        if (isWired || simonButtons == null) return;
        isWired = true;

        for (int i = 0; i < simonButtons.Length; i++)
        {
            int index = i;
            if (simonButtons[i] != null)
            {
                simonButtons[i].onClick.RemoveAllListeners();
                simonButtons[i].onClick.AddListener(() => OnButtonClicked(index));
            }
        }
    }

    public override void StartMinigame()
    {
        base.StartMinigame();
        WireButtons();

        currentRound = 0;
        StartNewRound();
    }

    private void StartNewRound()
    {
        UpdateRoundDisplay();
        GenerateSequenceForRound();
        StartCoroutine(PlaySequenceRoutine());
    }

    private void GenerateSequenceForRound()
    {
        currentSequence.Clear();
        int targetLength = sequenceLengths[currentRound];

        for (int i = 0; i < targetLength; i++)
        {
            int randomButtonIndex = Random.Range(0, simonButtons.Length);
            currentSequence.Add(randomButtonIndex);
        }
    }

    private IEnumerator PlaySequenceRoutine()
    {
        isPlayerTurn = false;
        playerInputIndex = 0;

        // Let the player know they need to memorize
        if (progressDisplay != null) progressDisplay.text = "WATCH...";

        yield return new WaitForSeconds(0.8f);

        for (int i = 0; i < currentSequence.Count; i++)
        {
            if (!IsActive) yield break;

            int buttonIndex = currentSequence[i];

            PlayBeep(buttonIndex);

            Image btnImage = simonButtons[buttonIndex].GetComponent<Image>();
            btnImage.color = highlightColor;

            yield return new WaitForSeconds(playbackSpeed * 0.7f);

            btnImage.color = originalButtonColors[buttonIndex];

            yield return new WaitForSeconds(playbackSpeed * 0.3f);
        }

        isPlayerTurn = true;
        UpdateProgressDisplay(); // Initialize the _ _ _ _ display
    }

    private void OnButtonClicked(int buttonIndex)
    {
        if (!IsActive || !isPlayerTurn) return;

        if (currentSequence[playerInputIndex] == buttonIndex)
        {
            PlayBeep(buttonIndex);
            StartCoroutine(FlashButtonRoutine(buttonIndex, highlightColor, 0.15f));

            playerInputIndex++;
            UpdateProgressDisplay(); // Update the dashes to blocks

            if (playerInputIndex >= currentSequence.Count)
            {
                isPlayerTurn = false;
                StartCoroutine(RoundCompleteRoutine());
            }
        }
        else
        {
            isPlayerTurn = false;
            StartCoroutine(FailureRoutine(buttonIndex));
        }
    }

    private IEnumerator RoundCompleteRoutine()
    {
        if (sfxSource != null && roundWinSound != null) sfxSource.PlayOneShot(roundWinSound);

        currentRound++;

        if (roundDisplay != null) roundDisplay.color = Color.green;
        if (progressDisplay != null) progressDisplay.text = "<color=green>OK!</color>";

        yield return new WaitForSeconds(0.5f);

        if (roundDisplay != null) roundDisplay.color = Color.white;

        if (currentRound >= sequenceLengths.Length)
        {
            CompleteMinigame();
        }
        else
        {
            StartNewRound();
        }
    }

    private IEnumerator FailureRoutine(int wrongButtonIndex)
    {
        if (sfxSource != null && errorSound != null) sfxSource.PlayOneShot(errorSound);
        StartCoroutine(FlashButtonRoutine(wrongButtonIndex, errorColor, 0.5f));

        if (roundDisplay != null) roundDisplay.color = Color.red;
        if (progressDisplay != null) progressDisplay.text = "<color=red>ERROR</color>";

        yield return new WaitForSeconds(0.8f);

        if (roundDisplay != null) roundDisplay.color = Color.white;

        // Restart the same sequence
        StartCoroutine(PlaySequenceRoutine());
    }

    private IEnumerator FlashButtonRoutine(int index, Color flashColor, float duration)
    {
        Image img = simonButtons[index].GetComponent<Image>();
        img.color = flashColor;
        yield return new WaitForSeconds(duration);
        img.color = originalButtonColors[index];
    }

    private void PlayBeep(int buttonIndex)
    {
        if (sfxSource != null && beepSound != null)
        {
            sfxSource.pitch = 1f + (buttonIndex * 0.05f);
            sfxSource.PlayOneShot(beepSound);
        }
    }

    private void UpdateRoundDisplay()
    {
        if (roundDisplay != null)
        {
            int roundsRemaining = sequenceLengths.Length - currentRound;
            roundDisplay.text = new string('*', roundsRemaining);
        }
    }

    private void UpdateProgressDisplay()
    {
        if (progressDisplay == null) return;

        string progressStr = "";
        for (int i = 0; i < currentSequence.Count; i++)
        {
            if (i < playerInputIndex)
            {
                // Replaced the unicode block with standard ASCII characters
                progressStr += "* ";
            }
            else
            {
                progressStr += "- ";
            }
        }

        progressDisplay.text = progressStr.Trim();
    }
}