using System;
using System.Collections;
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
    public const int Flexible = 1 << 4;

    public int[] values;
    public Suit suit;
    public int properties;
}

public sealed class DeckGenerator : MonoBehaviour
{
    private static (int property, Color color, string name)[] PropertyColors =
    {
        (CardData.Transparent, new Color(.35f, .85f, 1, 1), "Transparent"),
        (CardData.AutoPlay, new Color(.3f, 1, .35f, 1), "Auto Play"),
        (CardData.BonusTime, new Color(1, .8f, .15f, 1), "Bonus Time"),
        (CardData.WildCard, new Color(.55f, .55f, .55f, 1), "Wild Card"),
        (CardData.Flexible, new Color(1, .2f, .2f, 1), "Flexible")
    };

    private const int HandSize = 5;
    private const float HandSpacing = 1.5f;
    private const float HandY = -2.3f;
    private const float SpeedPileY = 1.3f;
    private const float DrawPileX = 6f;
    private const float DrawPileY = 0f;

    [SerializeField] private GameObject cardTemplate;
    [SerializeField] private Material transparentCardMaterial;
    [SerializeField] private Material wildCardMaterial;
    [SerializeField] private Material transparentWildCardMaterial;
    [SerializeField] private Material wildCardTextMaterial;

    private int numberOfPiles = 2;
    private Material[] cardMaterials;
    private Material[] textMaterials;
    private Dictionary<GameObject, CardData> cardData = new();
    private List<GameObject> handCards = new();
    private List<GameObject>[] piles;
    private List<GameObject> drawPile = new();
    private HashSet<GameObject> animatingCards = new();
    private GameObject selectedHandCard;
    private GameObject draggedCard;
    private bool reshuffledCurrentState;
    private bool cardsChanged;
    private float cardMoveDuration = .18f;

    private void Start()
    {
        List<CardData> deck = CreateDeck();
        Shuffle(deck);
        cardMaterials = new Material[]
        {
            cardTemplate.GetComponent<SpriteRenderer>().sharedMaterial,
            transparentCardMaterial,
            wildCardMaterial,
            transparentWildCardMaterial
        };
        textMaterials = new Material[]
        {
            cardTemplate.GetComponentInChildren<TMP_Text>(true).fontSharedMaterial,
            wildCardTextMaterial
        };
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
        cardsChanged = true;
    }

    private void HandleClicks()
    {
        if(draggedCard == null && !Mouse.current.leftButton.wasPressedThisFrame) return;
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        if(draggedCard != null)
        {
            if(Mouse.current.leftButton.isPressed)
            {
                draggedCard.transform.position = new Vector3(mousePosition.x, mousePosition.y, 0);
                return;
            }

            if(Mouse.current.leftButton.wasReleasedThisFrame)
            {
                for(int i = 0; i < piles.Length; i++)
                {
                    GameObject pileCard = piles[i][piles[i].Count - 1];
                    if(!pileCard.GetComponent<Collider2D>().OverlapPoint(mousePosition)) continue;
                    if(!CardsWork(draggedCard, i)) break;

                    PlayCard(draggedCard, i);
                    break;
                }

                draggedCard = null;
                return;
            }
        }

        if (!Mouse.current.leftButton.wasPressedThisFrame) return;

        Collider2D clickedCollider = Physics2D.OverlapPoint(mousePosition);
        if (clickedCollider == null) return;

        GameObject clickedCard = clickedCollider.gameObject;

        if (handCards.Contains(clickedCard))
        {
            selectedHandCard = clickedCard;
            draggedCard = clickedCard;
            return;
        }

        for (int i = 0; i < piles.Length; i++)
        {
            if (clickedCard == piles[i][piles[i].Count - 1])
            {
                if (selectedHandCard == null) return;

                if(!CardsWork(selectedHandCard, i)) return;

                PlayCard(selectedHandCard, i);
                return;
            }
        }

        if (drawPile.Count > 0 && clickedCard == drawPile[drawPile.Count - 1])
        {
            selectedHandCard = null;

            for (int i = 0; i < handCards.Count && drawPile.Count > 0; i++)
            {
                if (handCards[i] != null) continue;

                GameObject card = drawPile[drawPile.Count - 1];
                drawPile.RemoveAt(drawPile.Count - 1);
                handCards[i] = card;
                reshuffledCurrentState = false;
                cardsChanged = true;
            }
        }
   }

    private void PlayCard(GameObject card, int pileIndex)
    {
        handCards[handCards.IndexOf(card)] = null;
        piles[pileIndex].Add(card);
        StartCoroutine(AnimateCardToPile(card, pileIndex));
        if(selectedHandCard == card) selectedHandCard = null;
        reshuffledCurrentState = false;
        cardsChanged = true;
    }

    private void HandleAutoPlay()
    {
        bool playedCard = true;

        while(playedCard)
        {
            playedCard = false;
            for(int i = 0; i < handCards.Count; i++)
            {
                if(handCards[i] == null) continue;
                if((cardData[handCards[i]].properties & CardData.AutoPlay) == 0) continue;

                for(int j = 0; j < piles.Length; j++)
                {
                    if(!CardsWork(handCards[i], j)) continue;

                    GameObject card = handCards[i];
                    PlayCard(card, j);
                    playedCard = true;
                    break;
                }

                if(playedCard) break;
            }
        }

        cardsChanged = false;
    }

    private bool IsHandPlayable()
    {
        foreach(GameObject cardObj in handCards)
        {
            if(cardObj == null) continue;
            for(int i = 0; i < piles.Length; i++)
                if(CardsWork(cardObj, i)) return true;
        }
        return false;
    }

    private bool CardsWork(GameObject card, int pileIndex)
    {
        GameObject topCard = piles[pileIndex][GetEffectiveCardIndex(pileIndex)];

        if ((cardData[card].properties & CardData.WildCard) != 0
        || (cardData[topCard].properties & CardData.WildCard) != 0) return true;

        int differenceLimit = 2;
        if((cardData[card].properties & CardData.Flexible) != 0
        || (cardData[topCard].properties & CardData.Flexible) != 0)
            differenceLimit = 3;

        foreach (int value1 in cardData[card].values)
        {
            foreach(int value2 in cardData[topCard].values)
            {
                int difference = Mathf.Abs(value1 - value2);
                int cyclicDifference = Mathf.Min(difference, 13 - difference);
                if(cyclicDifference > 0 && cyclicDifference < differenceLimit) return true;
            }
        }
        return false;
    }

    private void HandleReShuffle()
    {
        if(handCards.Contains(null) && drawPile.Count > 0) return;

        if(IsHandPlayable())
        {
            reshuffledCurrentState = false;
            return;
        }

        if(reshuffledCurrentState) return;
        reshuffledCurrentState = true;

        foreach(List<GameObject> pile in piles) Shuffle(pile);
        cardsChanged = true;

        bool foundPlayableTop = IsHandPlayable();
        for(int i = 0; i < piles.Length && !foundPlayableTop; i++)
        {
            for(int j = 0; j < piles[i].Count && !foundPlayableTop; j++)
            {
                GameObject pileCard = piles[i][j];
                piles[i].RemoveAt(j);
                piles[i].Add(pileCard);

                foreach(GameObject handCard in handCards)
                {
                    if(handCard == null) continue;
                    if(CardsWork(handCard, i))
                    {
                        foundPlayableTop = true;
                        break;
                    }
                }

                if(!foundPlayableTop)
                {
                    piles[i].RemoveAt(piles[i].Count - 1);
                    piles[i].Insert(j, pileCard);
                }
            }
        }
    }

    private void Update()
    {
        HandleClicks();
        if(cardsChanged) HandleAutoPlay();
        HandleReShuffle();
        if(cardsChanged) HandleAutoPlay();
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
            if(handCards[i] != draggedCard)
                handCards[i].transform.position = new Vector3(firstCardX + i * HandSpacing, HandY, 0);
            SetSortingOrder(handCards[i], handCards[i] == draggedCard ? 1000 : 0);
            handCards[i].GetComponent<Collider2D>().enabled = handCards[i] != draggedCard;
            handCards[i].GetComponentInChildren<Canvas>(true).enabled = true;
        }

        for(int i = 0; i < piles.Length; i++)
        {
            Vector3 pilePosition = GetPilePosition(i);
            int effectiveCardIndex = GetEffectiveCardIndex(i);

            for(int j = 0; j < piles[i].Count; j++)
            {
                bool isTopCard = j == piles[i].Count - 1;
                bool isEffectiveCard = j == effectiveCardIndex;
                if(!animatingCards.Contains(piles[i][j]))
                    piles[i][j].transform.position = pilePosition;
                SetSortingOrder(piles[i][j], j + 10);
                piles[i][j].GetComponent<Collider2D>().enabled =
                    isTopCard && !animatingCards.Contains(piles[i][j]);
                piles[i][j].GetComponentInChildren<Canvas>(true).enabled =
                    isTopCard || isEffectiveCard;

                TMP_Text[] labels = piles[i][j].GetComponentsInChildren<TMP_Text>(true);
                for(int k = 0; k < labels.Length; k++)
                {
                    if(labels[k].gameObject.name == "Card Text")
                        labels[k].enabled = isEffectiveCard
                            || (isTopCard
                            && (cardData[piles[i][j]].properties & CardData.WildCard) != 0);
                    else if(labels[k].gameObject.name == "Property Text")
                        labels[k].enabled = isTopCard;
                }
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

    private IEnumerator AnimateCardToPile(GameObject card, int pileIndex)
    {
        animatingCards.Add(card);
        Vector3 startPosition = card.transform.position;
        Vector3 endPosition = GetPilePosition(pileIndex);
        float time = 0;

        while(time < cardMoveDuration)
        {
            time += Time.deltaTime;
            float amount = Mathf.Clamp01(time / cardMoveDuration);
            amount = amount * amount * (3 - 2 * amount);
            card.transform.position = Vector3.Lerp(startPosition, endPosition, amount);
            yield return null;
        }

        card.transform.position = endPosition;
        animatingCards.Remove(card);
    }

    private Vector3 GetPilePosition(int pileIndex)
    {
        float x = (pileIndex - (numberOfPiles - 1) * .5f) * 2;
        return new Vector3(x, SpeedPileY, 0);
    }

    private int GetEffectiveCardIndex(int pileIndex)
    {
        int i = piles[pileIndex].Count - 1;
        if((cardData[piles[pileIndex][i]].properties & CardData.Transparent) != 0
        && i > 0) i--;
        return i;
    }

    private static List<CardData> CreateDeck()
    {
        var deck = new List<CardData>();
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            for (int i = 1; i <= 13; i++)
            {
                int properties = 0;
                for(int j = 0; j < PropertyColors.Length; j++)
                    if(UnityEngine.Random.Range(0, 5) == 1)
                        properties |= PropertyColors[j].property;

                deck.Add(new CardData
                {
                    values = new[] { i },
                    suit = suit,
                    properties = properties
                });
            }

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
            if((data.properties & CardData.Flexible) != 0) label.text += " +-1";
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;
            label.rectTransform.anchoredPosition = Vector2.zero;

            TMP_Text propertyLabel = Instantiate(label, label.transform.parent);
            propertyLabel.gameObject.name = "Property Text";
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
        int transparentIndex =
            (cardData[card].properties & CardData.Transparent) / CardData.Transparent;
        int wildCardIndex =
            (cardData[card].properties & CardData.WildCard) / CardData.WildCard;
        int cardMaterialIndex = transparentIndex + wildCardIndex * 2;

        cardRenderer.sharedMaterial = cardMaterials[cardMaterialIndex];
        cardRenderer.color = GetCardColor(cardData[card]);
        cardRenderer.sortingOrder = cardOrder;
        highlightRenderer.sortingOrder = cardOrder - 1;
        highlightRenderer.enabled = card == selectedHandCard;

        TMP_Text[] labels = card.GetComponentsInChildren<TMP_Text>(true);
        for(int i = 0; i < labels.Length; i++)
        {
            if(labels[i].gameObject.name == "Card Text")
            {
                labels[i].enabled = true;
                labels[i].fontSharedMaterial = textMaterials[wildCardIndex];
                labels[i].color = Color.black;
            }
            else if(labels[i].gameObject.name == "Property Text")
            {
                labels[i].enabled = true;
                labels[i].fontSharedMaterial = textMaterials[wildCardIndex];
                labels[i].text = GetPropertyText(cardData[card].properties);
            }
        }

        card.GetComponentInChildren<Canvas>(true).sortingOrder = cardOrder + 2;
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

        return color / propertyCount;
    }
}
