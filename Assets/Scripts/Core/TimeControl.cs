using UnityEngine;

// Static wrapper over the existing global engine state (Time.timeScale) — it guards
// state that is already global rather than introducing new global state.
// GameSys.Awake calls Resume(), which is what resets IsPaused across scene loads.
public static class TimeControl
{
    public static bool IsPaused { get; private set; }

    public static void Resume()
    {
        Time.timeScale = 1f;
        IsPaused = false;
    }

    public static void Pause()
    {
        Time.timeScale = 0f;
        IsPaused = true;
    }

    public static void Freeze()
    {
        Time.timeScale = 0f;
        IsPaused = false;
    }
}
