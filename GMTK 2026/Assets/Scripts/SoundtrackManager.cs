using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SoundtrackManager : MonoBehaviour
{
    private static SoundtrackManager instance;
    private string[] scenesWithIntensity = new string[] { "MainScene", "ShopScene", "PowerUpShopScene" };
    [SerializeField] AudioClip introTrack;
    [SerializeField] AudioClip mainLoop;
    [SerializeField] AudioClip intenseLoop;
    [SerializeField] AudioClip shopLoop;
    [SerializeField] AudioClip pauseLoop;
    // Assign your Soundtrack mixer group here — the sources are created in
    // code, so they need to be routed to it manually.
    [SerializeField] AudioMixerGroup musicGroup;
    [SerializeField] int intenseThreshold;
    [SerializeField] float crossfadeDuration;
    [SerializeField, Range(0, 1)] float volume;

    // --- Intro sequence settings ---
    // The loops stay silent for this many seconds while the intro plays,
    // then begin (all three in sync). Set to your intro's length.
    [SerializeField] float loopDelay = 4.8f;
    // true  = intro ends, main loop snaps in at full volume (for intros
    //         composed to resolve straight into the loop).
    // false = intro crossfades into the main loop over crossfadeDuration.
    [SerializeField] bool hardHandoff = false;

    private AudioSource[] players;
    private int activeSource = 0;
    private int inactiveSource = 1;
    private bool dynamicEffects = false;
    private bool wasIntense = false;
    private bool isIntense = false;
    private float pitchValue;
    private Coroutine crossfade;

    private bool introPlaying;
    private float introTimer;
    private bool loopsStarted;

    private bool isPaused;
    private int prePauseSource = 1;   // loop to return to when unpaused

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            enabled = false;
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Create exactly 5 sources in code so the count/order is guaranteed
        // (intro, main, intense, shop, pause) — no components to attach by hand.
        players = new AudioSource[5];
        for (int i = 0; i < players.Length; i++)
        {
            players[i] = gameObject.AddComponent<AudioSource>();
            players[i].playOnAwake = false;
            players[i].outputAudioMixerGroup = musicGroup;
        }
    }

    void Start()
    {
        players[0].clip = introTrack;
        players[1].clip = mainLoop;
        players[2].clip = intenseLoop;
        players[3].clip = shopLoop;
        players[4].clip = pauseLoop;

        SceneManager.activeSceneChanged += sceneChange;

        pitchValue = 1f;            // 1 = normal pitch on the mixer param
        activeSource = 0;           // intro is what's audible first
        inactiveSource = 1;
        dynamicEffects = false;     // no pitch ramp during the intro
        introPlaying = true;
        introTimer = 0f;
        loopsStarted = false;

        // Intro: plays once, no loop, full volume, starts now.
        players[0].loop = false;
        players[0].volume = 1f;
        players[0].time = 0f;
        players[0].Play();

        // Loops are NOT started yet — that is the "loopDelay seconds of
        // silence." They begin together in StartLoops() at handoff so the
        // main loop is at its head exactly when it becomes audible. This
        // avoids PlayScheduled/PlayDelayed, which are unreliable on WebGL.
        for (int i = 1; i < 5; i++)
            players[i].volume = 0f;
    }

    void Update()
    {
        // ---- Intro sequence ----
        if (introPlaying)
        {
            introTimer += Time.unscaledDeltaTime;

            // For a crossfade, start the loops crossfadeDuration early so the
            // main loop can rise as the intro tails out, hitting full at
            // loopDelay. For a hard handoff, start exactly at loopDelay.
            float handoffPoint = hardHandoff ? loopDelay
                : Mathf.Max(0f, loopDelay - Mathf.Max(.01f, crossfadeDuration));

            if (introTimer >= handoffPoint)
            {
                StartLoops();
                introPlaying = false;

                int target = LoopForScene(SceneManager.GetActiveScene().name);
                dynamicEffects = target == 1 || target == 3;

                if (hardHandoff)
                {
                    for (int i = 0; i < players.Length; i++)
                        players[i].volume = i == target ? 1f : 0f;
                    activeSource = target;
                    pitchValue = 1f;
                }
                else
                {
                    inactiveSource = target;
                    StartCrossfade();
                }
            }

            ApplyMixer();
            return;   // hold off on intensity logic until the intro is done
        }

        // ---- Paused: hold the pause mix, ignore intensity/pitch ----
        if (isPaused)
        {
            ReSyncLoops();
            ApplyMixer();
            return;
        }

        ReSyncLoops();

        // ---- Dynamic pitch ramp ----
        if (dynamicEffects)
        {
            pitchValue = Mathf.Clamp(pitchValue + 0.001f * Time.deltaTime * (pitchValue * (3 - pitchValue)), 1, 3);
        }

        // ---- Intensity switching ----
        if (RunData.instance.countdown <= intenseThreshold && !wasIntense)
        {
            if (scenesWithIntensity.Contains(SceneManager.GetActiveScene().name))
            {
                isIntense = true;
                dynamicEffects = false;
            }
        }
        else if (RunData.instance.countdown > intenseThreshold && wasIntense)
        {
            if (!RunData.instance.bossRound)
            {
                isIntense = false;
            }
        }

        if (isIntense && !wasIntense)
        {
            inactiveSource = 2;
            StartCrossfade();
        }
        else if (!isIntense && wasIntense)
        {
            changeMusicToScene(SceneManager.GetActiveScene());
        }

        wasIntense = isIntense;

        ApplyMixer();
    }

    // Call from your pause menu: SoundtrackManager.SetPaused(true/false).
    public static void SetPaused(bool paused)
    {
        if (instance != null) instance.SetPausedInternal(paused);
    }

    private void SetPausedInternal(bool paused)
    {
        if (paused == isPaused) return;
        isPaused = paused;

        // Make sure the loops (incl. pause) are actually running. If the
        // player pauses during the intro, this brings them in early.
        StartLoops();

        if (paused)
        {
            // Remember what was playing so we can return to it. Mid-crossfade,
            // the intended track is the fade target (inactiveSource).
            prePauseSource = crossfade != null ? inactiveSource : activeSource;
            // No time reset: the pause mix shares the playhead (ReSyncLoops),
            // so crossfading holds the exact moment in the song.
            inactiveSource = 4;
            StartCrossfade();
        }
        else
        {
            inactiveSource = prePauseSource;
            StartCrossfade();
        }
    }

    // Begin the loops together, silent and looping, from their heads.
    private void StartLoops()
    {
        if (loopsStarted) return;
        loopsStarted = true;
        for (int i = 1; i < 5; i++)   // 1..4: main, intense, shop, pause
        {
            players[i].loop = true;
            players[i].volume = 0f;
            players[i].time = 0f;
            players[i].Play();
        }
    }

    // Same song, different mixes: keep every stem locked to the main loop's
    // playhead so they stay phase-aligned. Independent looping sources drift
    // on WebGL (no reliable PlayScheduled), so we correct it each frame.
    // Only nudge when drift exceeds a threshold, to avoid audible re-seeking.
    private void ReSyncLoops()
    {
        if (!loopsStarted) return;
        AudioSource reference = players[1];      // main loop is the clock
        if (!reference.isPlaying) return;
        float t = reference.time;
        for (int i = 2; i < 5; i++)              // intense, shop, pause
        {
            if (!players[i].isPlaying) continue;
            if (Mathf.Abs(players[i].time - t) > 0.03f)
                players[i].time = t;
        }
    }

    private void ApplyMixer()
    {
        AudioMixerGroup group = players[activeSource].outputAudioMixerGroup;
        if (group == null) return;   // guard: unassigned mixer group would throw
        AudioMixer mixer = group.audioMixer;
        float musicVolume = volume * GameSettings.masterVolume
            * GameSettings.musicVolume;
        mixer.SetFloat("SoundtrackVolume", 80.0f * (musicVolume - 1.0f));
        mixer.SetFloat("SoundtrackPitch", pitchValue);
    }

    private int LoopForScene(string name)
    {
        if (name == "MainScene") return 1;
        if (name == "ShopScene" || name == "PowerUpShopScene") return 3;
        return 1;   // default to main loop; change if you add a menu track
    }

    private IEnumerator crossfadeAudio()
    {
        float initialPitchValue = pitchValue;
        float[] initialVolumes = new float[players.Length];
        for (int i = 0; i < players.Length; i++)
            initialVolumes[i] = players[i].volume;
        float time = 0;
        float duration = Mathf.Max(.01f, crossfadeDuration);
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float amount = Mathf.Clamp01(time / duration);
            pitchValue = Mathf.Lerp(initialPitchValue, 1, amount);
            for (int i = 0; i < players.Length; i++)
                players[i].volume = Mathf.Lerp(initialVolumes[i],
                    i == inactiveSource ? 1 : 0, amount);
            yield return null;
        }
        for (int i = 0; i < players.Length; i++)
            players[i].volume = i == inactiveSource ? 1 : 0;
        activeSource = inactiveSource;
        crossfade = null;
    }

    private void StartCrossfade()
    {
        if (crossfade != null) StopCoroutine(crossfade);
        crossfade = StartCoroutine(crossfadeAudio());
    }

    void changeMusicToScene(Scene scene)
    {
        if (scene.name == "MainScene")
        {
            inactiveSource = 1;
            dynamicEffects = true;
        }
        else if (scene.name == "ShopScene" || scene.name == "PowerUpShopScene")
        {
            inactiveSource = 3;
            dynamicEffects = true;
        }
        else
        {
            inactiveSource = 1;   // was 0 (intro); intro no longer loops, so
            dynamicEffects = false;   // fall back to the main loop instead
            isIntense = false;
        }
        StartCrossfade();
    }

    void sceneChange(Scene scene, Scene next)
    {
        // During the intro, let the handoff pick the loop for the live scene.
        if (introPlaying) return;

        if (!isIntense)
        {
            changeMusicToScene(next);
        }
        if (next.name == "MainScene" && RunData.instance.bossRound)
        {
            isIntense = true;
        }
    }

    private void OnDestroy()
    {
        if (instance != this) return;
        SceneManager.activeSceneChanged -= sceneChange;
        instance = null;
    }
}