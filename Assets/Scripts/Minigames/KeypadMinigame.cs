using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeypadMinigame : Minigame
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI statusDisplay;
    [SerializeField] private Button button1;
    [SerializeField] private Button button2;
    [SerializeField] private Button button3;

    private int nextExpectedNumber = 1;

    private void Start()
    {
        if (button1 != null) button1.onClick.AddListener(() => PressKey(1));
        if (button2 != null) button2.onClick.AddListener(() => PressKey(2));
        if (button3 != null) button3.onClick.AddListener(() => PressKey(3));
    }

    public override void StartMinigame()
    {
        base.StartMinigame();
        ResetKeypad();
    }

    public void PressKey(int number)
    {
        if (!IsActive || IsCompleted) return;

        if (number == nextExpectedNumber)
        {
            nextExpectedNumber++;
            UpdateStatusDisplay();

            if (nextExpectedNumber > 3)
            {
                if (statusDisplay != null) statusDisplay.text = "<color=green>CLEARED!</color>";
                Invoke(nameof(TriggerCompletion), 0.3f);
            }
        }
        else
        {
            // Wrong key pressed - reset
            Debug.Log("[KeypadMinigame] Wrong sequence! Resetting...");
            ResetKeypad();
            if (statusDisplay != null) statusDisplay.text = "<color=red>WRONG! RESET</color>";
        }
    }

    private void TriggerCompletion()
    {
        CompleteMinigame();
    }

    private void ResetKeypad()
    {
        nextExpectedNumber = 1;
        UpdateStatusDisplay();
    }

    private void UpdateStatusDisplay()
    {
        if (statusDisplay == null) return;

        switch (nextExpectedNumber)
        {
            case 1:
                statusDisplay.text = "PRESS: <b>1 - 2 - 3</b>";
                break;
            case 2:
                statusDisplay.text = "PRESS: <b><color=yellow>[1]</color> - 2 - 3</b>";
                break;
            case 3:
                statusDisplay.text = "PRESS: <b><color=yellow>[1] - [2]</color> - 3</b>";
                break;
        }
    }
}
