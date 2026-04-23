using UnityEngine;
using System.Collections;

public class SurveillanceCameraController : MonoBehaviour
{
    [Header("Rotation")]
    public float angleMax  = 45f;
    public float vitesse   = 30f;
    public float rotationX = 20f;

    [Header("LED")]
    public Renderer led;
    public float dureeAllumee = 1f;
    public float dureeEteinte = 1f;

    private float angleActuel = 0f;
    private float direction   = 1f;
    private bool  active      = true;

    void Start()
    {
        if (led != null)
            StartCoroutine(ClignoterLED());
    }

    void Update()
    {
        if (!active) return;

        angleActuel += vitesse * direction * Time.deltaTime;

        if (angleActuel >= angleMax)
        {
            angleActuel = angleMax;
            direction = -1f;
        }
        else if (angleActuel <= -angleMax)
        {
            angleActuel = -angleMax;
            direction = 1f;
        }

        transform.localRotation = Quaternion.Euler(rotationX, angleActuel, 0f);
    }

    public void CutPower()
    {
        active = false;
        StopAllCoroutines();
        if (led != null) led.enabled = false;
    }

    private IEnumerator ClignoterLED()
    {
        while (true)
        {
            led.enabled = true;
            yield return new WaitForSeconds(dureeAllumee);
            led.enabled = false;
            yield return new WaitForSeconds(dureeEteinte);
        }
    }
}
