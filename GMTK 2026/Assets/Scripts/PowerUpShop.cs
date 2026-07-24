using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PowerUpShop : MonoBehaviour
{
    [SerializeField] private Button rerollButton;
    [SerializeField] private TMP_Text rerollText;
    [SerializeField] private Button moveToGameButton;

    private Button[] powerButtons = new Button[3];
    private TMP_Text[] powerTexts = new TMP_Text[3];
    private int[] shownPowers = new int[3];
    private bool[] purchased = new bool[3];
    private Sprite[] powerSprites = new Sprite[8];
    private int rerolls;

    private void Start()
    {
        Sprite fallbackSprite =
            Resources.LoadAll<Sprite>("Powerups/Extra Playable Stacks")[0];
        for(int i = 0; i < powerSprites.Length; i++)
        {
            Sprite[] sprites = RunData.Powerups[i].sprite == null ?
                new Sprite[0] : Resources.LoadAll<Sprite>(
                    "Powerups/" + RunData.Powerups[i].sprite);
            powerSprites[i] = sprites.Length > 0 ? sprites[0] : fallbackSprite;
        }

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
            rect.sizeDelta = new Vector2(250, 250);
            powerTexts[i] = powerButtons[i].GetComponentInChildren<TMP_Text>();
            RectTransform textRect = powerTexts[i].rectTransform;
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 0);
            textRect.pivot = new Vector2(.5f, 1);
            textRect.anchoredPosition = new Vector2(0, -10);
            textRect.sizeDelta = new Vector2(0, 88);
            powerTexts[i].fontSize = 24;
            powerTexts[i].enableAutoSizing = true;
            powerTexts[i].fontSizeMin = 15;
            powerTexts[i].fontSizeMax = 24;
            powerButtons[i].GetComponent<AnimatedButton>().idleFloat = 8;
            int j = i;
            powerButtons[i].onClick.AddListener(() => BuyPowerup(j));
        }

        rerollButton.onClick.AddListener(Reroll);
        moveToGameButton.onClick.AddListener(MoveToGame);
        ShowPowerups();
    }

    private void MoveToGame()
    {
        SceneTransition.Load("MainScene");
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
            powerButtons[i].GetComponent<AnimatedButton>()
                .PlayEntrance(i * .045f);
            powerTexts[i].color = Color.white;
            Image image = powerButtons[i].GetComponent<Image>();
            image.sprite = powerSprites[shownPowers[i]];
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }
    }

    private void BuyPowerup(int slot)
    {
        int power = shownPowers[slot];
        int cost = RunData.PowerupCost(power);
        if(purchased[slot] || RunData.instance.countdown < cost)
            return;

        string progress = GetProgress(power);
        RunData.instance.countdown -= cost;
        RunData.instance.AddPowerup(power);

        purchased[slot] = true;
        powerTexts[slot].text =
            $"PURCHASED!\n{RunData.Powerups[power].name}\n{progress}";
        powerTexts[slot].color = new Color(.25f, 1, .35f);
        powerButtons[slot].GetComponent<Image>().color =
            new Color(.55f, 1, .6f);
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
            int cost = RunData.PowerupCost(power);
            powerTexts[i].text =
                $"{RunData.Powerups[power].name}\n{GetProgress(power)}\n-{cost}s";
            powerButtons[i].interactable =
                RunData.instance.countdown >= cost;
        }
    }
}
