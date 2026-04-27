using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class AmbientZone : MonoBehaviour
{
    [Tooltip("Durée du fade in/out en secondes")]
    public float fadeDuration = 1.5f;

    private AudioSource _source;
    private Coroutine   _fadeCoroutine;

    void Awake()
    {
        _source             = GetComponent<AudioSource>();
        _source.loop        = true;
        _source.spatialBlend = 0f;   // 2D — même volume partout dans la zone
        _source.volume      = 0f;
        _source.Play();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Fade(to: 1f);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        Fade(to: 0f);
    }

    private void Fade(float to)
    {
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeCoroutine(to));
    }

    private IEnumerator FadeCoroutine(float target)
    {
        float start   = _source.volume;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed        += Time.deltaTime;
            _source.volume  = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }

        _source.volume = target;
    }
}
