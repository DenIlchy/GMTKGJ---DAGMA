using UnityEngine;

public class StartGate
{
    private readonly GameObject panel;

    public StartGate(GameObject panel)
    {
        this.panel = panel;
    }

    public bool IsWaiting => panel != null && panel.activeInHierarchy;

    public void LogConfigurationWarnings()
    {
        if (panel == null)
            Debug.LogWarning("[GameSys] Start Panel is not assigned in the Inspector — the game will start immediately without waiting for it.");
        else if (!panel.activeInHierarchy)
            Debug.LogWarning("[GameSys] Start Panel is assigned but already inactive at scene start — the game will start immediately. Make sure the panel is active in the scene.");
    }

    public void Dismiss()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}
