using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    [Header("Text References")]
    [Tooltip("Countdown for the current phase (green light / warning).")]
    [SerializeField] private TMP_Text phaseTimerText;
    [Tooltip("Current state label (Green Light! / Red Light! / etc.).")]
    [SerializeField] private TMP_Text stateText;
    [Tooltip("Feedback label shown when the player moved during Red Light.")]
    [SerializeField] private TMP_Text youMovedText;
    [Tooltip("Result label shown on Victory or Game Over.")]
    [SerializeField] private TMP_Text resultText;
    [Tooltip("Panel shown on Victory or Game Over.")]
    [SerializeField] private GameObject resultPanel;
    [Tooltip("Panel shown when the game is paused.")]
    [SerializeField] private GameObject pausePanel;

    [Header("Colors")]
    [SerializeField] private Color greenColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color redColor = Color.red;

    private GameSys gameSys;
    private bool isPaused;
    private bool resultShown;

    private void Start()
    {
        gameSys = GameSys.Instance;
        if (gameSys == null)
        {
            Debug.LogWarning("GameUI: no GameSys instance found in the scene.");
            enabled = false;
            return;
        }

        gameSys.OnPhaseTimerUpdated += HandlePhaseTimerUpdated;
        gameSys.OnPenaltyFeedbackStarted += HandlePenaltyFeedbackStarted;
        gameSys.OnPenaltyApplied += HandlePenaltyApplied;

        if (youMovedText != null)
            youMovedText.gameObject.SetActive(false);

        if (resultText != null)
            resultText.gameObject.SetActive(false);

        if (resultPanel != null)
            resultPanel.SetActive(false);

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;

        if (gameSys == null)
            return;

        gameSys.OnPhaseTimerUpdated -= HandlePhaseTimerUpdated;
        gameSys.OnPenaltyFeedbackStarted -= HandlePenaltyFeedbackStarted;
        gameSys.OnPenaltyApplied -= HandlePenaltyApplied;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            TogglePause();

        if (gameSys == null)
            return;

        GameState state = gameSys.CurrentState;
        UpdateStateText(state);
        ShowResultIfNeeded(state);
    }

    private void UpdateStateText(GameState state)
    {
        if (phaseTimerText != null)
            phaseTimerText.gameObject.SetActive(state == GameState.Intro || state == GameState.GreenLight || state == GameState.RedLightWarning || state == GameState.RedLight);

        if (stateText == null)
            return;

        switch (state)
        {
            case GameState.Intro:
                stateText.text = "Get Ready...";
                stateText.color = Color.white;
                break;
            case GameState.GreenLight:
                stateText.text = "Green Light!";
                stateText.color = greenColor;
                break;
            case GameState.RedLightWarning:
                stateText.text = "Red Light Soon!";
                stateText.color = warningColor;
                break;
            case GameState.RedLight:
                stateText.text = "Red Light!";
                stateText.color = redColor;
                break;
            case GameState.Victory:
                stateText.text = "Victory!";
                stateText.color = greenColor;
                break;
            case GameState.GameOver:
                stateText.text = "Game Over";
                stateText.color = redColor;
                break;
        }
    }

    private void ShowResultIfNeeded(GameState state)
    {
        if (resultShown || resultPanel == null)
            return;

        if (state == GameState.Victory)
        {
            ShowResult("You Win!", greenColor);
            resultShown = true;
        }
        else if (state == GameState.GameOver)
        {
            ShowResult("You Lose!", redColor);
            resultShown = true;
        }
    }

    private void HandlePhaseTimerUpdated(float remaining)
    {
        if (phaseTimerText != null)
            phaseTimerText.text = Mathf.CeilToInt(remaining).ToString();
    }

    private void HandlePenaltyFeedbackStarted(List<IMovable> violators)
    {
        bool playerViolated = violators.Exists(v => v.IsPlayer);
        if (youMovedText != null && playerViolated)
        {
            youMovedText.text = "You moved!";
            youMovedText.gameObject.SetActive(true);
        }
    }

    private void HandlePenaltyApplied(List<IMovable> violators)
    {
        if (youMovedText != null)
            youMovedText.gameObject.SetActive(false);
    }

    private void ShowResult(string message, Color color)
    {
        if (resultText != null)
        {
            resultText.text = message;
            resultText.color = color;
            resultText.gameObject.SetActive(true);
        }

        if (resultPanel != null)
            resultPanel.SetActive(true);
    }

    public void TogglePause()
    {
        GameState currentState = gameSys.CurrentState;
        if (currentState == GameState.Victory || currentState == GameState.GameOver)
            return;

        if (isPaused)
            Continue();
        else
            Pause();
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);
    }

    public void Continue()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
