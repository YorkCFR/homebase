/**
 *  Create a simple choice dialog that works in the headset and using the gamepad for selection
 *  The gamepad arrows move you up and down the list and the x button selects. There are keyboard
 *  options for the editor.
 * 
 *  Version History
 *. v1.1 - Dec 1, 2025. Deal with new input model
 *  V1.0 - July 27, 2020. Initial version
 * 
 *  Michael Jenkin
 **/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dialog : MonoBehaviour
{

    public GameObject title;
    public GameObject choice;
    public GameObject instructions;

    private InputHandler inputHandler;
   

    private string[] choices;

    private int currentChoice = 0;
    private bool hasResponse = false;

    public void SetDialogTitle(string title)
    {
        this.title.GetComponent<TextMesh>().text = title;
        this.hasResponse = false;
    }

    public void SetDialogInstructions(string instructions)
    {
        this.instructions.GetComponent<TextMesh>().text = instructions;
        this.hasResponse = false;
    }

    public  void SetDialogChoices(string[] choices)
    {
        this.choices = choices;
        this.choice.GetComponent<TextMesh>().text = choices[0];
        this.currentChoice = 0;
        this.hasResponse = false;
    }

    public void SetDialogElements(string title, string[] choices)
    {
        this.choices = choices;

        SetDialogTitle(title);
        this.choice.GetComponent<TextMesh>().text = choices[0];
        this.currentChoice = 0;
        this.hasResponse = false;
        Debug.Log("set title to " + title);
    }

    public void SetBackground(Material m)
    {
        int childCount = transform.childCount;
        Debug.Log("There are n children n=" + childCount );
        for(int i=0;i<childCount;i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            Debug.Log("Child is " + child.name);
            if("Background" == child.name)
            {
                Debug.Log("Material setting");
                Renderer r = child.GetComponent<Renderer>();
                r.material = m;
                Debug.Log("Material set");
                return;
            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        GameObject cameraHolder = GameObject.Find("Camera Holder");
        inputHandler = cameraHolder.GetComponent<InputHandler>();

        this.hasResponse = false;
    }

    public int GetResponse()
    {
        if (this.hasResponse)
            return this.currentChoice;
        return -1;
    }

    // Update is called once per frame
    void Update()
    {  
        if (this.hasResponse)
            return;
        
        if(inputHandler.KeyUp)
       // if (Input.GetKeyDown("up")|| Input.GetKeyDown(KeyCode.JoystickButton5))
        {
            this.currentChoice = (this.currentChoice + this.choices.Length - 1) % this.choices.Length;
            this.choice.GetComponent<TextMesh>().text = choices[this.currentChoice];
            inputHandler.InputReset();
        }
        if(inputHandler.KeyDown)
        //if(Input.GetKeyDown("down")|| Input.GetKeyDown(KeyCode.JoystickButton4))
        {
            this.currentChoice = (this.currentChoice + 1) % this.choices.Length;
            this.choice.GetComponent<TextMesh>().text = choices[this.currentChoice];
            inputHandler.InputReset();
        }
        if(inputHandler.TriggerPressed)
        //if(Input.GetKeyDown("x")|| Input.GetKeyDown(KeyCode.JoystickButton0))
        {
            Debug.Log("Got an input event");
            Debug.Log("Choice is " + this.currentChoice);
            this.hasResponse = true;
        }
    }
}
