using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private CanvasGroup startScreen;
    [SerializeField] private CanvasGroup menu;
    [SerializeField] private RectTransform title;
    [SerializeField] private RectTransform prompt;
    [SerializeField] private RectTransform tvZoom;
    [SerializeField] private RectTransform menuPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    private bool showingMenu;
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
        playButton.onClick.AddListener(Play);
        quitButton.onClick.AddListener(Quit);
    }

    private void Update()
    {
        title.localScale = Vector3.one
            * (1 + Mathf.Sin(Time.unscaledTime * 2) * .012f);
        prompt.localScale = Vector3.one
            * (1 + Mathf.Sin(Time.unscaledTime * 4) * .025f);
        menuPanel.anchoredPosition = menuPosition + Vector2.up
            * Mathf.Sin(Time.unscaledTime * 1.5f) * 3;

        if (showingMenu) return;
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame
        || Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame
        || Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame
        || Gamepad.current.startButton.wasPressedThisFrame)
        || Touchscreen.current != null
        && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            StartCoroutine(ShowMenu());
    }

    private IEnumerator ShowMenu()
    {
        showingMenu = true;
        startScreen.blocksRaycasts = false;
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
        RunData.instance.SetMenu(false);
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
