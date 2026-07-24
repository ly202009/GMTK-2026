using System;
using System.Collections;
using System.Collections.Generic;
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
    public List<CardData> deck = new();

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
        CreateDeck();
        StartCoroutine(CountdownTimer());
    }

    private IEnumerator CountdownTimer()
    {
        while(true)
        {
            yield return new WaitForSecondsRealtime(1);
            if(countdown > 0) countdown--;
        }
    }

    private void CreateDeck()
    {
        if(deck.Count > 0) return;

        foreach(Suit suit in Enum.GetValues(typeof(Suit)))
            for(int i = 1; i <= 13; i++)
            {
                int properties = 0;
                for(int j = 0; j < 5; j++)
                    if(UnityEngine.Random.Range(0, 5) == 1)
                        properties |= 1 << j;

                deck.Add(new CardData
                {
                    values = new[] { i },
                    suit = suit,
                    properties = properties
                });
            }
    }
}
