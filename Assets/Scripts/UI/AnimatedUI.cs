using UnityEngine;
using UnityEngine.UI;

public class AnimatedUI : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float framesPerSecond = 12f;

    private int currentFrame;
    private float timer;

    void Update()
    {
        if (frames == null || frames.Length == 0 || image == null) return;

        timer += Time.deltaTime;
        if (timer >= 1f / framesPerSecond)
        {
            timer = 0f;
            currentFrame = (currentFrame + 1) % frames.Length;
            image.sprite = frames[currentFrame];
        }
    }
}