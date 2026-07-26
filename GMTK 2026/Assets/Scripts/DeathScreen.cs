using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class DeathScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private RectTransform background;
    [SerializeField] private RectTransform panel;
    [SerializeField] private RectTransform title;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button quitButton;

    private bool shown;
    private bool loading;

    private void OnEnable()
    {
        Hide();
        loading = false;
    }

    private void Start()
    {
        retryButton.onClick.AddListener(Retry);
        quitButton.onClick.AddListener(Quit);
    }

    private void Update()
    {
        if(!shown && RunData.instance.countdown <= 0)
            StartCoroutine(Show());

        if(!shown) return;
        background.localScale = Vector3.one
            * (1.045f + Mathf.Sin(Time.unscaledTime * 1.4f) * .008f);
        title.localScale = Vector3.one
            * (1 + Mathf.Sin(Time.unscaledTime * 3.5f) * .018f);

        if(Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            Retry();
        if(Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Quit();
    }

    private IEnumerator Show()
    {
        shown = true;
        loading = false;
        RunData.instance.SetTimerFrozen(true);
        Time.timeScale = 0;
        resultText.text = $"ROUND {RunData.instance.round}\nTHE CLOCK WON";
        group.blocksRaycasts = true;

        float time = 0;
        while(time < .38f)
        {
            time += Time.unscaledDeltaTime;
            float amount = Mathf.Clamp01(time / .38f);
            float pop = 1 + 2.7f * Mathf.Pow(amount - 1, 3)
                + 1.7f * Mathf.Pow(amount - 1, 2);
            group.alpha = amount;
            panel.localScale = Vector3.one * Mathf.Lerp(.55f, 1, pop);
            panel.anchoredPosition = Vector2.up * Mathf.Lerp(-80, 0, pop);
            panel.localRotation = Quaternion.Euler(0, 0,
                Mathf.Lerp(-6, 0, pop));
            yield return null;
        }

        panel.localScale = Vector3.one;
        panel.anchoredPosition = Vector2.zero;
        panel.localRotation = Quaternion.identity;
        group.interactable = true;
        retryButton.Select();
    }

    private void Retry()
    {
        if(loading) return;
        loading = true;
        Hide();
        Time.timeScale = 1;
        RunData.instance.ResetRun();
        RunData.instance.SetMenu(false);
        SceneTransition.Load("MainScene");
    }

    private void Quit()
    {
        if(loading) return;
        loading = true;
        Hide();
        Time.timeScale = 1;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void Hide()
    {
        StopAllCoroutines();
        shown = false;
        group.alpha = 0;
        group.interactable = false;
        group.blocksRaycasts = false;
        background.localScale = Vector3.one;
        panel.localScale = Vector3.one;
        panel.anchoredPosition = Vector2.zero;
        panel.localRotation = Quaternion.identity;
    }
}
