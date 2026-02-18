using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils; // for XROrigin
using System.IO;  // for USB access

/**
 * Create the basic experimental environment.
 * 
 * Copyright Michael Jenkin 2025, 2026
 * Version History
 * 
 * V2.1 - ensure output even if no user input
 * V2.0 - updated triangle completion task
 * V1.6 - Modifications to run in the HMD
 * V1.5 - Now with manual start between conditions
 * V1.4 - better graphcis, getting ready for Android version
 * V1.3 - basically deal with everything except VR (and head tracking)
 * V1.2 - set up for new two task version
 * V1.1 - use a pointer for the final task rather than moving the point of view.
 *      - do some general cleanup in code style
 * V1.0 - based on the OSC software
 **/

public class WorldCreator : MonoBehaviour
{
    [SerializeField]
    public InputActionProperty thumbstickAction; 
    [SerializeField]
    public InputActionProperty triggerAction;

    public InputActionProperty aButton;
    public InputActionProperty bButton;

    private enum UIState
    {
        Initialize,
        WelcomeScreen,
        ConfirmScreen,
        DoingExperiment,
        ExperimentDone,
    };

    private enum ExperimentState
    {
        Initialize,
        Setup,
        BeforeMotion,
        WaitForAdjustTarget,
        LegOne,
        AdjustTarget,
        Turning,
        AdjustOrientation,
        WaitForAdjustRotation,
        LegTwo,
        WaitForWhereDidIGo,
        PlaceTarget,
        Done
    };

 
    private const float MIN_MOTION_STEP = 0.01f;            // motion step minimum
    private const float MAX_MOTION_STEP = 0.5f;            // motion step maximum
    private const float MOTION_STEP_MULTIPLIER = 1.1f;      // motion step multiplier
    private float _motionStep = MIN_MOTION_STEP;            // how big a step to make for a keypress (m)

    private const float MIN_TURN_STEP = 0.1f;               // minimum turn step (deg)
    private const float MAX_TURN_STEP = 2.0f;               // maximm turn step (deg)   
    private const float TURN_STEP_MULTIPLIER = 1.01f;       // turn step multiplier
    private float _turnStep = MIN_TURN_STEP;                // step size for turn keypress (deg)

    private const float RETICLE_DISTANCE = 2.0f;            // distance to orientaiton reticle (m)
    private const float MIN_RETICLE_DISTANCE = 2.0f;        // min triangle reticle distance (m)
    private const float MAX_RETICLE_DISTANCE = 20.0f;       // max triancle reticle distance (m)
    private const float START_MIN_RETICLE_DISTANCE = 5.0f;  // min start range
    private const float START_MAX_RETICLE_DISTANCE = 15.0f; // max start range
    private const float _velocity = 2.0f;                   // speed along a straight edge m/sec
    private const float _spinV = 30.0f;                     // abs rotational velocity deg/sec
    private const int NSPHERES = 12000;                     // number of spheres

    public bool TriggerPressed = false;
    public bool KeyUp = false, KeyUpOld = false;
    public bool KeyDown = false, KeyDownOld = false;

    public bool Astate = false, AstateOld = false;
    public bool Bstate = false, BstateOld = false;

    public Vector2 ThumbstickValue = new Vector2(0.0f, 0.0f); 
    private bool UpDownReset = false;
    private bool LastTriggerPress = false;


    private UIState _uiState = UIState.Initialize;
    private ExperimentState _experimentState = ExperimentState.Initialize;

    private SphereField _sf = null;

    private float _motion1Start = 0.0f;
    private float _turnStart = 0.0f;
    private float _motion2Start = 0.0f;

    private float _length1 = 4.0f;
    private float _length2 = 6.0f;
    private float _turn = 60.0f;  // absolute value of turn

    private float _spinDir = 1.0f;  // +1 right, -1 left

    private bool _pitch = true;   // pitch (true) or yaw (false)
    private bool _turnRight = false; // true is to the rigght (yaw) or up (pitch)
    private float _pan, _tilt;

    private GameObject _adjustTarget = null;
    private GameObject _reticle = null;
    private GameObject _camera = null;
    private GameObject _dialog = null;
    private GameObject _home = null;

    private ResponseLog _responseLog = null;
    private string _outputHeader;
    private long _startTime;

    private float _targetDistance1, _targetDistance2, _turnAngle, _directionAngle, _directionDistance;

    private const int NTRIANG = 48; // number of triangle completion conditions
    private const int NLINEAR = 4; // number of linear conditions
    private const int NROTATE = 12; // number of rotation conditions
    private int _cond = 0;
    private int _experiment = -1; // 0 is LINEAR, 1 is ROTATE, 2 is TRIANGLE


    private XROrigin xrOrigin;
    
    // define the conditions. This could be cleaner


    float[][] _linear_conditions = new float[NLINEAR][];
    float[][] _rotational_conditions = new float[NROTATE][];
    float[][] _triangle_conditions = new float[NTRIANG][];
    
    /**
     * Setup the conditions structures for each of the (three) conditions
     **/
    private void SetUpConditions()
    {
        // do linear
        float[] l1 = new float[2] {4.0f, 1.0f}; // l1, pan/tilt
        float[] l2 = new float[2] {8.0f, 1.0f}; // l1, pan/tilt
        float[] l3 = new float[2] {4.0f, -1.0f}; // l1, pan/tilt
        float[] l4 = new float[2] {8.0f, -1.0f}; // l1, pan/tilt
        _linear_conditions[0] = l1;
        _linear_conditions[1] = l2;
        _linear_conditions[2] = l3;
        _linear_conditions[3] = l4;

        for(int i = 0; i < NLINEAR*10; i++)
        {
            int index1 = UnityEngine.Random.Range(0, NLINEAR);
            int index2 = UnityEngine.Random.Range(0, NLINEAR);
            float[] z = _linear_conditions[index1];
            _linear_conditions[index1] = _linear_conditions[index2];
            _linear_conditions[index2] = z;
        }

        // do rotation
        float[] r1  = new float[3] {135.0f, 1.0f, 1.0f}; // l1, pan/tilt, dir1/dir2
        float[] r2  = new float[3] {150.0f, 1.0f, 1.0f}; // l1, pan/tilt, dir1/dir2
        float[] r3 = new float[3] {165.0f, 1.0f, 1.0f}; // l1, pan/tilt, dir1/dir2
        float[] r4  = new float[3] {135.0f, 1.0f,-1.0f}; // l1, pan/tilt, dir1/dir2
        float[] r5  = new float[3] {150.0f, 1.0f,-1.0f}; // l1, pan/tilt, dir1/dir2
        float[] r6  = new float[3] {165.0f, 1.0f,-1.0f}; // l1, pan/tilt, dir1/dir2
        float[] r7  = new float[3] {135.0f,-1.0f, 1.0f}; // l1, pan/tilt, dir1/dir2
        float[] r8  = new float[3] {150.0f,-1.0f, 1.0f}; // l1, pan/tilt, dir1/dir2
        float[] r9  = new float[3] {165.0f,-1.0f, 1.0f}; // l1, pan/tilt, dir1/dir2
        float[] r10 = new float[3] {135.0f,-1.0f,-1.0f}; // l1, pan/tilt, dir1/dir2
        float[] r11 = new float[3] {150.0f,-1.0f,-1.0f}; // l1, pan/tilt, dir1/dir2
        float[] r12 = new float[3] {165.0f,-1.0f,-1.0f}; // l1, pan/tilt, dir1/dir2
        _rotational_conditions[0] = r1;
        _rotational_conditions[1] = r2;
        _rotational_conditions[2] = r3;
        _rotational_conditions[3] = r4;
        _rotational_conditions[4] = r5;
        _rotational_conditions[5] = r6;
        _rotational_conditions[6] = r7;
        _rotational_conditions[7] = r8;
        _rotational_conditions[8] = r9;
        _rotational_conditions[9] = r10;
        _rotational_conditions[10] = r11;
        _rotational_conditions[11] = r12;

        for(int i = 0; i < NTRIANG*10; i++)
        {
            int index1 = UnityEngine.Random.Range(0, NROTATE);
            int index2 = UnityEngine.Random.Range(0, NROTATE);
            float[] z = _rotational_conditions[index1];
            _rotational_conditions[index1] = _rotational_conditions[index2];
            _rotational_conditions[index2] = z;
        }

        // do triangle
        float[] c1  = new float[5] { 4.0f, 4.0f, 165.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c2  = new float[5] { 4.0f, 4.0f, 150.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c3  = new float[5] { 4.0f, 4.0f, 135.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c4  = new float[5] { 4.0f, 8.0f, 165.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c5  = new float[5] { 4.0f, 8.0f, 150.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c6  = new float[5] { 4.0f, 8.0f, 135.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c7  = new float[5] { 8.0f, 8.0f, 165.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c8  = new float[5] { 8.0f, 8.0f, 150.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c9  = new float[5] { 8.0f, 8.0f, 135.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c10 = new float[5] { 8.0f, 4.0f, 165.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c11 = new float[5] { 8.0f, 4.0f, 150.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c12 = new float[5] { 8.0f, 4.0f, 135.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c13 = new float[5] { 4.0f, 4.0f, 165.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c14 = new float[5] { 4.0f, 4.0f, 150.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c15 = new float[5] { 4.0f, 4.0f, 135.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c16 = new float[5] { 4.0f, 8.0f, 165.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c17 = new float[5] { 4.0f, 8.0f, 150.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c18 = new float[5] { 4.0f, 8.0f, 135.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c19 = new float[5] { 8.0f, 8.0f, 165.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c20 = new float[5] { 8.0f, 8.0f, 150.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c21 = new float[5] { 8.0f, 8.0f, 135.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c22 = new float[5] { 8.0f, 4.0f, 165.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c23 = new float[5] { 8.0f, 4.0f, 150.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c24 = new float[5] { 8.0f, 4.0f, 135.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c25 = new float[5] { 4.0f, 4.0f, 165.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c26 = new float[5] { 4.0f, 4.0f, 150.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c27 = new float[5] { 4.0f, 4.0f, 135.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c28 = new float[5] { 4.0f, 8.0f, 165.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c29 = new float[5] { 4.0f, 8.0f, 150.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c30 = new float[5] { 4.0f, 8.0f, 135.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c31 = new float[5] { 8.0f, 8.0f, 165.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c32 = new float[5] { 8.0f, 8.0f, 150.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c33 = new float[5] { 8.0f, 8.0f, 135.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c34 = new float[5] { 8.0f, 4.0f, 165.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c35 = new float[5] { 8.0f, 4.0f, 150.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c36 = new float[5] { 8.0f, 4.0f, 135.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c37 = new float[5] { 4.0f, 4.0f, 165.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c38 = new float[5] { 4.0f, 4.0f, 150.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c39 = new float[5] { 4.0f, 4.0f, 135.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c40 = new float[5] { 4.0f, 8.0f, 165.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c41 = new float[5] { 4.0f, 8.0f, 150.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c42 = new float[5] { 4.0f, 8.0f, 135.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c43 = new float[5] { 8.0f, 8.0f, 165.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c44 = new float[5] { 8.0f, 8.0f, 150.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c45 = new float[5] { 8.0f, 8.0f, 135.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c46 = new float[5] { 8.0f, 4.0f, 165.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c47 = new float[5] { 8.0f, 4.0f, 150.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c48 = new float[5] { 8.0f, 4.0f, 135.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        _triangle_conditions[0] = c1; _triangle_conditions[1] = c2; _triangle_conditions[2] = c3;
        _triangle_conditions[3] = c4; _triangle_conditions[4] = c5; _triangle_conditions[5] = c6;
        _triangle_conditions[6] = c7; _triangle_conditions[7] = c8; _triangle_conditions[8] = c9;
        _triangle_conditions[9] = c10; _triangle_conditions[10] = c11; _triangle_conditions[11] = c12;

        _triangle_conditions[12] = c13; _triangle_conditions[13] = c14; _triangle_conditions[14] = c15;
        _triangle_conditions[15] = c16; _triangle_conditions[16] = c17; _triangle_conditions[17] = c18;
        _triangle_conditions[18] = c19; _triangle_conditions[19] = c20; _triangle_conditions[20] = c21;
        _triangle_conditions[21] = c22; _triangle_conditions[22] = c23; _triangle_conditions[23] = c24;

        _triangle_conditions[24] = c25; _triangle_conditions[25] = c26; _triangle_conditions[26] = c27;
        _triangle_conditions[27] = c28; _triangle_conditions[28] = c29; _triangle_conditions[29] = c30;
        _triangle_conditions[30] = c31; _triangle_conditions[31] = c32; _triangle_conditions[32] = c33;
        _triangle_conditions[33] = c34; _triangle_conditions[34] = c35; _triangle_conditions[35] = c36;

        _triangle_conditions[36] = c37; _triangle_conditions[37] = c38; _triangle_conditions[38] = c39;
        _triangle_conditions[39] = c40; _triangle_conditions[40] = c41; _triangle_conditions[41] = c42;
        _triangle_conditions[42] = c43; _triangle_conditions[43] = c44; _triangle_conditions[44] = c45;
        _triangle_conditions[45] = c46; _triangle_conditions[46] = c47; _triangle_conditions[47] = c48;

        for(int i = 0; i < 10*NTRIANG; i++)
        {
            int index1 = UnityEngine.Random.Range(0, NTRIANG);
            int index2 = UnityEngine.Random.Range(0, NTRIANG);
            float[] z = _triangle_conditions[index1];
            _triangle_conditions[index1] = _triangle_conditions[index2];
            _triangle_conditions[index2] = z;
        }
    }

    /**
     * All of the non-scene texture objects are initially active (so they are easy to find). 
     * Get links to them, then make them inactive (except for the camera :-)
     **/
    private void GetGameObjects()
    {
        _adjustTarget = GameObject.Find("Target");
        _adjustTarget.SetActive(false);

        _reticle = GameObject.Find("Reticle");
        _reticle.SetActive(false);

        _camera = GameObject.Find("Camera Holder");

        _dialog = GameObject.Find("Dialog");
        //_dialog.SetActive(false);

        _home = GameObject.Find("Homebase");
        _home.SetActive(false);
    }

    /**
     * Start is called once before the first execution of Update after the MonoBehaviour is created
     *
     **/
    void Start()
    {
        _startTime = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
        
        GetGameObjects();
        
        Debug.Log("In WordCreator");
        SetUpConditions();
        
        Dialog d = _dialog.GetComponent<Dialog>();

        xrOrigin = FindAnyObjectByType<XROrigin>();
        if(xrOrigin == null)
        {
            Debug.LogError("No XROrigin found");
        } 
        else
        {
            //xrOrigin.MoveCameraToWorldLocation(Vector3.zero);
            //xrOrigin.MatchOriginUpCameraForward(Vector3.up, Vector3.forward);
  
        }

        _sf = new SphereField(NSPHERES); // we regenerate locations as needed
    }

    

    /**
     * This is called once every time tick. The entire experiment's UI is in one of a small
     * number of states. The work of the real experiment is done elsewhere
     **/
    void Update()
    {
        Debug.Log("Update starts");
        Dialog d = _dialog.GetComponent<Dialog>();
        Debug.Log("Got dialog");


        ProcessInput();
        Debug.Log("Processing input returns");
        _sf.FLickerDisplay(0.0001f);

        Debug.Log($"In update uistate is {_uiState}");
        switch (_uiState)
        {
            case UIState.Initialize:
                _dialog.SetActive(true);
                Debug.Log("Initializing dialog entry and making it visible");
                d.SetDialogElements("Choose Experiment", new string[] { "Linear Motion", "Rotational Motion", "Triangle Completion" });
                _uiState = UIState.WelcomeScreen;
                break;
            case UIState.WelcomeScreen:
                int response = d.GetResponse();
                Debug.Log("Welcome screen got response " + response);
                if (response >= 0)
                {
                    _experiment = response;
                    string[] confirmString = { "????", "Back" };
                    if (_experiment == 0)
                        confirmString[0] = "Do 'Linear Motion'";
                    else if (_experiment == 1)
                        confirmString[0] = "Do 'Rotational Motion'";
                    else
                        confirmString[0] = "Do 'Triangle Completion'";

                    d.SetDialogElements("Confirm Choice", confirmString);
                    _uiState = UIState.ConfirmScreen;
                }
                break;
            case UIState.ConfirmScreen:
                int confirm = d.GetResponse();
                if (confirm >= 0)
                {
                    if (confirm == 0)
                    {
                        _responseLog = new ResponseLog();
                        if(_experiment == 0)
                        {
                            _outputHeader = "HomeBase Linear Motion Dataset";
                        } else if(_experiment == 1) {
                            _outputHeader = "HomeBase Rotational Motion Dataset";
                        } else
                        {
                            _outputHeader = "HomeBase Triangle Completion Dataset";
                        }
                        _uiState = UIState.DoingExperiment;
                        _experimentState = ExperimentState.Initialize;
                        _dialog.SetActive(false);
                    } else // back
                    {
                        _uiState = UIState.Initialize;
                    }
                }
                break;
            case UIState.DoingExperiment:
                if (_experiment == 0)
                {
                    DoAdjustLinearTarget();
                }
                else if(_experiment == 1)
                {
                    DoAdjustRotationalTarget();
                }
                else
                {
                    DoWhereDidIGo();
                }

                break;
            case UIState.ExperimentDone:
                Debug.Log("In experiment done!");
                if(TriggerPressed) { //if(d.GetResponse() >= 0) {
                    //DirectoryHacking(Application.persistentDataPath);
                    //DirectoryHacking("/storage/emulated/0/Android/data/com.YorkUniversity.Homebase");
                    QuitPlaying();
                }
                break;
        }
    }

    private void DirectoryHacking(string directoryPath)
    {

        Debug.Log("**********************************************");
        // Ensure the directory exists before trying to get files
        if (Directory.Exists(directoryPath))
        {
            // Get all files in the directory as an array of strings (full paths)
            string[] files = Directory.GetFiles(directoryPath);

            Debug.Log("Files found in: " + directoryPath);

            // Iterate through the array and print each file name
            foreach (string file in files)
            {
                // Use Path.GetFileName to just get the file name, without the full path
                Debug.Log(Path.GetFileName(file));
            }
        }
        else
        {
            Debug.Log("Directory not found: " + directoryPath);
            // Optionally, create the directory if it doesn't exist
        }
    }

    private void DoAdjustLinearTarget()
    {
        float _distance;
        Dialog d = _dialog.GetComponent<Dialog>();

        switch (_experimentState)
        {
            case ExperimentState.Initialize: // provide instructions

                d.SetDialogElements("Linear Motion", new string[] { "Indicate distance" });
                d.SetDialogInstructions("Press trigger to start");
                _dialog.SetActive(true);
                _home.SetActive(true);
                _experimentState = ExperimentState.Setup;
                break;
            case ExperimentState.Setup: // clean things up to start
                if(TriggerPressed) //if (Input.GetKeyDown("x"))
                {
                    _dialog.SetActive(false);
                    _experimentState = ExperimentState.BeforeMotion;
                    _cond = 0;
                }
                break;
            case ExperimentState.BeforeMotion: // waiting before motion
                Debug.Log($"BeforeMotion {_cond}");

                if (_cond < NLINEAR)
                {
                    _length1 = _linear_conditions[_cond][0];
                    _pitch = _linear_conditions[_cond][1] > 0;

                    _sf.RePaint(_pitch);
                    _sf.EnableHomeBaseDisplay();

                    Debug.Log("STARTING CONDITION");
                    Debug.Log(_cond);

                    _experimentState = ExperimentState.WaitForAdjustTarget;
                    _distance = 0;
                    _camera.transform.position = new Vector3(0, 0, _distance);
                    _camera.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                    d.SetDialogElements("Adjust Target", new string[] { "Condition " + _cond });
                    d.SetDialogInstructions("Press trigger to start");
                    _dialog.SetActive(true);
                    _home.SetActive(false);
                }
                break;
            case ExperimentState.WaitForAdjustTarget:
                if(TriggerPressed) //if (Input.GetKeyDown("x"))
                {
                    _dialog.SetActive(false);
                    _motion1Start = Time.time;
                    _experimentState = ExperimentState.LegOne;
                    Debug.Log("Starting condition");
                }
                break;
            case ExperimentState.LegOne: // moving in direction (0,0,1) to _length1

                // move the camera
                _distance = _velocity * (Time.time - _motion1Start);
                _camera.transform.position = new Vector3(0, 0, _distance);

                // got to where we want to go
                if (_distance >= _length1) // got there, do the adjust target task
                {
                    _distance = _length1;
                    _camera.transform.position = new Vector3(0, 0, _distance);
                    _experimentState = ExperimentState.AdjustTarget;
                    _targetDistance1 = UnityEngine.Random.Range(1.5f, 2.0f * _length1);
                    _adjustTarget.transform.position = new Vector3(0, 0, _targetDistance1 + _length1);
                    _adjustTarget.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                    _adjustTarget.SetActive(true);
                }
                break;
            case ExperimentState.AdjustTarget: // adjust target task (arrows to adjust, x to select)
                if(Astate) // if (Input.GetKey("a"))
                {
                    if(AstateOld)
                    {
                        _motionStep = Mathf.Min(MOTION_STEP_MULTIPLIER*_motionStep, MAX_MOTION_STEP);
                    } else
                    {
                        _motionStep = MIN_MOTION_STEP;
                    }
                    AstateOld = true;
                    BstateOld = false;
                    _targetDistance1 = Mathf.Min(_targetDistance1 + _motionStep, 2.0f * _length1);
                }
                else if(Bstate) //if (Input.GetKey("b"))
                {
                    if(BstateOld)
                    {
                        _motionStep = Mathf.Min(MOTION_STEP_MULTIPLIER*_motionStep, MAX_MOTION_STEP);
                    } else
                    {
                        _motionStep = MIN_MOTION_STEP;
                    }
                    AstateOld = false;
                    BstateOld = true;
                    _targetDistance1 = Mathf.Max(_targetDistance1 - _motionStep, 0.1f * _length1);
                } 
                else
                {
                    AstateOld = false;
                    BstateOld = false;
                }
                _adjustTarget.transform.position = new Vector3(0, 0, _targetDistance1 + _length1);
                if(TriggerPressed) //if (Input.GetKeyDown("x"))
                {
                    _responseLog.Add(ResponseLog.LINEAR_TARGET, Time.time, _linear_conditions[_cond][0], -1000.0f, -1000.0f,
                                     _linear_conditions[_cond][1], -1000.0f, _targetDistance1, -1000.0f, -1000.0f, -1000.0f, -1000.0f);
                    _adjustTarget.SetActive(false);
                    _camera.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
                    _camera.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

                    if(_cond < NLINEAR - 1)
                    {
                        _cond = _cond + 1;
                        _experimentState = ExperimentState.BeforeMotion;
                    } else {
                        _responseLog.Dump(Application.persistentDataPath + "/Responses_linear_" + _startTime + ".txt", _outputHeader);
                        Debug.Log($"Output is in {Application.persistentDataPath}");
                        d.SetDialogElements("Completed", new string[] { "" });
                        d.SetDialogInstructions("Press trigger to quit");
                        _dialog.SetActive(true);
                        _experimentState = ExperimentState.Done;
                        _uiState = UIState.ExperimentDone;
                    }

                }
                break;
            case ExperimentState.Done: // should never get here
                break;
        }
    }

    /**
     * Do the rotation target task
     **/
    private void DoAdjustRotationalTarget()
    {
        Dialog d = _dialog.GetComponent<Dialog>();
        float _angle;
        float tx, ty, tz;

        switch (_experimentState)
        {
            case ExperimentState.Initialize: // provide instructions

                d.SetDialogElements("Rotational Motion", new string[] { "Indicate rotation" });
                d.SetDialogInstructions("Press trigger to start");
                _dialog.SetActive(true);
                _home.SetActive(true);
                _experimentState = ExperimentState.Setup;
                break;
            case ExperimentState.Setup: // clean things up to start
                if(TriggerPressed) //if (Input.GetKeyDown("x"))
                {
                    _dialog.SetActive(false);
                    _experimentState = ExperimentState.BeforeMotion;
                    _cond = 0;
                }
                break;
            case ExperimentState.BeforeMotion: // waiting before motion
                Debug.Log($"BeforeMotion {_cond}");

                if (_cond < NROTATE)
                {
                    _turn = 180 - _rotational_conditions[_cond][0]; // angles measured the other way
                    _pitch = _rotational_conditions[_cond][1] > 0;
                    _turnRight = _rotational_conditions[_cond][2] > 0;

                    _sf.RePaint(_pitch);
                    _sf.EnableHomeBaseDisplay();

                    Debug.Log($"STARTING CONDITION {_turn}");
                    Debug.Log(_cond);

                    _experimentState = ExperimentState.WaitForAdjustRotation;
                    _camera.transform.position = new Vector3(0, 0, 0);
                    _camera.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                    d.SetDialogElements("Adjust Target", new string[] { "Condition " + _cond });
                    d.SetDialogInstructions("Press trigger to start");
                    _dialog.SetActive(true);
                    _home.SetActive(false);
                }
                break;
            case ExperimentState.WaitForAdjustRotation:
                if(TriggerPressed) //if (Input.GetKeyDown("x"))
                {
                    _dialog.SetActive(false);
                    _turnStart = Time.time;
                    _experimentState = ExperimentState.Turning;
                    Debug.Log("Starting condition");
                }
                break;
            case ExperimentState.Turning: // rotate (_turn is an amplitude)
                _angle = _spinV * (Time.time - _turnStart);
                if(_angle >= _turn)
                    _angle = _turn;

                // update the camera orientation
                if (_pitch)
                {
                    _pan = 0.0f;
                    _tilt = _spinDir * _angle;
                    _camera.transform.rotation = Quaternion.Euler(_tilt, _pan, 0.0f);
                } else {
                    _pan = _spinDir * _angle;
                    _tilt = 0.0f;
                    _camera.transform.rotation = Quaternion.Euler(_tilt, _pan, 0.0f);
                }

                // deal with finished rotation
                if (_angle >= _turn)
                {
                    _angle = _turn;
                    _experimentState = ExperimentState.AdjustOrientation;
                    if (_pitch)
                    {
                        _tilt = _spinDir * _angle;
                        tx = 0;
                        ty = -RETICLE_DISTANCE * Mathf.Sin(3.1415f * _tilt / 180.0f);
                        tz = RETICLE_DISTANCE * Mathf.Cos(3.1415f * _tilt / 180.0f);
                        _reticle.transform.rotation = Quaternion.Euler(_tilt, 0.0f, 0.0f);
                        _reticle.transform.position = new Vector3(tx, ty, tz);
                    } else {
                        _pan = _spinDir * _angle;
                        tx = RETICLE_DISTANCE * Mathf.Sin(3.1415f * _pan / 180.0f);
                        ty = 0;
                        tz = RETICLE_DISTANCE * Mathf.Cos(3.1415f * _pan / 180.0f);
                        _reticle.transform.rotation = Quaternion.Euler(0.0f, _pan, 0.0f);
                        _reticle.transform.position = new Vector3(tx, ty, tz);
                    }

                    _reticle.SetActive(true);
                    _turnAngle = 0.0f;
                }
                break;
            case ExperimentState.AdjustOrientation: // indicate orientation we just went through
                if(TriggerPressed) //if (Input.GetKeyDown("x")) // make the hall appear
                {
                    _responseLog.Add(ResponseLog.LINEAR_TARGET, Time.time, -1000.0f, -1000.0f, _rotational_conditions[_cond][0],
                                     _rotational_conditions[_cond][1], _rotational_conditions[_cond][2], -1000.0f, -1000.0f, _turnAngle, -1000.0f, -1000.0f);

                    _reticle.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                    _reticle.transform.position = new Vector3(0.0f, 0.0f, RETICLE_DISTANCE + _length1);
                    _reticle.SetActive(false);
                    _camera.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
                    _camera.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

                    if(_cond < NROTATE - 1)
                    {
                        _cond = _cond + 1;
                        _experimentState = ExperimentState.BeforeMotion;
                    } else
                    {
                        _responseLog.Dump(Application.persistentDataPath + "/Responses_rotation_" + _startTime + ".txt", _outputHeader);
                        Debug.Log($"Output is in {Application.persistentDataPath}");
                        d.SetDialogElements("Completed", new string[] { "" });
                        d.SetDialogInstructions("Press trigger to quit");
                        _experimentState = ExperimentState.Done;
                        _uiState = UIState.ExperimentDone;
                    }
                    return;
                }

                if(KeyUp) //if (Input.GetKey(KeyCode.UpArrow))
                {
                    if(KeyUpOld)
                    {
                        _turnStep = Mathf.Min(TURN_STEP_MULTIPLIER * _turnStep, MAX_TURN_STEP);
                    } else {
                        _turnStep = MIN_TURN_STEP;
                    }
                    KeyUpOld = true;
                    KeyDownOld = false;
                    _turnAngle = Mathf.Min(_turnAngle + _turnStep, 180.0f);
                } else if(KeyDown) {// if (Input.GetKey(KeyCode.DownArrow))
                    if(KeyDownOld)
                    {
                        _turnStep = Mathf.Min(TURN_STEP_MULTIPLIER * _turnStep, MAX_TURN_STEP);
                    } else {
                        _turnStep = MIN_TURN_STEP;
                    }
                    KeyUpOld = false;
                    KeyDownOld = true;
                    _turnAngle = Mathf.Max(_turnAngle - _turnStep, 0.0f);
                } else {
                    KeyUpOld = false;
                    KeyDownOld = true;
                }

                if (_pitch)
                {
                    _pan = 0.0f;
                    _tilt = _spinDir * (_turn + _turnAngle);
                    _camera.transform.rotation = Quaternion.Euler(_tilt, _pan, 0.0f);
                    Debug.Log($"Pitch angle {_turn} {_turnAngle}");
                } else {
                    _pan = _spinDir * (_turn + _turnAngle);
                    _tilt = 0.0f;
                    _camera.transform.rotation = Quaternion.Euler(_tilt, _pan, 0.0f);
                    Debug.Log($"Yaw angle {_turn} {_turnAngle}");
                }
                break;
            case ExperimentState.Done: // should never get here
                break;
        }
    }

    private void DoWhereDidIGo()
    {
        float _distance, _angle;
        float x, y, z, tx, ty, tz;
        Dialog d = _dialog.GetComponent<Dialog>();

        switch (_experimentState)
        {
             case ExperimentState.Initialize: // provide instructions

                d.SetDialogElements("Where did I go", new string[] { "Indicate direction/distance" });
                d.SetDialogInstructions("Press trigger to start");
                _dialog.SetActive(true);
                _home.SetActive(true);
                _experimentState = ExperimentState.Setup;
                break;
            case ExperimentState.Setup: // clean things up to start
                if(TriggerPressed) //if (Input.GetKeyDown("x"))
                {
                    _dialog.SetActive(false);
                    _experimentState = ExperimentState.BeforeMotion;
                    _cond = 0;
                }
                break;
            case ExperimentState.BeforeMotion: // waiting befofre the first arm (of length _length1)
                Debug.Log("Before motion");
                if (_cond < NTRIANG)
                {
                    _length1 = _triangle_conditions[_cond][0];
                    _length2 = _triangle_conditions[_cond][1];
                    _turn = 180 - _triangle_conditions[_cond][2]; // angles measured the other way
                    _pitch = _triangle_conditions[_cond][3] > 0;
                    _turnRight = _triangle_conditions[_cond][4] > 0;

                    _sf.RePaint(_pitch);
                    _sf.EnableHomeBaseDisplay();

                    Debug.Log("STARTING CONDITION");
                    Debug.Log(_cond);


                    // set +ve spin direction depending on pitch and turnRight values
                    if (_pitch)
                    {
                        if (_turnRight)
                            _spinDir = -1.0f;
                        else
                            _spinDir = 1.0f;
                    }
                    else
                    {
                        if (_turnRight)
                            _spinDir = 1.0f;
                        else
                            _spinDir = -1.0f;
                    }
                    _experimentState = ExperimentState.WaitForWhereDidIGo;
                    _distance = 0;
                    _camera.transform.position = new Vector3(0, 0, _distance);
                    _camera.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                    d.SetDialogElements("Where Did I Go?", new string[] { "Condition " + _cond });
                    d.SetDialogInstructions("Press trigger to start");
                    _dialog.SetActive(true);
                }
                break;
            case ExperimentState.WaitForWhereDidIGo:
                if(TriggerPressed) //if (Input.GetKeyDown("x"))
                {
                    _dialog.SetActive(false);
                    _home.SetActive(false);
                    _sf.EnableHomeBaseDisplay();
                    _experimentState = ExperimentState.LegOne;
                    _motion1Start = Time.time;
                    Debug.Log("Starting condition");
                }
                break;
            case ExperimentState.LegOne: // moving in direction (0,0,1) to _length1
                Debug.Log("leg1");
                _distance = _velocity * (Time.time - _motion1Start);
                _camera.transform.position = new Vector3(0, 0, _distance);
                if (_distance >= _length1) // got there, turn
                {
                    _distance = _length1;
                    _camera.transform.position = new Vector3(0, 0, _distance);
                    _turnStart = Time.time;
                    _experimentState = ExperimentState.Turning;
                }
                break;
            case ExperimentState.Turning: // rotate about the angle bewteen the two lengths (_turn an amplitude)
                Debug.Log("Turning");
                _angle = _spinV * (Time.time - _turnStart);
                Debug.Log(_angle);
                Debug.Log(_turn);
                if (_angle >= _turn) // rotation is finished
                {
                    _angle = _turn;
                    _experimentState = ExperimentState.LegTwo;
                    if (_pitch)
                    {
                        _tilt = _spinDir * _angle;
                    }
                    else
                    {
                        _pan = _spinDir * _angle;
                    }
                    _motion2Start = Time.time;
                }
                else
                {
                    if (_pitch)
                    {
                        _pan = 0.0f;
                        _tilt = _spinDir * _angle;
                        _camera.transform.rotation = Quaternion.Euler(_tilt, _pan, 0.0f);
                    }
                    else
                    {
                        _pan = _spinDir * _angle;
                        _tilt = 0.0f;
                        _camera.transform.rotation = Quaternion.Euler(_tilt, _pan, 0.0f);
                    }
                }
                break;
            case ExperimentState.LegTwo: // present stimulus for leg 2
                Debug.Log("Legtwo");
                _distance = _velocity * (Time.time - _motion2Start);
                if(_distance >= _length2)
                    _distance = _length2;

                // update camera position
                if (_pitch)
                {
                    x = 0;
                    y = -_distance * Mathf.Sin(3.1415f * _spinDir * _turn / 180.0f);
                    z = _distance * Mathf.Cos(3.1415f * _spinDir * _turn / 180.0f) + _length1;
                }
                else
                {
                    x = _distance * Mathf.Sin(3.1415f * _spinDir * _turn / 180.0f);
                    y = 0;
                    z = _distance * Mathf.Cos(3.1415f * _spinDir * _turn / 180.0f) + _length1;
                }
                _camera.transform.position = new Vector3(x, y, z);

                // at the end of the motion
                if (_distance >= _length2) // got to the stimulus distance for leg 2
                {
                    _distance = _length2;
                    _experimentState = ExperimentState.PlaceTarget;
                    _directionDistance = UnityEngine.Random.Range(START_MAX_RETICLE_DISTANCE, START_MAX_RETICLE_DISTANCE);

                    if (_pitch)
                    {
                        _tilt = _spinDir * _turn;
                        tx = 0;
                        ty = -_directionDistance * Mathf.Sin(3.1415f * _tilt / 180.0f);
                        tz = _directionDistance * Mathf.Cos(3.1415f * _tilt / 180.0f);
                        _reticle.transform.rotation = Quaternion.Euler(_tilt, 0.0f, 0.0f);
                        _reticle.transform.position = new Vector3(tx + _camera.transform.position.x, ty + _camera.transform.position.y, tz + _camera.transform.position.z);
                    } else {
                        _pan = _spinDir * _turn;
                        tx = _directionDistance * Mathf.Sin(3.1415f * _pan / 180.0f);
                        ty = 0;
                        tz = _directionDistance * Mathf.Cos(3.1415f * _pan / 180.0f);
                        _reticle.transform.rotation = Quaternion.Euler(0.0f, _pan, 0.0f);
                        _reticle.transform.position = new Vector3(tx + _camera.transform.position.x, ty + _camera.transform.position.y, tz + _camera.transform.position.z);
                    }

                    _reticle.SetActive(true);
                    _directionAngle = 0.0f;
                    break;
                }
                break;
             case ExperimentState.PlaceTarget: // point in direction to goal location
                Debug.Log("TargetDirection");
                if(KeyUp) { //if (Input.GetKey(KeyCode.RightArrow))
                    if(KeyUpOld)
                    {
                        _turnStep = Mathf.Min(TURN_STEP_MULTIPLIER * _turnStep, MAX_TURN_STEP);
                    } else {
                        _turnStep = MIN_TURN_STEP;
                    }
                    KeyUpOld = true;
                    KeyDownOld = false;
                    _directionAngle = Mathf.Min(_directionAngle + _turnStep, 180.0f);
                } else if(KeyDown) { //if (Input.GetKey(KeyCode.LeftArrow))
                     if(KeyDownOld)
                    {
                        _turnStep = Mathf.Min(TURN_STEP_MULTIPLIER * _turnStep, MAX_TURN_STEP);
                    } else {
                        _turnStep = MIN_TURN_STEP;
                    }
                    KeyUpOld = false;
                    KeyDownOld = true;
                    _directionAngle = Mathf.Max(_directionAngle - _turnStep, 0.0f);
                } else {
                    KeyUpOld = false;
                    KeyDownOld = false;
                }
                if(Astate) //if (Input.GetKey("a"))
                {
                    if(AstateOld)
                    {
                        _motionStep = Mathf.Min(MOTION_STEP_MULTIPLIER*_motionStep, MAX_MOTION_STEP);
                    } else
                    {
                        _motionStep = MIN_MOTION_STEP;
                    }
                    AstateOld = true;
                    BstateOld = false;
                    _directionDistance = Mathf.Max(_directionDistance - _motionStep, MIN_RETICLE_DISTANCE);

                } else if(Bstate) {//if (Input.GetKey("b"))
                    if(BstateOld)
                    {
                        _motionStep = Mathf.Min(MOTION_STEP_MULTIPLIER*_motionStep, MAX_MOTION_STEP);
                    } else
                    {
                        _motionStep = MIN_MOTION_STEP;
                    }
                    AstateOld = false;
                    BstateOld = true;
                    _directionDistance = Mathf.Min(_directionDistance + _motionStep, MAX_RETICLE_DISTANCE);
                } else {
                    AstateOld = false;
                    BstateOld = false;
                }

                // rotate the camera based on the update
                if (_pitch)
                {
                    _pan = 0.0f;
                    _tilt = _spinDir * (_turn + _directionAngle);
                    _camera.transform.rotation = Quaternion.Euler(_tilt, _pan, 0.0f);
                    Debug.Log($"Pitch angle {_turn} {_directionAngle}");
                } else {
                    _pan = _spinDir * (_turn + _directionAngle);
                    _tilt = 0.0f;
                    _camera.transform.rotation = Quaternion.Euler(_tilt, _pan, 0.0f);
                    Debug.Log($"Yaw angle {_turn} {_directionAngle}");
                }

                //now update the position and orientation of the recticle
                if (_pitch)
                {
                    _tilt = _spinDir * (_turn + _directionAngle);
                    tx = 0;
                    ty = -_directionDistance * Mathf.Sin(3.1415f * _tilt / 180.0f);
                    tz = _directionDistance * Mathf.Cos(3.1415f * _tilt / 180.0f);
                    _reticle.transform.rotation = Quaternion.Euler(_tilt, 0.0f, 0.0f);
                    _reticle.transform.position = new Vector3(tx + _camera.transform.position.x, ty + _camera.transform.position.y, tz + _camera.transform.position.z);
                } else {
                    _pan = _spinDir * (_turn + _directionAngle);
                    tx = _directionDistance * Mathf.Sin(3.1415f * _pan / 180.0f);
                    ty = 0;
                    tz = _directionDistance * Mathf.Cos(3.1415f * _pan / 180.0f);
                    _reticle.transform.rotation = Quaternion.Euler(0.0f, _pan, 0.0f);
                    _reticle.transform.position = new Vector3(tx + _camera.transform.position.x, ty + _camera.transform.position.y, tz + _camera.transform.position.z);
                }

                // deal with selection
                if(TriggerPressed) //if (Input.GetKeyDown("x"))
                {
                    _reticle.SetActive(false);
                    _responseLog.Add(ResponseLog.WHERE_DID_I_GO, Time.time, _triangle_conditions[_cond][0], _triangle_conditions[_cond][1], _triangle_conditions[_cond][2],
                                     _triangle_conditions[_cond][3], _triangle_conditions[_cond][4], -1000.0f, -1000.0f, -1000.0f, _directionDistance, _directionAngle);
                    if (_cond < NTRIANG - 1)
                    {
                        _cond = _cond + 1;
                        _experimentState = ExperimentState.BeforeMotion;
                    }
                    else
                    {
                        _responseLog.Dump(Application.persistentDataPath + "/Responses_triangle_" + _startTime + ".txt", _outputHeader);
                        _camera.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
                        _camera.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                        d.SetDialogElements("Completed", new string[] { "" });
                        d.SetDialogInstructions("Press trigger to quit");
                        _experimentState = ExperimentState.Done;
                        _uiState = UIState.ExperimentDone;
                    }
                }
                break;
            case ExperimentState.Done: // should never get here
                break;
        }

    }





    void setTargetPosition(string item, float x, float y, float z)
    {
        GameObject target = GameObject.Find(item);
        target.transform.position = new Vector3(x, y, z);
    }

      /**
     * Quit the application
     **/
    public void QuitPlaying()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /**
     * Process the input items we are interested in and encode KeyUp and KeyDown
     * and debounce these values. 
     **/
    void ProcessInput()
    {
        Vector2 thumbstickValue = thumbstickAction.action.ReadValue<Vector2>();

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
