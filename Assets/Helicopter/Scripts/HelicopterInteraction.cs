using UnityEngine;
using System.Collections;

public class HelicopterInteraction : MonoBehaviour
{
    [Header("Player References")]
    public FirstPersonController playerController;
    public Transform playerCameraTransform;
    
    [Header("Helicopter References")]
    public HelicopterController helicopterController;
    public Camera helicopterCamera; 
    
    [Header("Settings")]
    public float interactionDistance = 5f;
    public KeyCode interactKey = KeyCode.F;

    private bool isInHelicopter = false;
    private float exitCooldown = 0f;

    IEnumerator Start()
    {
        // Auto-assign player
        if (playerController == null)
            playerController = FindObjectOfType<FirstPersonController>();

        // Auto-assign player camera
        if (playerCameraTransform == null && Camera.main != null)
            playerCameraTransform = Camera.main.transform;

        // Auto-assign helicopter controller
        if (helicopterController == null)
            helicopterController = GetComponent<HelicopterController>();

        // Pre-assign ControlPanel to avoid NullReferenceException later
        if (helicopterController != null && helicopterController.ControlPanel == null)
        {
            helicopterController.ControlPanel = GetComponent<ControlPanel>() ?? GetComponentInChildren<ControlPanel>(true) ?? FindObjectOfType<ControlPanel>();
        }

        // Auto-assign helicopter camera if one exists, or create one if missing
        if (helicopterCamera == null)
        {
            var camScript = FindObjectOfType<FollowTargetCamera>();
            if (camScript == null)
            {
                // The user's scene lacks a Helicopter camera, let's create it automatically!
                GameObject camObj = new GameObject("AutoHelicopterCamera");
                helicopterCamera = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>(); // Optional, sound
                camScript = camObj.AddComponent<FollowTargetCamera>();

                // We create a target point properly placed BEHIND the helicopter (3rd person offset)
                GameObject targetObj = new GameObject("HelicopterCameraTarget");
                targetObj.transform.SetParent(transform);
                targetObj.transform.localPosition = new Vector3(0, 5f, -12f); // 12 meters behind, 5 meters high
                targetObj.transform.localRotation = Quaternion.identity;

                camScript.Target = targetObj.transform;
                camScript.PositionFolowForce = 5f;
                camScript.RotationFolowForce = 5f;
                camObj.transform.position = targetObj.transform.position;
                camObj.SetActive(false);
            }
            else
            {
                helicopterCamera = camScript.GetComponent<Camera>();
                // If they have the camera, but the target is exactly the helicopter center, they see inside the mesh!
                // Let's create an offset for it so they get a beautiful 3rd person view!
                if (camScript.Target == null || camScript.Target == transform) 
                {
                    GameObject targetObj = new GameObject("HelicopterCameraTarget");
                    targetObj.transform.SetParent(transform);
                    targetObj.transform.localPosition = new Vector3(0, 5f, -12f); // 12 meters behind, 5 meters high
                    camScript.Target = targetObj.transform;
                }
            }
        }

        // Wait one frame to ensure HelicopterController and ControlPanel have run their own Start() methods
        yield return null;

        // Initialize default states (player walks, helicopter sleeps)
        if (helicopterController != null) 
        {
            helicopterController.enabled = false;
            if (helicopterController.ControlPanel) helicopterController.ControlPanel.enabled = false;
        }

        if (helicopterCamera != null) 
        {
            helicopterCamera.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (exitCooldown > 0) exitCooldown -= Time.deltaTime;

        if (!isInHelicopter)
        {
            // Enter Helicopter
            if (playerController != null && Vector3.Distance(transform.position, playerController.transform.position) < interactionDistance)
            {
                if (Input.GetKeyDown(interactKey) && exitCooldown <= 0)
                {
                    EnterHelicopter();
                    exitCooldown = 1.0f; // Wait 1 second before you can exit
                }
            }
        }
        else
        {
            // Exit Helicopter (F, E, or X) just in case!
            if ((Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.X)) && exitCooldown <= 0)
            {
                ExitHelicopter();
                exitCooldown = 1.0f; // Wait 1 second before you can enter again
            }
        }
    }

    void EnterHelicopter()
    {
        // Disable player FirstPersonController
        playerController.playerCanMove = false;
        playerController.cameraCanMove = false;
        
        Collider playerCollider = playerController.GetComponent<Collider>();
        if (playerCollider) playerCollider.enabled = false;
        
        Rigidbody playerRb = playerController.GetComponent<Rigidbody>();
        if (playerRb) playerRb.isKinematic = true;

        // Parent player controller to helicopter so it follows the helicopter around
        playerController.transform.SetParent(transform);

        // Turn on helicopter controls
        if (helicopterController != null)
        {
            helicopterController.enabled = true;
            if (helicopterController.ControlPanel) 
                helicopterController.ControlPanel.enabled = true;
        }
        
        // Hide First Person camera, show Helicopter camera
        if (helicopterCamera != null) {
            helicopterCamera.gameObject.SetActive(true);
            playerCameraTransform.gameObject.SetActive(false);
        }

        isInHelicopter = true;
    }

    void ExitHelicopter()
    {
        // Turn off Helicopter controls
        if (helicopterController != null)
        {
            helicopterController.enabled = false;
            helicopterController.EngineForce = 0;
            if (helicopterController.ControlPanel) 
                helicopterController.ControlPanel.enabled = false;
        }
        
        // Switch back to FirstPerson camera 
        if (helicopterCamera != null) {
            playerCameraTransform.gameObject.SetActive(true);
            helicopterCamera.gameObject.SetActive(false);
        }

        // Target position to exit (5 units to the right of the helicopter)
        Vector3 exitPos = transform.position + transform.right * 5f;
        exitPos.y = transform.position.y + 0.5f;

        // Unparent player controller and move to exit position
        playerController.transform.SetParent(null);
        playerController.transform.position = exitPos;
        playerController.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y + 90, 0);

        // Re-enable Player physics and scripts
        Collider playerCollider = playerController.GetComponent<Collider>();
        if (playerCollider) playerCollider.enabled = true;
        
        Rigidbody playerRb = playerController.GetComponent<Rigidbody>();
        if (playerRb) playerRb.isKinematic = false;

        playerController.playerCanMove = true;
        playerController.cameraCanMove = true;

        isInHelicopter = false;
    }
}
