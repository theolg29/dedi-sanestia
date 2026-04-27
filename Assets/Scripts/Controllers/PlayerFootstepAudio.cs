using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerFootstepAudio : MonoBehaviour
{
    [Header("Pas (clip loopé ~5s)")]
    public AudioClip stepClip;

    [Header("Saut / Atterrissage")]
    public AudioClip[] jumpClips;
    public AudioClip[] landingClips;

    [Header("Accroupissement")]
    public AudioClip[] crouchStartClips;
    public AudioClip[] crouchEndClips;

    [Header("Volume")]
    [Range(0f, 1f)] public float stepVolume  = 0.8f;
    [Range(0f, 1f)] public float otherVolume = 0.8f;

    private AudioSource           _stepSource;
    private AudioSource           _otherSource;
    private FirstPersonController _fpc;

    private bool _wasGrounded = true;
    private bool _isCrouched  = false;

    void Awake()
    {
        _stepSource              = GetComponent<AudioSource>();
        _stepSource.clip         = stepClip;
        _stepSource.loop         = true;
        _stepSource.spatialBlend = 0f;
        _stepSource.playOnAwake  = false;
        _stepSource.volume       = stepVolume;

        _otherSource              = gameObject.AddComponent<AudioSource>();
        _otherSource.spatialBlend = 0f;
        _otherSource.playOnAwake  = false;
        _otherSource.volume       = otherVolume;

        _fpc = GetComponent<FirstPersonController>();
    }

    void Update()
    {
        bool grounded     = IsGrounded();
        bool moving       = Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f
                         || Mathf.Abs(Input.GetAxis("Vertical"))   > 0.1f;
        KeyCode sprintKey = _fpc != null ? _fpc.sprintKey    : KeyCode.LeftShift;
        KeyCode jumpKey   = _fpc != null ? _fpc.jumpKey      : KeyCode.Space;
        KeyCode crouchKey = _fpc != null ? _fpc.crouchKey    : KeyCode.LeftControl;
        bool holdToCrouch = _fpc != null ? _fpc.holdToCrouch : true;

        if (grounded && moving) { if (!_stepSource.isPlaying) _stepSource.Play(); }
        else                    { if (_stepSource.isPlaying)  _stepSource.Stop(); }

        if (grounded && Input.GetKeyDown(jumpKey))    PlayRandom(jumpClips);
        if (grounded && !_wasGrounded)                PlayRandom(landingClips);

        if (holdToCrouch)
        {
            if (Input.GetKeyDown(crouchKey)) { _isCrouched = true;  PlayRandom(crouchStartClips); }
            if (Input.GetKeyUp(crouchKey))   { _isCrouched = false; PlayRandom(crouchEndClips);   }
        }
        else if (Input.GetKeyDown(crouchKey))
        {
            _isCrouched = !_isCrouched;
            PlayRandom(_isCrouched ? crouchStartClips : crouchEndClips);
        }

        _wasGrounded = grounded;
    }

    private bool IsGrounded()
    {
        Vector3 origin = new Vector3(
            transform.position.x,
            transform.position.y - transform.localScale.y * 0.5f,
            transform.position.z);
        return Physics.Raycast(origin, Vector3.down, 0.75f);
    }

    private void PlayRandom(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        _otherSource.PlayOneShot(clips[Random.Range(0, clips.Length)], otherVolume);
    }
}
