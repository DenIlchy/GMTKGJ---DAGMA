using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GamePhaseAudio : MonoBehaviour
{
    public static GamePhaseAudio Instance { get; private set; }

    [Header("Music")]
    [Tooltip("AudioSource used for background music. Created at runtime if left unassigned and no AudioSource exists on this GameObject.")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip musicClip;
    [SerializeField] private bool playMusicOnStart = true;

    [Header("Phase SFX")]
    [Tooltip("AudioSource used for green/red phase sounds. Created at runtime if left unassigned and no AudioSource exists on this GameObject.")]
    [SerializeField] private AudioSource sfxSource;
    [Tooltip("One-shot sound played when Green Light starts.")]
    [SerializeField] private AudioClip greenLightStartClip;
    [Tooltip("Looping sound played during the entire Green Light duration. Stops when Red Light Warning begins.")]
    [SerializeField] private AudioClip greenLightLoopClip;
    [Tooltip("One-shot sound played when Red Light Warning starts.")]
    [SerializeField] private AudioClip redLightWarningClip;

    [Header("Optional Volume Sliders")]
    [Tooltip("Optional UI Slider to control background music volume.")]
    [SerializeField] private Slider musicVolumeSlider;
    [Tooltip("Optional UI Slider to control phase SFX volume.")]
    [SerializeField] private Slider sfxVolumeSlider;

    private GameSys gameSys;
    private bool greenLoopIsPlaying;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        EnsureAudioSource(ref musicSource);
        EnsureAudioSource(ref sfxSource);

        if (musicSource != null && musicClip != null && playMusicOnStart)
        {
            musicSource.clip = musicClip;
            musicSource.loop = true;
            musicSource.Play();
        }

        BindVolumeSlider(musicVolumeSlider, musicSource);
        BindVolumeSlider(sfxVolumeSlider, sfxSource);

        SubscribeToGameSys();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        UnsubscribeFromGameSys();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SubscribeToGameSys();
    }

    private void SubscribeToGameSys()
    {
        UnsubscribeFromGameSys();

        gameSys = GameSys.Instance;
        if (gameSys == null)
        {
            Debug.LogWarning("GamePhaseAudio: no GameSys instance found in the scene.");
            enabled = false;
            return;
        }

        enabled = true;
        gameSys.OnGreenLightStarted += HandleGreenLightStarted;
        gameSys.OnRedLightWarningStarted += HandleRedLightWarningStarted;
        gameSys.OnStateChanged += HandleStateChanged;
        HandleStateChanged(gameSys.CurrentState);
    }

    private void UnsubscribeFromGameSys()
    {
        if (gameSys == null)
            return;

        gameSys.OnGreenLightStarted -= HandleGreenLightStarted;
        gameSys.OnRedLightWarningStarted -= HandleRedLightWarningStarted;
        gameSys.OnStateChanged -= HandleStateChanged;
        gameSys = null;
    }

    private void EnsureAudioSource(ref AudioSource source)
    {
        if (source != null)
            return;

        source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();
    }

    private void BindVolumeSlider(Slider slider, AudioSource source)
    {
        if (slider == null || source == null)
            return;

        slider.SetValueWithoutNotify(source.volume);
        slider.onValueChanged.AddListener(value => source.volume = value);
    }

    private void HandleGreenLightStarted(float duration)
    {
        if (greenLightLoopClip != null && sfxSource != null)
        {
            sfxSource.clip = greenLightLoopClip;
            sfxSource.loop = true;
            sfxSource.Play();
            greenLoopIsPlaying = true;
        }

        if (greenLightStartClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(greenLightStartClip);
        }
    }

    private void HandleRedLightWarningStarted(float duration)
    {
        StopGreenLoop();

        if (redLightWarningClip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(redLightWarningClip);
        }
    }

    private void HandleStateChanged(GameState state)
    {
        if (state != GameState.GreenLight)
            StopGreenLoop();
    }

    private void StopGreenLoop()
    {
        if (!greenLoopIsPlaying)
            return;

        if (sfxSource != null && sfxSource.clip == greenLightLoopClip)
            sfxSource.Stop();

        greenLoopIsPlaying = false;
    }
}
