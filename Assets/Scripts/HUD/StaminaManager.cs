using UnityEngine;
using UnityEngine.UI;

public class StaminaManager : MonoBehaviour
{
    public static StaminaManager instance;

    [Header("UI Reference")]
    public Image sprintBarUI;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float drainRate  = 20f;
    public float regenRate  = 15f;

    [Header("Exhaustion Settings")]
    public float exhaustionCooldown = 3f;

    public bool canSprint = true;

    private float currentStamina;
    private bool  isExhausted    = false;
    private float exhaustionTimer = 0f;

    void Awake() => instance = this;

    void Start() => currentStamina = maxStamina;

    void Update()
    {
        if (currentStamina <= 0f && !isExhausted)
        {
            isExhausted    = true;
            exhaustionTimer = exhaustionCooldown;
            canSprint      = false;
        }

        if (isExhausted)
        {
            exhaustionTimer -= Time.deltaTime;
            canSprint        = false;
            if (exhaustionTimer <= 0f && currentStamina >= maxStamina * 0.2f)
                isExhausted = false;
        }
        else if (currentStamina >= maxStamina * 0.2f)
        {
            canSprint = true;
        }

        bool sprinting = Input.GetKey(KeyCode.LeftShift) && canSprint
                      && (Input.GetAxis("Vertical") != 0 || Input.GetAxis("Horizontal") != 0);

        currentStamina += (sprinting ? -drainRate : regenRate) * Time.deltaTime;
        currentStamina  = Mathf.Clamp(currentStamina, 0f, maxStamina);

        if (sprintBarUI != null)
            sprintBarUI.fillAmount = currentStamina / maxStamina;
    }
}
