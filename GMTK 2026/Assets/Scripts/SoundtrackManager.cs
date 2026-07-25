using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SoundtrackManager : MonoBehaviour
{
    private string[] scenesWithIntensity = new string[]{"MainScene", "ShopScene", "PowerUpShopScene"};
    [SerializeField] AudioClip introTrack;
    [SerializeField] AudioClip mainLoop;
    [SerializeField] AudioClip intenseLoop;
    [SerializeField] AudioClip shopLoop;
    [SerializeField] int intenseThreshold;
    [SerializeField] float crossfadeDuration;
    [SerializeField, Range(0, 1)] float volume;

    private AudioSource[] players;
    private int activeSource = 0;
    private int inactiveSource = 1;
    private bool dynamicEffects = false;
    private bool wasIntense = false;
    private bool isIntense = false;
    private float pitchValue;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (FindObjectsByType<SoundtrackManager>().Length > 1)
        {
            Destroy(this.gameObject);
        }
        DontDestroyOnLoad(this.gameObject);

        players = GetComponents<AudioSource>();
        players[0].clip = introTrack;
        players[1].clip = mainLoop;
        players[2].clip = intenseLoop;
        players[3].clip = shopLoop;

        SceneManager.activeSceneChanged += sceneChange;

        if (SceneManager.GetActiveScene().name == "MainScene")
        {
            activeSource = 1;
            dynamicEffects = true;
        } else 
        {
            activeSource = 0;
            dynamicEffects = false;
        }

        players[activeSource].volume = 1.0f;
        for (int i = 0; i < 4; i++)
        {
            if (i != activeSource)
            {
                players[i].volume = 0;
            }
            players[i].Play();
            players[i].loop = true;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (dynamicEffects)
        {
            pitchValue = Mathf.Clamp(pitchValue + 0.001f*Time.deltaTime*(pitchValue*(3-pitchValue)), 1, 3);
        }
        if (RunData.instance.countdown <= intenseThreshold && !wasIntense)
        {
            if (scenesWithIntensity.Contains(SceneManager.GetActiveScene().name))
            {
                isIntense = true;
                dynamicEffects = false;
            }
        } else if (RunData.instance.countdown > intenseThreshold && wasIntense)
        {
            if (!RunData.instance.bossRound)
            {
                isIntense = false;
            }
        }

        if (isIntense && !wasIntense)
        {
            inactiveSource = 2;
            StartCoroutine(crossfadeAudio());

        } else if (!isIntense && wasIntense)
        {
            changeMusicToScene(SceneManager.GetActiveScene());
        }

        wasIntense = isIntense;
        float musicVolume = volume * GameSettings.masterVolume
            * GameSettings.musicVolume;
        players[activeSource].outputAudioMixerGroup.audioMixer.SetFloat(
            "SoundtrackVolume", 80.0f * (musicVolume - 1.0f));
        players[activeSource].outputAudioMixerGroup.audioMixer.SetFloat(
                "SoundtrackPitch", 
                pitchValue
                );
    }
    private IEnumerator crossfadeAudio()
    {
        float initialPitchValue = pitchValue;
        for (float t = 0; t <= crossfadeDuration; t += Time.deltaTime)
        {
            pitchValue = Mathf.Lerp(initialPitchValue , 1, t/crossfadeDuration);
            players[inactiveSource].volume = t/crossfadeDuration;
            players[activeSource].volume = 1-t/crossfadeDuration;
            yield return null;
        }
        players[activeSource].volume = 0.0f;
        for (int i = 0; i < 4; i++)
        {
            if (i != inactiveSource)
            {
                players[i].volume = 0.0f;
            }
        }
        players[inactiveSource].volume = 1.0f;
        activeSource = inactiveSource;
    }
    void changeMusicToScene(Scene scene){
        if (scene.name == "MainScene")
        {
            inactiveSource = 1;
            dynamicEffects = true;
        } else if (scene.name == "ShopScene" || scene.name == "PowerUpShopScene")
        {
            inactiveSource = 3;
            dynamicEffects = true;
        } else
        {
            inactiveSource = 0;
            dynamicEffects = false;
            isIntense = false;
        }
        StartCoroutine(crossfadeAudio());
}
    void sceneChange(Scene scene, Scene next)
    {
        if (!isIntense){
            changeMusicToScene(next);
        }
        if (next.name == "MainScene" && RunData.instance.bossRound)
        {
            isIntense = true;
        }
    }

    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= sceneChange;
    }
}
