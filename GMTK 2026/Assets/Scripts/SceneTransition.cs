using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    public static SceneTransition instance;

    [SerializeField] private CanvasGroup fadeGroup;
    private bool loading;

    private void Awake()
    {
        instance = this;
        fadeGroup.alpha = 1;
        fadeGroup.blocksRaycasts = true;
        StartCoroutine(FadeIn());
    }

    public static void Load(string scene)
    {
        if(instance == null)
        {
            SceneManager.LoadScene(scene);
            return;
        }
        if(!instance.loading)
            instance.StartCoroutine(instance.ChangeScene(scene));
    }

    private IEnumerator FadeIn()
    {
        yield return Fade(1, 0, .2f);
        fadeGroup.blocksRaycasts = false;
    }

    private IEnumerator ChangeScene(string scene)
    {
        loading = true;
        fadeGroup.blocksRaycasts = true;
        yield return Fade(fadeGroup.alpha, 1, .14f);
        SceneManager.LoadScene(scene);
        yield return null;
        yield return Fade(1, 0, .2f);
        fadeGroup.blocksRaycasts = false;
        loading = false;
    }

    private IEnumerator Fade(float start, float end, float duration)
    {
        float time = 0;
        while(time < duration)
        {
            time += Time.unscaledDeltaTime;
            float amount = Mathf.Clamp01(time / duration);
            amount = amount * amount * (3 - 2 * amount);
            fadeGroup.alpha = Mathf.Lerp(start, end, amount);
            yield return null;
        }
        fadeGroup.alpha = end;
    }
}
