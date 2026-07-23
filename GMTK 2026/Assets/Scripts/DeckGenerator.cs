using System;
using System.Collections;
using System.Collections.Generic;
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
    private static (int property, string seal, Vector3 sealPosition)[] Properties =
    {
        (CardData.Transparent, null, Vector3.zero),
        (CardData.AutoPlay, "Autoplay", new Vector3(-.2f, -.1f, -.01f)),
        (CardData.BonusTime, "Bonus Time", new Vector3(0, -.45f, -.01f)),
        (CardData.WildCard, null, Vector3.zero),
        (CardData.Flexible, "+-1", new Vector3(.2f, -.1f, -.01f))
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

    private int numberOfPiles = 2;
    private Material[] cardMaterials;
    private Sprite[] cardSprites;
    private Sprite cardBack;
    private Dictionary<int, Sprite> propertySeals = new();
    private Dictionary<GameObject, CardData> cardData = new();
    private Dictionary<GameObject, Sprite> cardFaces = new();
    private List<GameObject> handCards = new();
    private List<GameObject>[] piles;
    private List<GameObject> drawPile = new();
    private HashSet<GameObject> animatingCards = new();
    private GameObject selectedHandCard;
    private GameObject pressedCard;
    private GameObject draggedCard;
    private Vector2 pressedPosition;
    private bool reshuffledCurrentState;
    private bool cardsChanged;
    private float cardMoveDuration = .18f;
    private float dragThreshold = .15f;

    private void Start()
    {
        List<CardData> deck = CreateDeck();
        Shuffle(deck);
        cardSprites = Resources.LoadAll<Sprite>("ClassicCards");
        cardBack = Resources.LoadAll<Sprite>("LightClassic")[0];
        for(int i = 0; i < Properties.Length; i++)
            if(Properties[i].seal != null)
            {
                Sprite[] seals = Resources.LoadAll<Sprite>(Properties[i].seal);
                if(seals.Length > 0)
                    propertySeals.Add(Properties[i].property, seals[0]);
            }
        cardMaterials = new Material[]
        {
            cardTemplate.GetComponent<SpriteRenderer>().sharedMaterial,
            transparentCardMaterial,
            wildCardMaterial,
            transparentWildCardMaterial
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
        if(draggedCard == null && pressedCard == null
        && !Mouse.current.leftButton.wasPressedThisFrame) return;
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        if(pressedCard != null)
        {
            if(Mouse.current.leftButton.isPressed
            && Vector2.Distance(mousePosition, pressedPosition) > dragThreshold)
            {
                draggedCard = pressedCard;
                pressedCard = null;
                selectedHandCard = null;
            }
            else if(Mouse.current.leftButton.wasReleasedThisFrame)
            {
                selectedHandCard = pressedCard;
                pressedCard = null;
                return;
            }
        }

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
            pressedCard = clickedCard;
            pressedPosition = mousePosition;
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
                cardsChanged = false;
                StartCoroutine(AnimateCardToHand(card, i));
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
        cardsChanged = (cardData[card].properties & CardData.Transparent) == 0;
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
                if(animatingCards.Contains(handCards[i])) continue;
                if((cardData[handCards[i]].properties & CardData.AutoPlay) == 0) continue;

                for(int j = 0; j < piles.Length; j++)
                {
                    if(!CardsWork(handCards[i], j)) continue;

                    GameObject card = handCards[i];
                    PlayCard(card, j);
                    if((cardData[card].properties & CardData.Transparent) != 0) return;
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
        if(animatingCards.Count > 0) return;
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
        for(int i = 0; i < handCards.Count; i++)
        {
            if(handCards[i] == null) continue;
            if(handCards[i] != draggedCard && !animatingCards.Contains(handCards[i]))
            {
                Vector3 handPosition = GetHandPosition(i);
                if(handCards[i] == selectedHandCard) handPosition.y += .35f;
                float amount = 1 - Mathf.Exp(-18 * Time.deltaTime);
                handCards[i].transform.position =
                    Vector3.Lerp(handCards[i].transform.position, handPosition, amount);
            }
            int sortingOrder = handCards[i] == draggedCard ? 1000 :
                handCards[i] == selectedHandCard ? 100 : 0;
            SetSortingOrder(handCards[i], sortingOrder);
            handCards[i].GetComponent<Collider2D>().enabled =
                handCards[i] != draggedCard && !animatingCards.Contains(handCards[i]);
        }

        for(int i = 0; i < piles.Length; i++)
        {
            Vector3 pilePosition = GetPilePosition(i);

            for(int j = 0; j < piles[i].Count; j++)
            {
                bool isTopCard = j == piles[i].Count - 1;
                if(!animatingCards.Contains(piles[i][j]))
                    piles[i][j].transform.position = pilePosition;
                SetSortingOrder(piles[i][j], j + 10);
                piles[i][j].GetComponent<Collider2D>().enabled =
                    isTopCard && !animatingCards.Contains(piles[i][j]);
            }
        }

        for(int i = 0; i < drawPile.Count; i++)
        {
            bool isTopCard = i == drawPile.Count - 1;
            drawPile[i].transform.position = new Vector3(DrawPileX, DrawPileY, 0);
            SetSortingOrder(drawPile[i], i);
            SpriteRenderer drawRenderer = drawPile[i].GetComponent<SpriteRenderer>();
            drawRenderer.sprite = cardBack;
            drawRenderer.sharedMaterial = cardMaterials[0];
            SpriteRenderer[] seals =
                drawPile[i].GetComponentsInChildren<SpriteRenderer>(true);
            for(int j = 0; j < seals.Length; j++)
                if(seals[j].gameObject != drawPile[i]) seals[j].enabled = false;
            drawPile[i].GetComponent<Collider2D>().enabled = isTopCard;
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
        if((cardData[card].properties & CardData.Transparent) != 0)
        {
            SpriteRenderer[] renderers =
                card.GetComponentsInChildren<SpriteRenderer>(true);
            time = 0;

            while(time < .25f)
            {
                time += Time.deltaTime;
                float amount = Mathf.Clamp01(time / .25f);
                for(int i = 0; i < renderers.Length; i++)
                    renderers[i].color = new Color(1, 1, 1, 1 - amount);
                yield return null;
            }

            piles[pileIndex].Remove(card);
            cardData.Remove(card);
            cardFaces.Remove(card);
            animatingCards.Remove(card);
            Destroy(card);
            cardsChanged = true;
            yield break;
        }

        animatingCards.Remove(card);
    }

    private IEnumerator AnimateCardToHand(GameObject card, int handIndex)
    {
        animatingCards.Add(card);
        Vector3 startPosition = card.transform.position;
        Vector3 endPosition = GetHandPosition(handIndex);
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
        cardsChanged = true;
    }

    private Vector3 GetPilePosition(int pileIndex)
    {
        float x = (pileIndex - (numberOfPiles - 1) * .5f) * 2;
        return new Vector3(x, SpeedPileY, 0);
    }

    private Vector3 GetHandPosition(int handIndex)
    {
        float x = -(HandSize - 1) * HandSpacing * .5f + handIndex * HandSpacing;
        return new Vector3(x, HandY, 0);
    }

    private int GetEffectiveCardIndex(int pileIndex)
    {
        int i = piles[pileIndex].Count - 1;
        while((cardData[piles[pileIndex][i]].properties & CardData.Transparent) != 0
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
                for(int j = 0; j < Properties.Length; j++)
                    if(UnityEngine.Random.Range(0, 5) == 1)
                        properties |= Properties[j].property;

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
        card.transform.localScale = new Vector3(card.transform.localScale.x, card.transform.localScale.x, 1);

        card.name = $"Card {cardData.Count + 1}";
        cardData.Add(card, data);

        SpriteRenderer cardRenderer = card.GetComponent<SpriteRenderer>();
        int suitRow = data.suit switch
        {
            Suit.Heart => 3,
            Suit.Club => 1,
            Suit.Diamond => 0,
            Suit.Spade => 2,
            _ => 0
        };

        for(int i = 0; i < cardSprites.Length; i++)
        {
            int x = Mathf.RoundToInt(cardSprites[i].rect.x / 24);
            int y = Mathf.RoundToInt(cardSprites[i].rect.y / 36);
            if(x != data.values[0] - 1 || y != suitRow) continue;
            cardRenderer.sprite = cardSprites[i];
            break;
        }
        cardRenderer.drawMode = SpriteDrawMode.Simple;
        cardFaces.Add(card, cardRenderer.sprite);

        Destroy(card.GetComponentInChildren<Canvas>(true).gameObject);

        for(int i = 0; i < Properties.Length; i++)
        {
            if(Properties[i].seal == null
            || !propertySeals.ContainsKey(Properties[i].property)) continue;
            GameObject seal = new GameObject($"Seal {Properties[i].property}");
            seal.transform.SetParent(card.transform, false);
            seal.transform.localPosition = Properties[i].sealPosition;
            seal.transform.localScale = new Vector3(1.2f, 1.2f, 1);
            SpriteRenderer sealRenderer = seal.AddComponent<SpriteRenderer>();
            sealRenderer.sprite = propertySeals[Properties[i].property];
            sealRenderer.sharedMaterial = cardMaterials[0];
            sealRenderer.enabled = false;
        }

        if (card.GetComponent<Collider2D>() == null) card.AddComponent<BoxCollider2D>();

        card.SetActive(true);
        return card;
    }

    private void SetSortingOrder(GameObject card, int sortingOrder)
    {
        int cardOrder = sortingOrder * 3 + 10;
        SpriteRenderer cardRenderer = card.GetComponent<SpriteRenderer>();
        int transparentIndex =
            (cardData[card].properties & CardData.Transparent) / CardData.Transparent;
        int wildCardIndex =
            (cardData[card].properties & CardData.WildCard) / CardData.WildCard;
        int cardMaterialIndex = transparentIndex + wildCardIndex * 2;

        cardRenderer.sprite = cardFaces[card];
        cardRenderer.sharedMaterial = cardMaterials[cardMaterialIndex];
        if(!animatingCards.Contains(card)) cardRenderer.color = Color.white;
        cardRenderer.sortingOrder = cardOrder;
        for(int i = 0; i < Properties.Length; i++)
        {
            if(Properties[i].seal == null
            || !propertySeals.ContainsKey(Properties[i].property)) continue;
            SpriteRenderer sealRenderer = card.transform
                .Find($"Seal {Properties[i].property}").GetComponent<SpriteRenderer>();
            if(!animatingCards.Contains(card)) sealRenderer.color = Color.white;
            sealRenderer.sortingOrder = cardOrder + 1;
            sealRenderer.enabled =
                (cardData[card].properties & Properties[i].property) != 0;
            sealRenderer.transform.localPosition = Properties[i].sealPosition;
        }

    }

}
