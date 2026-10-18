using System;

public class MovementGate
{
    private readonly MoverRegistry registry;
    private readonly Func<GameState> currentState;
    private readonly Func<bool> minigameActive;

    public bool PhaseBlocked { get; private set; } = true;

    public MovementGate(MoverRegistry registry, Func<GameState> currentState, Func<bool> minigameActive)
    {
        this.registry = registry;
        this.currentState = currentState;
        this.minigameActive = minigameActive;
    }

    public void SetPhaseBlocked(bool blocked)
    {
        PhaseBlocked = blocked;
        Reevaluate();
    }

    public void Reevaluate()
    {
        foreach (var mover in registry.Movers)
        {
            Apply(mover);
        }
    }

    public void Apply(IMovable mover)
    {
        if (mover == null)
            return;

        mover.SetMovementBlocked(!CanMove(mover));
    }

    public bool CanMove(IMovable mover)
    {
        if (mover == null)
            return false;

        bool isMinigameActive = minigameActive != null && minigameActive();
        bool isGreenLight = currentState != null && currentState() == GameState.GreenLight;

        return !PhaseBlocked
            && isGreenLight
            && !mover.IsStunned
            && !(mover.IsPlayer && isMinigameActive);
    }
}
