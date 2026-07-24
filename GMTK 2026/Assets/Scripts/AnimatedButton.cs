using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class AnimatedButton : MonoBehaviour, IPointerEnterHandler,
    IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
    IPointerClickHandler
{
    private RectTransform rect;
    private Button button;
    private Vector3 normalScale;
    private Vector2 normalPosition;
    private float targetScale = 1;
    private float targetLift;
    private float punch;
    private float appearTime;
    private float appearDelay;
    private bool hovered;
    private bool entrancePlayed;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        button = GetComponent<Button>();
        normalScale = Vector3.one;
        appearTime = 0;
        appearDelay = 0;
        punch = 0;
        rect.localScale = normalScale * .86f;
    }

    private void Start()
    {
        normalPosition = rect.anchoredPosition;
        if(!entrancePlayed)
            appearDelay = Mathf.Min(rect.GetSiblingIndex() * .02f, .1f);
        TMP_Text text = GetComponentInChildren<TMP_Text>();
        if(text != null)
        {
            text.fontStyle |= FontStyles.Bold;
            text.characterSpacing = 2;
        }
    }

    private void Update()
    {
        if(!button.interactable)
        {
            hovered = false;
            targetScale = 1;
            targetLift = 0;
        }

        if(appearDelay > 0)
            appearDelay -= Time.unscaledDeltaTime;
        else
            appearTime = Mathf.MoveTowards(appearTime, 1,
                Time.unscaledDeltaTime * 6.5f);
        float appearAmount = 1 + 2.7f * Mathf.Pow(appearTime - 1, 3)
            + 1.7f * Mathf.Pow(appearTime - 1, 2);
        float appear = .86f + appearAmount * .14f;
        punch = Mathf.MoveTowards(punch, 0, Time.unscaledDeltaTime * .7f);
        float phase = rect.GetSiblingIndex() * .8f;
        float breathe = Mathf.Sin(Time.unscaledTime * 2.2f + phase) * .008f;
        if(hovered) breathe += Mathf.Sin(Time.unscaledTime * 5) * .004f;
        Vector3 scale = normalScale
            * ((targetScale + punch + breathe) * appear);
        rect.localScale = Vector3.Lerp(rect.localScale, scale,
            1 - Mathf.Exp(-22 * Time.unscaledDeltaTime));
        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition,
            normalPosition + Vector2.up * (targetLift
                + Mathf.Sin(Time.unscaledTime * 1.8f + phase) * 4),
            1 - Mathf.Exp(-18 * Time.unscaledDeltaTime));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(!button.interactable) return;
        hovered = true;
        targetScale = 1.045f;
        targetLift = 4;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        targetScale = 1;
        targetLift = 0;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(!button.interactable) return;
        targetScale = .965f;
        targetLift = -1;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if(!button.interactable) return;
        targetScale = hovered ? 1.045f : 1;
        targetLift = hovered ? 4 : 0;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(!button.interactable) return;
        punch = .075f;
    }

    public void PlayEntrance(float delay)
    {
        entrancePlayed = true;
        appearTime = 0;
        appearDelay = delay;
        rect.localScale = normalScale * .86f;
    }

    private void OnDisable()
    {
        if(rect == null) return;
        rect.localScale = normalScale;
        rect.anchoredPosition = normalPosition;
    }
}
