using System;
using System.Collections.Generic;
using UnityEngine;

public class ControlPanel : MonoBehaviour {
    public AudioSource MusicSound;

    [SerializeField]
    KeyCode SpeedUp = KeyCode.Space;
    [SerializeField]
    KeyCode SpeedDown = KeyCode.W;
    [SerializeField]
    KeyCode Forward = KeyCode.Z;
    [SerializeField]
    KeyCode Back = KeyCode.S;
    [SerializeField]
    KeyCode Left = KeyCode.Q;
    [SerializeField]
    KeyCode Right = KeyCode.D;
    [SerializeField]
    KeyCode TurnLeft = KeyCode.A;
    [SerializeField]
    KeyCode TurnRight = KeyCode.E;
    [SerializeField]
    KeyCode MusicOffOn = KeyCode.M;
    
    private KeyCode[] keyCodes;

    public Action<PressedKeyCode[]> KeyPressed;
    private void Awake()
    {
        keyCodes = new[] {
                            SpeedUp,
                            SpeedDown,
                            Forward,
                            Back,
                            Left,
                            Right,
                            TurnLeft,
                            TurnRight
                        };

    }

    void Start () {
	
	}

	void FixedUpdate ()
	{
	    var pressedKeyCode = new List<PressedKeyCode>();
	    for (int index = 0; index < keyCodes.Length; index++)
	    {
	        var keyCode = keyCodes[index];
	        if (Input.GetKey(keyCode))
                pressedKeyCode.Add((PressedKeyCode)index);
	    }

	    if (KeyPressed != null)
	        KeyPressed(pressedKeyCode.ToArray());

        // for test
        if (Input.GetKey(MusicOffOn))
        {
           if (  MusicSound.volume == 1) return;
/*            if (MusicSound.isPlaying)
                MusicSound.Stop();
            else*/
                MusicSound.volume = 1;
                MusicSound.Play();
        }
      
	}
}
