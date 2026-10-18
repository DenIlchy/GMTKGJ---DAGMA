using System;
using System.Collections.Generic;

public class MoverRegistry
{
    private readonly List<IMovable> movers = new List<IMovable>();

    public IReadOnlyList<IMovable> Movers => movers;

    public event Action<IMovable> OnMoverRegistered;

    public bool Register(IMovable mover)
    {
        if (mover == null || movers.Contains(mover))
            return false;

        movers.Add(mover);
        OnMoverRegistered?.Invoke(mover);
        return true;
    }

    public bool Unregister(IMovable mover)
    {
        return movers.Remove(mover);
    }

    public void Clear()
    {
        movers.Clear();
    }
}
