using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public int countdown = 120;
    public bool autoDraw;
    public int round = 1;
    public List<CardData> deck = new();
    public bool bossRound => round > 0 && round % 3 == 0;

    private float countdownTime;
    private bool timerFrozen;
    public float countdownValue => Mathf.Max(0, countdown - countdownTime);

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
        SceneManager.sceneLoaded += HandleSceneLoaded;
        Instantiate(Resources.Load<GameObject>("CountdownHUD"), transform);
        CreateDeck();
        StartCoroutine(CountdownTimer());
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "ShopScene" || scene.name == "PowerUpShopScene") round++;
    }

    private void OnDestroy()
    {
        if(instance == this)
            SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private IEnumerator CountdownTimer()
    {
        while(true)
        {
            yield return null;
            if(timerFrozen) continue;
            if(countdown <= 0)
            {
                countdownTime = 0;
                continue;
            }

            countdownTime += Time.unscaledDeltaTime * timerSpeed;
            if(countdownTime < 1) continue;
            countdownTime -= 1;
            countdown--;
        }
    }

    public void SetTimerFrozen(bool frozen)
    {
        timerFrozen = frozen;
    }

    private void CreateDeck()
    {
        if(deck.Count > 0) return;

        foreach(Suit suit in Enum.GetValues(typeof(Suit)))
            for(int i = 1; i <= 13; i++)
            {
                int properties = 0;
                // for(int j = 0; j < 5; j++)
                //     if(UnityEngine.Random.Range(0, 5) == 1)
                //         properties |= 1 << j;

                deck.Add(new CardData
                {
                    values = new[] { i },
                    suit = suit,
                    properties = properties
                });
            }
    }
}
