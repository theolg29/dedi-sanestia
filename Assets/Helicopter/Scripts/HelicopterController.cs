using UnityEngine;
using UnityEngine.UI;

public class HelicopterController : MonoBehaviour
{
    public AudioSource HelicopterSound;
    public ControlPanel ControlPanel;
    public Rigidbody HelicopterModel;
    public HeliRotorController MainRotorController;
    public HeliRotorController SubRotorController;

    public float TurnForce = 3f;
    public float ForwardForce = 10f;
    public float ForwardTiltForce = 20f;
    public float TurnTiltForce = 30f;
    public float EffectiveHeight = 100f;
    [Tooltip("Force additionnelle max quand Shift (monter) ou Espace (descendre) est tenu")]
    public float VerticalBoostForce = 20f;
    [Tooltip("Vitesse de rampe montée/descente — plus élevé = plus réactif")]
    public float VerticalSmoothSpeed = 3f;
    [Tooltip("Amortissement de la vélocité verticale résiduelle — stabilise le hover")]
    public float HoverDamping = 2f;

    public float turnTiltForcePercent = 1.5f;
    public float turnForcePercent = 1.3f;

    private float _engineForce;
    public float EngineForce
    {
        get { return _engineForce; }
        set
        {
            if (MainRotorController != null) MainRotorController.RotarSpeed = value * 80;
            if (SubRotorController != null) SubRotorController.RotarSpeed = value * 40;
            if (HelicopterSound != null) HelicopterSound.pitch = Mathf.Clamp(value / 40, 0, 1.2f);

            if (UIGameController.runtime != null && UIGameController.runtime.EngineForceView != null)
                UIGameController.runtime.EngineForceView.text = string.Format("Engine value [ {0} ] ", (int)value);

            _engineForce = value;
        }
    }

    private Vector2 hMove = Vector2.zero;
    private Vector2 hTilt = Vector2.zero;
    private float hTurn = 0f;
    private float _verticalTarget   = 0f;  // 0, 1, ou -1 — mis à jour depuis Update via l'event
    private float _verticalSmoothed = 0f;  // lerp de _verticalTarget — utilisé dans LiftProcess
    public bool IsOnGround = true;

    void Start()
    {
        if (HelicopterModel == null)
            HelicopterModel = GetComponent<Rigidbody>();

        if (HelicopterModel != null)
            HelicopterModel.interpolation = RigidbodyInterpolation.Interpolate;

        if (ControlPanel == null)
            ControlPanel = FindObjectOfType<ControlPanel>();

        if (ControlPanel != null)
            ControlPanel.KeyPressed += OnKeyPressed;
        else
            Debug.LogError("ControlPanel reference is missing on HelicopterController!");
    }

    void FixedUpdate()
    {
        LiftProcess();
        MoveProcess();
        TiltProcess();
    }

    private void LiftProcess()
    {
        _verticalSmoothed = Mathf.Lerp(_verticalSmoothed, _verticalTarget, Time.fixedDeltaTime * VerticalSmoothSpeed);

        // AddForce world-space — indépendant de l'inclinaison de l'hélico
        float hoverForce = -Physics.gravity.y * HelicopterModel.mass;
        float dampForce  = -HelicopterModel.linearVelocity.y * HelicopterModel.mass * HoverDamping;
        float boostForce = _verticalSmoothed * VerticalBoostForce * HelicopterModel.mass;
        HelicopterModel.AddForce(Vector3.up * (hoverForce + dampForce + boostForce));
    }

    private void MoveProcess()
    {
        var turn = TurnForce * Mathf.Lerp(hMove.x, hMove.x * (turnTiltForcePercent - Mathf.Abs(hMove.y)), Mathf.Max(0f, hMove.y));
        hTurn = Mathf.Lerp(hTurn, turn, Time.fixedDeltaTime * TurnForce);
        HelicopterModel.AddRelativeTorque(0f, hTurn * HelicopterModel.mass, 0f);
        HelicopterModel.AddRelativeForce(Vector3.forward * Mathf.Max(0f, hMove.y * ForwardForce * HelicopterModel.mass));
    }

    private void TiltProcess()
    {
        hTilt.x = Mathf.Lerp(hTilt.x, hMove.x * TurnTiltForce, Time.fixedDeltaTime);
        hTilt.y = Mathf.Lerp(hTilt.y, hMove.y * ForwardTiltForce, Time.fixedDeltaTime);
        HelicopterModel.transform.localRotation = Quaternion.Euler(hTilt.y, HelicopterModel.transform.localEulerAngles.y, -hTilt.x);
    }

    private void OnKeyPressed(PressedKeyCode[] obj)
    {
        // Réinitialise chaque frame — si la touche est relâchée, le lerp revient vers 0
        _verticalTarget = 0f;

        float tempY = 0;
        float tempX = 0;

        if (hMove.y > 0)
            tempY = -Time.deltaTime;
        else if (hMove.y < 0)
            tempY = Time.deltaTime;

        if (hMove.x > 0)
            tempX = -Time.deltaTime;
        else if (hMove.x < 0)
            tempX = Time.deltaTime;

        foreach (var pressedKeyCode in obj)
        {
            switch (pressedKeyCode)
            {
                case PressedKeyCode.SpeedUpPressed:
                    _verticalTarget = 1f;
                    break;

                case PressedKeyCode.SpeedDownPressed:
                    _verticalTarget = -1f;
                    break;

                case PressedKeyCode.ForwardPressed:
                    if (IsOnGround) break;
                    tempY = Time.deltaTime;
                    break;

                case PressedKeyCode.BackPressed:
                    if (IsOnGround) break;
                    tempY = -Time.deltaTime;
                    break;

                case PressedKeyCode.LeftPressed:
                    if (IsOnGround) break;
                    tempX = -Time.deltaTime;
                    break;

                case PressedKeyCode.RightPressed:
                    if (IsOnGround) break;
                    tempX = Time.deltaTime;
                    break;

                case PressedKeyCode.TurnRightPressed:
                    if (IsOnGround) break;
                    HelicopterModel.AddRelativeTorque(0f, (turnForcePercent - Mathf.Abs(hMove.y)) * HelicopterModel.mass, 0f);
                    break;

                case PressedKeyCode.TurnLeftPressed:
                    if (IsOnGround) break;
                    HelicopterModel.AddRelativeTorque(0f, -(turnForcePercent - Mathf.Abs(hMove.y)) * HelicopterModel.mass, 0f);
                    break;
            }
        }

        hMove.x = Mathf.Clamp(hMove.x + tempX, -1f, 1f);
        hMove.y = Mathf.Clamp(hMove.y + tempY, -1f, 1f);
    }

    private void OnCollisionEnter()
    {
        IsOnGround = true;
    }

    private void OnCollisionExit()
    {
        IsOnGround = false;
    }
}
