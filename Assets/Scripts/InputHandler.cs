using UnityEngine;
using UnityEngine.InputSystem;

/**
 * This has been moved out of the monolithic code base as a start at refactoring.
 * This uses unity's input mmodel to map to the A, B, trigger and joystick inputs.
 *
 * Version History
 * V1.0 - extracted from the monolithic code from earlier versions.
 * Michael Jenkin, 2026.
 **/

public class InputHandler : MonoBehaviour
{
    [SerializeField]
    public InputActionProperty thumbstickAction; 

    [SerializeField]
    public InputActionProperty triggerAction;

    [SerializeField]
    public InputActionProperty aButton;
    [SerializeField]
    public InputActionProperty bButton;

    public bool TriggerPressed = false;
    public bool KeyUp = false;
   
    public bool KeyDown = false;
   

    public bool Astate = false;
    public bool Bstate = false;


    private Vector2 ThumbstickValue = new Vector2(0.0f, 0.0f); 
    private bool UpDownReset = false;
    private bool LastTriggerPress = false;

    /**
     * Process the input items we are interested in and encode KeyUp and KeyDown
     * and debounce these values. 
     **/
    void Update()
    {
        Vector2 thumbstickValue = thumbstickAction.action.ReadValue<Vector2>();
        //Debug.Log("Thumbstick is " + thumbstickValue[0]);

        if (thumbstickValue != Vector2.zero) 
        {
            if(UpDownReset)
            {
                if(thumbstickValue[0] > 0.5f)
                {
                    KeyUp = true;
                    UpDownReset = false;
                }
                if(thumbstickValue[0] < -0.5f)
                {
                    KeyDown = true;
                    UpDownReset = false;
                }
            }
        } 
        else
        {
            UpDownReset = true;   
            KeyUp = false;
            KeyDown = false;
        }


        float triggerValue = triggerAction.action.ReadValue<float>();
        //Debug.Log("Pre Triggervalue is " + triggerValue);
        //Debug.Log("Pre LastTriggerPress is " + LastTriggerPress);
        if(LastTriggerPress) {
            TriggerPressed = false;
            if(triggerValue <= 0.5f)
                LastTriggerPress = false;
        }
        else
        {
            if(triggerValue > 0.5f)
            {
                TriggerPressed = true;
                LastTriggerPress = true;
            }
        }
        //Debug.Log("Post TriggerPressed is " + TriggerPressed);
        //Debug.Log("Post LastTriggerPress is " + LastTriggerPress);

        Astate = aButton.action.ReadValue<float>() >0.5f;
        //Debug.Log($" a button is {Astate}");
        Bstate = bButton.action.ReadValue<float>() > 0.5;;
        //Debug.Log($"b button is {Bstate}");
    }

    public void InputReset()
    {
            KeyUp = false;
            KeyDown = false;
            UpDownReset = false;
    }

    private void OnEnable()
    {
        thumbstickAction.action.Enable();
        triggerAction.action.Enable();
        aButton.action.Enable();
        bButton.action.Enable();
    }
    
    private void OnDisable()
    {
        thumbstickAction.action.Disable();
        triggerAction.action.Disable();
        aButton.action.Disable();
        bButton.action.Disable();
    }
    
}
