using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CountdownBar : MonoBehaviour
{
    [SerializeField] private RectTransform countdownBar;
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private Image countdownImage;
    [SerializeField] private RectTransform numberBox;

    private float shownHeight;
    private float numberPunch;
    private float barPunch;
    private int shownCountdown;
    private Color barColor;

    private void Start()
    {
        shownHeight = RunData.instance.countdownValue * 9;
        shownCountdown = RunData.instance.countdown;
        barColor = countdownImage.color;
        countdownText.fontStyle = FontStyles.Bold;
    }

    private void Update()
    {
        int countdown = RunData.instance.countdown;
        shownHeight = Mathf.Lerp(shownHeight,
            RunData.instance.countdownValue * 9,
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
    }
}
