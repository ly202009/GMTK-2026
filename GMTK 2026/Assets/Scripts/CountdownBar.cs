using TMPro;
using UnityEngine;

public class CountdownBar : MonoBehaviour
{
    [SerializeField] private RectTransform countdownBar;
    [SerializeField] private TMP_Text countdownText;

    private void Update()
    {
        countdownBar.sizeDelta =
            new Vector2(48, RunData.instance.countdownValue * 9);
        countdownText.text = RunData.instance.countdown.ToString();
    }
}
