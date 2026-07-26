using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeOverlay : MonoBehaviour
{
    [SerializeField] private Image overlay;
    [SerializeField] private float delay = 2f;
    [SerializeField] private float fadeTime = 1f;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(delay);

        Color c = overlay.color;

        float t = 0;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / fadeTime);
            overlay.color = c;
            yield return null;
        }

        c.a = 0;
        overlay.color = c;

        // Optional
        overlay.raycastTarget = false;
    }
}