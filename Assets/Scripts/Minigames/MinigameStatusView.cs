using TMPro;
using UnityEngine;

public class MinigameStatusView
{
    private readonly TextMeshProUGUI text;

    public MinigameStatusView(TextMeshProUGUI text)
    {
        this.text = text;
    }

    public void Show(string message)
    {
        if (text != null)
        {
            text.text = message;
            text.gameObject.SetActive(true);
        }
    }

    public void Hide()
    {
        if (text != null)
        {
            text.gameObject.SetActive(false);
        }
    }
}
