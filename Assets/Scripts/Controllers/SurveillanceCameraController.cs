using UnityEngine;
using System.Collections;

public class SurveillanceCameraController : MonoBehaviour
{
    [Header("Rotation")]
    public float angleMax   = 45f;
    public float vitesse    = 30f;
    public float rotationX  = 20f;
    [Tooltip("Durée de la pause quand la caméra atteint l'angle max")]
    public float pauseAngle = 1.2f;

    [Header("LED")]
    public Renderer led;

    private float angleActuel = 0f;
    private float direction   = 1f;
    private bool  active      = true;
    private bool  enPause     = false;

    void Start()
    {
        if (led != null)
            StartCoroutine(ClignoterLED());
    }

    void Update()
    {
        if (!active || enPause) return;

        angleActuel += vitesse * direction * Time.deltaTime;

        if (angleActuel >= angleMax)
        {
            angleActuel = angleMax;
            StartCoroutine(Pause(-1f));
        }
        else if (angleActuel <= -angleMax)
        {
            angleActuel = -angleMax;
            StartCoroutine(Pause(1f));
        }

        transform.localRotation = Quaternion.Euler(rotationX, angleActuel, 0f);
    }

    public void CutPower()
    {
        active = false;
        StopAllCoroutines();
        if (led != null) led.enabled = false;
    }

    private IEnumerator Pause(float nouvelleDirection)
    {
        enPause = true;
        yield return new WaitForSeconds(pauseAngle);
        direction = nouvelleDirection;
        enPause = false;
    }

    private IEnumerator ClignoterLED()
    {
        while (true)
        {
            led.enabled = true;
            yield return new WaitForSeconds(1f);
            led.enabled = false;
            yield return new WaitForSeconds(1f);
        }
    }
}
