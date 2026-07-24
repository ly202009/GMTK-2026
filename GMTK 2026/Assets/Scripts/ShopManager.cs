using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;
using System.Data;
using UnityEngine.InputSystem;
using System.Linq;
using System;

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
    [SerializeField] private int[] cost;
    [SerializeField] private float dragThreshold;
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

    private int cardSelected = -1;
    private int sealSelected = -1;
    private bool canDrag = false;
    private bool isDragging = false;
    private static (int property, string seal, Vector3 sealPosition)[] Properties =
    {
        (CardData.Transparent, "Transparent", new Vector3(.2f, .1f, -.01f)),
        (CardData.AutoPlay, "Autoplay", new Vector3(-.2f, -.1f, -.01f)),
        (CardData.BonusTime, "Bonus Time", new Vector3(0, -.45f, -.01f)),
        (CardData.WildCard, "wildcard", new Vector3(.25f, -15, -.01f)),
        (CardData.Flexible, "+-1", new Vector3(.2f, -.1f, -.01f))
    };

    private IEnumerator MoveThing(GameObject thing, Vector3 start, Vector3 end, float duration)
    {
        for (float t = 0.0f; t <= duration; t += Time.deltaTime)
        {
            thing.transform.position = Vector3.Lerp(start, end, Mathf.Sqrt(t/duration));
            yield return null;
        }
        thing.transform.position = end;
    }
    private IEnumerator ShakeCard(GameObject thing, float duration)
    {
        int track = 0;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            if (track % 2 == 0){
                thing.transform.localRotation = Quaternion.Euler(0, 0, 15f);
            }
            else
            {
                thing.transform.localRotation = Quaternion.Euler(0, 0, -15f);
            }
            yield return null;
        }
        thing.transform.localRotation = Quaternion.identity;
    }

    void ShowCards()
    {
        cardSelected = -1;
        sealSelected = -1;

        DeckGenerator.Shuffle(RunData.instance.deck);
        for (int i = 0; i < numberOfShownCards; i++)
        {
            cardPositions[i] = new Vector3(shownCardSpacing*(i-numberOfShownCards/2), shownCardPos, 0);
            shownCards[i] = RunData.instance.deck[i];
            cards[i] = DrawCard(shownCards[i]);
            cards[i].transform.position = shop.position;
            cards[i].name = "Card " + i.ToString();
            cards[i].AddComponent<BoxCollider2D>();
        }
        for (int i = 0; i < numberOfShownSeals; i++)
        {
            sealPositions[i] = new Vector3(shownPowerSpacing*(i-numberOfShownSeals/2), shownPowerPos, 0);
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
            isAvailable[i] = true;
        }
        for (int i = 0; i < numberOfShownCards; i++)
        {
            StartCoroutine(MoveThing(cards[i], shop.position, cardPositions[i], 0.1f));
        }
        for (int i = 0; i < numberOfShownSeals; i++)
        {
            StartCoroutine(MoveThing(seals[i], shop.position, sealPositions[i], 0.1f));
        }
    }
    private void ChooseCards()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D clickedCollider = Physics2D.OverlapPoint(mousePos);

        if (Mouse.current.leftButton.wasPressedThisFrame && clickedCollider != null)
        {
            if (cards.Contains(clickedCollider.gameObject))
            {
                int clickedCard = Array.IndexOf(cards, clickedCollider.gameObject);
                if (cardSelected != -1 && cardSelected != clickedCard)
                {
                    StartCoroutine(MoveThing(cards[cardSelected], cardPositions[cardSelected]+new Vector3(0, 0.2f, 0), cardPositions[cardSelected], 0.1f));
                    StartCoroutine(MoveThing(cards[clickedCard], cardPositions[clickedCard], cardPositions[clickedCard]+new Vector3(0, 0.2f, 0), 0.1f));
                } else if (cardSelected == -1)
                {
                    StartCoroutine(MoveThing(cards[clickedCard], cardPositions[clickedCard], cardPositions[clickedCard]+new Vector3(0, 0.2f, 0), 0.1f));
                }
                cardSelected = clickedCard;
            }
            if (seals.Contains(clickedCollider.gameObject))
            {
                canDrag = true;
                int clickedPower = Array.IndexOf(seals, clickedCollider.gameObject);
                if (sealSelected != -1 && sealSelected != clickedPower)
                {
                    StartCoroutine(MoveThing(seals[sealSelected], sealPositions[sealSelected]+new Vector3(0, 0.2f, 0), sealPositions[sealSelected], 0.1f));
                    StartCoroutine(MoveThing(seals[clickedPower], sealPositions[clickedPower], sealPositions[clickedPower]+new Vector3(0, 0.2f, 0), 0.1f));
                }
                sealSelected = clickedPower;
            }
        } else if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            if (canDrag)
            {
                canDrag = false;
            }
        }

        
    } 
    private IEnumerator ApplySealToCard()
    {
        int cardSelect = cardSelected;
        int sealSelect = sealSelected;
        CardData thisCard = shownCards[cardSelect];
        thisCard.properties = shownCards[cardSelect].properties | Properties[shownSeal[sealSelect]].property;
        RunData.instance.deck[cardSelect] = thisCard;
        isAvailable[sealSelect] = false;
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

        
    }
    GameObject DrawCard(CardData cardData)
    {
        GameObject card = Instantiate(cardTemplate);
        card.transform.localScale = new Vector3(card.transform.localScale.x, card.transform.localScale.x, 1);
        SpriteRenderer cardRenderer = card.GetComponent<SpriteRenderer>();

        for(int i = 0; i < cardSprites.Length; i++)
        {
            int x = Mathf.RoundToInt(cardSprites[i].rect.x / 24);
            int y = Mathf.RoundToInt(cardSprites[i].rect.y / 36);
            if(x != cardData.values[0] - 1 || y != (int)cardData.suit) continue;
            cardRenderer.sprite = cardSprites[i];
            break;
        }
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
            sealRenderer.enabled = (cardData.properties & Properties[i].property) != 0;
            sealRenderer.sortingOrder = cardRenderer.sortingOrder+1;
        }
        Destroy(card.GetComponent<Canvas>());
        card.SetActive(true);
        return card;
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
        ShowCards();

        
    }
    void Update()
    {
        ChooseCards();
        if (cardSelected != -1 && sealSelected != -1)
        {
            StartCoroutine(ApplySealToCard());
            cardSelected = -1;
            sealSelected = -1;
        }
    }
}
