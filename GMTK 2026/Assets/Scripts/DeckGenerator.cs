using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

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
    public static DeckGenerator instance;

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
    private const float DrawPileY = -1.7f;

    [SerializeField] private GameObject cardTemplate;
    [SerializeField] private Material transparentCardMaterial;
    [SerializeField] private Material wildCardMaterial;
    [SerializeField] private Material transparentWildCardMaterial;
    [SerializeField] private TMP_Text drawPileCountText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private RectTransform comboPanel;
    [SerializeField] private CanvasGroup comboGroup;
    [SerializeField] private Image comboTimerFill;
    [SerializeField] private RectTransform comboTimerPanel;
    [SerializeField] private CanvasGroup comboTimerGroup;
    [SerializeField] private TMP_Text comboLevelText;
    [SerializeField] private RectTransform comboLevelPanel;
    [SerializeField] private CanvasGroup comboLevelGroup;
    [SerializeField] private TMP_Text bossText;

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
    private bool usedFreeze;
    private float doublesCountdown;
    private float suitMatchingCountdown;
    private float freezeCountdown;
    private float powerupDuration = 10;
    private float cardMoveDuration = .18f;
    private float dragThreshold = .15f;
    private int combo;
    private float comboTime;
    private float comboWindow = 2.25f;
    private float comboStepWindow = 2.25f;
    private int comboDecayGear;
    private List<CardData> comboHistory = new();
    private float comboFractionalTime;
    private Coroutine comboAnimation;
    private Vector2 comboPosition;
    private float comboBarPunch;
    private Vector2 comboLevelPosition;
    private float comboLevelPunch;
    private int boss;
    private float bossTime;
    private float bossExtraTime;
    private Vector2 bossDirection;
    private bool stickyDisabled;
    private List<RectTransform> screensavers = new();
    private List<TMP_Text> screensaverTexts = new();
    private List<Vector2> screensaverDirections = new();
    private Coroutine timeShake;
    private Vector3 timeShakePosition;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        numberOfPiles = RunData.instance.numberOfPiles;
        handSize = RunData.instance.handSize;
        autoDraw = RunData.instance.autoDraw;
        boss = RunData.instance.currentBoss;
        bossText.gameObject.SetActive(boss >= 0);
        if(boss >= 0)
        {
            bossText.text = RunData.Bosses[boss]
                + "\n<size=22>" + RunData.BossDescriptions[boss] + "</size>";
            bossText.rectTransform.localScale = Vector3.zero;
        }
        if(boss == 3) handSize = Mathf.Max(1, handSize - 2);
        if(boss == 4)
            bossDirection = UnityEngine.Random.insideUnitCircle.normalized;
        if(boss == 6)
        {
            for(int i = 0; i < 3; i++)
            {
                GameObject box = new GameObject("Screensaver " + i,
                    typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                box.transform.SetParent(bossText.transform.parent, false);
                RectTransform rect = box.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(.5f, .5f);
                rect.anchorMax = new Vector2(.5f, .5f);
                rect.sizeDelta = new Vector2(420, 100);
                rect.anchoredPosition =
                    new Vector2((i - 1) * 420, (i % 2 * 2 - 1) * 220);
                box.GetComponent<Image>().color =
                    new Color(.015f, .018f, .025f, .94f);

                TMP_Text text = Instantiate(bossText, box.transform);
                text.name = "Screensaver " + i;
                text.text = "DVD";
                text.rectTransform.anchorMin = Vector2.zero;
                text.rectTransform.anchorMax = Vector2.one;
                text.rectTransform.sizeDelta = new Vector2(-16, -10);
                text.rectTransform.anchoredPosition = Vector2.zero;
                text.rectTransform.localScale = Vector3.one * 1.5f;
                screensavers.Add(rect);
                screensaverTexts.Add(text);
                screensaverDirections.Add(
                    UnityEngine.Random.insideUnitCircle.normalized);
            }
        }
        comboPosition = comboPanel.anchoredPosition;
        comboGroup.alpha = 0;
        comboTimerGroup.alpha = 0;
        comboLevelPosition = comboLevelPanel.anchoredPosition;
        comboLevelGroup.alpha = 0;

        List<CardData> deck = new(RunData.instance.deck);
        Shuffle(deck);
        cardSprites = Resources.LoadAll<Sprite>("ClassicCards");
        cardBack = Resources.LoadAll<Sprite>("LightClassic")[0];
        for(int i = 0; i < Properties.Length; i++)
            if(Properties[i].seal != null)
            {
                Sprite[] seals = Resources.LoadAll<Sprite>(
                    "modifiers/" + Properties[i].seal);
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
        if(boss == 6 && pressedCard == null && draggedCard == null)
            foreach(RectTransform rect in screensavers)
                if(RectTransformUtility.RectangleContainsScreenPoint(
                rect, Mouse.current.position.ReadValue())) return;
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
                    if(!CardsWork(draggedCard, i))
                    {
                        StartCoroutine(RejectCard(draggedCard));
                        break;
                    }

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

                if(!CardsWork(selectedHandCard, i))
                {
                    StartCoroutine(RejectCard(selectedHandCard));
                    return;
                }

                PlayCard(selectedHandCard, i);
                return;
            }
        }

        if (drawPile.Count > 0 && clickedCard == drawPile[drawPile.Count - 1])
            DrawCards();
   }

    private void PlayCard(GameObject card, int pileIndex)
    {
        CardData playedCard = cardData[card];
        bool closeCall = RunData.instance.countdownValue <= 10;
        combo++;
        comboDecayGear = 0;
        comboStepWindow = comboWindow;
        comboTime = comboWindow;
        comboBarPunch = .16f;
        comboLevelPunch = .18f;

        List<string> comboTypes = new();
        float timeMultiplier = 1;
        if(comboHistory.Count > 0)
        {
            CardData previousCard =
                comboHistory[comboHistory.Count - 1];
            if(CanRepresentValue(previousCard, 13)
            && CanRepresentValue(playedCard, 1))
            {
                comboTypes.Add("OVER THE TOP");
                timeMultiplier += .18f;
            }
            if(CanRepresentValue(previousCard, 1)
            && CanRepresentValue(playedCard, 13))
            {
                comboTypes.Add("NEGATIVE");
                timeMultiplier += .18f;
            }
            if(previousCard.suit == playedCard.suit)
            {
                comboTypes.Add("MATCHER");
                timeMultiplier += .1f;
            }
        }

        comboHistory.Add(playedCard);
        if(comboHistory.Count >= 4)
        {
            int i = comboHistory.Count;
            if(comboHistory[i - 4].values[0] == comboHistory[i - 2].values[0]
            && comboHistory[i - 3].values[0] == comboHistory[i - 1].values[0]
            && comboHistory[i - 4].values[0] != comboHistory[i - 3].values[0])
            {
                comboTypes.Add("REPEAT");
                timeMultiplier += .22f;
            }
        }
        if(comboHistory.Count > 4) comboHistory.RemoveAt(0);
        if(closeCall)
        {
            comboTypes.Add("CLOSE CALL");
            timeMultiplier += .15f;
        }

        bool bonusTime =
            (playedCard.properties & CardData.BonusTime) != 0;
        int timeGain = combo <= 1 ? 0 :
            Mathf.CeilToInt((combo - 1) / 4f);
        if(bonusTime) timeGain++;
        comboFractionalTime += timeGain * (timeMultiplier - 1);
        int multiplierGain = Mathf.FloorToInt(comboFractionalTime);
        comboFractionalTime -= multiplierGain;
        timeGain += multiplierGain;
        GainTime(timeGain);
        if(comboAnimation != null) StopCoroutine(comboAnimation);
        comboAnimation = StartCoroutine(ShowCombo(timeGain, bonusTime,
            string.Join("  •  ", comboTypes), timeMultiplier));

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
        int cardValue = cardData[card].values[0];
        int topValue = cardData[topCard].values[0];

        if(boss == 0 && cardValue == 4
        && RunData.instance.countdown % 4 != 0) return false;
        if(boss == 7 && (cardValue == 1 && topValue == 13
        || cardValue == 13 && topValue == 1)) return false;

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

    private bool CanRepresentValue(CardData card, int value)
    {
        if(card.values[0] == value) return true;
        if((card.properties & CardData.Flexible) == 0) return false;

        int lowerValue = card.values[0] == 1 ? 13 : card.values[0] - 1;
        int upperValue = card.values[0] == 13 ? 1 : card.values[0] + 1;
        return lowerValue == value || upperValue == value;
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

        if(RunData.instance.handInvalidGain) GainTime(3);
        yield return StartCoroutine(AnimateShuffle());
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

    private IEnumerator AnimateShuffle()
    {
        List<GameObject> cards = new();
        List<Vector3> starts = new();
        List<Vector3> centers = new();
        List<Vector3> scales = new();
        List<int> depths = new();
        for(int i = 0; i < piles.Count; i++)
            for(int j = 0; j < piles[i].Count; j++)
            {
                cards.Add(piles[i][j]);
                starts.Add(piles[i][j].transform.position);
                centers.Add(GetPilePosition(i));
                scales.Add(piles[i][j].transform.localScale);
                depths.Add(j);
                animatingCards.Add(piles[i][j]);
            }

        if(cards.Count == 0) yield break;

        float time = 0;
        while(time < .14f)
        {
            time += Time.deltaTime;
            float amount = Mathf.Clamp01(time / .14f);
            amount = amount * amount * (3 - 2 * amount);
            for(int i = 0; i < cards.Count; i++)
            {
                float direction = depths[i] % 2 == 0 ? -1 : 1;
                Vector3 target = centers[i] + new Vector3(
                    direction * (.16f + depths[i] % 4 * .015f),
                    .05f + depths[i] % 5 * .012f, 0);
                cards[i].transform.position =
                    Vector3.Lerp(starts[i], target, amount);
                cards[i].transform.localRotation =
                    Quaternion.Euler(0, 0, direction * amount * 12);
                cards[i].transform.localScale = scales[i]
                    * (1 + Mathf.Sin(amount * Mathf.PI) * .05f);
            }
            yield return null;
        }

        foreach(List<GameObject> pile in piles) Shuffle(pile);

        starts.Clear();
        for(int i = 0; i < cards.Count; i++)
            starts.Add(cards[i].transform.position);

        time = 0;
        while(time < .13f)
        {
            time += Time.deltaTime;
            float amount = Mathf.Clamp01(time / .13f);
            amount = 1 - Mathf.Pow(1 - amount, 3);
            for(int i = 0; i < cards.Count; i++)
            {
                float direction = depths[i] % 2 == 0 ? -1 : 1;
                Vector3 target = centers[i] + new Vector3(
                    direction * -.18f,
                    .035f + depths[i] % 5 * .01f, 0);
                cards[i].transform.position =
                    Vector3.Lerp(starts[i], target, amount);
                cards[i].transform.localRotation = Quaternion.Lerp(
                    Quaternion.Euler(0, 0, direction * 12),
                    Quaternion.Euler(0, 0, direction * -10), amount);
            }
            yield return null;
        }

        starts.Clear();
        for(int i = 0; i < cards.Count; i++)
            starts.Add(cards[i].transform.position);

        time = 0;
        while(time < .18f)
        {
            time += Time.deltaTime;
            float amount = Mathf.Clamp01(time / .18f);
            amount = 1 - Mathf.Pow(1 - amount, 3);
            for(int i = 0; i < cards.Count; i++)
            {
                Vector3 position =
                    Vector3.Lerp(starts[i], centers[i], amount);
                position.y += Mathf.Sin(amount * Mathf.PI)
                    * (.08f + depths[i] % 3 * .018f);
                cards[i].transform.position = position;
                cards[i].transform.localRotation = Quaternion.Lerp(
                    cards[i].transform.localRotation,
                    Quaternion.identity, amount);
                cards[i].transform.localScale = Vector3.Lerp(
                    cards[i].transform.localScale, scales[i], amount);
            }
            yield return null;
        }

        for(int i = 0; i < cards.Count; i++)
        {
            cards[i].transform.position = centers[i];
            cards[i].transform.localRotation = Quaternion.identity;
            cards[i].transform.localScale = scales[i];
            animatingCards.Remove(cards[i]);
        }
    }

    private IEnumerator ShowCombo(int timeGain, bool bonusTime,
        string comboType, float timeMultiplier)
    {
        string typeText = comboType.Length > 0 ?
            $"\n<size=20><color=#62D9FF>{comboType}  "
            + $"x{timeMultiplier:0.00}</color></size>" : "";
        comboText.text = timeGain > 0 ?
            $"<size=42>{combo}x COMBO</size>\n"
            + $"<size=30><color=#{(bonusTime ? "FFE45C" : "71FF8D")}>"
            + $"+{timeGain} SECOND{(timeGain == 1 ? "" : "S")}</color></size>"
            + typeText :
            "<size=42>1x COMBO</size>" + typeText;
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

    private IEnumerator ShowComboDecay()
    {
        comboLevelText.text = $"{combo}x";
        comboLevelGroup.alpha = 1;
        comboLevelPunch = .18f;
        float direction = comboDecayGear % 2 == 0 ? -1 : 1;

        float time = 0;
        while(time < .22f)
        {
            time += Time.unscaledDeltaTime;
            float strength = 1 - Mathf.Clamp01(time / .22f);
            comboLevelPanel.localRotation = Quaternion.Euler(0, 0,
                Mathf.Sin(time * 38) * direction
                    * (3 + comboDecayGear) * strength);
            yield return null;
        }

        comboLevelPanel.localRotation = Quaternion.identity;
        comboAnimation = null;
    }

    private void Update()
    {
        bossTime += Time.unscaledDeltaTime;
        if(boss == 1 && bossTime >= 3)
        {
            bossTime = 0;
            int j = UnityEngine.Random.Range(0, piles.Count);
            for(int i = 0; i < piles.Count; i++)
            {
                int k = (i + j) % piles.Count;
                if(piles[k].Count == 1 && drawPile.Count == 0) continue;
                GameObject card = piles[k][piles[k].Count - 1];
                if(animatingCards.Contains(card)) continue;
                piles[k].RemoveAt(piles[k].Count - 1);
                if(piles[k].Count == 0)
                {
                    GameObject replacement = drawPile[drawPile.Count - 1];
                    drawPile.RemoveAt(drawPile.Count - 1);
                    piles[k].Add(replacement);
                }
                cardData.Remove(card);
                cardFaces.Remove(card);
                StartCoroutine(WhiskCard(card));
                cardsChanged = true;
                break;
            }
        }
        if(boss == 4 && Keyboard.current.escapeKey.wasPressedThisFrame)
            stickyDisabled = true;
        if(boss == 4 && !stickyDisabled && Application.isFocused)
        {
            Vector2 mouse = Mouse.current.position.ReadValue()
                + bossDirection * 180 * Time.unscaledDeltaTime;
            mouse.x = Mathf.Clamp(mouse.x, 0, Screen.width);
            mouse.y = Mathf.Clamp(mouse.y, 0, Screen.height);
            Mouse.current.WarpCursorPosition(mouse);
            InputState.Change(Mouse.current.position, mouse);
        }
        if(boss >= 0 && freezeCountdown <= 0)
        {
            bossExtraTime += Time.unscaledDeltaTime
                * RunData.instance.timerSpeed * (boss == 5 ? .5f : .2f);
            if(bossExtraTime >= 1)
            {
                bossExtraTime--;
                RunData.instance.countdown =
                    Mathf.Max(0, RunData.instance.countdown - 1);
            }
        }
        if(boss == 6)
        {
            for(int i = 0; i < screensavers.Count; i++)
            {
                RectTransform rect = screensavers[i];
                RectTransform parent = rect.parent.GetComponent<RectTransform>();
                Vector2 limit = (parent.rect.size - rect.rect.size) * .5f;
                Vector2 direction = screensaverDirections[i];
                rect.anchoredPosition += direction
                    * 320 * Time.unscaledDeltaTime;
                if(Mathf.Abs(rect.anchoredPosition.x) > limit.x)
                    direction.x *= -1;
                if(Mathf.Abs(rect.anchoredPosition.y) > limit.y)
                    direction.y *= -1;
                rect.anchoredPosition = new Vector2(
                    Mathf.Clamp(rect.anchoredPosition.x, -limit.x, limit.x),
                    Mathf.Clamp(rect.anchoredPosition.y, -limit.y, limit.y));
                screensaverDirections[i] = direction;
                screensaverTexts[i].color = Color.HSVToRGB(
                    Mathf.Repeat(bossTime * .14f + i / 3f, 1), .75f, 1);
            }
        }
        if(boss >= 0)
        {
            float pulse = 1 + Mathf.Sin(Time.unscaledTime * 3) * .035f;
            bossText.rectTransform.localScale = Vector3.Lerp(
                bossText.rectTransform.localScale, Vector3.one * pulse,
                1 - Mathf.Exp(-9 * Time.unscaledDeltaTime));
            bossText.rectTransform.localRotation = Quaternion.Euler(
                0, 0, Mathf.Sin(Time.unscaledTime * 2.2f) * 1.5f);
        }

        if(combo > 0)
        {
            comboTime -= Time.unscaledDeltaTime;
            if(comboTime <= 0)
            {
                combo--;
                comboDecayGear++;
                comboBarPunch = .1f;
                comboLevelPunch = .14f;
                if(combo > 0)
                {
                    comboStepWindow = Mathf.Max(.18f,
                        comboWindow * Mathf.Pow(.62f, comboDecayGear));
                    comboTime = comboStepWindow;
                }
                else
                {
                    comboTime = 0;
                    comboStepWindow = comboWindow;
                    comboHistory.Clear();
                    comboFractionalTime = 0;
                }

                if(comboAnimation != null) StopCoroutine(comboAnimation);
                comboAnimation = StartCoroutine(ShowComboDecay());
            }
        }
        float comboAmount = combo > 0 ?
            Mathf.Clamp01(comboTime / comboStepWindow) : 0;
        comboTimerFill.rectTransform.sizeDelta = new Vector2(
            -6, (comboTimerPanel.rect.height - 6) * comboAmount);
        comboTimerGroup.alpha = Mathf.Lerp(comboTimerGroup.alpha,
            combo > 0 ? 1 : 0,
            1 - Mathf.Exp(-12 * Time.unscaledDeltaTime));
        comboLevelGroup.alpha = Mathf.Lerp(comboLevelGroup.alpha,
            combo > 0 ? 1 : 0,
            1 - Mathf.Exp(-12 * Time.unscaledDeltaTime));
        comboBarPunch = Mathf.MoveTowards(comboBarPunch, 0,
            Time.unscaledDeltaTime * 1.5f);
        comboLevelPunch = Mathf.MoveTowards(comboLevelPunch, 0,
            Time.unscaledDeltaTime * 1.5f);
        float warningPulse = comboAmount < .25f && combo > 0 ?
            Mathf.Sin(Time.unscaledTime * (12 + comboDecayGear * 3))
                * (.035f + comboDecayGear * .008f) : 0;
        comboTimerPanel.localScale =
            Vector3.one * (1 + comboBarPunch + warningPulse);
        float levelShake = comboDecayGear > 0 && combo > 0 ?
            Mathf.Sin(Time.unscaledTime * (18 + comboDecayGear * 3))
                * Mathf.Min(8, comboDecayGear * 1.3f) : 0;
        if(handSize > 0 && Camera.main != null)
        {
            Vector3 comboScreenPosition = Camera.main.WorldToScreenPoint(
                GetHandPosition(0) + new Vector3(-1.7f, .25f, 0));
            comboScreenPosition.x = Mathf.Max(70, comboScreenPosition.x);
            comboLevelPanel.position = comboScreenPosition;
            comboLevelPosition = comboLevelPanel.anchoredPosition;
        }
        comboLevelPanel.anchoredPosition =
            comboLevelPosition + Vector2.right * levelShake;
        comboLevelPanel.localScale = Vector3.one
            * (1 + comboLevelPunch + warningPulse * .7f);
        comboTimerPanel.position = comboLevelPanel.TransformPoint(
            new Vector3(comboLevelPanel.rect.width * .5f + 18, 0, 0));
        comboLevelText.text = $"{combo}x";
        comboLevelText.color = comboDecayGear > 0 ?
            Color.Lerp(new Color(1, .2f, .08f),
                new Color(1, .72f, .12f), comboAmount) :
            new Color(.45f, 1, .55f);
        comboTimerFill.color = comboDecayGear > 0 ?
            Color.Lerp(new Color(1, .08f, .03f),
                new Color(1, .55f, .08f), comboAmount) :
            comboAmount > .5f ?
                Color.Lerp(new Color(1, .82f, .12f),
                    new Color(.25f, 1, .4f), (comboAmount - .5f) * 2) :
                Color.Lerp(new Color(1, .15f, .08f),
                    new Color(1, .82f, .12f), comboAmount * 2);

        HandlePowerups();
        HandleClicks();
        if(cardsChanged) HandleAutoPlay();
        HandleReShuffle();
        if(cardsChanged) HandleAutoPlay();

        if(movingToShop || drawPile.Count > 0 || animatingCards.Count > 0) return;
        foreach(GameObject card in handCards)
            if(card != null) return;
        GainTime(50);
        movingToShop = true;
        SceneTransition.Load(RunData.instance.bossRound ?
            "PowerUpShopScene" : "ShopScene");
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

        if(!usedFreeze && RunData.instance.allowFreeze
        && Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            usedFreeze = true;
            freezeCountdown = powerupDuration;
        }

        if(suitMatchingCountdown > 0)
            suitMatchingCountdown = Mathf.Max(0,
                suitMatchingCountdown - Time.deltaTime);

        if(doublesCountdown > 0)
            doublesCountdown = Mathf.Max(0, doublesCountdown - Time.deltaTime);

        if(freezeCountdown > 0)
            freezeCountdown = Mathf.Max(0,
                freezeCountdown - Time.deltaTime);

        RunData.instance.SetTimerFrozen(freezeCountdown > 0);
    }

    public bool PowerupUsed(int power)
    {
        if(power == 2) return usedDoubles;
        if(power == 4) return usedSuitMatching;
        if(power == 5) return usedFreeze;
        return false;
    }

    public float PowerupCountdown(int power)
    {
        if(power == 2) return doublesCountdown;
        if(power == 4) return suitMatchingCountdown;
        if(power == 5) return freezeCountdown;
        return 0;
    }

    public void RemovePowerup(int power)
    {
        if(power == 2) doublesCountdown = 0;
        if(power == 4) suitMatchingCountdown = 0;
        if(power == 5)
        {
            freezeCountdown = 0;
            RunData.instance.SetTimerFrozen(false);
        }
    }

    private void OnDestroy()
    {
        if(instance == this) instance = null;
        if(RunData.instance != null)
            RunData.instance.SetTimerFrozen(false);
    }

    private void LateUpdate()
    {
        RenderCards();
        if(boss < 0) return;
        bossText.ForceMeshUpdate();
        TMP_TextInfo info = bossText.textInfo;
        for(int i = 0; i < info.characterCount; i++)
        {
            if(i >= RunData.Bosses[boss].Length) continue;
            TMP_CharacterInfo character = info.characterInfo[i];
            if(!character.isVisible) continue;
            Vector3[] vertices =
                info.meshInfo[character.materialReferenceIndex].vertices;
            int j = character.vertexIndex;
            float x = (vertices[j].x + vertices[j + 2].x) * .5f;
            Vector3 offset = Vector3.up * (-x * x / 2600
                + Mathf.Sin(Time.unscaledTime * 2 + i * .4f) * 2);
            for(int k = 0; k < 4; k++) vertices[j + k] += offset;
        }
        bossText.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
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
                float idle = Time.time * 1.8f + i * 1.25f;
                handPosition.x += Mathf.Cos(idle) * .012f;
                handPosition.y += Mathf.Sin(idle) * .05f;
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
                handCards[i] == selectedHandCard ? 100 : i;
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
                bool hidden = boss == 2 && Mathf.Repeat(bossTime, 6) > 3;
                SpriteRenderer[] renderers =
                    piles[i][j].GetComponentsInChildren<SpriteRenderer>(true);
                for(int k = 0; k < renderers.Length; k++)
                    if(hidden) renderers[k].enabled = false;
                if(!hidden)
                    piles[i][j].GetComponent<SpriteRenderer>().enabled = true;
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
                GetDrawPilePosition() + idlePosition;
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
            GetDrawPilePosition() + Vector3.up * .85f);
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

    private void GainTime(int amount)
    {
        if(amount <= 0) return;
        RunData.instance.countdown += amount;
        if(timeShake != null)
        {
            StopCoroutine(timeShake);
            Camera.main.transform.position = timeShakePosition;
        }
        timeShakePosition = Camera.main.transform.position;
        timeShake = StartCoroutine(ShakeTimeGain());
    }

    private IEnumerator ShakeTimeGain()
    {
        float time = 0;
        float strength = Mathf.Min(.12f, .018f + combo * .006f);
        while(time < .14f)
        {
            time += Time.unscaledDeltaTime;
            Camera.main.transform.position = timeShakePosition
                + (Vector3)UnityEngine.Random.insideUnitCircle
                * strength * (1 - time / .14f);
            yield return null;
        }
        Camera.main.transform.position = timeShakePosition;
        timeShake = null;
    }

    private IEnumerator RejectCard(GameObject card)
    {
        if(jumpingCards.Contains(card)) yield break;
        jumpingCards.Add(card);
        Vector3 position = card.transform.position;
        float time = 0;
        while(time < .22f)
        {
            if(!handCards.Contains(card)) break;
            time += Time.unscaledDeltaTime;
            card.transform.position = position + Vector3.right
                * Mathf.Sin(time * 65) * .13f * (1 - time / .22f);
            yield return null;
        }
        jumpingCards.Remove(card);
    }

    private IEnumerator WhiskCard(GameObject card)
    {
        animatingCards.Add(card);
        SpriteRenderer[] renderers =
            card.GetComponentsInChildren<SpriteRenderer>(true);
        for(int i = 0; i < renderers.Length; i++) renderers[i].enabled = true;
        float time = 0;
        while(time < .3f)
        {
            time += Time.unscaledDeltaTime;
            float amount = time / .3f;
            card.transform.position += new Vector3(3, 5, 0)
                * Time.unscaledDeltaTime;
            card.transform.localRotation =
                Quaternion.Euler(0, 0, amount * 35);
            for(int i = 0; i < renderers.Length; i++)
                renderers[i].color = new Color(1, 1, 1, 1 - amount);
            yield return null;
        }
        animatingCards.Remove(card);
        Destroy(card);
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
        float halfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        float availableWidth = Mathf.Min(10.5f,
            Mathf.Max(2, (halfWidth - 1.4f) * 2));
        float spacing = piles.Count > 1 ?
            Mathf.Min(2, availableWidth / (piles.Count - 1)) : 0;
        float x = (pileIndex - (piles.Count - 1) * .5f) * spacing;
        return new Vector3(x, SpeedPileY, 0);
    }

    private Vector3 GetHandPosition(int handIndex)
    {
        float halfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        float drawPileX = GetDrawPilePosition().x;
        float halfHandWidth = Mathf.Max(2,
            Mathf.Min(drawPileX - 1.7f, halfWidth - 2.3f));
        float spacing = handSize > 1 ?
            Mathf.Min(HandSpacing, halfHandWidth * 2 / (handSize - 1)) : 0;
        float x = -(handSize - 1) * spacing * .5f + handIndex * spacing;
        return new Vector3(x, HandY, 0);
    }

    private Vector3 GetDrawPilePosition()
    {
        float halfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        return new Vector3(Mathf.Min(DrawPileX, halfWidth - 1.2f),
            DrawPileY, 0);
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
