using System.Collections.Generic;

public static class ViolationDetector
{
    public static List<IMovable> FindViolators(IReadOnlyList<IMovable> movers, float speedThreshold)
    {
        List<IMovable> violators = new List<IMovable>();
        if (movers == null)
            return violators;

        foreach (var mover in movers)
        {
            if (mover != null && mover.GetCurrentSpeed() > speedThreshold)
                violators.Add(mover);
        }
        return violators;
    }
}
