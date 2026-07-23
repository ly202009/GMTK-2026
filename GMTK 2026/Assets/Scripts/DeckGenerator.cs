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
    public List<int> values;
    public Suit suit;
    public bool isTransparent;
    public bool isAutoPlay;
    public bool isBonusTime;
    public bool isWildCard;
    public bool isBomb;
}

public sealed class DeckGenerator : MonoBehaviour
{
    private int handSize = 5;
    private float handSpacing = 1.5f;
    private float handY = -2.3f;
    private float speedPileY = 1.3f;
    private float drawPileX = 6f;
    private float drawPileY = 0f;

    [SerializeField] private GameObject cardTemplate;

    private int numberOfPiles = 2;
    private Dictionary<GameObject, CardData> cardData = new();
    private List<GameObject> handCards = new();
    private Stack<GameObject>[] piles;
    private int[] pileDepth;
    private List<GameObject> drawPile = new();
    private GameObject selectedHandCard;

    private void Start()
    {
        List<CardData> deck = CreateDeck();
        Shuffle(deck);
        cardTemplate.SetActive(false);

        piles = new Stack<GameObject>[numberOfPiles];
        pileDepth = new int[numberOfPiles];
        int deckIndex = 0;
        float firstCardX = -(handSize - 1) * handSpacing * .5f;

        for (int i = 0; i < handSize; i++)
        {
            Vector3 position = new(firstCardX + i * handSpacing, handY, 0);
            handCards.Add(CreateCard(deck[deckIndex], position, deckIndex, 0));
            deckIndex++;
        }

        for (int i = 0; i < numberOfPiles; i++)
        {
            piles[i] = new Stack<GameObject>();
            float x = (i - (numberOfPiles - 1) * .5f) * 2;
            GameObject card = CreateCard(deck[deckIndex], new Vector3(x, speedPileY, 0), deckIndex, 10);
            piles[i].Push(card);
            pileDepth[i] = 10;
            deckIndex++;
        }

        while (deckIndex < deck.Count)
        {
            GameObject card = CreateCard(deck[deckIndex], new Vector3(drawPileX, drawPileY, 0), deckIndex, drawPile.Count);
            card.GetComponent<Collider2D>().enabled = false;
            card.GetComponentInChildren<Canvas>(true).enabled = false;
            drawPile.Add(card);
            deckIndex++;
        }

        drawPile[drawPile.Count - 1].GetComponent<Collider2D>().enabled = true;
        drawPile[drawPile.Count - 1].GetComponentInChildren<Canvas>(true).enabled = true;
    }

    private void Update()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D clickedCollider = Physics2D.OverlapPoint(mousePosition);
        if (clickedCollider == null) return;

        GameObject clickedCard = clickedCollider.gameObject;
        int handSlot = handCards.IndexOf(clickedCard);

        if (handSlot >= 0)
        {
            if (selectedHandCard == clickedCard)
            {
                clickedCard.GetComponent<SpriteRenderer>().color = Color.white;
                selectedHandCard = null;
                return;
            }

            if (selectedHandCard != null)
                selectedHandCard.GetComponent<SpriteRenderer>().color = Color.white;

            selectedHandCard = clickedCard;
            clickedCard.GetComponent<SpriteRenderer>().color = new Color(1, .85f, .35f);
            return;
        }

        for (int i = 0; i < numberOfPiles; i++)
        {
            if (clickedCard == piles[i].Peek())
            {
                if (selectedHandCard == null) return;

                int playedRank = cardData[selectedHandCard].values[0];
                int pileRank = cardData[piles[i].Peek()].values[0];
                int difference = Mathf.Abs(playedRank - pileRank);
                if (difference != 1 && difference != 12) return;

                piles[i].Peek().GetComponent<Collider2D>().enabled = false;
                piles[i].Peek().GetComponentInChildren<Canvas>(true).enabled = false;
                selectedHandCard.GetComponent<SpriteRenderer>().color = Color.white;
                handCards[handCards.IndexOf(selectedHandCard)] = null;
                selectedHandCard.transform.position = piles[i].Peek().transform.position;
                pileDepth[i]++;
                SetSortingOrder(selectedHandCard, pileDepth[i]);
                piles[i].Push(selectedHandCard);
                selectedHandCard = null;
                return;
            }
        }

        if (drawPile.Count > 0 && clickedCard == drawPile[drawPile.Count - 1])
        {
            if (selectedHandCard != null)
            {
                selectedHandCard.GetComponent<SpriteRenderer>().color = Color.white;
                selectedHandCard = null;
            }

            float firstCardX = -(handSize - 1) * handSpacing * .5f;
            for (int i = 0; i < handCards.Count && drawPile.Count > 0; i++)
            {
                if (handCards[i] != null) continue;

                GameObject card = drawPile[drawPile.Count - 1];
                drawPile.RemoveAt(drawPile.Count - 1);
                card.transform.position = new Vector3(firstCardX + i * handSpacing, handY, 0);
                SetSortingOrder(card, 0);
                card.GetComponent<Collider2D>().enabled = true;
                card.GetComponentInChildren<Canvas>(true).enabled = true;
                handCards[i] = card;
            }

            if (drawPile.Count > 0)
            {
                drawPile[drawPile.Count - 1].GetComponent<Collider2D>().enabled = true;
                drawPile[drawPile.Count - 1].GetComponentInChildren<Canvas>(true).enabled = true;
            }
        }
    }

    private static List<CardData> CreateDeck()
    {
        var deck = new List<CardData>();
        for (int i = 0; i < 52; i++)
            deck.Add(new CardData
            {
                values = new List<int> { i % 13 + 1 },
                suit = (Suit)(i / 13)
            });

        return deck;
    }

    private static void Shuffle(List<CardData> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (deck[i], deck[randomIndex]) = (deck[randomIndex], deck[i]);
        }
    }

    private GameObject CreateCard(CardData data, Vector3 position, int deckIndex, int sortingOrder)
    {
        GameObject card = Instantiate(cardTemplate, position, cardTemplate.transform.rotation, cardTemplate.transform.parent);

        card.name = $"Card {deckIndex + 1}";
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
        if (label != null) label.text = rank + suit;
        if (card.GetComponent<Collider2D>() == null) card.AddComponent<BoxCollider2D>();

        SetSortingOrder(card, sortingOrder);
        card.SetActive(true);
        return card;
    }

    private void SetSortingOrder(GameObject card, int sortingOrder)
    {
        card.GetComponent<SpriteRenderer>().sortingOrder = sortingOrder;
        card.GetComponentInChildren<Canvas>(true).sortingOrder = sortingOrder + 1;
    }
}
