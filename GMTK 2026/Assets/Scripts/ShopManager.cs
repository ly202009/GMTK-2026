using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;
using System.Data;
using UnityEngine.InputSystem;

public class Shop : MonoBehaviour
{
    [SerializeField] public float shownCardPos;
    [SerializeField] public float shownCardSpacing;
    [SerializeField] public float shownPowerPos;
    [SerializeField] public float shownPowerSpacing;
    [SerializeField] private GameObject cardTemplate;
    [SerializeField] public bool SellPowerups;
    [SerializeField] private Material transparentCardMaterial;
    [SerializeField] private Material wildCardMaterial;
    [SerializeField] private Material transparentWildCardMaterial;
    [SerializeField] private int numberOfShownCards;
    [SerializeField] private int numberOfShownPowerups;
    [SerializeField] private int[] cost;
    private Material[] cardMaterials;
    private Sprite[] cardSprites;
    private Sprite cardBack;
    private Dictionary<int, Sprite> propertySeals = new();
    private Transform shop;
    CardData[] shownCards;
    Vector3[] cardPositions;
    GameObject[] cards;
    int[] shownPowerup;
    Vector3[] powerupPositions;
    GameObject[] powerups;

    private int cardSelected = -1;
    private int powerupSelected = -1;
    private bool drag;


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

    void ShowCards()
    {
        
        DeckGenerator.Shuffle(RunData.instance.deck);
        for (int i = 0; i < numberOfShownCards; i++)
        {
            cardPositions[i] = new Vector3(shownCardSpacing*(i-numberOfShownCards/2), shownCardPos, 0);
            shownCards[i] = RunData.instance.deck[i];
            cards[i] = DrawCard(shownCards[i]);
            cards[i].transform.position = shop.position;
            cards[i].name = "Card " + i.ToString();
        }
        for (int i = 0; i < numberOfShownPowerups; i++)
        {
            powerupPositions[i] = new Vector3(shownPowerSpacing*(i-numberOfShownPowerups/2), shownPowerPos, 0);
            shownPowerup[i] = UnityEngine.Random.Range(0, 6);

            powerups[i] = new GameObject();
            powerups[i].name = "Powerup " + i.ToString();
            powerups[i].transform.position = shop.position;
            SpriteRenderer powerupRenderer = powerups[i].AddComponent<SpriteRenderer>();
            powerupRenderer.transform.SetParent(powerups[i].transform, false);
            powerupRenderer.transform.localPosition = new Vector3(0, 0, -0.1f);
            powerupRenderer.transform.localScale = new Vector3(4f, 4f, 1);
            powerupRenderer.sprite = propertySeals[Properties[i].property];
            powerupRenderer.enabled = true;
        }
        for (int i = 0; i < numberOfShownCards; i++)
        {
            StartCoroutine(MoveThing(cards[i], shop.position, cardPositions[i], 0.1f));
        }
        for (int i = 0; i < numberOfShownPowerups; i++)
        {
            StartCoroutine(MoveThing(powerups[i], shop.position, powerupPositions[i], 0.1f));
        }
    }
    private void ChooseCards() //Don't use for powerups
    {
    }
    private void ApplySealToCard()
    {
        
    }
    private void ChoosePowerup() //Don't Use for Cards
    {
        
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

        shownPowerup = new int[numberOfShownPowerups];
        powerupPositions = new Vector3[numberOfShownPowerups];
        powerups = new GameObject[numberOfShownPowerups];

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
        
    }
}
