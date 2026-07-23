using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [Header("Input Keys")]
    [SerializeField] private Key leftKey = Key.Q;
    [SerializeField] private Key rightKey = Key.E;

    [Header("Momentum")]
    [Tooltip("Speed added per successful alternating tap.")]
    [SerializeField] private float accelerationPerTap = 3f;
    [Tooltip("Maximum movement speed.")]
    [SerializeField] private float maxSpeed = 15f;
    [Tooltip("How fast speed decays when not tapping.")]
    [SerializeField] private float deceleration = 4f;
    [Tooltip("Reduce speed by this amount when the same key is pressed twice.")]
    [SerializeField] private float wrongTapPenalty = 1f;

    [Header("UI")]
    [Tooltip("Drag the Scrollbar here to visualize current speed.")]
    [SerializeField] private Scrollbar speedScrollbar;

    private float currentSpeed;
    private Key? lastPressedKey;

    private void Update()
    {
        HandleInput();
        MoveForward();
        ApplyDeceleration();
        UpdateSpeedUI();
    }

    private void HandleInput()
    {
        if (Keyboard.current == null)
            return;

        bool leftPressed = Keyboard.current[leftKey].wasPressedThisFrame;
        bool rightPressed = Keyboard.current[rightKey].wasPressedThisFrame;

        if (leftPressed && rightPressed)
            return;

        if (leftPressed)
        {
            if (lastPressedKey == leftKey)
            {
                currentSpeed = Mathf.Max(0f, currentSpeed - wrongTapPenalty);
            }
            else
            {
                lastPressedKey = leftKey;
                AddMomentum();
            }
        }
        else if (rightPressed)
        {
            if (lastPressedKey == rightKey)
            {
                currentSpeed = Mathf.Max(0f, currentSpeed - wrongTapPenalty);
            }
            else
            {
                lastPressedKey = rightKey;
                AddMomentum();
            }
        }
    }

    private void AddMomentum()
    {
        currentSpeed += accelerationPerTap;
        currentSpeed = Mathf.Clamp(currentSpeed, 0f, maxSpeed);
    }

    private void MoveForward()
    {
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }

    private void ApplyDeceleration()
    {
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
    }

    private void UpdateSpeedUI()
    {
        if (speedScrollbar != null)
        {
            speedScrollbar.value = currentSpeed / maxSpeed;
        }
    }

    public float GetCurrentSpeed() => currentSpeed;
    public float GetMaxSpeed() => maxSpeed;
}
