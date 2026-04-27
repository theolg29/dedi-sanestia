using UnityEngine;
using System.Collections;

public class HelicopterInteraction : MonoBehaviour
{
    [Header("Player References")]
    public FirstPersonController playerController;
    public Transform playerCameraTransform;

    [Header("Helicopter References")]
    public HelicopterController helicopterController;

    [Header("HUD")]
    [Tooltip("HUD_Panel dans le GameCanvas")]
    public GameObject hudPanel;
    [Tooltip("Minimap a garder visible en helico")]
    public GameObject minimapPanel;

    [Header("Settings")]
    public float interactionDistance = 5f;
    public KeyCode interactKey = KeyCode.F;

    [Header("Third-Person Camera")]
    [Tooltip("Offset derrière/au-dessus de l'hélico (espace local yaw uniquement)")]
    public Vector3 thirdPersonOffset = new Vector3(0f, 4f, -9f);
    [Tooltip("Cible de regard — point au-dessus du centre de l'hélico")]
    public float lookHeightOffset = 1f;
    public float camFollowSpeed = 6f;
    public float camRotateSpeed = 5f;

    [Header("Son")]
    public AudioSource helicopterAudio;
    public float pitchNeutral = 1f;
    public float pitchUp      = 1.35f;
    public float pitchDown    = 0.75f;
    public float pitchSmooth  = 3f;

    [Header("Startup Sequence")]
    [Tooltip("Durée du démarrage moteur avant de pouvoir voler")]
    public float startupDuration = 2.5f;
    [Tooltip("Force moteur atteinte à la fin du démarrage (ralenti)")]
    public float idleEngineForce = 8f;

    private bool isInHelicopter = false;
    private float exitCooldown  = 0f;
    private bool isStartingUp   = false;

    // Sauvegarde de la caméra avant entrée
    private Transform  originalCameraParent;
    private Vector3    originalCameraLocalPos;
    private Quaternion originalCameraLocalRot;

    IEnumerator Start()
    {
        if (playerController == null)
            playerController = FindFirstObjectByType<FirstPersonController>();

        if (playerCameraTransform == null && Camera.main != null)
            playerCameraTransform = Camera.main.transform;

        if (helicopterController == null)
            helicopterController = GetComponent<HelicopterController>();

        if (helicopterController != null && helicopterController.ControlPanel == null)
        {
            helicopterController.ControlPanel = GetComponent<ControlPanel>()
                ?? GetComponentInChildren<ControlPanel>(true)
                ?? FindFirstObjectByType<ControlPanel>();
        }

        yield return null;

        if (helicopterController != null)
        {
            helicopterController.enabled = false;
            if (helicopterController.ControlPanel)
                helicopterController.ControlPanel.enabled = false;
        }
    }

    void Update()
    {
        if (exitCooldown > 0) exitCooldown -= Time.deltaTime;

        if (!isInHelicopter)
        {
            if (playerController != null &&
                Vector3.Distance(transform.position, playerController.transform.position) < interactionDistance &&
                Input.GetKeyDown(interactKey) && exitCooldown <= 0)
            {
                EnterHelicopter();
                exitCooldown = 1f;
            }
        }
        else
        {
            if ((Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.X))
                && exitCooldown <= 0)
            {
                ExitHelicopter();
                exitCooldown = 1f;
            }

            if (!isStartingUp && helicopterAudio != null)
            {
                float targetPitch = pitchNeutral;
                if (Input.GetKey(KeyCode.LeftShift))       targetPitch = pitchUp;
                else if (Input.GetKey(KeyCode.Space))      targetPitch = pitchDown;

                helicopterAudio.pitch = Mathf.Lerp(helicopterAudio.pitch, targetPitch, Time.deltaTime * pitchSmooth);
            }
        }
    }

    void LateUpdate()
    {
        if (isInHelicopter) UpdateThirdPersonCamera();
    }

    void UpdateThirdPersonCamera()
    {
        if (playerCameraTransform == null) return;

        // Applique uniquement le yaw de l'hélico (pas le pitch/roll) — feel GTA
        Quaternion heliYaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        Vector3 targetPos = transform.position + heliYaw * thirdPersonOffset;

        playerCameraTransform.position = Vector3.Lerp(
            playerCameraTransform.position, targetPos, camFollowSpeed * Time.deltaTime);

        Vector3 lookTarget = transform.position + Vector3.up * lookHeightOffset;
        Quaternion targetRot = Quaternion.LookRotation(lookTarget - playerCameraTransform.position);
        playerCameraTransform.rotation = Quaternion.Slerp(
            playerCameraTransform.rotation, targetRot, camRotateSpeed * Time.deltaTime);
    }

    void EnterHelicopter()
    {
        // Désactiver le joueur
        playerController.playerCanMove = false;
        playerController.cameraCanMove = false;

        Collider col = playerController.GetComponent<Collider>();
        if (col) col.enabled = false;

        Rigidbody rb = playerController.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        playerController.transform.SetParent(transform);

        // Libérer la caméra pour le suivi troisième personne
        if (playerCameraTransform != null)
        {
            originalCameraParent   = playerCameraTransform.parent;
            originalCameraLocalPos = playerCameraTransform.localPosition;
            originalCameraLocalRot = playerCameraTransform.localRotation;

            playerCameraTransform.SetParent(null);

            // Placement immédiat pour éviter un saut au premier frame
            Quaternion heliYaw = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
            playerCameraTransform.position = transform.position + heliYaw * thirdPersonOffset;
            Vector3 lookTarget = transform.position + Vector3.up * lookHeightOffset;
            playerCameraTransform.rotation = Quaternion.LookRotation(lookTarget - playerCameraTransform.position);
        }

        // Activer la physique hélico mais pas encore les contrôles
        if (helicopterController != null)
        {
            helicopterController.enabled = true;
            if (helicopterController.ControlPanel)
                helicopterController.ControlPanel.enabled = false;
        }

        // Masquer le HUD
        if (hudPanel != null) hudPanel.SetActive(false);
        if (minimapPanel != null) minimapPanel.SetActive(true);

        // Masquer la main du joueur (item tenu, flashlight, clé...)
        PlayerInventory inv = playerController.GetComponent<PlayerInventory>();
        if (inv != null && inv.playerHand != null)
            inv.playerHand.gameObject.SetActive(false);

        if (helicopterAudio != null)
        {
            helicopterAudio.pitch = 0f;
            helicopterAudio.Play();
        }

        isInHelicopter = true;
        StartCoroutine(StartupSequence());
    }

    IEnumerator StartupSequence()
    {
        isStartingUp = true;
        float elapsed = 0f;

        while (elapsed < startupDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / startupDuration;

            if (helicopterController != null)
                helicopterController.EngineForce = Mathf.Lerp(0f, idleEngineForce, t);

            if (helicopterAudio != null)
                helicopterAudio.pitch = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        if (helicopterController != null)
        {
            helicopterController.EngineForce = idleEngineForce;
            if (helicopterController.ControlPanel)
                helicopterController.ControlPanel.enabled = true;
        }

        if (helicopterAudio != null)
            helicopterAudio.pitch = 1f;

        isStartingUp = false;
    }

    void ExitHelicopter()
    {
        // Désactiver les contrôles hélico
        if (helicopterController != null)
        {
            helicopterController.enabled = false;
            helicopterController.EngineForce = 0;
            if (helicopterController.ControlPanel)
                helicopterController.ControlPanel.enabled = false;
        }

        // Restaurer la caméra
        if (playerCameraTransform != null && originalCameraParent != null)
        {
            playerCameraTransform.SetParent(originalCameraParent);
            playerCameraTransform.localPosition = originalCameraLocalPos;
            playerCameraTransform.localRotation = originalCameraLocalRot;
        }

        // Repositionner le joueur
        Vector3 exitPos = transform.position + transform.right * 5f;
        exitPos.y = transform.position.y + 0.5f;

        playerController.transform.SetParent(null);
        playerController.transform.position = exitPos;
        playerController.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y + 90, 0);

        Collider col = playerController.GetComponent<Collider>();
        if (col) col.enabled = true;

        Rigidbody rb = playerController.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = false;

        playerController.playerCanMove = true;
        playerController.cameraCanMove = true;

        // Réafficher le HUD
        if (hudPanel != null) hudPanel.SetActive(true);

        PlayerInventory inv = playerController.GetComponent<PlayerInventory>();
        if (inv != null && inv.playerHand != null)
            inv.playerHand.gameObject.SetActive(true);

        if (helicopterAudio != null) helicopterAudio.Stop();

        isInHelicopter = false;
    }
}
