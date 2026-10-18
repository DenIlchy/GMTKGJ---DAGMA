using System.Collections;
using UnityEngine;

public class OpponentMovement : MonoBehaviour, IMovable
{
    [Header("Config (preferred) — overrides all Legacy values below")]
    [SerializeField] private OpponentProfileConfig profile;

    [Header("Momentum (Legacy)")]
    [SerializeField] private float accelerationPerStep = 3f;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float deceleration = 4f;

    [Header("Step Rhythm (Legacy)")]
    [SerializeField] private Vector2 stepIntervalRange = new Vector2(0.2f, 0.5f);

    [Header("Warning Risk (Legacy)")]
    [SerializeField] [Range(0f, 1f)] private float riskyWarningStepChance = 0.25f;
    [SerializeField] private Vector2 warningReactionDelayRange = new Vector2(0.1f, 0.6f);

    private float AccelerationPerStep => profile != null ? profile.AccelerationPerStep : accelerationPerStep;
    private float MaxSpeed => profile != null ? profile.MaxSpeed : maxSpeed;
    private float Deceleration => profile != null ? profile.Deceleration : deceleration;
    private Vector2 StepIntervalRange => profile != null ? profile.StepIntervalRange : stepIntervalRange;
    private float RiskyWarningStepChance => profile != null ? profile.RiskyWarningStepChance : riskyWarningStepChance;
    private Vector2 WarningReactionDelayRange => profile != null ? profile.WarningReactionDelayRange : warningReactionDelayRange;

    private float currentSpeed;
    private bool movementBlocked;
    private bool canStep;
    private bool nextStepIsLeft;
    private float nextStepTime;
    private Coroutine warningRiskCoroutine;
    private GameSys gameSys;

    public bool IsPlayer => false;
    // Opponents do not currently track stun state at all (ApplyStunAnimation only fires the
    // animator trigger and does not block movement), so returning false preserves today's
    // behaviour; when the opponent gains a real stun presenter it will start participating
    // in the gate automatically.
    public bool IsStunned => false;
    public Transform MoverTransform => transform;

    private void Start()
    {
        ConnectToGameSystem();
    }

    private void OnEnable()
    {
        ConnectToGameSystem();
    }

    private void OnDisable()
    {
        CancelWarningRisk();
        canStep = false;

        if (gameSys == null)
            return;

        gameSys.OnStateChanged -= HandleStateChanged;
        gameSys.UnregisterMover(this);
        gameSys = null;
    }

    private void Update()
    {
        if (!movementBlocked && canStep && Time.time >= nextStepTime)
            TakeStep();

        if (!movementBlocked)
            MoveForward();

        ApplyDeceleration();
        UpdateAnimator();
    }

    private void UpdateAnimator()
    {
        var anim = GetComponentInChildren<Animator>();
        if (anim != null)
        {
            float normalizedSpeed = MaxSpeed > 0f ? Mathf.Clamp01(currentSpeed / MaxSpeed) : 0f;
            anim.SetFloat("Speed", normalizedSpeed);
        }
    }

    private void ConnectToGameSystem()
    {
        if (gameSys != null)
            return;

        gameSys = GameSys.Instance;
        if (gameSys == null)
            return;

        gameSys.OnStateChanged += HandleStateChanged;
        gameSys.RegisterMover(this);
        HandleStateChanged(gameSys.CurrentState);
    }

    private void HandleStateChanged(GameState state)
    {
        CancelWarningRisk();

        switch (state)
        {
            case GameState.GreenLight:
                canStep = true;
                ScheduleNextStep();
                break;
            case GameState.RedLightWarning:
                canStep = false;
                if (!movementBlocked && Random.value <= RiskyWarningStepChance)
                    warningRiskCoroutine = StartCoroutine(TakeRiskyWarningStep());
                break;
            default:
                canStep = false;
                break;
        }
    }

    private IEnumerator TakeRiskyWarningStep()
    {
        yield return new WaitForSeconds(GetRandomRangeValue(WarningReactionDelayRange));

        if (!movementBlocked && gameSys != null && gameSys.CurrentState == GameState.RedLightWarning)
            TakeStep();

        warningRiskCoroutine = null;
    }

    private void TakeStep()
    {
        nextStepIsLeft = !nextStepIsLeft;
        currentSpeed = Mathf.Clamp(currentSpeed + AccelerationPerStep, 0f, MaxSpeed);

        if (canStep)
            ScheduleNextStep();
    }

    private void ScheduleNextStep()
    {
        nextStepTime = Time.time + GetRandomRangeValue(StepIntervalRange);
    }

    private void MoveForward()
    {
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }

    private void ApplyDeceleration()
    {
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, Deceleration * Time.deltaTime);
    }

    private void CancelWarningRisk()
    {
        if (warningRiskCoroutine == null)
            return;

        StopCoroutine(warningRiskCoroutine);
        warningRiskCoroutine = null;
    }

    private static float GetRandomRangeValue(Vector2 range)
    {
        return Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));
    }

    public float GetCurrentSpeed() => currentSpeed;

    public void PushBack(float distance)
    {
        currentSpeed = 0f;
        nextStepIsLeft = false;
        CancelWarningRisk();
        transform.position -= transform.forward * distance;
    }

    public void ApplyStunAnimation(float duration)
    {
        var anim = GetComponentInChildren<Animator>();
        if (anim != null) anim.SetTrigger("Stun");
        Debug.Log($"[OpponentMovement] Stun animation applied to opponent for {duration}s!");
    }

    public void SetMovementBlocked(bool blocked)
    {
        movementBlocked = blocked;
        if (!blocked)
            return;

        currentSpeed = 0f;
        canStep = false;
        nextStepIsLeft = false;
        CancelWarningRisk();
    }
}
