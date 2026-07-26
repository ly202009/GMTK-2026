using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundTrackerHUD : MonoBehaviour
{
    [SerializeField] private RectTransform trackerRoot;

    private List<Image> nodes = new();
    private List<TMP_Text> bossNames = new();
    private Sprite arrowSprite;
    private Sprite circleSprite;
    private Sprite bossSprite;
    private int shownRound;
    private float spacing = 112;
    private float firstPosition = 40;

    private void Start()
    {
        Sprite[] timelineSprites =
            Resources.LoadAll<Sprite>("Icons and other sprites");
        foreach(Sprite sprite in timelineSprites)
        {
            if(sprite.name == "Arrow")
                arrowSprite = sprite;
            if(sprite.name == "Icon Active")
                circleSprite = sprite;
            if(sprite.name == "Boss Icon")
                bossSprite = sprite;
        }

        for(int i = 0; i < 5; i++)
        {
            GameObject nodeObject = new GameObject("Round " + i,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            nodeObject.transform.SetParent(trackerRoot, false);
            RectTransform nodeRect = nodeObject.GetComponent<RectTransform>();
            nodeRect.anchorMin = new Vector2(0, .5f);
            nodeRect.anchorMax = new Vector2(0, .5f);
            nodeRect.anchoredPosition =
                new Vector2(firstPosition + i * spacing, 0);
            Image node = nodeObject.GetComponent<Image>();
            node.preserveAspect = true;
            node.raycastTarget = false;
            nodes.Add(node);

            GameObject nameObject = new GameObject("Boss Name",
                typeof(RectTransform), typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            nameObject.transform.SetParent(nodeObject.transform, false);
            RectTransform nameRect = nameObject.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(.5f, .5f);
            nameRect.anchorMax = new Vector2(.5f, .5f);
            nameRect.anchoredPosition = new Vector2(0, -47);
            nameRect.sizeDelta = new Vector2(140, 24);
            TMP_Text bossName = nameObject.GetComponent<TMP_Text>();
            bossName.font = TMP_Settings.defaultFontAsset;
            bossName.fontSize = 16;
            bossName.enableAutoSizing = true;
            bossName.fontSizeMin = 8;
            bossName.fontSizeMax = 16;
            bossName.fontStyle = FontStyles.Bold;
            bossName.alignment = TextAlignmentOptions.Center;
            bossName.color = Color.white;
            bossName.raycastTarget = false;
            bossNames.Add(bossName);

            if(i < 4)
            {
                GameObject arrowObject = new GameObject("Arrow " + i,
                    typeof(RectTransform), typeof(CanvasRenderer),
                    typeof(Image));
                arrowObject.transform.SetParent(trackerRoot, false);
                RectTransform arrowRect =
                    arrowObject.GetComponent<RectTransform>();
                arrowRect.anchorMin = new Vector2(0, .5f);
                arrowRect.anchorMax = new Vector2(0, .5f);
                arrowRect.anchoredPosition = new Vector2(
                    firstPosition + (i + .5f) * spacing, 0);
                arrowRect.sizeDelta = new Vector2(30, 30);
                Image arrow = arrowObject.GetComponent<Image>();
                arrow.sprite = arrowSprite;
                arrow.preserveAspect = true;
                arrow.raycastTarget = false;
                arrow.color = new Color(1, 1, 1, .65f);
            }
        }

        shownRound = Mathf.Max(1, RunData.instance.round);
        Refresh();
    }

    private void Update()
    {
        int round = Mathf.Max(1, RunData.instance.round);
        if(round == shownRound)
            return;
        shownRound = round;
        Refresh();
    }

    private void Refresh()
    {
        for(int i = 0; i < nodes.Count; i++)
        {
            int round = shownRound + i;
            bool boss = round % 3 == 0;
            bool current = i == 0;
            nodes[i].sprite = boss ? bossSprite : circleSprite;
            nodes[i].color = new Color(1, 1, 1, current ? 1 : .55f);
            float size = boss ? 60 : 52;
            if(current)
                size += 4;
            nodes[i].rectTransform.sizeDelta = new Vector2(size, size);
            bossNames[i].text = boss ?
                RunData.Bosses[RunData.instance.bossOrder[
                    (round / 3 - 1) % RunData.instance.bossOrder.Count]] : "";
            bossNames[i].color =
                new Color(1, .08f, .08f, current ? 1 : .75f);
        }
    }
}
