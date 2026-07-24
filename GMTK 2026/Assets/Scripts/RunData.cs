using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RunData : MonoBehaviour
{
    public static RunData instance;
    public static (string name, float secondsSaved, string sprite)[] Powerups =
    {
        ("EXTRA PILE", 14, "Extra Playable Stacks"),
        ("BIGGER HAND", 10, null),
        ("ALLOW DOUBLES", 6, "Doubles Powerup"),
        ("SLOW TIMER", 9, "Time Slow Powerup"),
        ("SUIT MATCHING", 7, "Matching suits Powerup"),
        ("ALLOW FREEZE", 8, "Freeze Powerup"),
        ("INVALID HAND GAIN", 6, null),
        ("AUTO DRAW", 5, null)
    };
    public static string[] Bosses =
    {
        "0100", "OVERFLOW", "BLACK BOX", "TRUNCATE",
        "STICKY KEYS", "OVERCLOCK", "SCREENSAVER", "UNSIGNED"
    };
    public static string[] BossDescriptions =
    {
        "4s only play when the timer is divisible by 4",
        "Every 3s, a pile's top card is whisked away",
        "The pile cards periodically disappear",
        "Your hand is two cards smaller",
        "The cursor is constantly pushed",
        "The timer drains another 30% faster",
        "A bouncing logo blocks your inputs",
        "Aces and kings cannot play onto each other"
    };

    public int numberOfPiles = 2;
    public int handSize = 5;
    public bool allowDoubles;
    public float timerSpeed = 1;
    public bool allowSuitMatching;
    public bool allowFreeze;
    public bool handInvalidGain;
    public int countdown = 120;
    public bool autoDraw;
    public int round = 3;
    public List<int> bossOrder = new();
    public int currentBoss = -1;
    public List<CardData> deck = new();
    public int[] powerupLevels = new int[8];
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
        GameObject hud = Instantiate(
            Resources.Load<GameObject>("CountdownHUD"), transform);
        GameObject powerupHud = Instantiate(
            Resources.Load<GameObject>("PowerupHUD"), hud.transform);
        powerupHud.transform.SetSiblingIndex(
            Mathf.Max(0, hud.transform.childCount - 2));
        powerupHud.AddComponent<PowerupHUD>();
        CreateDeck();
        for(int i = 0; i < Bosses.Length; i++) bossOrder.Add(i);
        for(int i = bossOrder.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (bossOrder[i], bossOrder[j]) = (bossOrder[j], bossOrder[i]);
        }
        StartCoroutine(CountdownTimer());
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "ShopScene" || scene.name == "PowerUpShopScene") round++;
        currentBoss = bossRound ?
            bossOrder[(round / 3 - 1) % bossOrder.Count] : -1;
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

    public static int PowerupCost(int power)
    {
        return Mathf.CeilToInt(Powerups[power].secondsSaved * 3);
    }

    public int GetPowerupLevel(int power)
    {
        if(powerupLevels[power] > 0) return powerupLevels[power];
        if(power == 0) return Mathf.Max(0, numberOfPiles - 2);
        if(power == 1) return Mathf.Max(0, handSize - 5);
        if(power == 2) return allowDoubles ? 1 : 0;
        if(power == 3 && timerSpeed < .99f)
            return Mathf.Max(1, Mathf.RoundToInt(
                Mathf.Log(timerSpeed) / Mathf.Log(.7f)));
        if(power == 4) return allowSuitMatching ? 1 : 0;
        if(power == 5) return allowFreeze ? 1 : 0;
        if(power == 6) return handInvalidGain ? 1 : 0;
        if(power == 7) return autoDraw ? 1 : 0;
        return 0;
    }

    public void AddPowerup(int power)
    {
        int level = GetPowerupLevel(power) + 1;
        powerupLevels[power] = level;
        if(power == 0) numberOfPiles = 2 + level;
        if(power == 1) handSize = 5 + level;
        if(power == 2) allowDoubles = true;
        if(power == 3) timerSpeed = Mathf.Pow(.7f, level);
        if(power == 4) allowSuitMatching = true;
        if(power == 5) allowFreeze = true;
        if(power == 6) handInvalidGain = true;
        if(power == 7) autoDraw = true;
    }

    public int SellPowerup(int power)
    {
        int level = GetPowerupLevel(power);
        if(level <= 0) return 0;

        level--;
        powerupLevels[power] = level;
        if(power == 0) numberOfPiles = 2 + level;
        if(power == 1) handSize = 5 + level;
        if(power == 2) allowDoubles = level > 0;
        if(power == 3) timerSpeed = Mathf.Pow(.7f, level);
        if(power == 4) allowSuitMatching = level > 0;
        if(power == 5) allowFreeze = level > 0;
        if(power == 6) handInvalidGain = level > 0;
        if(power == 7) autoDraw = level > 0;

        int refund = Mathf.RoundToInt(PowerupCost(power) * .7f);
        countdown += refund;
        return refund;
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

public class PowerupHUD : MonoBehaviour
{
    private GameObject[] entries = new GameObject[8];
    private Image[] icons = new Image[8];
    private Image[] cooldownFills = new Image[8];
    private TMP_Text[] statusTexts = new TMP_Text[8];
    private TMP_Text[] sellTexts = new TMP_Text[8];
    private AnimatedButton[] entryAnimations = new AnimatedButton[8];
    private int selectedPower = -1;
    private float selectedTime;

    private void Start()
    {
        RectTransform listRect = transform.Find("Powerup List")
            .GetComponent<RectTransform>();
        Button entryTemplate = listRect.Find("Powerup Entry Template")
            .GetComponent<Button>();
        entryTemplate.gameObject.SetActive(false);

        Sprite fallbackSprite =
            Resources.LoadAll<Sprite>("Powerups/Extra Playable Stacks")[0];

        for(int i = 0; i < entries.Length; i++)
        {
            int j = i;
            entries[i] = Instantiate(entryTemplate, listRect).gameObject;
            entries[i].name = RunData.Powerups[i].name;
            RectTransform rect = entries[i].GetComponent<RectTransform>();
            Button button = entries[i].GetComponent<Button>();
            button.onClick.AddListener(() => ClickPowerup(j));
            entryAnimations[i] =
                entries[i].GetComponent<AnimatedButton>();
            icons[i] = entries[i].transform.Find("Icon")
                .GetComponent<Image>();
            Sprite[] sprites = RunData.Powerups[i].sprite == null ?
                new Sprite[0] : Resources.LoadAll<Sprite>(
                    "Powerups/" + RunData.Powerups[i].sprite);
            icons[i].sprite = sprites.Length > 0 ?
                sprites[0] : fallbackSprite;
            cooldownFills[i] = entries[i].transform.Find("Cooldown")
                .GetComponent<Image>();
            statusTexts[i] = entries[i].transform.Find("Status")
                .GetComponent<TMP_Text>();
            sellTexts[i] = entries[i].transform.Find("Sell")
                .GetComponent<TMP_Text>();
            rect.anchoredPosition = Vector2.zero;
        }
    }

    private void ClickPowerup(int power)
    {
        if(selectedPower != power)
        {
            selectedPower = power;
            selectedTime = 3;
            return;
        }

        int refund = RunData.instance.SellPowerup(power);
        if(refund > 0 && DeckGenerator.instance != null)
            DeckGenerator.instance.RemovePowerup(power);
        selectedPower = -1;
    }

    private void Update()
    {
        if(RunData.instance == null) return;
        if(selectedTime > 0)
            selectedTime -= Time.unscaledDeltaTime;
        else
            selectedPower = -1;

        int j = 0;
        for(int i = 0; i < entries.Length; i++)
        {
            int level = RunData.instance.GetPowerupLevel(i);
            if(level <= 0)
            {
                entries[i].SetActive(false);
                continue;
            }

            RectTransform rect =
                entries[i].GetComponent<RectTransform>();
            Vector2 position = new Vector2(0,
                -j * (rect.rect.height + 14));
            entryAnimations[i].SetBasePosition(position);
            if(!entries[i].activeSelf)
                rect.anchoredPosition = position;
            entries[i].SetActive(true);
            j++;

            int key = i == 4 ? 1 : i == 2 ? 2 : i == 5 ? 3 : 0;
            float cooldown = DeckGenerator.instance == null ?
                0 : DeckGenerator.instance.PowerupCountdown(i);
            bool used = DeckGenerator.instance != null
                && DeckGenerator.instance.PowerupUsed(i);
            bool finished = used && cooldown <= 0;

            if(key == 0)
                statusTexts[i].text = level > 1 ? $"x{level}" : "";
            else if(DeckGenerator.instance == null)
                statusTexts[i].text = $"[{key}]";
            else if(!used)
                statusTexts[i].text = $"[{key}]\nREADY";
            else if(cooldown > 0)
                statusTexts[i].text = $"[{key}]\n{cooldown:0.0}";
            else
                statusTexts[i].text = $"[{key}]\nUSED";

            icons[i].color = finished ?
                new Color(.28f, .28f, .28f, .72f) : Color.white;
            entries[i].GetComponent<Image>().color = finished ?
                new Color(.025f, .025f, .03f, .7f) :
                new Color(.015f, .018f, .025f, .92f);
            statusTexts[i].color = finished ?
                new Color(.48f, .48f, .52f) : Color.white;

            RectTransform cooldownRect =
                cooldownFills[i].rectTransform;
            cooldownRect.anchorMax = new Vector2(1,
                Mathf.Clamp01(cooldown / 10));
            cooldownFills[i].enabled = cooldown > 0;

            int refund = Mathf.RoundToInt(
                RunData.PowerupCost(i) * .7f);
            sellTexts[i].text = selectedPower == i ?
                $"{RunData.Powerups[i].name}\nSELL +{refund}s" : "";
        }
    }
}
