using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;
using System.Data;
using UnityEngine.InputSystem;
using System.Linq;
using System;
using TMPro;
using UnityEngine.UI;

public class Shop : MonoBehaviour
{
    [SerializeField] public float shownCardPos;
    [SerializeField] public float shownCardSpacing;
    [SerializeField] public float shownPowerPos;
    [SerializeField] public float shownPowerSpacing;
    [SerializeField] private GameObject cardTemplate;
    [SerializeField] private Material transparentCardMaterial;
    [SerializeField] private Material wildCardMaterial;
    [SerializeField] private Material transparentWildCardMaterial;
    [SerializeField] private int numberOfShownCards;
    [SerializeField] private int numberOfShownSeals;
    [SerializeField] private float dragThreshold;
    [SerializeField] private Button rerollButton;
    [SerializeField] private TMP_Text rerollText;
    [SerializeField] private Button moveToGameButton;
    private Material[] cardMaterials;
    private Sprite[] cardSprites;
    private Sprite cardBack;
    private Dictionary<int, Sprite> propertySeals = new();
    private Transform shop;
    CardData[] shownCards;
    Vector3[] cardPositions;
    GameObject[] cards;
    int[] shownSeal;
    Vector3[] sealPositions;
    GameObject[] seals;
    bool[] isAvailable;
    private HashSet<GameObject> movingThings = new();
    private GameObject applyingCard;

    private int cardSelected = -1;
    private int sealSelected = -1;
    private int rerolls;
    private static (int property, string seal, Vector3 sealPosition, int cost)[] Properties =
    {
        (CardData.Transparent, "Transparent", new Vector3(.2f, .1f, -.01f), 1),
        (CardData.AutoPlay, "Autoplay", new Vector3(-.2f, -.1f, -.01f), 3),
        (CardData.BonusTime, "Bonus Time", new Vector3(0, -.45f, -.01f), 2),
        (CardData.WildCard, "wildcard", new Vector3(.25f, -15, -.01f), 2),
        (CardData.Flexible, "+-1", new Vector3(.2f, -.1f, -.01f), 1)
    };

    private IEnumerator MoveThing(GameObject thing, Vector3 start, Vector3 end,
        float duration, float delay = 0)
    {
        movingThings.Add(thing);
        if(delay > 0) yield return new WaitForSeconds(delay);
        Vector3 normalScale = thing.transform.localScale;
        for (float t = 0.0f; t <= duration; t += Time.deltaTime)
        {
            float amount = Mathf.Clamp01(t / duration);
            amount = 1 - Mathf.Pow(1 - amount, 3);
            Vector3 position = Vector3.Lerp(start, end, amount);
            position.y += Mathf.Sin(amount * Mathf.PI) * .08f;
            thing.transform.position = position;
            thing.transform.localScale = normalScale
                * (1 + Mathf.Sin(amount * Mathf.PI) * .035f);
            yield return null;
        }
        thing.transform.position = end;
        thing.transform.localScale = normalScale;
        movingThings.Remove(thing);
    }
    private IEnumerator ShakeCard(GameObject thing, float duration)
    {
        movingThings.Add(thing);
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            thing.transform.localRotation =
                Quaternion.Euler(0, 0, Mathf.Sin(t * 35) * 5);
            yield return null;
        }
        thing.transform.localRotation = Quaternion.identity;
        movingThings.Remove(thing);
    }

    private IEnumerator JumpThing(GameObject thing, Vector3 heldPosition)
    {
        yield return StartCoroutine(MoveThing(thing, thing.transform.position,
            heldPosition + new Vector3(0, .18f, 0), .06f));
        yield return StartCoroutine(MoveThing(thing, thing.transform.position,
            heldPosition, .08f));
    }

    void ShowCards()
    {
        cardSelected = -1;
        sealSelected = -1;

        DeckGenerator.Shuffle(RunData.instance.deck);
        for (int i = 0; i < numberOfShownCards; i++)
        {
            cardPositions[i] = new Vector3(
                shownCardSpacing * (i - numberOfShownCards / 2) + 1,
                shownCardPos, 0);
            shownCards[i] = RunData.instance.deck[i];
            cards[i] = DrawCard(shownCards[i]);
            cards[i].transform.position = shop.position;
            cards[i].name = "Card " + i.ToString();
            cards[i].AddComponent<BoxCollider2D>();
        }
        for (int i = 0; i < numberOfShownSeals; i++)
        {
            sealPositions[i] = new Vector3(
                shownPowerSpacing * (i - numberOfShownSeals / 2) + 1,
                shownPowerPos, 0);
            shownSeal[i] = UnityEngine.Random.Range(0, 5);

            seals[i] = new GameObject("Seal " + i.ToString());
            seals[i].transform.position = shop.position;
            SpriteRenderer sealRenderer = seals[i].AddComponent<SpriteRenderer>();
            sealRenderer.transform.SetParent(seals[i].transform, false);
            sealRenderer.transform.localPosition = new Vector3(0, 0, -0.1f);
            sealRenderer.transform.localScale = new Vector3(4f, 4f, 1);
            sealRenderer.sprite = propertySeals[Properties[shownSeal[i]].property];
            sealRenderer.enabled = true;
            seals[i].AddComponent<BoxCollider2D>();

            GameObject costObject = new GameObject("Cost");
            costObject.transform.SetParent(seals[i].transform, false);
            costObject.transform.localPosition = new Vector3(0, -.16f, -.01f);
            costObject.transform.localScale = new Vector3(.15f, .15f, 1);
            TextMeshPro costText = costObject.AddComponent<TextMeshPro>();
            costText.text = $"-{Properties[shownSeal[i]].cost}s";
            costText.fontSize = 6;
            costText.fontStyle = FontStyles.Bold;
            costText.alignment = TextAlignmentOptions.Center;
            costText.color = new Color(1, .05f, .05f);
            costText.sortingOrder = sealRenderer.sortingOrder + 1;
            isAvailable[i] = true;
        }
        for (int i = 0; i < numberOfShownCards; i++)
        {
            StartCoroutine(MoveThing(cards[i], shop.position,
                cardPositions[i], .16f, i * .025f));
        }
        for (int i = 0; i < numberOfShownSeals; i++)
        {
            StartCoroutine(MoveThing(seals[i], shop.position,
                sealPositions[i], .16f, (i + 1) * .025f));
        }
    }
    private void ChooseCards()
    {
        if(applyingCard != null) return;
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D hoveredCollider = Physics2D.OverlapPoint(mousePos);

        if (Mouse.current.leftButton.wasPressedThisFrame && hoveredCollider != null)
        {
            if(cards.Contains(hoveredCollider.gameObject))
            {
                cardSelected = Array.IndexOf(cards, hoveredCollider.gameObject);
                if(!movingThings.Contains(cards[cardSelected]))
                    StartCoroutine(JumpThing(cards[cardSelected],
                        cardPositions[cardSelected] + new Vector3(0, .28f, 0)));
            }
            if(seals.Contains(hoveredCollider.gameObject))
            {
                sealSelected = Array.IndexOf(seals, hoveredCollider.gameObject);
                if(!movingThings.Contains(seals[sealSelected]))
                    StartCoroutine(JumpThing(seals[sealSelected],
                        sealPositions[sealSelected] + new Vector3(0, .28f, 0)));
            }
        }

        for(int i = 0; i < cards.Length; i++)
        {
            if(cards[i] == null || cards[i] == applyingCard
            || movingThings.Contains(cards[i])) continue;
            float lift = i == cardSelected ? .28f :
                hoveredCollider != null && hoveredCollider.gameObject == cards[i] ? .08f : 0;
            cards[i].transform.position = Vector3.Lerp(cards[i].transform.position,
                cardPositions[i] + new Vector3(0, lift, 0),
                1 - Mathf.Exp(-20 * Time.deltaTime));
            bool tilted = hoveredCollider != null
                && hoveredCollider.gameObject == cards[i];
            Quaternion rotation = tilted ?
                GetPressureRotation(cards[i], mousePos) : Quaternion.identity;
            cards[i].transform.rotation = Quaternion.Lerp(
                cards[i].transform.rotation, rotation,
                1 - Mathf.Exp(-15 * Time.deltaTime));
        }

        for(int i = 0; i < seals.Length; i++)
        {
            if(seals[i] == null || !seals[i].activeSelf
            || movingThings.Contains(seals[i])) continue;
            float lift = i == sealSelected ? .28f :
                hoveredCollider != null && hoveredCollider.gameObject == seals[i] ? .08f : 0;
            float idle = Mathf.Sin(Time.time * 2.1f + i * 1.7f) * .065f;
            seals[i].transform.position = Vector3.Lerp(seals[i].transform.position,
                sealPositions[i] + new Vector3(0, lift + idle, 0),
                1 - Mathf.Exp(-20 * Time.deltaTime));
            bool tilted = hoveredCollider != null
                && hoveredCollider.gameObject == seals[i];
            Quaternion rotation = tilted ?
                GetPressureRotation(seals[i], mousePos) :
                Quaternion.Euler(0, 0,
                    Mathf.Sin(Time.time * 1.7f + i * 1.4f) * 3);
            seals[i].transform.rotation = Quaternion.Lerp(
                seals[i].transform.rotation, rotation,
                1 - Mathf.Exp(-15 * Time.deltaTime));
        }
    }

    private Quaternion GetPressureRotation(GameObject thing, Vector2 mousePosition)
    {
        BoxCollider2D box = thing.GetComponent<BoxCollider2D>();
        float width = box.size.x * Mathf.Abs(thing.transform.lossyScale.x);
        float height = box.size.y * Mathf.Abs(thing.transform.lossyScale.y);
        float x = Mathf.Clamp((mousePosition.x - thing.transform.position.x)
            / (width * .5f), -1, 1);
        float y = Mathf.Clamp((mousePosition.y - thing.transform.position.y)
            / (height * .5f), -1, 1);
        return Quaternion.Euler(y * 14, x * -14, 0);
    }

    private IEnumerator ApplySealToCard()
    {
        int cardSelect = cardSelected;
        int sealSelect = sealSelected;
        int property = Properties[shownSeal[sealSelect]].property;
        int modifierCost = Properties[shownSeal[sealSelect]].cost;
        applyingCard = cards[cardSelect];
        if((shownCards[cardSelect].properties & property) != 0
        || RunData.instance.countdown < modifierCost || !isAvailable[sealSelect])
        {
            yield return StartCoroutine(ShakeCard(seals[sealSelect], 0.2f));
            applyingCard = null;
            yield break;
        }

        CardData thisCard = shownCards[cardSelect];
        thisCard.properties = shownCards[cardSelect].properties | property;
        RunData.instance.countdown -= modifierCost;
        RunData.instance.deck[cardSelect] = thisCard;
        shownCards[cardSelect] = thisCard;
        isAvailable[sealSelect] = false;
        SetCardMaterial(cards[cardSelect], thisCard);
        SpriteRenderer[] sealRenderers = cards[cardSelect].GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sealRenderer in sealRenderers)
        {
            if (sealRenderer.name == "Seal " + Properties[shownSeal[sealSelect]].property)
            {
                sealRenderer.enabled = true;
            }
        }

        seals[sealSelect].SetActive(false);

        yield return StartCoroutine(MoveThing(cards[cardSelect], cards[cardSelect].transform.position, new Vector3(0, 0, 0), 0.1f));
        yield return StartCoroutine(ShakeCard(cards[cardSelect], 0.2f)); 
        yield return StartCoroutine(MoveThing(cards[cardSelect], cards[cardSelect].transform.position, cardPositions[cardSelect], 0.1f));
        applyingCard = null;

        
    }
    GameObject DrawCard(CardData cardData)
    {
        GameObject card = Instantiate(cardTemplate);
        card.transform.localScale = new Vector3(card.transform.localScale.x, card.transform.localScale.x, 1);
        SpriteRenderer cardRenderer = card.GetComponent<SpriteRenderer>();
        int suitRow = cardData.suit switch
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
            if(x != cardData.values[0] - 1 || y != suitRow) continue;
            cardRenderer.sprite = cardSprites[i];
            break;
        }
        SetCardMaterial(card, cardData);
        for(int i = 0; i < Properties.Length; i++)
        {
            if(Properties[i].property == CardData.Transparent
            || Properties[i].property == CardData.WildCard) continue;
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
            sealRenderer.enabled = (cardData.properties & Properties[i].property) != 0;
            sealRenderer.sortingOrder = cardRenderer.sortingOrder+1;
        }
        Destroy(card.GetComponent<Canvas>());
        card.SetActive(true);
        return card;
    }

    private void SetCardMaterial(GameObject card, CardData data)
    {
        int transparentIndex =
            (data.properties & CardData.Transparent) / CardData.Transparent;
        int wildCardIndex =
            (data.properties & CardData.WildCard) / CardData.WildCard;
        card.GetComponent<SpriteRenderer>().sharedMaterial =
            cardMaterials[transparentIndex + wildCardIndex * 2];
    }

    private void Reroll()
    {
        int rerollCost = 3 + rerolls * 2;
        if(RunData.instance.countdown < rerollCost
        || movingThings.Count > 0 || applyingCard != null) return;

        RunData.instance.countdown -= rerollCost;
        rerolls++;
        for(int i = 0; i < cards.Length; i++)
            if(cards[i] != null) Destroy(cards[i]);
        for(int i = 0; i < seals.Length; i++)
            if(seals[i] != null) Destroy(seals[i]);
        movingThings.Clear();
        ShowCards();
    }

    private void MoveToGame()
    {
        SceneTransition.Load("MainScene");
    }

    void Start()
    {
        shownCards = new CardData[numberOfShownCards];
        cardPositions = new Vector3[numberOfShownCards];
        cards = new GameObject[numberOfShownCards];
        isAvailable = new bool[numberOfShownSeals];

        shownSeal = new int[numberOfShownSeals];
        sealPositions = new Vector3[numberOfShownSeals];
        seals = new GameObject[numberOfShownSeals];

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

        shop = GetComponent<Transform>();
        rerollButton.onClick.AddListener(Reroll);
        moveToGameButton.onClick.AddListener(MoveToGame);
        ShowCards();

        
    }
    void Update()
    {
        int rerollCost = 3 + rerolls * 2;
        rerollText.text = $"REROLL\n-{rerollCost}s";
        rerollButton.interactable = RunData.instance.countdown >= rerollCost
            && movingThings.Count == 0 && applyingCard == null;
        ChooseCards();
        if(cardSelected != -1 && sealSelected != -1 && applyingCard == null)
        {
            StartCoroutine(ApplySealToCard());
            cardSelected = -1;
            sealSelected = -1;
        }
    }
}