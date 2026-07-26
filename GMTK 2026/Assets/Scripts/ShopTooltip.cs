using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ShopTooltip : MonoBehaviour
{
    public static ShopTooltip instance;

    [SerializeField] private CanvasGroup group;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    private float target;
    private object owner;
    private Canvas canvas;

    private void Awake()
    {
        instance = this;
        canvas = GetComponentInParent<Canvas>();
        Image background = panel.GetComponent<Image>();
        background.sprite = UIIcons.Get("Tooltip Background");
        background.color = Color.white;
        background.type = Image.Type.Simple;
        group.alpha = 0;
        group.blocksRaycasts = false;
    }

    private void Update()
    {
        bool opening = group.alpha == 0 && target > 0;
        group.alpha = Mathf.MoveTowards(group.alpha, target,
            Time.unscaledDeltaTime * 10);
        panel.localScale = Vector3.one * (.92f + group.alpha * .08f);
        if(target == 0 || Mouse.current == null) return;

        Vector2 position = Mouse.current.position.ReadValue();
        position += new Vector2(24, -24);
        Vector2 size = panel.rect.size * canvas.scaleFactor;
        position.x = Mathf.Clamp(position.x, 12,
            Screen.width - size.x - 12);
        position.y = Mathf.Clamp(position.y, size.y + 12,
            Screen.height - 12);
        panel.position = opening ? position : Vector2.Lerp(panel.position,
            position, 1 - Mathf.Exp(-24 * Time.unscaledDeltaTime));
    }

    public void Show(object newOwner, string title, string body)
    {
        owner = newOwner;
        titleText.text = title;
        bodyText.text = body;
        target = 1;
    }

    public void Hide(object oldOwner)
    {
        if(owner != oldOwner) return;
        owner = null;
        target = 0;
    }

    private void OnDisable()
    {
        target = 0;
        owner = null;
        group.alpha = 0;
    }
}

public class ShopTooltipTarget : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler
{
    private string title;
    private string body;
    private bool hovered;

    public void Configure(string newTitle, string newBody)
    {
        title = newTitle;
        body = newBody;
        if(hovered && ShopTooltip.instance != null)
            ShopTooltip.instance.Show(this, title, body);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
        if(ShopTooltip.instance != null)
            ShopTooltip.instance.Show(this, title, body);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        if(ShopTooltip.instance != null)
            ShopTooltip.instance.Hide(this);
    }

    private void OnDisable()
    {
        hovered = false;
        if(ShopTooltip.instance != null)
            ShopTooltip.instance.Hide(this);
    }
}
