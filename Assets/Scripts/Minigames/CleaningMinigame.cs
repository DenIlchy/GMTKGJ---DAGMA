using System.Collections.Generic;
using UnityEngine;

public class CleaningMinigame : Minigame
{
    [Header("Spawning Configuration")]
    [SerializeField] private DirtSplat dirtPrefab;
    [SerializeField] private RectTransform dirtSpawnArea;
    [SerializeField] private int minSplats = 3;
    [SerializeField] private int maxSplats = 7;

    [Header("WebGL Custom Cursor")]
    [SerializeField] private Texture2D wipeCursorTexture;
    [SerializeField] private Vector2 cursorHotspot = Vector2.zero;

    [Header("Audio Settings")]
    [Tooltip("The AudioSource that will play the squeaky sound.")]
    [SerializeField] private AudioSource squeakAudioSource;

    private int splatsRemaining;
    private List<DirtSplat> activeSplats = new List<DirtSplat>();

    public override void StartMinigame()
    {
        base.StartMinigame();

        ClearSplats();
        splatsRemaining = Random.Range(minSplats, maxSplats + 1);

        for (int i = 0; i < splatsRemaining; i++)
        {
            SpawnDirt();
        }

        if (wipeCursorTexture != null)
        {
            Cursor.SetCursor(wipeCursorTexture, cursorHotspot, CursorMode.Auto);
        }
    }

    public override void CloseMinigame()
    {
        base.CloseMinigame();

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        ClearSplats();

        if (squeakAudioSource != null && squeakAudioSource.isPlaying)
        {
            squeakAudioSource.Stop();
        }
    }

    private void Update()
    {
        if (!IsActive || squeakAudioSource == null) return;

        // Check if ANY of the active splats are currently being scrubbed
        bool isScrubbingAnything = false;
        foreach (var splat in activeSplats)
        {
            if (splat.IsActivelyScrubbing)
            {
                isScrubbingAnything = true;
                break; // We only need one to be true
            }
        }

        // Play or Pause the sound based on the scrubbing status
        if (isScrubbingAnything && !squeakAudioSource.isPlaying)
        {
            squeakAudioSource.Play();
        }
        else if (!isScrubbingAnything && squeakAudioSource.isPlaying)
        {
            // Pausing is better than Stopping so the squeak sound seamlessly resumes 
            // from where it left off, rather than restarting from the beginning every time.
            squeakAudioSource.Pause();
        }
    }

    private void SpawnDirt()
    {
        DirtSplat newSplat = Instantiate(dirtPrefab, dirtSpawnArea);

        float randomX = Random.Range(dirtSpawnArea.rect.xMin, dirtSpawnArea.rect.xMax);
        float randomY = Random.Range(dirtSpawnArea.rect.yMin, dirtSpawnArea.rect.yMax);

        RectTransform splatRect = newSplat.GetComponent<RectTransform>();
        splatRect.anchoredPosition = new Vector2(randomX, randomY);
        splatRect.localRotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        newSplat.OnCleaned += HandleSplatCleaned;
        activeSplats.Add(newSplat);
    }

    private void ClearSplats()
    {
        foreach (var splat in activeSplats)
        {
            if (splat != null)
            {
                splat.OnCleaned -= HandleSplatCleaned;
                Destroy(splat.gameObject);
            }
        }
        activeSplats.Clear();
    }

    private void HandleSplatCleaned(DirtSplat splat)
    {
        if (!IsActive || IsCompleted) return;

        splatsRemaining--;

        if (splatsRemaining <= 0)
        {
            if (squeakAudioSource != null) squeakAudioSource.Stop();
            CompleteMinigame();
        }
    }
}