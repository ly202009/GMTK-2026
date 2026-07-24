using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PowerUpShop : MonoBehaviour
{
    private static (string name, float secondsSaved)[] Powerups =
    {
        ("EXTRA PILE", 14),
        ("BIGGER HAND", 10),
        ("ALLOW DOUBLES", 6),
        ("SLOW TIMER", 9),
        ("SUIT MATCHING", 7),
        ("ALLOW FREEZE", 8),
        ("INVALID HAND GAIN", 6),
        ("AUTO DRAW", 5)
    };

    [SerializeField] private Button rerollButton;
    [SerializeField] private TMP_Text rerollText;
    [SerializeField] private Button moveToGameButton;

    private Button[] powerButtons = new Button[3];
    private TMP_Text[] powerTexts = new TMP_Text[3];
    private int[] shownPowers = new int[3];
    private bool[] purchased = new bool[3];
    private int rerolls;

    private void Start()
    {
        for(int i = 0; i < powerButtons.Length; i++)
        {
            powerButtons[i] = Instantiate(rerollButton, rerollButton.transform.parent);
            powerButtons[i].name = "Powerup " + i;
            powerButtons[i].onClick.RemoveAllListeners();
            RectTransform rect = powerButtons[i].GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(.5f, .5f);
            rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2((i - 1) * 310, 0);
            rect.sizeDelta = new Vector2(280, 160);
            powerTexts[i] = powerButtons[i].GetComponentInChildren<TMP_Text>();
            int j = i;
            powerButtons[i].onClick.AddListener(() => BuyPowerup(j));
        }

        rerollButton.onClick.AddListener(Reroll);
        moveToGameButton.onClick.AddListener(MoveToGame);
        ShowPowerups();
    }

    private void MoveToGame()
    {
        SceneManager.LoadScene("MainScene");
    }

    private void ShowPowerups()
    {
        List<int> available = new() { 0, 1, 3 };
        if(!RunData.instance.allowDoubles) available.Add(2);
        if(!RunData.instance.allowSuitMatching) available.Add(4);
        if(!RunData.instance.allowFreeze) available.Add(5);
        if(!RunData.instance.handInvalidGain) available.Add(6);
        if(!RunData.instance.autoDraw) available.Add(7);

        for(int i = available.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (available[i], available[j]) = (available[j], available[i]);
        }

        for(int i = 0; i < powerButtons.Length; i++)
        {
            shownPowers[i] = available[i];
            purchased[i] = false;
            powerTexts[i].color = new Color(1, .15f, .15f);
            powerButtons[i].GetComponent<Image>().color =
                new Color(.04f, .04f, .055f, .95f);
        }
    }

    private void BuyPowerup(int slot)
    {
        int power = shownPowers[slot];
        int cost = Mathf.CeilToInt(Powerups[power].secondsSaved * 3);
        if(purchased[slot] || RunData.instance.countdown < cost)
            return;

        string progress = GetProgress(power);
        RunData.instance.countdown -= cost;
        if(power == 0) RunData.instance.numberOfPiles++;
        if(power == 1) RunData.instance.handSize++;
        if(power == 2) RunData.instance.allowDoubles = true;
        if(power == 3) RunData.instance.timerSpeed *= .7f;
        if(power == 4) RunData.instance.allowSuitMatching = true;
        if(power == 5) RunData.instance.allowFreeze = true;
        if(power == 6) RunData.instance.handInvalidGain = true;
        if(power == 7) RunData.instance.autoDraw = true;

        purchased[slot] = true;
        powerTexts[slot].text =
            $"PURCHASED!\n{Powerups[power].name}\n{progress}";
        powerTexts[slot].color = new Color(.25f, 1, .35f);
        powerButtons[slot].GetComponent<Image>().color =
            new Color(.03f, .25f, .08f, .95f);
        powerButtons[slot].interactable = false;
    }

    private void Reroll()
    {
        int rerollCost = 3 + rerolls * 2;
        if(RunData.instance.countdown < rerollCost) return;
        RunData.instance.countdown -= rerollCost;
        rerolls++;
        ShowPowerups();
    }

    private string GetProgress(int power)
    {
        if(power == 0)
            return $"{RunData.instance.numberOfPiles} PILES"
                + $" \u2192 {RunData.instance.numberOfPiles + 1} PILES";
        if(power == 1)
            return $"HAND {RunData.instance.handSize}"
                + $" \u2192 {RunData.instance.handSize + 1}";
        if(power == 3)
            return $"SPEED {RunData.instance.timerSpeed:0.0}"
                + $" \u2192 {RunData.instance.timerSpeed * .7f:0.0}";
        return "OFF \u2192 ON";
    }

    private void Update()
    {
        int rerollCost = 3 + rerolls * 2;
        rerollText.text = $"REROLL\n-{rerollCost}s";
        rerollButton.interactable = RunData.instance.countdown >= rerollCost;

        for(int i = 0; i < powerButtons.Length; i++)
        {
            if(purchased[i]) continue;
            int power = shownPowers[i];
            int cost = Mathf.CeilToInt(Powerups[power].secondsSaved * 3);
            powerTexts[i].text =
                $"{Powerups[power].name}\n{GetProgress(power)}\n-{cost}s";
            powerButtons[i].interactable =
                RunData.instance.countdown >= cost;
        }
    }
}
