using System.Collections;
using UnityEngine;

public class OpponentMovement : MonoBehaviour, IMovable
{
    [Header("Momentum")]
    [SerializeField] private float accelerationPerStep = 3f;
    [SerializeField] private float maxSpeed = 15f;
    [SerializeField] private float deceleration = 4f;

    [Header("Step Rhythm")]
    [SerializeField] private Vector2 stepIntervalRange = new Vector2(0.2f, 0.5f);

    [Header("Warning Risk")]
    [SerializeField] [Range(0f, 1f)] private float riskyWarningStepChance = 0.25f;
    [SerializeField] private Vector2 warningReactionDelayRange = new Vector2(0.1f, 0.6f);

    private float currentSpeed;
    private bool movementBlocked;
    private bool canStep;
    private bool nextStepIsLeft;
    private float nextStepTime;
    private Coroutine warningRiskCoroutine;
    private GameSys gameSys;

    public bool IsPlayer => false;
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
                if (!movementBlocked && Random.value <= riskyWarningStepChance)
                    warningRiskCoroutine = StartCoroutine(TakeRiskyWarningStep());
                break;
            default:
                canStep = false;
                break;
        }
    }

    private IEnumerator TakeRiskyWarningStep()
    {
        yield return new WaitForSeconds(GetRandomRangeValue(warningReactionDelayRange));

        if (!movementBlocked && gameSys != null && gameSys.CurrentState == GameState.RedLightWarning)
            TakeStep();

        warningRiskCoroutine = null;
    }

    private void TakeStep()
    {
        nextStepIsLeft = !nextStepIsLeft;
        currentSpeed = Mathf.Clamp(currentSpeed + accelerationPerStep, 0f, maxSpeed);

        if (canStep)
            ScheduleNextStep();
    }

    private void ScheduleNextStep()
    {
        nextStepTime = Time.time + GetRandomRangeValue(stepIntervalRange);
    }

    private void MoveForward()
    {
        transform.Translate(Vector3.forward * currentSpeed * Time.deltaTime);
    }

    private void ApplyDeceleration()
    {
        currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, deceleration * Time.deltaTime);
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
