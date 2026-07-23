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
    public int[] values;
    public Suit suit;
    public bool isTransparent;
    public bool isAutoPlay;
    public bool isBonusTime;
    public bool isWildCard;
    public bool isBomb;
}

public sealed class DeckGenerator : MonoBehaviour
{
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

        for (int slot = 0; slot < HandSize; slot++)
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
            if (clickedCard == piles[i][piles[i].Count - 1])
            {
                if (selectedHandCard == null) return;

                if(!CardsWork(selectedHandCard, piles[i][piles[i].Count - 1])) return;

                selectedHandCard.GetComponent<SpriteRenderer>().color = Color.white;
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
            {
                selectedHandCard.GetComponent<SpriteRenderer>().color = Color.white;
                selectedHandCard = null;
            }

            for (int slot = 0; slot < handCards.Count && drawPile.Count > 0; slot++)
            {
                if (handCards[slot] != null) continue;

                GameObject card = drawPile[^1];
                drawPile.RemoveAt(drawPile.Count - 1);
                handCards[slot] = card;
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
        for(int pileIndex = 0; pileIndex < piles.Length && !foundPlayableTop; pileIndex++)
        {
            for(int cardIndex = 0; cardIndex < piles[pileIndex].Count && !foundPlayableTop; cardIndex++)
            {
                GameObject pileCard = piles[pileIndex][cardIndex];
                foreach(GameObject handCard in handCards)
                {
                    if(handCard == null) continue;
                    if(CardsWork(handCard, pileCard))
                    {
                        piles[pileIndex].RemoveAt(cardIndex);
                        piles[pileIndex].Add(pileCard);
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
            for (int value = 1; value <= 13; value++)
                deck.Add(new CardData
                {
                    values = new[] { value },
                    suit = suit
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
        if (label != null) label.text = rank + suit;
        if (card.GetComponent<Collider2D>() == null) card.AddComponent<BoxCollider2D>();

        card.SetActive(true);
        return card;
    }

    private void SetSortingOrder(GameObject card, int sortingOrder)
    {
        card.GetComponent<SpriteRenderer>().sortingOrder = sortingOrder;
        card.GetComponentInChildren<Canvas>(true).sortingOrder = sortingOrder + 1;
    }
}
