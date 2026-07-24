using TMPro;
using UnityEngine;

public class ShopCountdown : MonoBehaviour
{
    [SerializeField] private TMP_Text countdownText;

    private void Update()
    {
        countdownText.text = RunData.instance.countdown.ToString();
    }
}
