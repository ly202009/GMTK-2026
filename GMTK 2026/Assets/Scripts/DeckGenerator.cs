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

public class ModifierIdleMotion : MonoBehaviour
{
    private static int nextPhase;
    private Vector3 startPosition;
    private float phase;

    private void Start()
    {
        startPosition = transform.localPosition;
        phase = nextPhase++ * .35f;
    }

    private void LateUpdate()
    {
        float movement = Time.time * 2 + phase;
        transform.localPosition = startPosition
            + Vector3.up * Mathf.Sin(movement) * .035f;
        transform.localRotation =
            Quaternion.Euler(0, 0, Mathf.Cos(movement) * 3.2f);
    }
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

    private const float HandSpacing = 1.5f;
    private const float HandY = -2.3f;
    private const float SpeedPileY = 1.3f;
    private const float DrawPileX = 6f;
    private const float DrawPileY = 0f;

    [SerializeField] private GameObject cardTemplate;
    [SerializeField] private Material transparentCardMaterial;
    [SerializeField] private Material wildCardMaterial;
    [SerializeField] private Material transparentWildCardMaterial;
    [SerializeField] private TMP_Text powerupText;
    [SerializeField] private RectTransform powerupPanel;
    [SerializeField] private TMP_Text drawPileCountText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private RectTransform comboPanel;
    [SerializeField] private CanvasGroup comboGroup;

    private Material[] cardMaterials;
    private Sprite[] cardSprites;
    private Sprite cardBack;
    private Dictionary<int, Sprite> propertySeals = new();
    private Dictionary<GameObject, CardData> cardData = new();
    private Dictionary<GameObject, Sprite> cardFaces = new();
    private List<GameObject> handCards = new();
    private List<List<GameObject>> piles = new();
    private List<GameObject> drawPile = new();
    private HashSet<GameObject> animatingCards = new();
    private HashSet<GameObject> jumpingCards = new();
    private GameObject selectedHandCard;
    private GameObject pressedCard;
    private GameObject draggedCard;
    private Vector2 pressedPosition;
    private bool reshuffledCurrentState;
    private bool cardsChanged;
    private bool movingToShop;
    private int shownDrawPileCount = -1;
    private float drawPileCountPunch;
    private int numberOfPiles;
    private int handSize;
    private bool autoDraw;
    private bool usedDoubles;
    private bool usedSuitMatching;
    private float doublesCountdown;
    private float suitMatchingCountdown;
    private float powerupDuration = 10;
    private float cardMoveDuration = .18f;
    private float dragThreshold = .15f;
    private int combo;
    private float comboTime;
    private float comboWindow = 3.5f;
    private Coroutine comboAnimation;
    private Vector2 comboPosition;

    private void Start()
    {
        numberOfPiles = RunData.instance.numberOfPiles;
        handSize = RunData.instance.handSize;
        autoDraw = RunData.instance.autoDraw;
        UpdatePowerupUI();
        comboPosition = comboPanel.anchoredPosition;
        comboGroup.alpha = 0;

        List<CardData> deck = new(RunData.instance.deck);
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

        int deckIndex = 0;

        for (int i = 0; i < handSize; i++)
        {
            handCards.Add(CreateCard(deck[deckIndex]));
            deckIndex++;
        }

        for (int i = 0; i < numberOfPiles; i++)
        {
            piles.Add(new List<GameObject>());
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
                StartCoroutine(AnimateSelectionJump(pressedCard));
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
                for(int i = 0; i < piles.Count; i++)
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

        for (int i = 0; i < piles.Count; i++)
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
            DrawCards();
   }

    private void PlayCard(GameObject card, int pileIndex)
    {
        if(comboTime <= 0) combo = 0;
        combo++;
        comboTime = comboWindow;
        bool bonusTime =
            (cardData[card].properties & CardData.BonusTime) != 0;
        int timeGain = combo <= 1 ? 0 :
            Mathf.CeilToInt((combo - 1) / 4f);
        if(bonusTime) timeGain++;
        RunData.instance.countdown += timeGain;
        if(comboAnimation != null) StopCoroutine(comboAnimation);
        comboAnimation = StartCoroutine(ShowCombo(timeGain, bonusTime));

        handCards[handCards.IndexOf(card)] = null;
        piles[pileIndex].Add(card);
        StartCoroutine(AnimateCardToPile(card, pileIndex));
        if(selectedHandCard == card) selectedHandCard = null;
        reshuffledCurrentState = false;
        cardsChanged = (cardData[card].properties & CardData.Transparent) == 0;
        if(autoDraw) DrawCards();
    }

    private void DrawCards()
    {
        selectedHandCard = null;
        for(int i = 0; i < handCards.Count && drawPile.Count > 0; i++)
        {
            if(handCards[i] != null) continue;

            GameObject card = drawPile[drawPile.Count - 1];
            drawPile.RemoveAt(drawPile.Count - 1);
            handCards[i] = card;
            reshuffledCurrentState = false;
            cardsChanged = false;
            StartCoroutine(AnimateCardToHand(card, i));
        }
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

                for(int j = 0; j < piles.Count; j++)
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
            for(int i = 0; i < piles.Count; i++)
                if(CardsWork(cardObj, i)) return true;
        }
        return false;
    }

    private bool CardsWork(GameObject card, int pileIndex)
    {
        GameObject topCard = piles[pileIndex][GetEffectiveCardIndex(pileIndex)];

        if ((cardData[card].properties & CardData.WildCard) != 0
        || (cardData[topCard].properties & CardData.WildCard) != 0) return true;

        if(suitMatchingCountdown > 0
        && cardData[card].suit == cardData[topCard].suit)
            return true;

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
                if(doublesCountdown > 0 && cyclicDifference == 0) return true;
                if(cyclicDifference > 0 && cyclicDifference < differenceLimit) return true;
            }
        }
        return false;
    }

    private void HandleReShuffle()
    {
        if(animatingCards.Count > 0) return;
        if(!handCards.Exists(i => i != null)) return;
        if(handCards.Contains(null) && drawPile.Count > 0) return;

        if(IsHandPlayable())
        {
            reshuffledCurrentState = false;
            return;
        }

        if(reshuffledCurrentState) return;
        reshuffledCurrentState = true;
        StartCoroutine(ShakeAndReShuffle());
    }

    private IEnumerator ShakeAndReShuffle()
    {
        Transform cameraTransform = Camera.main.transform;
        Vector3 cameraPosition = cameraTransform.position;
        float time = 0;
        while(time < .28f)
        {
            time += Time.deltaTime;
            float strength = (1 - time / .28f) * .12f;
            Vector2 offset = UnityEngine.Random.insideUnitCircle * strength;
            cameraTransform.position = cameraPosition
                + new Vector3(offset.x, offset.y, 0);
            yield return null;
        }
        cameraTransform.position = cameraPosition;

        if(IsHandPlayable())
        {
            reshuffledCurrentState = false;
            yield break;
        }

        if(RunData.instance.handInvalidGain)
            RunData.instance.countdown += 3;
        foreach(List<GameObject> pile in piles) Shuffle(pile);
        cardsChanged = true;

        bool foundPlayableTop = IsHandPlayable();
        for(int i = 0; i < piles.Count && !foundPlayableTop; i++)
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

        int pileIndex = 0;
        while(!foundPlayableTop && drawPile.Count > 0)
        {
            int cardsToMove = Mathf.Min(2, drawPile.Count);
            for(int i = 0; i < cardsToMove; i++)
            {
                GameObject card = drawPile[drawPile.Count - 1];
                drawPile.RemoveAt(drawPile.Count - 1);
                piles[pileIndex].Add(card);
                pileIndex = (pileIndex + 1) % piles.Count;
            }
            foundPlayableTop = IsHandPlayable();
        }
    }

    private IEnumerator ShowCombo(int timeGain, bool bonusTime)
    {
        comboText.text = timeGain > 0 ?
            $"{combo}x COMBO\n<color=#{(bonusTime ? "FFE45C" : "71FF8D")}>"
            + $"+{timeGain} SECOND{(timeGain == 1 ? "" : "S")}</color>" :
            "1x COMBO";
        Color comboColor = bonusTime ?
            new Color(1, .78f, .12f) : new Color(.55f, 1, .65f);
        comboGroup.alpha = 1;
        comboPanel.anchoredPosition = comboPosition - Vector2.up * 24;
        comboPanel.localScale = Vector3.one * .35f;
        float direction = combo % 2 == 0 ? -1 : 1;
        comboPanel.localRotation = Quaternion.Euler(0, 0, direction * 8);

        float time = 0;
        while(time < .16f)
        {
            time += Time.unscaledDeltaTime;
            float amount = Mathf.Clamp01(time / .16f);
            amount = 1 - Mathf.Pow(1 - amount, 3);
            comboPanel.localScale =
                Vector3.one * Mathf.Lerp(.35f, 1.24f, amount);
            comboPanel.anchoredPosition = Vector2.Lerp(
                comboPosition - Vector2.up * 24, comboPosition, amount);
            comboPanel.localRotation = Quaternion.Lerp(
                Quaternion.Euler(0, 0, direction * 8),
                Quaternion.Euler(0, 0, direction * -2), amount);
            comboText.color = Color.Lerp(Color.white, comboColor, amount);
            yield return null;
        }

        time = 0;
        while(time < .42f)
        {
            time += Time.unscaledDeltaTime;
            float bounce = Mathf.Sin(time * 20) * Mathf.Exp(-time * 9);
            comboPanel.localScale = Vector3.one * (1 + bounce * .11f);
            comboPanel.localRotation =
                Quaternion.Euler(0, 0, bounce * direction * 3);
            yield return null;
        }

        time = 0;
        while(time < .3f)
        {
            time += Time.unscaledDeltaTime;
            float amount = Mathf.Clamp01(time / .3f);
            comboGroup.alpha = 1 - amount;
            comboPanel.anchoredPosition =
                comboPosition + new Vector2(direction * amount * 16,
                    amount * 58);
            comboPanel.localScale = Vector3.one * (1 + amount * .08f);
            yield return null;
        }

        comboGroup.alpha = 0;
        comboPanel.anchoredPosition = comboPosition;
        comboPanel.localScale = Vector3.one;
        comboPanel.localRotation = Quaternion.identity;
        comboAnimation = null;
    }

    private void Update()
    {
        if(comboTime > 0)
        {
            comboTime -= Time.unscaledDeltaTime;
            if(comboTime <= 0) combo = 0;
        }
        HandlePowerups();
        HandleClicks();
        if(cardsChanged) HandleAutoPlay();
        HandleReShuffle();
        if(cardsChanged) HandleAutoPlay();

        if(movingToShop || drawPile.Count > 0 || animatingCards.Count > 0) return;
        foreach(GameObject card in handCards)
            if(card != null) return;
        movingToShop = true;
        RunData.instance.countdown += 90;
        SceneTransition.Load("ShopScene");
    }

    private void HandlePowerups()
    {
        if(!usedSuitMatching && RunData.instance.allowSuitMatching
        && Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            usedSuitMatching = true;
            suitMatchingCountdown = powerupDuration;
        }

        if(!usedDoubles && RunData.instance.allowDoubles
        && Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            usedDoubles = true;
            doublesCountdown = powerupDuration;
        }

        if(suitMatchingCountdown > 0)
            suitMatchingCountdown = Mathf.Max(0,
                suitMatchingCountdown - Time.deltaTime);

        if(doublesCountdown > 0)
            doublesCountdown = Mathf.Max(0, doublesCountdown - Time.deltaTime);

        UpdatePowerupUI();
    }

    private void UpdatePowerupUI()
    {
        List<string> lines = new();
        AddPowerupLine(lines, RunData.instance.allowSuitMatching, "1",
            "Suit Matching", usedSuitMatching, suitMatchingCountdown);
        AddPowerupLine(lines, RunData.instance.allowDoubles, "2",
            "Doubles", usedDoubles, doublesCountdown);

        powerupText.text = string.Join("\n", lines);
        powerupPanel.sizeDelta = new Vector2(520, lines.Count * 40 + 20);
    }

    private void AddPowerupLine(List<string> lines, bool allowed, string key,
        string name, bool used, float countdown)
    {
        if(!allowed) return;
        string status = !used ? "<color=#6CFF72>READY</color>" :
            countdown > 0 ? $"<color=#FFE066>{countdown:0.0}s</color>" :
            "<color=#888888>USED</color>";
        lines.Add($"[{key}] {name}  -  {status}");
    }

    private void LateUpdate()
    {
        RenderCards();
    }

    private void RenderCards()
    {
        Vector2 mousePosition =
            Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D hoveredCollider = Physics2D.OverlapPoint(mousePosition);

        for(int i = 0; i < handCards.Count; i++)
        {
            if(handCards[i] == null) continue;
            if(handCards[i] != draggedCard && !animatingCards.Contains(handCards[i])
            && !jumpingCards.Contains(handCards[i]))
            {
                Vector3 handPosition = GetHandPosition(i);
                float fan = i - (handCards.Count - 1) * .5f;
                handPosition.y -= Mathf.Abs(fan) * .018f;
                if(handCards[i] == selectedHandCard)
                    handPosition.y += .35f
                        + Mathf.Sin(Time.time * 5) * .008f;
                else if(hoveredCollider != null
                && hoveredCollider.gameObject == handCards[i])
                    handPosition.y += .08f
                        + Mathf.Sin(Time.time * 7) * .008f;
                float amount = 1 - Mathf.Exp(-18 * Time.deltaTime);
                handCards[i].transform.position =
                    Vector3.Lerp(handCards[i].transform.position, handPosition, amount);
                bool tilted = hoveredCollider != null
                    && hoveredCollider.gameObject == handCards[i];
                BoxCollider2D box =
                    handCards[i].GetComponent<BoxCollider2D>();
                float width = box.size.x
                    * Mathf.Abs(handCards[i].transform.lossyScale.x);
                float height = box.size.y
                    * Mathf.Abs(handCards[i].transform.lossyScale.y);
                float x = Mathf.Clamp(
                    (mousePosition.x - handCards[i].transform.position.x)
                    / (width * .5f), -1, 1);
                float y = Mathf.Clamp(
                    (mousePosition.y - handCards[i].transform.position.y)
                    / (height * .5f), -1, 1);
                Quaternion fanRotation = Quaternion.Euler(0, 0, fan * -1.35f);
                Quaternion rotation = tilted ?
                    Quaternion.Euler(y * 14, x * -14, 0) * fanRotation :
                    fanRotation;
                handCards[i].transform.rotation = Quaternion.Lerp(
                    handCards[i].transform.rotation, rotation,
                    1 - Mathf.Exp(-15 * Time.deltaTime));
            }
            int sortingOrder = handCards[i] == draggedCard ? 1000 :
                handCards[i] == selectedHandCard ? 100 : 0;
            SetSortingOrder(handCards[i], sortingOrder);
            handCards[i].GetComponent<Collider2D>().enabled =
                handCards[i] != draggedCard && !animatingCards.Contains(handCards[i])
                && !jumpingCards.Contains(handCards[i]);
        }

        for(int i = 0; i < piles.Count; i++)
        {
            Vector3 pilePosition = GetPilePosition(i);

            for(int j = 0; j < piles[i].Count; j++)
            {
                bool isTopCard = j == piles[i].Count - 1;
                if(!animatingCards.Contains(piles[i][j]))
                {
                    float idle = Time.time * 1.7f + i * 1.5f;
                    Vector3 idlePosition = isTopCard ?
                        new Vector3(Mathf.Cos(idle) * .018f,
                            Mathf.Sin(idle) * .055f, 0) : Vector3.zero;
                    piles[i][j].transform.position =
                        pilePosition + idlePosition;
                    piles[i][j].transform.localRotation = isTopCard ?
                        Quaternion.Euler(0, 0, Mathf.Sin(idle) * 1.4f) :
                        Quaternion.identity;
                }
                SetSortingOrder(piles[i][j], j + 10);
                piles[i][j].GetComponent<Collider2D>().enabled =
                    isTopCard && !animatingCards.Contains(piles[i][j]);
            }
        }

        for(int i = 0; i < drawPile.Count; i++)
        {
            bool isTopCard = i == drawPile.Count - 1;
            float idle = Time.time * 1.5f;
            Vector3 idlePosition = isTopCard ?
                new Vector3(Mathf.Cos(idle) * .022f,
                    Mathf.Sin(idle) * .07f, 0) : Vector3.zero;
            drawPile[i].transform.position =
                new Vector3(DrawPileX, DrawPileY, 0) + idlePosition;
            drawPile[i].transform.localRotation = isTopCard ?
                Quaternion.Euler(0, 0, Mathf.Sin(idle) * 1.7f) :
                Quaternion.identity;
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

        Transform drawCount = drawPileCountText.transform.parent;
        drawCount.position = Camera.main.WorldToScreenPoint(
            new Vector3(DrawPileX, DrawPileY + 1.5f, 0));
        if(shownDrawPileCount != drawPile.Count)
        {
            shownDrawPileCount = drawPile.Count;
            drawPileCountPunch = .18f;
        }
        drawPileCountPunch = Mathf.MoveTowards(drawPileCountPunch, 0,
            Time.deltaTime * 1.4f);
        drawCount.localScale = Vector3.one * (1 + drawPileCountPunch);
        drawPileCountText.color = drawPile.Count <= 5 ?
            new Color(1, .45f, .12f) : Color.white;
        drawPileCountText.text = drawPile.Count.ToString();
    }

    private IEnumerator AnimateCardToPile(GameObject card, int pileIndex)
    {
        animatingCards.Add(card);
        Vector3 startPosition = card.transform.position;
        Vector3 endPosition = GetPilePosition(pileIndex);
        Quaternion startRotation = card.transform.localRotation;
        Vector3 normalScale = card.transform.localScale;
        float time = 0;

        while(time < cardMoveDuration)
        {
            time += Time.deltaTime;
            float amount = Mathf.Clamp01(time / cardMoveDuration);
            amount = amount * amount * (3 - 2 * amount);
            Vector3 position = Vector3.Lerp(startPosition, endPosition, amount);
            position.y += Mathf.Sin(amount * Mathf.PI) * .25f;
            card.transform.position = position;
            card.transform.localScale = normalScale
                * (1 + Mathf.Sin(amount * Mathf.PI) * .045f);
            card.transform.localRotation =
                Quaternion.Lerp(startRotation, Quaternion.identity, amount);
            yield return null;
        }

        card.transform.position = endPosition;
        card.transform.localScale = normalScale;
        card.transform.localRotation = Quaternion.identity;
        time = 0;
        while(time < .08f)
        {
            time += Time.deltaTime;
            float amount = Mathf.Clamp01(time / .08f);
            card.transform.localScale = normalScale
                * (1 + Mathf.Sin(amount * Mathf.PI) * .06f);
            yield return null;
        }
        card.transform.localScale = normalScale;
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
        Vector3 normalScale = card.transform.localScale;
        float time = 0;

        while(time < cardMoveDuration)
        {
            time += Time.deltaTime;
            float amount = Mathf.Clamp01(time / cardMoveDuration);
            amount = amount * amount * (3 - 2 * amount);
            Vector3 position = Vector3.Lerp(startPosition, endPosition, amount);
            position.y += Mathf.Sin(amount * Mathf.PI) * .35f;
            card.transform.position = position;
            card.transform.localScale = normalScale
                * (1 + Mathf.Sin(amount * Mathf.PI) * .04f);
            yield return null;
        }

        card.transform.position = endPosition;
        card.transform.localScale = normalScale;
        animatingCards.Remove(card);
        cardsChanged = true;
    }

    private IEnumerator AnimateSelectionJump(GameObject card)
    {
        int handIndex = handCards.IndexOf(card);
        if(handIndex < 0) yield break;

        jumpingCards.Add(card);
        Vector3 heldPosition = GetHandPosition(handIndex) + new Vector3(0, .35f, 0);
        Vector3 startPosition = card.transform.position;
        float time = 0;

        while(time < .06f)
        {
            if(!handCards.Contains(card))
            {
                jumpingCards.Remove(card);
                yield break;
            }
            time += Time.deltaTime;
            card.transform.position = Vector3.Lerp(startPosition,
                heldPosition + new Vector3(0, .18f, 0), time / .06f);
            yield return null;
        }

        startPosition = card.transform.position;
        time = 0;
        while(time < .08f)
        {
            if(!handCards.Contains(card))
            {
                jumpingCards.Remove(card);
                yield break;
            }
            time += Time.deltaTime;
            card.transform.position =
                Vector3.Lerp(startPosition, heldPosition, time / .08f);
            yield return null;
        }

        card.transform.position = heldPosition;
        jumpingCards.Remove(card);
    }

    private Vector3 GetPilePosition(int pileIndex)
    {
        float x = (pileIndex - (piles.Count - 1) * .5f) * 2;
        return new Vector3(x, SpeedPileY, 0);
    }

    private Vector3 GetHandPosition(int handIndex)
    {
        float x = -(handSize - 1) * HandSpacing * .5f
            + handIndex * HandSpacing;
        return new Vector3(x, HandY, 0);
    }

    private int GetEffectiveCardIndex(int pileIndex)
    {
        int i = piles[pileIndex].Count - 1;
        while((cardData[piles[pileIndex][i]].properties & CardData.Transparent) != 0
        && i > 0) i--;
        return i;
    }

    public static void Shuffle<T>(List<T> deck)
    {
        for (int i = deck.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            (deck[i], deck[randomIndex]) = (deck[randomIndex], deck[i]);
        }
    }

    public GameObject CreateCard(CardData data)
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
            seal.AddComponent<ModifierIdleMotion>();
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
        }

    }

}
