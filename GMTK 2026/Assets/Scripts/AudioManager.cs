using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField] private AudioClip[] buyChip;
    [SerializeField] private AudioClip[] playCard;
    [SerializeField] private AudioClip[] shuffle;
    [SerializeField] private AudioClip[] combo;
    [SerializeField] private AudioClip[] uiDown;
    [SerializeField] private AudioClip[] uiUp;
    [SerializeField] private AudioClip[] invalidPlay;

    [SerializeField] private int poolSize = 16;
    [SerializeField, Range(0, 1)] private float masterVolume = .8f;

    private AudioSource[] pool;
    private int next;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        pool = new AudioSource[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0;       //non geeked audio frfr
            pool[i] = source;
        }
    }

    private void Play(AudioClip[] clips, float pitch, float volume)
    {
        if (clips == null || clips.Length == 0) return;
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null) return;

        AudioSource source = pool[next];
        next = (next + 1) % pool.Length;
        source.pitch = pitch;
        source.PlayOneShot(clip, volume * masterVolume
            * GameSettings.masterVolume * GameSettings.sfxVolume);
    }

    private static float Jitter(float amount) => 1 + Random.Range(-amount, amount);

    public static void BuyChip() { if (instance) instance.Play(instance.buyChip, Jitter(.05f), 0.4f); }
    public static void Shuffle() { if (instance) instance.Play(instance.shuffle, Jitter(.04f), 0.4f); }
    public static void PlayCard() { if (instance) instance.Play(instance.playCard, Jitter(.04f), 0.4f); }
    public static void UIDown() { if (instance) instance.Play(instance.uiDown, Jitter(.06f), .3f); }
    public static void UIUp() { if (instance) instance.Play(instance.uiUp, Jitter(.06f), .3f); }
    public static void InvalidPlay() { if (instance) instance.Play(instance.invalidPlay, Jitter(.06f), .6f); }

    public static void Combo(int comboTier = 0)
    {
        if (!instance) return;
        float climb = Mathf.Pow(1.0595f, Mathf.Min(comboTier, 24));
        instance.Play(instance.combo, climb, .85f);
    }
}
