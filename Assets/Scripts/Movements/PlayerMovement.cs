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
    [SerializeField] private float accelerationPerTap = 0.5f;
    [Tooltip("Maximum movement speed.")]
    [SerializeField] private float maxSpeed = 8f;
    [Tooltip("How fast speed decays NATURALLY while the player is actively tapping.")]
    [SerializeField] private float deceleration = 3f;
    [Tooltip("Reduce speed by this amount when the same key is pressed twice.")]
    [SerializeField] private float wrongTapPenalty = 0.5f;

    [Header("Braking (Stopping)")]
    [Tooltip("How fast speed decays when the player intentionally stops tapping.")]
    [SerializeField] private float brakingDeceleration = 15f;
    [Tooltip("Time (in seconds) without input before aggressive braking kicks in.")]
    [SerializeField] private float brakingDelay = 0.15f;

    [Header("UI")]
    [Tooltip("Drag the Scrollbar here to visualize current speed.")]
    [SerializeField] private Scrollbar speedScrollbar;

    [Header("Animation")]
    [Tooltip("Optional Animator reference. Auto-found in children if left unassigned.")]
    [SerializeField] private Animator animator;
    [Tooltip("Float parameter name in Animator Blend Tree (0.0 = idle/0%, 1.0 = max speed/100%).")]
    [SerializeField] private string speedParamName = "Speed";

    [Header("Footsteps")]
    [Tooltip("AudioSource used for player footsteps. Created at runtime if left unassigned and no AudioSource exists on this GameObject.")]
    [SerializeField] private AudioSource footstepSource;
    [Tooltip("Footstep clip played on each alternating step while the character is moving in the Blend Tree.")]
    [SerializeField] private AudioClip footstepClip;
    [Tooltip("Optional UI Slider to control footstep volume.")]
    [SerializeField] private Slider footstepVolumeSlider;
    [SerializeField] private float footstepPitchVariation = 0.05f;
    [SerializeField] private float minSpeedForFootstep = 0.01f;

    [Header("Stun & Reaction VFX")]
    [Tooltip("Optional child GameObject above player head enabled during stun (e.g. stars4 visual model).")]
    [SerializeField] private GameObject stunVFXObject;

    private float currentSpeed;
    private Key? lastPressedKey;
    private bool movementBlocked;
    private int speedParamHash;
    private float lastTapTime;

    private void Awake()
    {
        isStunned = false;
        movementBlocked = true;
        currentSpeed = 0f;

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

    private void Start()
    {
        EnsureFootstepAudioSource();
        BindFootstepVolumeSlider();

        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.OnMinigameClosed += HandleMinigameClosed;
        }
    }

    private void OnDestroy()
    {
        if (MinigameManager.Instance != null)
        {
            MinigameManager.Instance.OnMinigameClosed -= HandleMinigameClosed;
        }
    }

    private void HandleMinigameClosed()
    {
        // Re-evaluate movement unblock when minigame UI closes
        if (!isStunned && GameSys.Instance != null && GameSys.Instance.CurrentState == GameState.GreenLight)
        {
            SetMovementBlocked(false);
        }
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

        // Register ANY tap activity to reset the braking timer
        if (leftPressed || rightPressed)
        {
            lastTapTime = Time.time;
        }

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
        PlayFootstep();
    }

    private void EnsureFootstepAudioSource()
    {
        if (footstepSource != null)
            return;

        footstepSource = GetComponent<AudioSource>();
        if (footstepSource == null)
            footstepSource = gameObject.AddComponent<AudioSource>();
    }

    private void BindFootstepVolumeSlider()
    {
        if (footstepVolumeSlider == null || footstepSource == null)
            return;

        footstepVolumeSlider.SetValueWithoutNotify(footstepSource.volume);
        footstepVolumeSlider.onValueChanged.AddListener(value => footstepSource.volume = value);
    }

    private void PlayFootstep()
    {
        if (footstepSource == null || footstepClip == null)
            return;
        if (currentSpeed < minSpeedForFootstep)
            return;

        footstepSource.pitch = Random.Range(1f - footstepPitchVariation, 1f + footstepPitchVariation);
        footstepSource.PlayOneShot(footstepClip);
    }

    private void MoveForward()
    {
        // Don't move if speed is basically 0 to prevent micro-jitter
        if (currentSpeed > 0.01f)
        {
            transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
        }
    }

    private void ApplyDeceleration()
    {
        // If the player hasn't tapped anything recently, apply the hard brake.
        // Otherwise, apply the normal running deceleration.
        float activeDeceleration = (Time.time - lastTapTime > brakingDelay) ? brakingDeceleration : deceleration;

        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, activeDeceleration * Time.deltaTime);
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

    public void PlayGetShotAnimation()
    {
        var anim = animator != null ? animator : GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("mshoot");
            anim.SetTrigger("GetShot");
        }
    }

    public void PushBack(float distance)
    {
        currentSpeed = 0f;
        lastPressedKey = null;
        transform.position -= transform.forward * distance;
        var anim = animator != null ? animator : GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.SetTrigger("mstars");
            anim.SetTrigger("Pushback");
        }
    }

    public void ApplyStunAnimation(float duration)
    {
        StartCoroutine(StunAnimationRoutine(duration));
    }

    private System.Collections.IEnumerator StunAnimationRoutine(float duration)
    {
        isStunned = true;
        var anim = animator != null ? animator : GetComponentInChildren<Animator>();
        if (anim != null) anim.SetTrigger("Stun");

        if (stunVFXObject != null)
        {
            stunVFXObject.SetActive(true);
        }

        Debug.Log($"[PlayerMovement] Stun animation applied to Char for {duration}s!");
        yield return new WaitForSeconds(duration);

        if (stunVFXObject != null)
        {
            stunVFXObject.SetActive(false);
        }

        isStunned = false;
    }

    private bool isStunned;

    public void SetMovementBlocked(bool blocked)
    {
        bool isMinigameActive = MinigameManager.Instance != null && MinigameManager.Instance.IsMinigameActive;

        // Allow unblocking ONLY if current state is GreenLight, player is not stunned, and no minigame is active
        if (!blocked && (isStunned || isMinigameActive || (GameSys.Instance != null && GameSys.Instance.CurrentState != GameState.GreenLight)))
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