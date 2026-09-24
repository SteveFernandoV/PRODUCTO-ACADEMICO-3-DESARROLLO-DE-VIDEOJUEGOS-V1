using UnityEngine;
using UnityEngine.SceneManagement;

// Original procedural sounds: no external asset or licence is needed.
public sealed class PA3GameAudio : MonoBehaviour
{
    static PA3GameAudio instance;
    AudioClip footstep, crystal, atmosphere, guardianVoice;
    AudioSource effects, crystalEffects, ambience;
    AudioSource guardianSource;
    CharacterController player;
    float nextStep;
    float nextGuardianSearch;
    Vector3 lastPlayerPosition;
    bool hasPlayerPosition;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "PA3_VerticalSlice") return;
        if (instance != null) return;
        var audioObject = new GameObject("PA3_GameAudio");
        instance = audioObject.AddComponent<PA3GameAudio>();
    }

    void Awake()
    {
        instance = this;
        EnsureClips();

        effects = gameObject.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        effects.spatialBlend = 0f;
        effects.volume = 0.85f;

        crystalEffects = gameObject.AddComponent<AudioSource>();
        crystalEffects.playOnAwake = false;
        crystalEffects.spatialBlend = 0f;
        crystalEffects.volume = 1f;

        ambience = gameObject.AddComponent<AudioSource>();
        ambience.clip = atmosphere;
        ambience.loop = true;
        ambience.spatialBlend = 0f;
        ambience.volume = 0.22f;
        ambience.Play();
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }

    void EnsureClips()
    {
        if (footstep == null) footstep = MakeGrassFootstep();
        if (crystal == null) crystal = MakeClip("Cristal recogido", 0.9f, CrystalSample);
        if (atmosphere == null) atmosphere = MakeClip("Ambiente bosque oscuro", 8f, AtmosphereSample);
        if (guardianVoice == null) guardianVoice = MakeClip("Susurro del guardian", 6f, GuardianSample);
        if (ambience != null && ambience.clip != atmosphere)
        {
            ambience.clip = atmosphere;
            ambience.Play();
        }
        if (guardianSource != null && guardianSource.clip != guardianVoice)
        {
            guardianSource.clip = guardianVoice;
            guardianSource.Play();
        }
    }

    void LateUpdate()
    {
        EnsureClips();
        AttachGuardianSound();
        if (player == null)
        {
            var character = GameObject.FindGameObjectWithTag("Player");
            if (character != null) player = character.GetComponent<CharacterController>();
        }
        if (player == null) return;
        Vector3 position = player.transform.position;
        if (!hasPlayerPosition) { lastPlayerPosition = position; hasPlayerPosition = true; return; }
        Vector3 displacement = position - lastPlayerPosition;
        lastPlayerPosition = position;
        displacement.y = 0f;
        float speed = displacement.magnitude / Mathf.Max(Time.deltaTime, 0.001f);
        if (!player.isGrounded || speed < 0.7f || Time.time < nextStep) return;
        effects.pitch = Random.Range(0.97f, 1.03f);
        if (footstep != null) effects.PlayOneShot(footstep, 0.42f);
        nextStep = Time.time + Mathf.Clamp(2.2f / speed, 0.27f, 0.52f);
    }

    void AttachGuardianSound()
    {
        if (guardianSource != null || Time.time < nextGuardianSearch) return;
        nextGuardianSearch = Time.time + 1f;
        EnemyChaser guardian = Object.FindAnyObjectByType<EnemyChaser>();
        if (guardian == null) return;
        Transform existing = guardian.transform.Find("PA3_GuardianVoice");
        GameObject voice = existing != null ? existing.gameObject : new GameObject("PA3_GuardianVoice");
        voice.transform.SetParent(guardian.transform, false);
        voice.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        guardianSource = voice.GetComponent<AudioSource>();
        if (guardianSource == null) guardianSource = voice.AddComponent<AudioSource>();
        guardianSource.clip = guardianVoice;
        guardianSource.loop = true;
        guardianSource.playOnAwake = false;
        guardianSource.spatialBlend = 1f;
        guardianSource.rolloffMode = AudioRolloffMode.Linear;
        guardianSource.minDistance = 2f;
        guardianSource.maxDistance = 18f;
        guardianSource.volume = 0.9f;
        guardianSource.Play();
    }

    public static void PlayCrystalPickup()
    {
        if (instance == null)
        {
            GameObject audioObject = new GameObject("PA3_GameAudio");
            instance = audioObject.AddComponent<PA3GameAudio>();
        }
        instance.EnsureClips();
        if (instance.crystalEffects != null && instance.crystal != null)
            instance.crystalEffects.PlayOneShot(instance.crystal, 1f);
    }

    static AudioClip MakeClip(string name, float seconds, System.Func<float, float> sample)
    {
        const int rate = 22050;
        int count = Mathf.CeilToInt(seconds * rate);
        var data = new float[count];
        for (int i = 0; i < count; i++) data[i] = Mathf.Clamp(sample(i / (float)rate), -1f, 1f);
        var clip = AudioClip.Create(name, count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static AudioClip MakeGrassFootstep()
    {
        // Band-limited noise with a rounded attack/release sounds like brushing grass,
        // not the sharp transient and bass thud of the previous step.
        const int rate = 22050;
        const float duration = 0.26f;
        int count = Mathf.CeilToInt(duration * rate);
        var data = new float[count];
        uint seed = 0x9254A17u;
        float fast = 0f, slow = 0f;
        for (int i = 0; i < count; i++)
        {
            seed = unchecked(seed * 1664525u + 1013904223u);
            float noise = (seed / (float)uint.MaxValue) * 2f - 1f;
            fast += (noise - fast) * 0.3f;
            slow += (noise - slow) * 0.015f;
            float phase = i / (float)(count - 1);
            float envelope = Mathf.Sin(Mathf.PI * phase);
            envelope *= envelope;
            data[i] = (fast - slow) * envelope * 0.65f;
        }
        AudioClip clip = AudioClip.Create("Pasto suave", count, 1, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static float GuardianSample(float t)
    {
        const float loop = 6f;
        float fade = Mathf.Min(1f, Mathf.Min(t, loop - t) * 2f);
        float swell = 0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 0.32f * t);
        float wavering = 155f + 12f * Mathf.Sin(2f * Mathf.PI * 0.21f * t);
        float moan = 0.34f * Mathf.Sin(2f * Mathf.PI * wavering * t) +
                     0.16f * Mathf.Sin(2f * Mathf.PI * 2.01f * wavering * t);
        float breath = 0.13f * Mathf.Sin(2f * Mathf.PI * 633f * t) *
                       Mathf.Sin(2f * Mathf.PI * 917f * t);
        return fade * (moan * swell + breath);
    }

    static float CrystalSample(float t)
    {
        float envelope = Mathf.Exp(-4.5f * t);
        return envelope * (0.22f * Mathf.Sin(2f * Mathf.PI * 784f * t) +
                           0.18f * Mathf.Sin(2f * Mathf.PI * 1174.66f * t) +
                           0.15f * Mathf.Sin(2f * Mathf.PI * 1568f * t));
    }

    static float AtmosphereSample(float t)
    {
        const float loop = 8f;
        float fade = Mathf.Min(1f, Mathf.Min(t, loop - t) * 2f);
        float drone = 0.16f * Mathf.Sin(2f * Mathf.PI * 55f * t) +
                      0.09f * Mathf.Sin(2f * Mathf.PI * 82.41f * t) +
                      0.07f * Mathf.Sin(2f * Mathf.PI * 54.75f * t);
        float wind = 0.055f * Mathf.Sin(2f * Mathf.PI * 0.42f * t) *
                     Mathf.Sin(2f * Mathf.PI * 317f * t);
        return fade * (drone + wind);
    }
}
