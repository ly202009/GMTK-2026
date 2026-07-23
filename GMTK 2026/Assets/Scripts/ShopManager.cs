using UnityEngine;
using System.Collections.Generic;

public class Shop : MonoBehaviour
{
    private static (int property, string seal, Vector3 sealPosition)[] Properties =
    {
        (CardData.Transparent, null, Vector3.zero),
        (CardData.AutoPlay, "Autoplay", new Vector3(-.2f, -.1f, -.01f)),
        (CardData.BonusTime, "Bonus Time", new Vector3(0, -.45f, -.01f)),
        (CardData.WildCard, null, Vector3.zero),
        (CardData.Flexible, "+-1", new Vector3(.2f, -.1f, -.01f))
    };
    private Material[] cardMaterials;
    [SerializeField] public bool sellingPowerups;
    [SerializeField] private GameObject cardTemplate;
    [SerializeField] private Material transparentCardMaterial;
    [SerializeField] private Material wildCardMaterial;
    [SerializeField] private Material transparentWildCardMaterial;
    private Sprite[] cardSprites;
    private Sprite cardBack;
    private Dictionary<int, Sprite> propertySeals = new();

    private Transform shop;

    private CardData GetPropertiesForCard() 
    {
        CardData thisCard = new CardData();
        thisCard.values = new int[1];
        int generateProp = UnityEngine.Random.Range(0, 15);
        if (generateProp < 5)
        {
            thisCard.properties = 1 << generateProp;
        }

        thisCard.values[0] = UnityEngine.Random.Range(0, 14);
        thisCard.suit = (Suit)UnityEngine.Random.Range(0, 4);
        return thisCard;
    }
    private GameObject CreateCard(CardData currentCard) //Completely stolen from DeckGenerator lol.
    {
        GameObject card = Instantiate(cardTemplate, Vector3.zero, cardTemplate.transform.rotation, cardTemplate.transform.parent);
        card.transform.localScale = new Vector3(card.transform.localScale.x, card.transform.localScale.x, 1);

        SpriteRenderer cardRenderer = card.GetComponent<SpriteRenderer>();
        int suitRow = (int)currentCard.suit;

        for(int i = 0; i < cardSprites.Length; i++)
        {
            int x = Mathf.RoundToInt(cardSprites[i].rect.x / 24);
            int y = Mathf.RoundToInt(cardSprites[i].rect.y / 36);
            if(x != currentCard.values[0] - 1 || y != suitRow) continue;
            cardRenderer.sprite = cardSprites[i];
            break;
        }
        
        cardRenderer.drawMode = SpriteDrawMode.Simple;
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

    void SetUpShop()
    {
        if (sellingPowerups)
        {
            //Insert Properties here later.
            return;
        }

        GameObject[] cards = new GameObject[3];
        CardData[] cardsData = new CardData[3];
        for (int i = 0; i < 3; i++)
        {
            cardsData[i] = GetPropertiesForCard();
            cards[i] = CreateCard(cardsData[i]);
            cards[i].transform.position = shop.position + (i-1)*new Vector3(10, 0, 0);
        }

    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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

        SetUpShop();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
