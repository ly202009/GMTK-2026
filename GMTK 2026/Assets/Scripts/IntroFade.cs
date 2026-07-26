using UnityEngine;
using System.Collections;

public class IntroFade : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private SpriteRenderer sprite;

    [SerializeField] private Color startColor = Color.black;
    [SerializeField] private Color endColor = Color.white;

    [SerializeField] private float delay = 2f;
    [SerializeField] private float fadeDuration = 2f;

    private IEnumerator Start()
    {
        cam.backgroundColor = startColor;
        sprite.color = startColor;

        yield return new WaitForSeconds(delay);

        float t = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float p = t / fadeDuration;

            cam.backgroundColor = Color.Lerp(startColor, endColor, p);
            sprite.color = Color.Lerp(startColor, endColor, p);

            yield return null;
        }

        cam.backgroundColor = endColor;
        sprite.color = endColor;
    }
}