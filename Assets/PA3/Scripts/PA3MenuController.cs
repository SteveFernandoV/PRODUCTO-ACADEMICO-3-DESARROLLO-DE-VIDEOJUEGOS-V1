using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class PA3MenuController : MonoBehaviour
{
    public const string MainMenuSceneName = "PA3_MainMenu";
    public const string GameplaySceneName = "PA3_VerticalSlice";
    static string pendingMenuMessage;
    AudioSource menuMusic;
    AudioSource menuEffects;
    AudioClip buttonClip;
    bool changingScene;

    [SerializeField] GameObject mainMenuPanel;
    [SerializeField] GameObject instructionsPanel;
    [SerializeField] Button playButton;
    [SerializeField] Button instructionsButton;
    [SerializeField] Button instructionsBackButton;
    [SerializeField] Button quitButton;
    [SerializeField] Text feedbackText;

    public void ConfigureMainMenu(GameObject main, GameObject instructions, Button play, Button showInstructions, Button back, Button quit, Text feedback)
    {
        mainMenuPanel = main;
        instructionsPanel = instructions;
        playButton = play;
        instructionsButton = showInstructions;
        instructionsBackButton = back;
        quitButton = quit;
        feedbackText = feedback;
    }

    void Awake()
    {
        ConfigureAudio();
        if (playButton != null) playButton.onClick.AddListener(StartGame);
        if (instructionsButton != null) instructionsButton.onClick.AddListener(ShowInstructions);
        if (instructionsBackButton != null) instructionsBackButton.onClick.AddListener(ShowMainMenu);
        if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

        Time.timeScale = 1f;
        ShowOnly(mainMenuPanel);
        if (feedbackText != null)
            feedbackText.text = string.IsNullOrEmpty(pendingMenuMessage) ? "Reúne los cristales y alcanza el portal." : pendingMenuMessage;
        pendingMenuMessage = null;
    }

    public void StartGame()
    {
        if (changingScene) return;
        changingScene = true;
        PlayButtonSound();
        Time.timeScale = 1f;
        StartCoroutine(LoadGameAfterClick());
    }

    public void ShowInstructions() { PlayButtonSound(); ShowOnly(instructionsPanel); }
    public void ShowMainMenu() { PlayButtonSound(); ShowOnly(mainMenuPanel); }

    IEnumerator LoadGameAfterClick()
    {
        yield return new WaitForSecondsRealtime(0.16f);
        SceneManager.LoadScene(GameplaySceneName);
    }

    public static void ReturnToMainMenu(string message)
    {
        Time.timeScale = 1f;
        pendingMenuMessage = message;
        SceneManager.LoadScene(MainMenuSceneName);
    }

    public void QuitGame()
    {
        if (changingScene) return;
        changingScene = true;
        PlayButtonSound();
        Time.timeScale = 1f;
        StartCoroutine(QuitAfterClick());
    }

    IEnumerator QuitAfterClick()
    {
        yield return new WaitForSecondsRealtime(0.16f);
        Application.Quit();
    }

    void ConfigureAudio()
    {
        if (Object.FindAnyObjectByType<AudioListener>() == null)
            gameObject.AddComponent<AudioListener>();

        menuMusic = gameObject.AddComponent<AudioSource>();
        menuMusic.playOnAwake = false;
        menuMusic.spatialBlend = 0f;
        menuMusic.volume = 0.23f;
        menuMusic.loop = true;
        menuMusic.clip = CreateSound("Melodia del menu", 8f, MenuMusicSample);
        menuMusic.Play();

        menuEffects = gameObject.AddComponent<AudioSource>();
        menuEffects.playOnAwake = false;
        menuEffects.spatialBlend = 0f;
        menuEffects.volume = 0.7f;
        buttonClip = CreateSound("Clic del menu", 0.16f, ButtonSample);
    }

    void PlayButtonSound()
    {
        if (menuEffects != null && buttonClip != null)
            menuEffects.PlayOneShot(buttonClip);
    }

    static AudioClip CreateSound(string name, float seconds, System.Func<float, float> sample)
    {
        const int rate = 22050;
        int length = Mathf.CeilToInt(seconds * rate);
        var data = new float[length];
        for (int i = 0; i < length; i++) data[i] = Mathf.Clamp(sample(i / (float)rate), -1f, 1f);
        AudioClip clip = AudioClip.Create(name, length, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float MenuMusicSample(float t)
    {
        const float loop = 8f;
        float fade = Mathf.Min(1f, Mathf.Min(t, loop - t) * 2f);
        float swell = 0.72f + 0.28f * Mathf.Sin(2f * Mathf.PI * 0.25f * t);
        float bass = 0.25f * Mathf.Sin(2f * Mathf.PI * 55f * t);
        float chord = 0.11f * Mathf.Sin(2f * Mathf.PI * 110f * t) +
                      0.09f * Mathf.Sin(2f * Mathf.PI * 130.81f * t) +
                      0.08f * Mathf.Sin(2f * Mathf.PI * 164.81f * t);
        float bellEnvelope = Mathf.Exp(-3f * (t % 2f));
        float bell = 0.12f * bellEnvelope * Mathf.Sin(2f * Mathf.PI * 523.25f * t);
        return fade * ((bass + chord) * swell + bell);
    }

    static float ButtonSample(float t)
    {
        float envelope = Mathf.Exp(-23f * t);
        return envelope * (0.32f * Mathf.Sin(2f * Mathf.PI * 659.25f * t) +
                           0.18f * Mathf.Sin(2f * Mathf.PI * 987.77f * t));
    }

    void ShowOnly(GameObject visible)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(mainMenuPanel == visible);
        if (instructionsPanel != null) instructionsPanel.SetActive(instructionsPanel == visible);
    }
}
