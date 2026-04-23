using UnityEngine;

public class PowerCutTrigger : MonoBehaviour
{
    [Header("Son de coupure")]
    public AudioClip sonCoupure;
    [Range(0f, 1f)]
    public float volume = 1f;

    private bool triggered = false;

    void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.CompareTag("Player")) return;

        triggered = true;

        if (sonCoupure != null)
            AudioSource.PlayClipAtPoint(sonCoupure, transform.position, volume);

        foreach (FlickeringNeon neon in FindObjectsByType<FlickeringNeon>(FindObjectsSortMode.None))
            neon.SwitchToSecurity();
    }
}
