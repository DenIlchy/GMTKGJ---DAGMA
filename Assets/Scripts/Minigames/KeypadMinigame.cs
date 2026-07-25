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
    [SerializeField]
    private List<MicrowaveQuestion> questionPool = new List<MicrowaveQuestion>()
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

    [Header("Audio Settings")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip buttonClickClip;
    [SerializeField] private AudioClip correctClip;
    [SerializeField] private AudioClip errorClip;
    [SerializeField] private AudioClip winClip;

    private string currentInput = "";
    private MicrowaveQuestion currentQuestion;
    private string lastAskedPrompt = "";
    private List<MicrowaveQuestion> availableQuestionDeck = new List<MicrowaveQuestion>();

    private int currentSolveCount = 0;
    private bool isWired = false;
    private bool isWinning = false; // Prevents clicks during the final win delay
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

        isWinning = false;
        currentSolveCount = 0;
        lastAskedPrompt = "";
        RefillQuestionDeck();
        PickNextQuestion();
        ResetKeypad();
    }

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

        if (availableQuestionDeck.Count > 1 && availableQuestionDeck[0].promptText == lastAskedPrompt)
        {
            var first = availableQuestionDeck[0];
            availableQuestionDeck[0] = availableQuestionDeck[availableQuestionDeck.Count - 1];
            availableQuestionDeck[availableQuestionDeck.Count - 1] = first;
        }
    }

    private void PickNextQuestion()
    {
        if (availableQuestionDeck.Count == 0)
        {
            RefillQuestionDeck();
        }

        currentQuestion = availableQuestionDeck[0];
        availableQuestionDeck.RemoveAt(0);
        lastAskedPrompt = currentQuestion.promptText;
    }

    public void PressDigit(int digit)
    {
        if (IsCompleted || IsDebounced() || isWinning) return;

        PlaySFX(buttonClickClip);

        if (digitButtons != null && digit >= 0 && digit < digitButtons.Length && digitButtons[digit] != null)
        {
            FlashButton(digitButtons[digit]);
        }

        if (currentInput.Length < maxDigits)
        {
            currentInput += digit.ToString();
            UpdateDisplay();
        }
    }

    public void PressClear()
    {
        if (IsCompleted || IsDebounced() || isWinning) return;

        PlaySFX(buttonClickClip);

        if (clearButton != null) FlashButton(clearButton);

        currentInput = "";
        UpdateDisplay();
    }

    public void PressTime()
    {
        if (IsCompleted || IsDebounced() || isWinning) return;

        PlaySFX(buttonClickClip);

        if (timeButton != null) FlashButton(timeButton);

        if (currentQuestion == null) return;

        string target = currentQuestion.targetCode;

        bool isCorrect = (currentInput == target) ||
                         (currentInput.TrimStart('0') == target.TrimStart('0')) ||
                         (FormatAsTime(currentInput) == FormatAsTime(target));

        if (isCorrect)
        {
            currentSolveCount++;

            if (currentSolveCount >= requiredSolvesToWin)
            {
                if (outputDisplay != null) outputDisplay.text = "<color=green>COOKING!</color>";

                PlaySFX(winClip);
                isWinning = true;

                // Add a small delay so the sound plays before the UI disappears
                Invoke(nameof(TriggerWin), 0.75f);
            }
            else
            {
                if (outputDisplay != null) outputDisplay.text = $"<color=green>CORRECT! ({currentSolveCount}/{requiredSolvesToWin})</color>";

                PlaySFX(correctClip);
                Invoke(nameof(AdvanceToNextQuestion), 0.6f);
            }
        }
        else
        {
            if (outputDisplay != null) outputDisplay.text = "<color=red>ERROR</color>";

            PlaySFX(errorClip);
            Invoke(nameof(ResetKeypad), 0.5f);
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    private void TriggerWin()
    {
        CloseMinigame();
        CompleteMinigame();
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
        if (input.Length > 4) return input;

        string padded = input.PadLeft(4, '0');
        string minutes = padded.Substring(0, 2);
        string seconds = padded.Substring(2, 2);
        return $"{minutes}:{seconds}";
    }
}