using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class Kernel : MonoBehaviour, IPointerDownHandler
{
    public event Action<Kernel> OnKernelPopped;

    [Header("Visuals & Hitbox")]
    [SerializeField] private Image kernelVisual;
    [SerializeField] private Sprite unpoppedSprite;

    [Tooltip("Add all your popped popcorn variations here. One will be chosen at random.")]
    [SerializeField] private Sprite[] poppedSprites;

    [SerializeField] private Sprite burntSprite;

    [Header("Juice Settings")]
    [SerializeField] private float popScaleMultiplier = 1.0f;
    [SerializeField] private float fadeDelay = 0.5f;

    public bool IsBurnt { get; private set; }

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private PopcornMinigame.MovementMode currentMode;
    private RectTransform bounds;

    private Vector2 velocity;
    private float rotationSpeed;
    private float gravity = 1500f;
    private bool hasPopped = false;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (kernelVisual != null) kernelVisual.preserveAspect = true;
    }

    public void Setup(PopcornMinigame.MovementMode mode, RectTransform spawnBounds, bool isBurnt)
    {
        currentMode = mode;
        bounds = spawnBounds;
        IsBurnt = isBurnt;
        hasPopped = false;

        canvasGroup.alpha = 1f;
        kernelVisual.transform.localScale = Vector3.one;
        kernelVisual.sprite = IsBurnt ? burntSprite : unpoppedSprite;

        kernelVisual.SetNativeSize();

        Launch();
    }

    private void Launch()
    {
        float randomX = UnityEngine.Random.Range(-500f, 500f);
        float randomY = UnityEngine.Random.Range(800f, 1200f);
        velocity = new Vector2(randomX, randomY);

        rotationSpeed = UnityEngine.Random.Range(-200f, 200f);
    }

    private void Update()
    {
        kernelVisual.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

        Vector2 pos = rectTransform.anchoredPosition;

        if (currentMode == PopcornMinigame.MovementMode.Falling)
        {
            velocity.y -= gravity * Time.deltaTime;
            pos += velocity * Time.deltaTime;

            if (pos.y < bounds.rect.yMin && !hasPopped)
            {
                pos.y = bounds.rect.yMin;
                Launch();
            }

            if (pos.x < bounds.rect.xMin || pos.x > bounds.rect.xMax)
            {
                velocity.x *= -1;
                pos.x = Mathf.Clamp(pos.x, bounds.rect.xMin, bounds.rect.xMax);
            }
        }
        else if (currentMode == PopcornMinigame.MovementMode.Bouncing)
        {
            if (hasPopped)
            {
                velocity.y -= gravity * Time.deltaTime;
            }

            pos += velocity * Time.deltaTime;

            if (pos.x <= bounds.rect.xMin || pos.x >= bounds.rect.xMax)
            {
                velocity.x *= -1;
                pos.x = Mathf.Clamp(pos.x, bounds.rect.xMin, bounds.rect.xMax);
            }

            if (!hasPopped && (pos.y <= bounds.rect.yMin || pos.y >= bounds.rect.yMax))
            {
                velocity.y *= -1;
                pos.y = Mathf.Clamp(pos.y, bounds.rect.yMin, bounds.rect.yMax);
            }
        }

        rectTransform.anchoredPosition = pos;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (hasPopped) return;
        hasPopped = true;

        StartCoroutine(PopRoutine());
    }

    private IEnumerator PopRoutine()
    {
        if (IsBurnt)
        {
            velocity = new Vector2(0f, -600f);
            rotationSpeed = UnityEngine.Random.Range(500f, 1000f) * (UnityEngine.Random.value > 0.5f ? 1f : -1f);
        }
        else
        {
            velocity = new Vector2(UnityEngine.Random.Range(-300f, 300f), 1000f);
            rotationSpeed = UnityEngine.Random.Range(400f, 800f) * (UnityEngine.Random.value > 0.5f ? 1f : -1f);

            // Randomly select one of the popped sprites
            if (poppedSprites != null && poppedSprites.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, poppedSprites.Length);
                kernelVisual.sprite = poppedSprites[randomIndex];
            }

            kernelVisual.SetNativeSize();
            kernelVisual.transform.localScale = Vector3.one * popScaleMultiplier;
        }

        OnKernelPopped?.Invoke(this);

        yield return new WaitForSeconds(fadeDelay);

        float fadeTimer = 0f;
        while (fadeTimer < 0.2f)
        {
            fadeTimer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, fadeTimer / 0.2f);
            yield return null;
        }

        gameObject.SetActive(false);
    }
}