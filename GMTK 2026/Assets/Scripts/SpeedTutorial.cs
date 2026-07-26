using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class SpeedTutorial : MonoBehaviour
{
    public static SpeedTutorial instance;
    public bool IsOpen => open || waiting;
    private static bool shownThisSession;

    private CanvasGroup group;
    private RectTransform panel;
    private RectTransform focus;
    private TMP_Text stepText;
    private TMP_Text titleText;
    private TMP_Text bodyText;
    private TMP_Text tipText;
    private TMP_Text focusText;
    private TMP_Text nextText;
    private Button nextButton;
    private List<Image> cards = new();
    private List<TMP_Text> cardLabels = new();
    private List<Image> exampleArrows = new();
    private List<Image> dots = new();
    private Sprite[] cardSprites;
    private Sprite cardBack;
    private Sprite arrowSprite;
    private GameObject tutorial;
    private int step;
    private bool open;
    private bool waiting;

    private string[] titles =
    {
        "ONE UP OR ONE DOWN",
        "PLAY FROM YOUR HAND",
        "REFILL EMPTY SLOTS",
        "EMPTY EVERYTHING"
    };

    private string[] bodies =
    {
        "Play a hand card onto any center pile when its rank is exactly one higher or lower. Suits do not matter.",
        "Click a hand card, then click a valid center pile. You can also drag the card straight onto that pile.",
        "After playing cards, click the face-down draw pile to refill every empty slot in your hand.",
        "Empty the draw pile and your entire hand before the countdown reaches zero to clear the round."
    };

    private string[] tips =
    {
        "ACE AND KING WRAP AROUND.",
        "YOU CAN USE ANY CENTER PILE.",
        "DRAW ONLY FILLS EMPTY HAND SLOTS.",
        "THE COUNTDOWN RESUMES WHEN YOU START."
    };

    private void Awake()
    {
        instance = this;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession()
    {
        shownThisSession = false;
    }

    private void Start()
    {
        bool shouldShow = PlayerPrefs.GetInt("SpeedTutorialSeen", 0) == 0;
#if UNITY_EDITOR
        shouldShow = !shownThisSession;
#endif
        if(shouldShow)
        {
            shownThisSession = true;
            StartCoroutine(OpenAfterDelay());
        }
    }

    private IEnumerator OpenAfterDelay()
    {
        waiting = true;
        RunData.instance.SetPaused(true);
        yield return new WaitForSecondsRealtime(0.7f);
        waiting = false;
        Open();
    }

    private void Update()
    {
        if(!open) return;
        float pulse = 1 + Mathf.Sin(Time.unscaledTime * 3) * .006f;
        panel.localScale = Vector3.one * pulse;

        if(Keyboard.current == null) return;
        if(Keyboard.current.enterKey.wasPressedThisFrame
        || Keyboard.current.spaceKey.wasPressedThisFrame
        || Keyboard.current.rightArrowKey.wasPressedThisFrame)
            Next();
        if(Keyboard.current.leftArrowKey.wasPressedThisFrame && step > 0)
        {
            step--;
            Refresh();
        }
        if(Keyboard.current.escapeKey.wasPressedThisFrame)
            Close();
    }

    public void Open()
    {
        if(open) return;
        if(tutorial == null)
            Build();
        open = true;
        step = 0;
        tutorial.SetActive(true);
        group.alpha = 1;
        group.interactable = true;
        group.blocksRaycasts = true;
        RunData.instance.SetPaused(true);
        Time.timeScale = 0;
        Refresh();
        nextButton.Select();
    }

    private void Next()
    {
        if(step < titles.Length - 1)
        {
            step++;
            Refresh();
            return;
        }
        Close();
    }

    private void Close()
    {
        if(!open) return;
        open = false;
        PlayerPrefs.SetInt("SpeedTutorialSeen", 1);
        PlayerPrefs.Save();
        group.alpha = 0;
        group.interactable = false;
        group.blocksRaycasts = false;
        tutorial.SetActive(false);
        RunData.instance.SetPaused(false);
        Time.timeScale = 1;
    }

    private void Refresh()
    {
        stepText.text = $"SPEED BASICS  //  {step + 1} OF {titles.Length}";
        titleText.text = titles[step];
        bodyText.text = bodies[step];
        tipText.text = tips[step];
        nextText.text = step == titles.Length - 1 ? "START PLAYING" : "NEXT";
        for(int i = 0; i < dots.Count; i++)
        {
            dots[i].color = i == step ?
                new Color(1, .06f, .06f) : new Color(1, 1, 1, .2f);
            dots[i].rectTransform.sizeDelta =
                Vector2.one * (i == step ? 16 : 10);
        }

        if(step == 0)
        {
            SetFocus(new Vector2(.5f, .63f), new Vector2(620, 330),
                "CENTER PILES");
            SetCard(0, 4, new Vector2(-130, -35), "PILE");
            SetCard(1, 5, new Vector2(0, -35), "YOUR CARD");
            SetCard(2, 6, new Vector2(130, -35), "PILE");
            SetArrow(0, new Vector2(-65, -35), 180);
            SetArrow(1, new Vector2(65, -35), 0);
        }
        else if(step == 1)
        {
            SetFocus(new Vector2(.5f, .25f), new Vector2(850, 280),
                "YOUR HAND");
            SetCard(0, 5, new Vector2(-100, -35), "HAND");
            SetCard(1, 6, new Vector2(100, -35), "PILE");
            cards[2].gameObject.SetActive(false);
            SetArrow(0, new Vector2(0, -35), 0);
            exampleArrows[1].gameObject.SetActive(false);
        }
        else if(step == 2)
        {
            SetFocus(new Vector2(.84f, .34f), new Vector2(250, 330),
                "DRAW PILE");
            SetCard(0, 0, new Vector2(-100, -35), "DRAW PILE");
            SetCard(1, -1, new Vector2(100, -35), "EMPTY SLOT");
            cards[2].gameObject.SetActive(false);
            SetArrow(0, new Vector2(0, -35), 0);
            exampleArrows[1].gameObject.SetActive(false);
        }
        else
        {
            SetFocus(new Vector2(.58f, .27f), new Vector2(1350, 340),
                "DRAW PILE + HAND");
            SetCard(0, -1, new Vector2(-100, -35), "DRAW PILE: 0");
            SetCard(1, -1, new Vector2(100, -35), "HAND: 0");
            cards[2].gameObject.SetActive(false);
            exampleArrows[0].gameObject.SetActive(false);
            exampleArrows[1].gameObject.SetActive(false);
        }
    }

    private void SetFocus(Vector2 anchor, Vector2 size, string text)
    {
        focus.anchorMin = anchor;
        focus.anchorMax = anchor;
        focus.anchoredPosition = Vector2.zero;
        focus.sizeDelta = size;
        focusText.text = text;
    }

    private void SetCard(int i, int rank, Vector2 position, string label)
    {
        cards[i].gameObject.SetActive(true);
        cards[i].rectTransform.anchoredPosition = position;
        cards[i].sprite = rank == 0 ? cardBack :
            rank < 0 ? null : GetCard(rank);
        cards[i].color = rank < 0 ?
            new Color(1, .06f, .06f, .12f) : Color.white;
        cardLabels[i].text = label;
    }

    private void SetArrow(int i, Vector2 position, float rotation)
    {
        exampleArrows[i].gameObject.SetActive(true);
        exampleArrows[i].rectTransform.anchoredPosition = position;
        exampleArrows[i].rectTransform.localRotation =
            Quaternion.Euler(0, 0, rotation);
    }

    private Sprite GetCard(int rank)
    {
        foreach(Sprite sprite in cardSprites)
            if(Mathf.RoundToInt(sprite.rect.x / 24) + 1 == rank)
                return sprite;
        return cardSprites[0];
    }

    private void Build()
    {
        cardSprites = Resources.LoadAll<Sprite>("ClassicCards");
        cardBack = Resources.LoadAll<Sprite>("LightClassic")[0];
        Sprite[] timelineSprites =
            Resources.LoadAll<Sprite>("Icons and other sprites");
        foreach(Sprite sprite in timelineSprites)
            if(sprite.name == "Arrow")
                arrowSprite = sprite;

        tutorial = new GameObject("Speed Tutorial",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster), typeof(CanvasGroup));
        tutorial.transform.SetParent(transform, false);
        Canvas canvas = tutorial.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 6000;
        CanvasScaler scaler = tutorial.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0;
        group = tutorial.GetComponent<CanvasGroup>();

        Image shade = MakeImage(tutorial.transform, "Shade",
            Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero);
        shade.color = new Color(0, 0, 0, .48f);

        Image focusImage = MakeImage(tutorial.transform, "Focus",
            Vector2.zero, new Vector2(.5f, .5f),
            new Vector2(.5f, .5f), new Vector2(620, 330));
        focus = focusImage.rectTransform;
        focusImage.color = new Color(1, .04f, .04f, .06f);
        Outline focusOutline = focusImage.gameObject.AddComponent<Outline>();
        focusOutline.effectColor = new Color(1, .03f, .03f, .95f);
        focusOutline.effectDistance = new Vector2(5, -5);
        focusText = MakeText(focus, "Focus Label", "",
            new Vector2(0, -182), new Vector2(500, 48), 22,
            new Color(1, .06f, .06f), TextAlignmentOptions.Center);

        Image panelImage = MakeImage(tutorial.transform, "Tutorial Panel",
            new Vector2(-560, 215), new Vector2(.5f, .5f),
            new Vector2(.5f, .5f), new Vector2(620, 500));
        panel = panelImage.rectTransform;
        panelImage.color = new Color(.018f, .024f, .022f, .98f);
        Outline panelOutline = panelImage.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = new Color(1, .03f, .03f, .9f);
        panelOutline.effectDistance = new Vector2(4, -4);

        Image accent = MakeImage(panel, "Red Accent", new Vector2(-303, 0),
            new Vector2(.5f, .5f), new Vector2(.5f, .5f),
            new Vector2(10, 484));
        accent.color = new Color(1, .03f, .03f);

        stepText = MakeText(panel, "Step", "", new Vector2(0, 210),
            new Vector2(560, 36), 18, new Color(1, .08f, .08f),
            TextAlignmentOptions.Center);
        titleText = MakeText(panel, "Title", "", new Vector2(0, 160),
            new Vector2(560, 65), 36, Color.white,
            TextAlignmentOptions.Center);
        titleText.fontStyle = FontStyles.Bold;
        bodyText = MakeText(panel, "Body", "", new Vector2(0, 88),
            new Vector2(540, 90), 22, new Color(.92f, .94f, .92f),
            TextAlignmentOptions.Center);

        RectTransform example = MakeImage(panel, "Example",
            new Vector2(0, -55), new Vector2(.5f, .5f),
            new Vector2(.5f, .5f), new Vector2(550, 180)).rectTransform;
        example.GetComponent<Image>().color = new Color(1, 1, 1, .035f);
        for(int i = 0; i < 3; i++)
        {
            Image card = MakeImage(example, "Example Card " + i,
                Vector2.zero, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), new Vector2(74, 112));
            card.preserveAspect = true;
            card.raycastTarget = false;
            Outline outline = card.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0, 0, 0, .8f);
            outline.effectDistance = new Vector2(3, -3);
            cards.Add(card);
            cardLabels.Add(MakeText(card.transform, "Label", "",
                new Vector2(0, -72), new Vector2(150, 28), 15,
                new Color(1, .12f, .12f), TextAlignmentOptions.Center));
        }

        for(int i = 0; i < 2; i++)
        {
            Image arrow = MakeImage(example, "Example Arrow " + i,
                Vector2.zero, new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), new Vector2(34, 34));
            arrow.sprite = arrowSprite;
            arrow.preserveAspect = true;
            arrow.color = new Color(1, .08f, .08f);
            arrow.raycastTarget = false;
            exampleArrows.Add(arrow);
        }

        tipText = MakeText(panel, "Tip", "", new Vector2(0, -182),
            new Vector2(540, 32), 17, new Color(1, .12f, .12f),
            TextAlignmentOptions.Center);
        tipText.fontStyle = FontStyles.Bold;

        Button skip = MakeButton(panel, "SKIP", new Vector2(-195, -218),
            new Vector2(150, 52), new Color(.11f, .12f, .11f));
        skip.onClick.AddListener(Close);
        nextButton = MakeButton(panel, "NEXT", new Vector2(165, -218),
            new Vector2(210, 52), new Color(.72f, .035f, .035f));
        nextButton.onClick.AddListener(Next);
        nextText = nextButton.GetComponentInChildren<TMP_Text>();

        for(int i = 0; i < titles.Length; i++)
        {
            Image dot = MakeImage(panel, "Step Dot " + i,
                new Vector2(-27 + i * 18, -218), new Vector2(.5f, .5f),
                new Vector2(.5f, .5f), Vector2.one * 10);
            dot.raycastTarget = false;
            dots.Add(dot);
        }

        tutorial.SetActive(false);
    }

    private Image MakeImage(Transform parent, string name, Vector2 position,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return obj.GetComponent<Image>();
    }

    private TMP_Text MakeText(Transform parent, string name, string text,
        Vector2 position, Vector2 size, float fontSize, Color color,
        TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(.5f, .5f);
        rect.anchorMax = new Vector2(.5f, .5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text label = obj.GetComponent<TMP_Text>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMin = Mathf.Max(10, fontSize - 7);
        label.fontSizeMax = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.raycastTarget = false;
        return label;
    }

    private Button MakeButton(Transform parent, string text,
        Vector2 position, Vector2 size, Color color)
    {
        Image image = MakeImage(parent, text + " Button", position,
            new Vector2(.5f, .5f), new Vector2(.5f, .5f), size);
        image.color = color;
        Button button = image.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, .18f);
        colors.pressedColor = Color.Lerp(color, Color.black, .2f);
        button.colors = colors;
        TMP_Text label = MakeText(button.transform, "Label", text,
            Vector2.zero, size, 20, Color.white,
            TextAlignmentOptions.Center);
        label.fontStyle = FontStyles.Bold;
        button.gameObject.AddComponent<AnimatedButton>().idleFloat = 1;
        return button;
    }

    private void OnDestroy()
    {
        if(instance == this)
            instance = null;
        if(!open && !waiting) return;
        if(RunData.instance != null)
            RunData.instance.SetPaused(false);
        Time.timeScale = 1;
    }
}
