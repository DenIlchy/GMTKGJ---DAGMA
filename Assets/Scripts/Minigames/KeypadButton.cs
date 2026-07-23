using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class KeypadButton : MonoBehaviour
{
    public enum ButtonType { Digit, Clear, Time }

    [Header("Button Settings")]
    [SerializeField] private ButtonType type = ButtonType.Digit;
    [SerializeField] private int digitValue = 0;
    [SerializeField] private KeypadMinigame targetKeypad;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    public void Setup(ButtonType buttonType, int value, KeypadMinigame keypad)
    {
        type = buttonType;
        digitValue = value;
        targetKeypad = keypad;
    }

    private void OnButtonClicked()
    {
        if (targetKeypad == null)
        {
            targetKeypad = Object.FindFirstObjectByType<KeypadMinigame>(FindObjectsInactive.Include);
        }

        if (targetKeypad != null)
        {
            switch (type)
            {
                case ButtonType.Digit:
                    targetKeypad.PressDigit(digitValue);
                    break;
                case ButtonType.Clear:
                    targetKeypad.PressClear();
                    break;
                case ButtonType.Time:
                    targetKeypad.PressTime();
                    break;
            }
        }
    }
}
