using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CountdownBar : MonoBehaviour
{
    [SerializeField] private RectTransform countdownBar;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private Image countdownImage;
    [SerializeField] private RectTransform numberBox;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private RectTransform speedBox;
    [SerializeField] private TMP_Text overflowText;
    [SerializeField] private RectTransform overflowBox;
    [SerializeField] private CanvasGroup overflowGroup;
    [SerializeField] private TMP_Text anteText;
    [SerializeField] private RectTransform anteBox;
    [SerializeField] private CanvasGroup anteGroup;

    private float shownHeight;
    private float numberPunch;
    private float barPunch;
    private int shownCountdown;
    private Color barColor;
    private float shownSpeed;
    private float speedPunch;
    private float shownOverflow = 1;
    private float overflowAmount;
    private float overflowPunch;
    private Vector2 antePosition;

    private void Awake()
    {
        antePosition = anteBox.anchoredPosition;
        anteGroup.alpha = 0;
        overflowGroup.alpha = 0;
    }

    private void Start()
    {
        shownHeight = RunData.instance.countdownValue * 7.2f;
        shownCountdown = RunData.instance.countdown;
        barColor = countdownImage.color;
        countdownText.fontStyle = FontStyles.Bold;
        shownSpeed = RunData.instance.timerDrainSpeed;
        speedText.fontStyle = FontStyles.Bold;
    }

    private void Update()
    {
        int countdown = RunData.instance.countdown;
        shownHeight = Mathf.Lerp(shownHeight,
            RunData.instance.countdownValue * 7.2f,
            1 - Mathf.Exp(-18 * Time.unscaledDeltaTime));
        countdownBar.sizeDelta =
            new Vector2(48, shownHeight);

        if(countdown != shownCountdown)
        {
            numberPunch = countdown > shownCountdown ? .2f : .065f;
            barPunch = countdown > shownCountdown ? .14f : .045f;
            shownCountdown = countdown;
        }
        numberPunch = Mathf.MoveTowards(numberPunch, 0,
            Time.unscaledDeltaTime * 1.5f);
        numberBox.localScale = Vector3.one * (1 + numberPunch);
        barPunch = Mathf.MoveTowards(barPunch, 0,
            Time.unscaledDeltaTime * 1.2f);
        countdownBar.localScale = new Vector3(1 + barPunch, 1, 1);

        if(countdown <= 10)
        {
            float pulse = .5f + Mathf.Sin(Time.unscaledTime * 9) * .5f;
            countdownImage.color = Color.Lerp(barColor,
                new Color(1, .12f, .02f), pulse);
            countdownText.color = Color.Lerp(new Color(1, .08f, .08f),
                new Color(1, .75f, .12f), pulse);
        }
        else
        {
            countdownImage.color = barColor;
            countdownText.color = new Color(1, .08f, .08f);
        }

        countdownText.text = countdown.ToString();

        float speed = RunData.instance.timerDrainSpeed;
        if(Mathf.Abs(speed - shownSpeed) > .001f) speedPunch = .16f;
        shownSpeed = speed;
        speedPunch = Mathf.MoveTowards(speedPunch, 0,
            Time.unscaledDeltaTime * 1.8f);
        speedBox.localScale = Vector3.one * (1 + speedPunch);
        speedText.text = speed <= 0 ? "FROZEN" : $"SPEED {speed:0.00}x";
        speedText.color = speed <= 0 ? new Color(.2f, .85f, 1) :
            speed < .99f ? new Color(.3f, 1, .42f) :
            speed > 1.01f ? new Color(1, .45f, .12f) : Color.white;

        float overflow = RunData.instance.overflowDrainMultiplier;
        if(Mathf.Abs(overflow - shownOverflow) > .001f) overflowPunch = .2f;
        shownOverflow = overflow;
        overflowAmount = Mathf.MoveTowards(overflowAmount,
            overflow > 1 ? 1 : 0, Time.unscaledDeltaTime * 5);
        overflowPunch = Mathf.MoveTowards(overflowPunch, 0,
            Time.unscaledDeltaTime * 1.8f);
        overflowGroup.alpha = overflowAmount;
        overflowBox.localScale = Vector3.one
            * (.82f + overflowAmount * .18f + overflowPunch);
        overflowText.text = "OVERFLOW  +40% DRAIN";
        overflowText.color = new Color(1, .55f, .08f);
    }

    public void ShowAnte(int cost)
    {
        StartCoroutine(AnimateAnte(cost));
    }

    private IEnumerator AnimateAnte(int cost)
    {
        yield return new WaitForSecondsRealtime(.18f);
        anteText.text = $"ROUND {RunData.instance.round} ANTE\n-{cost} SECONDS";
        anteGroup.alpha = 1;
        float time = 0;
        while(time < .18f)
        {
            time += Time.unscaledDeltaTime;
            float amount = Mathf.Clamp01(time / .18f);
            amount = 1 - Mathf.Pow(1 - amount, 3);
            anteBox.localScale = Vector3.one * Mathf.Lerp(.25f, 1.12f, amount);
            anteBox.anchoredPosition = antePosition
                + Vector2.up * Mathf.Lerp(-45, 0, amount);
            anteBox.localRotation = Quaternion.Euler(0, 0,
                Mathf.Lerp(-8, 1, amount));
            yield return null;
        }

        time = 0;
        while(time < .65f)
        {
            time += Time.unscaledDeltaTime;
            float shake = (1 - time / .65f) * 7;
            anteBox.anchoredPosition = antePosition
                + UnityEngine.Random.insideUnitCircle * shake;
            anteBox.localScale = Vector3.one
                * (1 + Mathf.Sin(time * 18) * .025f);
            yield return null;
        }

        time = 0;
        while(time < .28f)
        {
            time += Time.unscaledDeltaTime;
            float amount = Mathf.Clamp01(time / .28f);
            anteGroup.alpha = 1 - amount;
            anteBox.localScale = Vector3.one * (1 + amount * .18f);
            yield return null;
        }
        anteGroup.alpha = 0;
        anteBox.anchoredPosition = antePosition;
        anteBox.localRotation = Quaternion.identity;
    }
}
