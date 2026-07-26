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
    private float shake;
    private float releaseLift;
    private bool hovered;
    private bool pressed;
    private bool entrancePlayed;
    public float idleFloat = 4;
    public bool holdUpOnPress;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        button = GetComponent<Button>();
        normalScale = rect.localScale;
        normalPosition = rect.anchoredPosition;
        appearTime = 0;
        appearDelay = 0;
        punch = 0;
        rect.localScale = normalScale * .86f;
    }

    private void Start()
    {
        if (!entrancePlayed)
            appearDelay = Mathf.Min(rect.GetSiblingIndex() * .02f, .1f);
        TMP_Text text = GetComponentInChildren<TMP_Text>();
        if (text != null)
        {
            text.fontStyle |= FontStyles.Bold;
            text.characterSpacing = 2;
        }
    }

    private void Update()
    {
        if (!button.interactable)
        {
            hovered = false;
            pressed = false;
            targetScale = 1;
            if (releaseLift <= 0) targetLift = 0;
        }

        if (appearDelay > 0)
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
        if (hovered) breathe += Mathf.Sin(Time.unscaledTime * 5) * .004f;
        shake = Mathf.Max(0, shake - Time.unscaledDeltaTime);
        float shakeOffset = Mathf.Sin((.22f - shake) * 65) * 10
            * (shake / .22f);
        if (releaseLift > 0)
        {
            releaseLift = Mathf.Max(0,
                releaseLift - Time.unscaledDeltaTime);
            targetLift = Mathf.Lerp(hovered ? 4 : 0,
                14, releaseLift / .14f);
        }
        Vector3 scale = normalScale
            * ((targetScale + punch + breathe) * appear);
        rect.localScale = Vector3.Lerp(rect.localScale, scale,
            1 - Mathf.Exp(-22 * Time.unscaledDeltaTime));
        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition,
            normalPosition + new Vector2(shakeOffset, targetLift
                + Mathf.Sin(Time.unscaledTime * 1.8f + phase) * idleFloat),
            1 - Mathf.Exp(-18 * Time.unscaledDeltaTime));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!button.interactable) return;
        hovered = true;
        targetScale = 1.045f;
        targetLift = 4;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        if (pressed && holdUpOnPress) return;
        targetScale = 1;
        targetLift = 0;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;
        pressed = true;

        AudioManager.UIDown();

        releaseLift = 0;
        targetScale = holdUpOnPress ? 1.035f : .965f;
        targetLift = holdUpOnPress ? 14 : -1;
        if (holdUpOnPress) punch = .075f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!button.interactable) return;
        pressed = false;

        AudioManager.UIUp();

        targetScale = hovered ? 1.045f : 1;
        if (holdUpOnPress) releaseLift = .14f;
        else targetLift = hovered ? 4 : 0;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!button.interactable) return;
        punch = .075f;
    }

    public void PlayEntrance(float delay)
    {
        entrancePlayed = true;
        appearTime = 0;
        appearDelay = delay;
        rect.localScale = normalScale * .86f;
    }

    public void SetBasePosition(Vector2 position)
    {
        normalPosition = position;
    }

    public void Reject()
    {
        shake = .22f;
    }

    private void OnDisable()
    {
        if (rect == null) return;
        pressed = false;
        releaseLift = 0;
        targetLift = 0;
        rect.localScale = normalScale;
        rect.anchoredPosition = normalPosition;
    }
}
