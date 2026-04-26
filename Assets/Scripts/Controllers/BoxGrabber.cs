using UnityEngine;

public class BoxGrabber : MonoBehaviour
{
    [Header("Paramètres")]
    [Tooltip("Portée max pour attraper un carton")]
    public float grabDistance = 3f;
    [Tooltip("Distance de maintien devant la caméra")]
    public float holdDistance = 2.0f;
    [Tooltip("Force de rappel vers la position cible")]
    public float moveForce    = 300f;
    [Tooltip("Amortissement - évite les oscillations")]
    public float damping      = 15f;
    [Tooltip("Distance max avant lâcher automatique")]
    public float maxHoldDist  = 4.5f;

    [Header("Référence")]
    [Tooltip("Auto-détectée si vide")]
    public Camera playerCamera;

    private Rigidbody _held;
    private bool      _hadGravity;

    void Start()
    {
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>() ?? Camera.main;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && _held == null) TryGrab();
        if (Input.GetMouseButtonUp(0))                    Release();

        if (_held != null && Vector3.Distance(transform.position, _held.position) > maxHoldDist)
            Release();
    }

    void FixedUpdate()
    {
        if (_held == null) return;

        Vector3 target = playerCamera.transform.position
                       + playerCamera.transform.forward * holdDistance;

        _held.AddForce((target - _held.position) * moveForce - _held.linearVelocity * damping);

        _held.angularVelocity = Vector3.Lerp(
            _held.angularVelocity, Vector3.zero, Time.fixedDeltaTime * 12f);
    }

    private void TryGrab()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, grabDistance)) return;
        if (!hit.collider.CompareTag("Carton")) return;

        Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
        if (rb == null) return;

        _held           = rb;
        _hadGravity     = rb.useGravity;
        rb.useGravity   = false;
    }

    private void Release()
    {
        if (_held == null) return;
        _held.useGravity = true;
        _held            = null;
    }
}
