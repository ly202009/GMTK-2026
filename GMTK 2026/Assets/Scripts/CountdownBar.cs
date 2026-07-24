using TMPro;
using UnityEngine;

public class CountdownBar : MonoBehaviour
{
    [SerializeField] private RectTransform countdownBar;
    [SerializeField] private TMP_Text countdownText;

    private void Update()
    {
        if(countdownBar != null)
            countdownBar.sizeDelta =
                new Vector2(Mathf.Max(0, RunData.instance.countdown) * 16, 48);
        countdownText.text = RunData.instance.countdown.ToString();
    }
}
