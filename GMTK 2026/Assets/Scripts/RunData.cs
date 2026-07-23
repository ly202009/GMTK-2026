using UnityEngine;

public class RunData : MonoBehaviour
{
    public static RunData instance;

    public int numberOfPiles = 2;
    public int handSize = 5;
    public bool allowDoubles;
    public float timerSpeed = 1;
    public bool allowSuitMatching;
    public bool allowFreeze;
    public bool handInvalidGain;
    public int countdown = 60;
    public bool autoDraw;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Create()
    {
        if(instance != null) return;
        GameObject runData = new GameObject("Run Data");
        runData.AddComponent<RunData>();
    }

    private void Awake()
    {
        if(instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
