using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CapsuleCollider))]
public class PlayerMovement : MonoBehaviour, IMovable
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

    [Header("Animation")]
    [Tooltip("Optional Animator reference. Auto-found in children if left unassigned.")]
    [SerializeField] private Animator animator;
    [Tooltip("Float parameter name in Animator Blend Tree (0.0 = idle/0%, 1.0 = max speed/100%).")]
    [SerializeField] private string speedParamName = "Speed";

    private float currentSpeed;
    private Key? lastPressedKey;
    private bool movementBlocked;
    private int speedParamHash;

    private void Awake()
    {
        if (GetComponent<Collider>() == null)
            gameObject.AddComponent<CapsuleCollider>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator != null && animator.applyRootMotion)
        {
            animator.applyRootMotion = false;
        }

        speedParamHash = Animator.StringToHash(speedParamName);
    }

    private void Update()
    {
        if (!movementBlocked)
        {
            HandleInput();
            MoveForward();
        }
        ApplyDeceleration();
        UpdateSpeedUI();
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        if (animator != null)
        {
            float normalizedSpeed = maxSpeed > 0f ? Mathf.Clamp01(currentSpeed / maxSpeed) : 0f;
            animator.SetFloat(speedParamHash, normalizedSpeed);
        }
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
            speedScrollbar.size = currentSpeed / maxSpeed;
        }
    }

    public float GetCurrentSpeed() => currentSpeed;
    public float GetMaxSpeed() => maxSpeed;

    public bool IsPlayer => true;
    public Transform MoverTransform => transform;

    public void PushBack(float distance)
    {
        currentSpeed = 0f;
        lastPressedKey = null;
        transform.position -= transform.forward * distance;
    }

    public void ApplyStunAnimation(float duration)
    {
        StartCoroutine(StunAnimationRoutine(duration));
    }

    private System.Collections.IEnumerator StunAnimationRoutine(float duration)
    {
        isStunned = true;
        var anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.SetTrigger("Stun");
        Debug.Log($"[PlayerMovement] Stun animation applied to Char for {duration}s!");
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }

    private bool isStunned;

    public void SetMovementBlocked(bool blocked)
    {
        // Allow unblocking ONLY if current state is GreenLight and player is not stunned
        if (!blocked && (isStunned || (GameSys.Instance != null && GameSys.Instance.CurrentState != GameState.GreenLight)))
        {
            movementBlocked = true;
            return;
        }

        movementBlocked = blocked;
        if (blocked)
        {
            currentSpeed = 0f;
            lastPressedKey = null;
        }
    }
}
