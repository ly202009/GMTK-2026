using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public static class GameSettings
{
    public static float masterVolume =
        PlayerPrefs.GetFloat("MasterVolume", 1);
    public static float musicVolume =
        PlayerPrefs.GetFloat("MusicVolume", 1);
    public static float sfxVolume =
        PlayerPrefs.GetFloat("SfxVolume", 1);
    public static bool fullscreen =
        PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;

    public static void Save()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SfxVolume", sfxVolume);
        PlayerPrefs.SetInt("Fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
}

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private CanvasGroup group;
    [SerializeField] private RectTransform panel;
    [SerializeField] private TMP_Text title;
    [SerializeField] private Button buttonTemplate;

    private Button[] buttons = new Button[6];
    private TMP_Text[] labels = new TMP_Text[6];
    private bool open;
    private bool settings;
    private float openAmount;

    private void Awake()
    {
        group.alpha = 0;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void Start()
    {
        Screen.fullScreen = GameSettings.fullscreen;
        for(int i = 0; i < buttons.Length; i++)
        {
            buttons[i] = i == 0 ? buttonTemplate :
                Instantiate(buttonTemplate, buttonTemplate.transform.parent);
            buttons[i].name = "Pause Option " + i;
            labels[i] = buttons[i].GetComponentInChildren<TMP_Text>();
        }
        ShowPause();
    }

    private void Update()
    {
        openAmount = Mathf.MoveTowards(openAmount, open ? 1 : 0,
            Time.unscaledDeltaTime * 7);
        group.alpha = openAmount;
        panel.localScale = Vector3.one
            * (.82f + openAmount * .18f);
        panel.localRotation = Quaternion.Euler(0, 0,
            (1 - openAmount) * -3);

        if(RunData.instance.countdown <= 0) return;
        if(SpeedTutorial.instance != null && SpeedTutorial.instance.IsOpen)
            return;
        if(Keyboard.current == null
        || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if(!open) Open();
        else if(settings) ShowPause();
        else Close();
    }

    private void Open()
    {
        open = true;
        settings = false;
        group.interactable = true;
        group.blocksRaycasts = true;
        RunData.instance.SetPaused(true);
        Time.timeScale = 0;
        ShowPause();
    }

    private void Close()
    {
        open = false;
        group.interactable = false;
        group.blocksRaycasts = false;
        RunData.instance.SetPaused(false);
        Time.timeScale = 1;
    }

    private void ShowPause()
    {
        settings = false;
        title.text = "PAUSED";
        if(SpeedTutorial.instance != null)
        {
            SetButton(0, "RESUME", Close, 130);
            SetButton(1, "HOW TO PLAY", Tutorial, 30);
            SetButton(2, "SETTINGS", ShowSettings, -70);
            SetButton(3, "MAIN MENU", MainMenu, -170);
            for(int i = 4; i < buttons.Length; i++)
                buttons[i].gameObject.SetActive(false);
        }
        else
        {
            SetButton(0, "RESUME", Close, 80);
            SetButton(1, "SETTINGS", ShowSettings, -35);
            SetButton(2, "MAIN MENU", MainMenu, -150);
            for(int i = 3; i < buttons.Length; i++)
                buttons[i].gameObject.SetActive(false);
        }
    }

    private void Tutorial()
    {
        if(SpeedTutorial.instance == null) return;
        Close();
        SpeedTutorial.instance.Open();
    }

    private void ShowSettings()
    {
        settings = true;
        title.text = "SETTINGS";
        RefreshSettings();
        SetButton(0, labels[0].text, CycleMaster, 170);
        SetButton(1, labels[1].text, CycleMusic, 78);
        SetButton(2, labels[2].text, CycleSfx, -14);
        SetButton(3, labels[3].text, ToggleFullscreen, -106);
        SetButton(4, "BACK", ShowPause, -198);
        buttons[5].gameObject.SetActive(false);
    }

    private void RefreshSettings()
    {
        labels[0].text = $"MASTER VOLUME  {GameSettings.masterVolume * 100:0}%";
        labels[1].text = $"MUSIC VOLUME  {GameSettings.musicVolume * 100:0}%";
        labels[2].text = $"SFX VOLUME  {GameSettings.sfxVolume * 100:0}%";
        labels[3].text = "FULLSCREEN  "
            + (GameSettings.fullscreen ? "ON" : "OFF");
    }

    private void SetButton(int i, string text,
        UnityEngine.Events.UnityAction action, float y)
    {
        buttons[i].gameObject.SetActive(true);
        buttons[i].onClick.RemoveAllListeners();
        buttons[i].onClick.AddListener(action);
        labels[i].text = text;
        RectTransform rect = buttons[i].GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0, y);
        buttons[i].GetComponent<AnimatedButton>()
            .SetBasePosition(rect.anchoredPosition);
    }

    private float NextVolume(float value)
    {
        return value >= 1 ? 0 : value + .25f;
    }

    private void CycleMaster()
    {
        GameSettings.masterVolume = NextVolume(GameSettings.masterVolume);
        SavedSettings();
    }

    private void CycleMusic()
    {
        GameSettings.musicVolume = NextVolume(GameSettings.musicVolume);
        SavedSettings();
    }

    private void CycleSfx()
    {
        GameSettings.sfxVolume = NextVolume(GameSettings.sfxVolume);
        SavedSettings();
    }

    private void ToggleFullscreen()
    {
        GameSettings.fullscreen = !GameSettings.fullscreen;
        Screen.fullScreen = GameSettings.fullscreen;
        SavedSettings();
    }

    private void SavedSettings()
    {
        GameSettings.Save();
        RefreshSettings();
    }

    private void MainMenu()
    {
        Close();
        SceneTransition.Load("MainMenu");
    }
}
