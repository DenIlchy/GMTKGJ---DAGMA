using UnityEngine;

[CreateAssetMenu(menuName = "DAGMA/UIThemeConfig")]
public class UIThemeConfig : ScriptableObject
{
    [SerializeField] private Color greenColor = new Color(0f, 1f, 0f, 1f);
    [SerializeField] private Color warningColor = new Color(1f, 0.92156863f, 0.015686275f, 1f);
    [SerializeField] private Color redColor = new Color(1f, 0f, 0f, 1f);
    [SerializeField] private string introLabel = "Get Ready...";
    [SerializeField] private string greenLightLabel = "Green Light!";
    [SerializeField] private string warningLabel = "Red Light Soon!";
    [SerializeField] private string redLightLabel = "Red Light!";
    [SerializeField] private string victoryLabel = "Victory!";
    [SerializeField] private string gameOverLabel = "Game Over";
    [SerializeField] private string winResultMessage = "You Win!";
    [SerializeField] private string loseResultMessage = "You Lose!";
    [SerializeField] private string graceExpiredMessage = "Better luck next time!";

    public Color GreenColor => greenColor;
    public Color WarningColor => warningColor;
    public Color RedColor => redColor;
    public string IntroLabel => introLabel;
    public string GreenLightLabel => greenLightLabel;
    public string WarningLabel => warningLabel;
    public string RedLightLabel => redLightLabel;
    public string VictoryLabel => victoryLabel;
    public string GameOverLabel => gameOverLabel;
    public string WinResultMessage => winResultMessage;
    public string LoseResultMessage => loseResultMessage;
    public string GraceExpiredMessage => graceExpiredMessage;
}
