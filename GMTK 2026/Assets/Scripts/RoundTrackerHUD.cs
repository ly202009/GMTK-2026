using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundTrackerHUD : MonoBehaviour
{
    [SerializeField] private RectTransform trackerRoot;

    private List<RectTransform> cards = new();
    private List<TMP_Text> texts = new();
    private List<Image> images = new();
    private List<Image> accents = new();
    private List<Outline> outlines = new();
    private List<CanvasGroup> groups = new();
    private List<TMP_Text> arrows = new();
    private int shownRound;
    private bool moving;
    private float spacing = 142;
    private float firstPosition = 64;

    private void Start()
    {
        for(int i = 0; i < 5; i++)
        {
            GameObject card = new GameObject("Round Slot " + i,
                typeof(RectTransform), typeof(CanvasRenderer),
                typeof(Image), typeof(CanvasGroup));
            card.transform.SetParent(trackerRoot, false);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, .5f);
            rect.anchorMax = new Vector2(0, .5f);
            rect.sizeDelta = new Vector2(116, 52);
            cards.Add(rect);
            Image image = card.GetComponent<Image>();
            image.raycastTarget = false;
            images.Add(image);
            Outline outline = card.AddComponent<Outline>();
            outline.effectDistance = new Vector2(2, -2);
            outline.useGraphicAlpha = true;
            outlines.Add(outline);
            CanvasGroup group = card.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            groups.Add(group);

            GameObject accentObject = new GameObject("Accent",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            accentObject.transform.SetParent(card.transform, false);
            RectTransform accentRect =
                accentObject.GetComponent<RectTransform>();
            accentRect.anchorMin = Vector2.zero;
            accentRect.anchorMax = Vector2.up;
            accentRect.pivot = new Vector2(0, .5f);
            accentRect.anchoredPosition = new Vector2(3, 0);
            accentRect.sizeDelta = new Vector2(6, -6);
            Image accent = accentObject.GetComponent<Image>();
            accent.raycastTarget = false;
            accents.Add(accent);

            GameObject textObject = new GameObject("Label",
                typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(card.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.anchoredPosition = new Vector2(4, 0);
            textRect.sizeDelta = new Vector2(-16, -4);
            TMP_Text text = textObject.GetComponent<TMP_Text>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = 17;
            text.enableAutoSizing = true;
            text.fontSizeMin = 11;
            text.fontSizeMax = 17;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.characterSpacing = 1;
            text.color = Color.white;
            text.raycastTarget = false;
            texts.Add(text);

            if(i < 4)
            {
                GameObject arrowObject = new GameObject("Next Arrow " + i,
                    typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(TextMeshProUGUI));
                arrowObject.transform.SetParent(trackerRoot, false);
                RectTransform arrowRect =
                    arrowObject.GetComponent<RectTransform>();
                arrowRect.anchorMin = new Vector2(0, .5f);
                arrowRect.anchorMax = new Vector2(0, .5f);
                arrowRect.sizeDelta = new Vector2(26, 52);
                arrowRect.anchoredPosition = new Vector2(
                    firstPosition + (i + .5f) * spacing, 0);
                TMP_Text arrow = arrowObject.GetComponent<TMP_Text>();
                arrow.font = TMP_Settings.defaultFontAsset;
                arrow.text = "›";
                arrow.fontSize = 30;
                arrow.fontStyle = FontStyles.Bold;
                arrow.alignment = TextAlignmentOptions.Center;
                arrow.raycastTarget = false;
                arrows.Add(arrow);
            }
        }

        shownRound = Mathf.Max(1, RunData.instance.round);
        Refresh();
    }

    private void Update()
    {
        int round = Mathf.Max(1, RunData.instance.round);
        if(!moving && round != shownRound)
        {
            if(round == shownRound + 1)
                StartCoroutine(Advance(round));
            else
            {
                shownRound = round;
                Refresh();
            }
        }

        if(!moving && cards.Count > 0)
        {
            bool boss = shownRound % 3 == 0;
            float pulse = Mathf.Sin(Time.unscaledTime * 2.8f)
                * (boss ? .014f : .008f);
            cards[0].localScale = Vector3.one * (1.12f + pulse);
        }
    }

    private void Refresh()
    {
        for(int i = 0; i < cards.Count; i++)
        {
            cards[i].anchoredPosition =
                new Vector2(firstPosition + i * spacing, 0);
            SetCard(i, shownRound + i);
        }
        SetArrows(1);
    }

    private void SetCard(int i, int round)
    {
        bool boss = round % 3 == 0;
        bool current = i == 0;
        string roundType = boss ?
            RunData.Bosses[RunData.instance.bossOrder[
                (round / 3 - 1) % RunData.instance.bossOrder.Count]] : "NORMAL";
        texts[i].text = current ?
            $"NOW  •  R{round}\n{roundType}" :
            $"ROUND {round}\n{roundType}";
        images[i].color = boss ?
            current ? new Color(.42f, .045f, .025f, .98f) :
                new Color(.18f, .025f, .025f, .94f) :
            current ? new Color(.025f, .22f, .36f, .98f) :
                new Color(.025f, .075f, .12f, .94f);
        accents[i].color = boss ?
            new Color(1, .28f, .08f) : new Color(.12f, .72f, 1);
        outlines[i].effectColor = current ?
            boss ? new Color(1, .55f, .08f, .9f) :
                new Color(.25f, .82f, 1, .9f) :
            new Color(0, 0, 0, .65f);
        groups[i].alpha = current ? 1 : .78f;
        cards[i].localScale =
            Vector3.one * (current ? 1.12f : .92f);
    }

    private void SetArrows(float alpha)
    {
        for(int i = 0; i < arrows.Count; i++)
        {
            bool leadsToBoss = (shownRound + i + 1) % 3 == 0;
            Color color = leadsToBoss ?
                new Color(1, .4f, .12f, .75f * alpha) :
                new Color(.35f, .76f, 1, .58f * alpha);
            arrows[i].color = color;
            arrows[i].rectTransform.anchoredPosition = new Vector2(
                firstPosition + (i + .5f) * spacing, 0);
        }
    }

    private IEnumerator Advance(int round)
    {
        moving = true;
        List<Vector2> starts = new();
        for(int i = 0; i < cards.Count; i++)
            starts.Add(cards[i].anchoredPosition);

        float time = 0;
        while(time < .2f)
        {
            time += Time.unscaledDeltaTime;
            float amount = Mathf.Clamp01(time / .2f);
            amount = amount * amount * (3 - 2 * amount);
            for(int i = 0; i < cards.Count; i++)
                cards[i].anchoredPosition =
                    starts[i] + Vector2.left * spacing * amount;
            groups[0].alpha = 1 - amount;
            SetArrows(1 - amount);
            yield return null;
        }

        RectTransform firstCard = cards[0];
        TMP_Text firstText = texts[0];
        Image firstImage = images[0];
        Image firstAccent = accents[0];
        Outline firstOutline = outlines[0];
        CanvasGroup firstGroup = groups[0];
        cards.RemoveAt(0);
        texts.RemoveAt(0);
        images.RemoveAt(0);
        accents.RemoveAt(0);
        outlines.RemoveAt(0);
        groups.RemoveAt(0);
        cards.Add(firstCard);
        texts.Add(firstText);
        images.Add(firstImage);
        accents.Add(firstAccent);
        outlines.Add(firstOutline);
        groups.Add(firstGroup);

        shownRound = round;
        SetCard(4, round + 4);
        Vector2 end = new Vector2(firstPosition + 4 * spacing, 0);
        Vector2 start = end + Vector2.right * spacing;
        cards[4].anchoredPosition = start;
        cards[4].localScale = Vector3.one * .65f;
        groups[4].alpha = 0;
        SetArrows(0);

        time = 0;
        while(time < .15f)
        {
            time += Time.unscaledDeltaTime;
            float amount = Mathf.Clamp01(time / .15f);
            amount = 1 - Mathf.Pow(1 - amount, 3);
            cards[4].anchoredPosition = Vector2.Lerp(start, end, amount);
            cards[4].localScale =
                Vector3.one * Mathf.Lerp(.65f, .92f, amount);
            groups[4].alpha = amount * .72f;
            SetArrows(amount);
            yield return null;
        }

        Refresh();
        moving = false;
    }
}
