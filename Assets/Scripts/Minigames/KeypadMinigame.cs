using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class MicrowaveQuestion
{
    [Tooltip("The riddle/hint prompt displayed on the screen.")]
    public string promptText;
    [Tooltip("The implied answer code (e.g. 911, 420, 80085).")]
    public string targetCode;

    public MicrowaveQuestion(string prompt, string code)
    {
        promptText = prompt;
        targetCode = code;
    }
}

public class KeypadMinigame : Minigame
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI targetDisplay;
    [SerializeField] private TextMeshProUGUI outputDisplay;
    
    [Header("Buttons (12 Total Grid)")]
    [SerializeField] private Button[] digitButtons; // 0 through 9
    [SerializeField] private Button clearButton;
    [SerializeField] private Button timeButton;

    [Header("Multi-Question Pool Settings")]
    [SerializeField] private List<MicrowaveQuestion> questionPool = new List<MicrowaveQuestion>()
    {
        new MicrowaveQuestion("Quick! Call the Police!", "911"),
        new MicrowaveQuestion("Blaze it!", "420"),
        new MicrowaveQuestion("They come in pairs:", "80085")
    };
    
    [Tooltip("Number of questions the player must solve to complete this minigame session.")]
    [SerializeField] private int requiredSolvesToWin = 3;
    [SerializeField] private int maxDigits = 5;

    [Header("Button Press Feedback Colors")]
    [SerializeField] private Color pressFlashColor = new Color(0.9f, 0.95f, 1f, 1f);

    private string currentInput = "";
    private MicrowaveQuestion currentQuestion;
    private string lastAskedPrompt = "";
    private List<MicrowaveQuestion> availableQuestionDeck = new List<MicrowaveQuestion>();

    private int currentSolveCount = 0;
    private bool isWired = false;
    private float lastInputTime = 0f;
    private const float inputCooldown = 0.05f;

    private void Awake()
    {
        AutoFindDisplays();
        WireButtons();
    }

    private void OnEnable()
    {
        AutoFindDisplays();
        isWired = false;
        WireButtons();
    }

    private void AutoFindDisplays()
    {
        if (targetDisplay == null)
        {
            Transform t = transform.Find("MicrowaveScreen/TargetText");
            if (t != null) targetDisplay = t.GetComponent<TextMeshProUGUI>();
        }

        if (outputDisplay == null)
        {
            Transform o = transform.Find("MicrowaveScreen/OutputText");
            if (o != null) outputDisplay = o.GetComponent<TextMeshProUGUI>();
        }
    }

    private void WireButtons()
    {
        if (isWired) return;
        isWired = true;

        AutoFindDisplays();

        // Wire digit buttons 0-9
        if (digitButtons != null)
        {
            for (int i = 0; i < digitButtons.Length; i++)
            {
                int digitIndex = i; // Closure capture
                if (digitButtons[i] != null)
                {
                    digitButtons[i].onClick.RemoveAllListeners();
                    digitButtons[i].onClick.AddListener(() => PressDigit(digitIndex));
                }
            }
        }

        if (clearButton != null)
        {
            clearButton.onClick.RemoveAllListeners();
            clearButton.onClick.AddListener(PressClear);
        }

        if (timeButton != null)
        {
            timeButton.onClick.RemoveAllListeners();
            timeButton.onClick.AddListener(PressTime);
        }
    }

    private bool IsDebounced()
    {
        if (Time.unscaledTime - lastInputTime < inputCooldown)
        {
            return true;
        }
        lastInputTime = Time.unscaledTime;
        return false;
    }

    public override void StartMinigame()
    {
        base.StartMinigame();
        AutoFindDisplays();
        WireButtons();

        currentSolveCount = 0;
        lastAskedPrompt = "";
        RefillQuestionDeck();
        PickNextQuestion();
        ResetKeypad();
    }

    /// <summary>
    /// Refills and shuffles the question deck when all questions have been burned.
    /// </summary>
    private void RefillQuestionDeck()
    {
        availableQuestionDeck.Clear();
        if (questionPool == null || questionPool.Count == 0)
        {
            questionPool = new List<MicrowaveQuestion>()
            {
                new MicrowaveQuestion("Quick! Call the Police!", "911"),
                new MicrowaveQuestion("Blaze it!", "420"),
                new MicrowaveQuestion("They come in pairs:", "80085")
            };
        }

        availableQuestionDeck.AddRange(questionPool);
        
        // Shuffle deck
        for (int i = 0; i < availableQuestionDeck.Count; i++)
        {
            var temp = availableQuestionDeck[i];
            int randomIndex = Random.Range(i, availableQuestionDeck.Count);
            availableQuestionDeck[i] = availableQuestionDeck[randomIndex];
            availableQuestionDeck[randomIndex] = temp;
        }

        // Ensure the first question of the new deck is NOT identical to the last asked prompt
        if (availableQuestionDeck.Count > 1 && availableQuestionDeck[0].promptText == lastAskedPrompt)
        {
            var first = availableQuestionDeck[0];
            availableQuestionDeck[0] = availableQuestionDeck[availableQuestionDeck.Count - 1];
            availableQuestionDeck[availableQuestionDeck.Count - 1] = first;
        }
    }

    /// <summary>
    /// Pulls the next non-repeating question from the deck.
    /// </summary>
    private void PickNextQuestion()
    {
        if (availableQuestionDeck.Count == 0)
        {
            RefillQuestionDeck();
        }

        currentQuestion = availableQuestionDeck[0];
        availableQuestionDeck.RemoveAt(0);
        lastAskedPrompt = currentQuestion.promptText;

        Debug.Log($"[KeypadMinigame] Picked Question: '{currentQuestion.promptText}' -> Target Code: '{currentQuestion.targetCode}' (Deck remaining: {availableQuestionDeck.Count})");
    }

    /// <summary>
    /// Microwave/Calculator left-shifted digit entry.
    /// </summary>
    public void PressDigit(int digit)
    {
        if (IsCompleted || IsDebounced()) return;

        // Play SFX Placeholder
        if (SoundManager.Instance != null) SoundManager.Instance.PlayKeypadClickSFX();

        // Flash visual feedback on pressed button
        if (digitButtons != null && digit >= 0 && digit < digitButtons.Length && digitButtons[digit] != null)
        {
            FlashButton(digitButtons[digit]);
        }

        if (currentInput.Length < maxDigits)
        {
            currentInput += digit.ToString();
            Debug.Log($"[KeypadMinigame] Digit {digit} entered! Current raw input: '{currentInput}'");
            UpdateDisplay();
        }
    }

    /// <summary>
    /// Clears the entered digits back to empty.
    /// </summary>
    public void PressClear()
    {
        if (IsCompleted || IsDebounced()) return;

        // Play SFX Placeholder
        if (SoundManager.Instance != null) SoundManager.Instance.PlayKeypadClickSFX();

        if (clearButton != null) FlashButton(clearButton);

        Debug.Log("[KeypadMinigame] Clear pressed! Input cleared.");
        currentInput = "";
        UpdateDisplay();
    }

    /// <summary>
    /// Submits the entered time code and checks validity against the current riddle target.
    /// </summary>
    public void PressTime()
    {
        if (IsCompleted || IsDebounced()) return;

        // Play SFX Placeholder
        if (SoundManager.Instance != null) SoundManager.Instance.PlayKeypadClickSFX();

        if (timeButton != null) FlashButton(timeButton);

        if (currentQuestion == null) return;

        string target = currentQuestion.targetCode;
        Debug.Log($"[KeypadMinigame] Submitting input '{currentInput}' against target '{target}'");

        // Flexible match checking (exact match, trimmed zero match, or time-formatted match)
        bool isCorrect = (currentInput == target) ||
                         (currentInput.TrimStart('0') == target.TrimStart('0')) ||
                         (FormatAsTime(currentInput) == FormatAsTime(target));

        if (isCorrect)
        {
            currentSolveCount++;
            Debug.Log($"[KeypadMinigame] Correct answer! Solved {currentSolveCount}/{requiredSolvesToWin}");

            if (currentSolveCount >= requiredSolvesToWin)
            {
                if (outputDisplay != null) outputDisplay.text = "<color=green>COOKING!</color>";
                
                // 1. Instantly hide the minigame UI panel
                CloseMinigame();

                // 2. Trigger completion (which notifies MinigameManager -> triggers reverse camera sweep!)
                CompleteMinigame();
            }
            else
            {
                // Advance to next question
                if (outputDisplay != null) outputDisplay.text = $"<color=green>CORRECT! ({currentSolveCount}/{requiredSolvesToWin})</color>";
                Invoke(nameof(AdvanceToNextQuestion), 0.6f);
            }
        }
        else
        {
            Debug.Log($"[KeypadMinigame] Incorrect code '{currentInput}' (Target: '{target}'). Resetting output...");
            if (outputDisplay != null) outputDisplay.text = "<color=red>ERROR</color>";
            Invoke(nameof(ResetKeypad), 0.5f);
        }
    }

    private void AdvanceToNextQuestion()
    {
        PickNextQuestion();
        ResetKeypad();
    }

    private void FlashButton(Button btn)
    {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            StartCoroutine(ButtonFlashCoroutine(img));
        }
    }

    private IEnumerator ButtonFlashCoroutine(Image img)
    {
        Color originalColor = img.color;
        img.color = pressFlashColor;
        yield return new WaitForSeconds(0.08f);
        img.color = originalColor;
    }

    private void ResetKeypad()
    {
        currentInput = "";
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        AutoFindDisplays();

        if (targetDisplay != null && currentQuestion != null)
        {
            targetDisplay.text = $"{currentQuestion.promptText}  <b>({currentSolveCount}/{requiredSolvesToWin})</b>";
        }

        if (outputDisplay != null)
        {
            outputDisplay.text = currentInput.Length > 0 ? FormatAsTime(currentInput) : "_ _ : _ _";
        }
    }

    private string FormatAsTime(string input)
    {
        if (string.IsNullOrEmpty(input)) return "00:00";
        if (input.Length > 4) return input; // Return raw string for 5+ digit easter egg codes like 80085
        
        string padded = input.PadLeft(4, '0');
        string minutes = padded.Substring(0, 2);
        string seconds = padded.Substring(2, 2);
        return $"{minutes}:{seconds}";
    }
}
