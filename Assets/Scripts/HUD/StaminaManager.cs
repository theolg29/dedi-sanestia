using UnityEngine;
using UnityEngine.UI;

public class StaminaManager : MonoBehaviour
{
    public static StaminaManager instance;

    [Header("UI Reference")]
    public Image sprintBarUI;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float drainRate = 20f;
    public float regenRate = 15f;

    // La variable qui gère l'autorisation de courir
    public bool canSprint = true; 

    private float currentStamina;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        currentStamina = maxStamina;
    }

    void Update()
    {
        // 1. Check exhaustion
        if (currentStamina <= 0f)
        {
            canSprint = false; 
        }
        else if (currentStamina >= 20f) // Recovers at least 20% before sprinting again
        {
            canSprint = true;
        }

        // 2. Drain or Regenerate Stamina
        if (Input.GetKey(KeyCode.LeftShift) && canSprint && (Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0))
        {
            currentStamina -= drainRate * Time.deltaTime;
        }
        else
        {
            currentStamina += regenRate * Time.deltaTime;
        }

        currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);

        // 3. Update UI Bar
        if (sprintBarUI != null)
        {
            sprintBarUI.fillAmount = currentStamina / maxStamina;
        }
    }
}