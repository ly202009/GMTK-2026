using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private CanvasGroup startScreen;
    [SerializeField] private CanvasGroup menu;
    [SerializeField] private RectTransform title;
    [SerializeField] private RectTransform prompt;
    [SerializeField] private Image promptImage;
    [SerializeField] private Button promptButton;
    [SerializeField] private RectTransform tvZoom;
    [SerializeField] private RectTransform menuPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    private bool starting;
    private Vector2 menuPosition;

    private void Start()
    {
        RunData.instance.SetMenu(true);
        startScreen.alpha = 1;
        startScreen.blocksRaycasts = true;
        menu.alpha = 0;
        menu.interactable = false;
        menu.blocksRaycasts = false;
        menuPosition = menuPanel.anchoredPosition;
        promptImage.sprite =
            Resources.LoadAll<Sprite>("start/PRESS TO START")[0];
        promptImage.color = Color.white;
        promptImage.preserveAspect = true;
        promptButton.onClick.AddListener(() => StartCoroutine(ShowMenu()));
        playButton.onClick.AddListener(Play);
        quitButton.onClick.AddListener(Quit);
    }

    private void Update()
    {
        title.localScale = Vector3.one
            * (1 + Mathf.Sin(Time.unscaledTime * 2) * .012f);
        float pulse = 1.5f + Mathf.Sin(Time.unscaledTime * 4) * .1f;
        prompt.localScale = Vector3.one * pulse;
        float flicker = .82f + Mathf.Sin(Time.unscaledTime * 3) * .12f
            + Mathf.Sin(Time.unscaledTime * 27) * .06f;
        promptImage.color = new Color(1, 1, 1, flicker);
        menuPanel.anchoredPosition = menuPosition + Vector2.up
            * Mathf.Sin(Time.unscaledTime * 1.5f) * 3;
    }

    private IEnumerator ShowMenu()
    {
        startScreen.blocksRaycasts = false;
        promptButton.interactable = false;
        Vector2 tvPosition = tvZoom.anchoredPosition;
        Vector3 tvScale = tvZoom.localScale;
        float time = 0;
        while (time < .55f)
        {
            time += Time.unscaledDeltaTime;
            float amount = Mathf.Clamp01(time / .55f);
            float zoom = amount * amount * (3 - 2 * amount);
            float fade = Mathf.Clamp01((amount - .7f) / .3f);
            fade = fade * fade * (3 - 2 * fade);
            tvZoom.localScale = tvScale * Mathf.Lerp(1, 3.6f, zoom);
            tvZoom.anchoredPosition = Vector2.Lerp(tvPosition, Vector2.zero, zoom);
            startScreen.alpha = 1 - fade;
            menu.alpha = fade;
            menuPanel.localScale = Vector3.one
                * Mathf.Lerp(.88f, 1, 1 - Mathf.Pow(1 - fade, 3));
            yield return null;
        }
        startScreen.alpha = 0;
        menu.alpha = 1;
        menu.interactable = true;
        menu.blocksRaycasts = true;
    }

    private void Play()
    {
        if (starting) return;
        starting = true;
        playButton.interactable = false;
        quitButton.interactable = false;
        RunData.instance.ResetRun();
        SceneTransition.Load("MainScene");
    }

    private void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
