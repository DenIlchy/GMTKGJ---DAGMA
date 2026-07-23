using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Image))]
public class DirtSplat : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public event Action<DirtSplat> OnCleaned;

    [Header("Scrubbing Settings")]
    [SerializeField] private float requiredScrubDistance = 800f;

    // We expose this so the manager can check if this splat is making noise
    public bool IsActivelyScrubbing { get; private set; }

    private Image dirtImage;
    private float currentScrubAmount = 0f;
    private bool isHovered = false;
    private Vector2 lastMousePosition;

    // A small buffer to stop the sound from stuttering when changing mouse directions
    private float lastScrubTime = 0f;
    private float scrubGracePeriod = 0.1f;

    private void Awake()
    {
        dirtImage = GetComponent<Image>();
    }

    public void ResetSplat()
    {
        currentScrubAmount = 0f;
        SetAlpha(1f);
        isHovered = false;
        IsActivelyScrubbing = false;
        gameObject.SetActive(true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (Mouse.current != null)
        {
            lastMousePosition = Mouse.current.position.ReadValue();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        IsActivelyScrubbing = false;
    }

    private void Update()
    {
        // Smooth out the scrubbing status
        if (Time.time - lastScrubTime > scrubGracePeriod)
        {
            IsActivelyScrubbing = false;
        }

        if (!isHovered || Mouse.current == null) return;

        if (Mouse.current.leftButton.isPressed)
        {
            Vector2 currentMousePosition = Mouse.current.position.ReadValue();
            float distanceMoved = Vector2.Distance(currentMousePosition, lastMousePosition);

            if (distanceMoved > 0f)
            {
                currentScrubAmount += distanceMoved;
                lastScrubTime = Time.time;
                IsActivelyScrubbing = true; // Player is actively moving the mouse

                float remainingAlpha = 1f - (currentScrubAmount / requiredScrubDistance);
                SetAlpha(remainingAlpha);

                if (remainingAlpha <= 0.01f)
                {
                    isHovered = false;
                    IsActivelyScrubbing = false;
                    gameObject.SetActive(false);
                    OnCleaned?.Invoke(this);
                }
            }

            lastMousePosition = currentMousePosition;
        }
        else
        {
            lastMousePosition = Mouse.current.position.ReadValue();
            IsActivelyScrubbing = false;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (dirtImage != null)
        {
            Color c = dirtImage.color;
            c.a = Mathf.Clamp01(alpha);
            dirtImage.color = c;
        }
    }
}