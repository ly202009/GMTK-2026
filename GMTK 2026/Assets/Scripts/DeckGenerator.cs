using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public enum Suit
{
    Heart,
    Club,
    Diamond,
    Spade
}

[Serializable]
public struct CardData
{
    public const int Transparent = 1 << 0;
    public const int AutoPlay = 1 << 1;
    public const int BonusTime = 1 << 2;
    public const int WildCard = 1 << 3;
    public const int Bomb = 1 << 4;

    public int[] values;
    public Suit suit;
    public int properties;
}

public sealed class DeckGenerator : MonoBehaviour
{
    private static (int property, Color color, string name)[] PropertyColors =
    {
        (CardData.Transparent, new Color(.35f, .85f, 1, .5f), "Transparent"),
        (CardData.AutoPlay, new Color(.3f, 1, .35f, 1), "Auto Play"),
        (CardData.BonusTime, new Color(1, .8f, .15f, 1), "Bonus Time"),
        (CardData.WildCard, new Color(.55f, .55f, .55f, 1), "Wild Card"),
        (CardData.Bomb, new Color(1, .2f, .2f, 1), "Bomb")
    };

    private const int HandSize = 5;
    private const float HandSpacing = 1.5f;
    private const float HandY = -2.3f;
    private const float SpeedPileY = 1.3f;
    private const float DrawPileX = 6f;
    private const float DrawPileY = 0f;

    [SerializeField] private GameObject cardTemplate;

    private int numberOfPiles = 2;
    private Dictionary<GameObject, CardData> cardData = new();
    private List<GameObject> handCards = new();
    private List<GameObject>[] piles;
    private List<GameObject> drawPile = new();
    private GameObject selectedHandCard;
    private bool reshuffledCurrentState;

    private void Start()
    {
        List<CardData> deck = CreateDeck();
        Shuffle(deck);
        cardTemplate.SetActive(false);

        piles = new List<GameObject>[numberOfPiles];
        int deckIndex = 0;

        for (int i = 0; i < HandSize; i++)
        {
            handCards.Add(CreateCard(deck[deckIndex]));
            deckIndex++;
        }

        for (int i = 0; i < numberOfPiles; i++)
        {
            piles[i] = new List<GameObject>();
            piles[i].Add(CreateCard(deck[deckIndex]));
            deckIndex++;
        }

        while (deckIndex < deck.Count)
        {
            drawPile.Add(CreateCard(deck[deckIndex]));
            deckIndex++;
        }

        RenderCards();
    }

    private void HandleClicks()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
 
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D clickedCollider = Physics2D.OverlapPoint(mousePosition);
        if (clickedCollider == null) return;

        GameObject clickedCard = clickedCollider.gameObject;

        if (handCards.Contains(clickedCard))
        {
            if (selectedHandCard == clickedCard)
            {
                selectedHandCard = null;
                return;
            }

            selectedHandCard = clickedCard;
            return;
        }

        for (int i = 0; i < numberOfPiles; i++)
        {
            if (clickedCard == piles[i][piles[i].Count - 1])
            {
                if (selectedHandCard == null) return;

                if(!CardsWork(selectedHandCard, piles[i][piles[i].Count - 1])) return;

                handCards[handCards.IndexOf(selectedHandCard)] = null;
                piles[i].Add(selectedHandCard);
                selectedHandCard = null;
                reshuffledCurrentState = false;
                return;
            }
        }

        if (drawPile.Count > 0 && clickedCard == drawPile[^1])
        {
            if (selectedHandCard != null)
                selectedHandCard = null;

            for (int i = 0; i < handCards.Count && drawPile.Count > 0; i++)
            {
                if (handCards[i] != null) continue;

                GameObject card = drawPile[^1];
                drawPile.RemoveAt(drawPile.Count - 1);
                handCards[i] = card;
                reshuffledCurrentState = false;
            }
        }
   }

    private bool IsHandPlayable()
    {
        foreach(GameObject cardObj in handCards)
        {
            if(cardObj == null) continue;
            foreach(List<GameObject> pile in piles)
                if(CardsWork(cardObj, pile[pile.Count - 1])) return true;
        }
        return false;
    }

    private bool CardsWork(GameObject card1, GameObject card2)
    {
        foreach(int value1 in cardData[card1].values)
        {
            foreach(int value2 in cardData[card2].values)
            {
                int difference = Mathf.Abs(value1 - value2);
                if(difference == 1 || difference == 12) return true;
            }
        }
        return false;
    }

    private void HandleReShuffle()
    {
        if(IsHandPlayable())
        {
            reshuffledCurrentState = false;
            return;
        }

        if(reshuffledCurrentState) return;
        reshuffledCurrentState = true;

        foreach(List<GameObject> pile in piles) Shuffle(pile);

        bool foundPlayableTop = IsHandPlayable();
        for(int i = 0; i < piles.Length && !foundPlayableTop; i++)
        {
            for(int j = 0; j < piles[i].Count && !foundPlayableTop; j++)
            {
                GameObject pileCard = piles[i][j];
                foreach(GameObject handCard in handCards)
                {
                    if(handCard == null) continue;
                    if(CardsWork(handCard, pileCard))
                    {
                        piles[i].RemoveAt(j);
                        piles[i].Add(pileCard);
                        foundPlayableTop = true;
                        break;
                    }
                }
            }
        }

    }

    private void Update()
    {
        HandleClicks();
        HandleReShuffle();
    }

    private void LateUpdate()
    {
        RenderCards();
    }

    private void RenderCards()
    {
        float firstCardX = -(HandSize - 1) * HandSpacing * .5f;

        for(int i = 0; i < handCards.Count; i++)
        {
            if(handCards[i] == null) continue;
            handCards[i].transform.position = new Vector3(firstCardX + i * HandSpacing, HandY, 0);
            SetSortingOrder(handCards[i], 0);
            handCards[i].GetComponent<Collider2D>().enabled = true;
            handCards[i].GetComponentInChildren<Canvas>(true).enabled = true;
        }

        for(int i = 0; i < piles.Length; i++)
        {
            float x = (i - (numberOfPiles - 1) * .5f) * 2;
            for(int j = 0; j < piles[i].Count; j++)
            {
                bool isTopCard = j == piles[i].Count - 1;
                piles[i][j].transform.position = new Vector3(x, SpeedPileY, 0);
                SetSortingOrder(piles[i][j], j + 10);
                piles[i][j].GetComponent<Collider2D>().enabled = isTopCard;
                piles[i][j].GetComponentInChildren<Canvas>(true).enabled = isTopCard;
            }
        }

        for(int i = 0; i < drawPile.Count; i++)
        {
            bool isTopCard = i == drawPile.Count - 1;
            drawPile[i].transform.position = new Vector3(DrawPileX, DrawPileY, 0);
            SetSortingOrder(drawPile[i], i);
            drawPile[i].GetComponent<Collider2D>().enabled = isTopCard;
            drawPile[i].GetComponentInChildren<Canvas>(true).enabled = isTopCard;
        }
    }

    private static List<CardData> CreateDeck()
    {
        var deck = new List<CardData>();
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            for (int i = 1; i <= 13; i++)
                deck.Add(new CardData
                {
                    values = new[] { i },
                    suit = suit,
                    properties = UnityEngine.Random.Range(0, CardData.Bomb << 1)
                });

        return deck;
    }

    private static void Shuffle<T>(List<T> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (deck[i], deck[randomIndex]) = (deck[randomIndex], deck[i]);
        }
    }

    private GameObject CreateCard(CardData data)
    {
        GameObject card = Instantiate(cardTemplate, Vector3.zero, cardTemplate.transform.rotation, cardTemplate.transform.parent);

        card.name = $"Card {cardData.Count + 1}";
        cardData.Add(card, data);

        string rank = data.values[0] switch
        {
            1 => "A",
            11 => "J",
            12 => "Q",
            13 => "K",
            _ => data.values[0].ToString()
        };

        string suit = data.suit switch
        {
            Suit.Heart => " Heart",
            Suit.Club => " Club",
            Suit.Diamond => " Diamond",
            Suit.Spade => " Spade",
            _ => ""
        };

        TMP_Text label = card.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.gameObject.name = "Card Text";
            label.text = rank + suit;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.rectTransform.anchoredPosition = Vector2.zero;

            TMP_Text propertyLabel = Instantiate(label, label.transform.parent);
            propertyLabel.gameObject.name = "Property Text";
            propertyLabel.text = GetPropertyText(data.properties);
            propertyLabel.alignment = TextAlignmentOptions.Top;
            propertyLabel.enableAutoSizing = true;
            propertyLabel.fontSizeMin = 10;
            propertyLabel.fontSizeMax = 18;
            propertyLabel.rectTransform.anchorMin = new Vector2(0, 1);
            propertyLabel.rectTransform.anchorMax = new Vector2(1, 1);
            propertyLabel.rectTransform.pivot = new Vector2(.5f, 1);
            propertyLabel.rectTransform.sizeDelta = new Vector2(-20, 110);
            propertyLabel.rectTransform.anchoredPosition = new Vector2(0, -40);
            propertyLabel.margin = new Vector4(5, 5, 5, 0);
        }

        SpriteRenderer cardRenderer = card.GetComponent<SpriteRenderer>();
        GameObject highlight = new GameObject("Highlight");
        highlight.layer = card.layer;
        highlight.transform.SetParent(card.transform, false);
        highlight.transform.localScale = new Vector3(1.1f, 1.1f, 1);

        SpriteRenderer highlightRenderer = highlight.AddComponent<SpriteRenderer>();
        highlightRenderer.sprite = cardRenderer.sprite;
        highlightRenderer.sharedMaterial = cardRenderer.sharedMaterial;
        highlightRenderer.color = new Color(1, .85f, .1f);
        highlightRenderer.enabled = false;

        if (card.GetComponent<Collider2D>() == null) card.AddComponent<BoxCollider2D>();

        card.SetActive(true);
        return card;
    }

    private void SetSortingOrder(GameObject card, int sortingOrder)
    {
        int cardOrder = sortingOrder * 3 + 10;
        SpriteRenderer cardRenderer = card.GetComponent<SpriteRenderer>();
        SpriteRenderer highlightRenderer = card.transform.Find("Highlight").GetComponent<SpriteRenderer>();

        cardRenderer.color = GetCardColor(cardData[card]);
        cardRenderer.sortingOrder = cardOrder;
        highlightRenderer.sortingOrder = cardOrder - 1;
        highlightRenderer.enabled = card == selectedHandCard;

        TMP_Text[] labels = card.GetComponentsInChildren<TMP_Text>(true);
        for(int i = 0; i < labels.Length; i++)
        {
            if(labels[i].gameObject.name == "Card Text")
                labels[i].enabled = (cardData[card].properties & CardData.WildCard) == 0;
            else if(labels[i].gameObject.name == "Property Text")
            {
                labels[i].enabled = true;
                labels[i].text = GetPropertyText(cardData[card].properties);
            }
        }

        card.GetComponentInChildren<Canvas>(true).sortingOrder = cardOrder + 1;
    }

    private string GetPropertyText(int properties)
    {
        string text = "";
        for(int i = 0; i < PropertyColors.Length; i++)
        {
            if((properties & PropertyColors[i].property) == 0) continue;
            if(text.Length > 0) text += "\n";
            text += PropertyColors[i].name;
        }
        return text;
    }

    private Color GetCardColor(CardData data)
    {
        if(data.properties == 0) return Color.white;

        Color color = Color.clear;
        int propertyCount = 0;
        for(int i = data.properties; i > 0; i >>= 1)
            propertyCount += i & 1;

        for(int i = 0; i < PropertyColors.Length; i++)
            if((data.properties & PropertyColors[i].property) != 0)
                color += PropertyColors[i].color;

        color /= propertyCount;
        if((data.properties & CardData.Transparent) != 0) color.a = .5f;
        return color;
    }
}
