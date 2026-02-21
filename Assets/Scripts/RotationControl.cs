using UnityEngine;
using UnityEditor;

/**
 * Rotation control experiment
 *
 * Rotation only
 *  - pointer appears in front
 *  - press to start
 *  - point is extinguished
 *  - rotate at constant velocity 30 degrees/s
 *  - when stopped the reticle appears attached to torso
 *  - rotate back to initial orientation
 *  - click when aligned
 *
 * Angles are 135, 150, 165 (with 180 being straight ahead)
 * Both signs
 * Pan and tilt
 * For 3 x 2 x 2 = 12 conditions
 *
 * Version control
 *.    V2.1 match the motion (rather than return to 0)
 *     V2.0 borrowed from the initial prototype version and modified as per the notes of the 17th
 *     V1.0 initial version from the monolith program
 *
 * Copyright (c) Michael Jenkin 2025, 2026
 */

public class RotationControl : MonoBehaviour
{

    public Material defaultDialogMaterial;
    public Material instructionMaterial;

    private enum ExperimentState
    {
        Initialize, 
        Setup,
        BeforeMotion,
        WaitForButtonPress,
        WaitToTurn,
        Turning,
        TurnBack,
        Wait,
        MoveForward,        
        AdjustTarget,
        Done
    };


    private const int NROTATION = 12;                       // number of rotation conditions
    private const float TARGET_DISTANCE = 2.0f;             // distance to orientaiton reticle (m)
    private const float RETICLE_DISTANCE = 2.0f;            // distance to reticle (m)
    private const float SPINV = 30.0f;                      // abs rotational velocity deg/sec
    private const float MIN_TURN_ANGLE = -180.0f;           // minimm turn angle
    private const float MAX_TURN_ANGLE = 180.0f;            // maximumm turn angle
    private const float MIN_TURN_STEP = 0.1f;               // minimum turn step (deg)
    private const float MAX_TURN_STEP = 2.0f;               // maximm turn step (deg)   
    private const float TURN_STEP_MULTIPLIER = 1.01f;       // turn step multiplier
    private float _turnStep = MIN_TURN_STEP;                // step size for turn keypress (deg)

    private Dialog _d;                                      // Dialog interface
    private GameObject _dialog;                             // Dialog Gameobject
    private GameObject _camera;
    private GameObject _reticle;
    private GameObject _target;
    private InputHandler _inputHandler;
    private ExperimentState _experimentState = ExperimentState.Initialize;    // current state of the experiment
    private ResponseLog _responseLog = new ResponseLog();   // the response log
    private HeadTrackerLog _trackerLog;

    private int _cond;                                      // current condition number (starts from 0)
    private float _spinDir = 1.0f;                          // spin direction
    private float _turn;
    private bool _pitch;                                    // is this a pitch or yaw
    private bool _turnRight;                                // is this trial coded as to the left or right?
  
    private float _turnStart;
    private float _turnAngle;
    private bool _KeyUpOld = false;
    private bool _KeyDownOld = false;

    float[][] _rotation_conditions = new float[NROTATION][];   // the conditions
    void Start()
    {
        HomeBaseDriver driver = GetComponent<HomeBaseDriver>();
        _dialog = driver.Dialog;
        _d = _dialog.GetComponent<Dialog>();
        _experimentState = ExperimentState.Initialize;
        _camera = GameObject.Find("Camera Holder");
        _reticle = driver.AdjustableTarget;
        _target = driver.FixedTarget;
        _inputHandler = _camera.GetComponent<InputHandler>();
        _trackerLog = GetComponent<HeadTrackerLog> ();
        ConstructConditions();
    }

     private void ConstructConditions()
    {
        //note: angles are 180-angle shown
        float[] r1  = new float[3] {135.0f, 1.0f, 1.0f}; // a1, pan/tilt, dir1/dir2
        float[] r2  = new float[3] {150.0f, 1.0f, 1.0f}; // a1, pan/tilt, dir1/dir2
        float[] r3  = new float[3] {165.0f, 1.0f, 1.0f}; // a1, pan/tilt, dir1/dir2
        float[] r4  = new float[3] {135.0f, 1.0f,-1.0f}; // a1, pan/tilt, dir1/dir2
        float[] r5  = new float[3] {150.0f, 1.0f,-1.0f}; // a1, pan/tilt, dir1/dir2
        float[] r6  = new float[3] {165.0f, 1.0f,-1.0f}; // a1, pan/tilt, dir1/dir2
        float[] r7  = new float[3] {135.0f,-1.0f, 1.0f}; // a1, pan/tilt, dir1/dir2
        float[] r8  = new float[3] {150.0f,-1.0f, 1.0f}; // a1, pan/tilt, dir1/dir2
        float[] r9  = new float[3] {165.0f,-1.0f, 1.0f}; // a1, pan/tilt, dir1/dir2
        float[] r10 = new float[3] {135.0f,-1.0f,-1.0f}; // a1, pan/tilt, dir1/dir2
        float[] r11 = new float[3] {150.0f,-1.0f,-1.0f}; // a1, pan/tilt, dir1/dir2
        float[] r12 = new float[3] {165.0f,-1.0f,-1.0f}; // a1, pan/tilt, dir1/dir2
        _rotation_conditions[0] = r1;
        _rotation_conditions[1] = r2;
        _rotation_conditions[2] = r3;
        _rotation_conditions[3] = r4;
        _rotation_conditions[4] = r5;
        _rotation_conditions[5] = r6;
        _rotation_conditions[6] = r7;
        _rotation_conditions[7] = r8;
        _rotation_conditions[8] = r9;
        _rotation_conditions[9] = r10;
        _rotation_conditions[10] = r11;
        _rotation_conditions[11] = r12;
        for(int i = 0; i < NROTATION*10; i++)
        {
            int index1 = UnityEngine.Random.Range(0, NROTATION);
            int index2 = UnityEngine.Random.Range(0, NROTATION);
            float[] z = _rotation_conditions[index1];
            _rotation_conditions[index1] = _rotation_conditions[index2];
            _rotation_conditions[index2] = z;
        }
    }


    public void DoRotationControl(long startTime, SphereField sf)
    {
        float angle, x, y, z, pan, tilt;

        Debug.Log("Do Rotation task " + _experimentState);
        switch (_experimentState)
        {
            case ExperimentState.Initialize:
                _d.SetBackground(instructionMaterial); 
                _d.SetDialogElements("Rotation Motion", new string[] { "" });
                _d.SetDialogInstructions("Press trigger to start");
                _experimentState = ExperimentState.Setup;
                _dialog.SetActive(true);
                break;
            case ExperimentState.Setup: // clean things up to start
                if(_inputHandler.TriggerPressed) 
                {
                    _d.SetBackground(defaultDialogMaterial); 
                    _experimentState = ExperimentState.BeforeMotion;
                    _cond = 0;
                }
                break;
            case ExperimentState.BeforeMotion: // waiting before motion
                Debug.Log($"BeforeMotion {_cond}");

                if (_cond < NROTATION)
                {
                    _turn = 180.0f - _rotation_conditions[_cond][0];
                    _pitch = _rotation_conditions[_cond][1] > 0;
                    _turnRight = _rotation_conditions[_cond][2] > 0;

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

                    sf.RePaint(_pitch);
                    sf.EnableHomeBaseDisplay();

                    _experimentState = ExperimentState.WaitForButtonPress;
                    _camera.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
                    _camera.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                    _d.SetDialogElements("Rotational Motion", new string[] { "Condition " + (1+_cond) + "/" + NROTATION });
                    _d.SetDialogInstructions("Press trigger to start");
                    _dialog.SetActive(true);
                    _trackerLog.StartRecording();
                }
                break;
            case ExperimentState.WaitForButtonPress:
                if(_inputHandler.TriggerPressed) 
                {
                    _dialog.SetActive(false);
                    _target.transform.position = new Vector3(0.0f, 0.0f, TARGET_DISTANCE);
                    _target.transform.rotation = Quaternion.Euler(0.0f,0.0f,0.0f);
                    _target.SetActive(true);
                    _experimentState = ExperimentState.WaitToTurn;
                }
                break;
            case ExperimentState.WaitToTurn:
                if(_inputHandler.TriggerPressed)
                {
                    _turnStart = Time.time;
                    _target.SetActive(false);
                    _experimentState = ExperimentState.Turning;
                }
                break;
            case ExperimentState.Turning: // rotate (_turn is an amplitude)
                angle = SPINV * (Time.time - _turnStart);
                if(angle >= _turn)
                    angle = _turn;

                // update the camera orientation
                if (_pitch)
                {
                    pan = 0.0f;
                    tilt = _spinDir * angle;
                    _camera.transform.rotation = Quaternion.Euler(tilt, pan, 0.0f);
                } else {
                    pan = _spinDir * angle;
                    tilt = 0.0f;
                    _camera.transform.rotation = Quaternion.Euler(tilt, pan, 0.0f);
                }

                // deal with finished rotation
                if (angle >= _turn)
                {
                    angle = _turn;
                    _experimentState = ExperimentState.TurnBack;
                    if (_pitch)
                    {
                        tilt = _spinDir * angle;
                        x = 0;
                        y = -RETICLE_DISTANCE * Mathf.Sin(3.1415f * tilt / 180.0f);
                        z = RETICLE_DISTANCE * Mathf.Cos(3.1415f * tilt / 180.0f);
                        _reticle.transform.rotation = Quaternion.Euler(tilt, 0.0f, 0.0f);
                        _reticle.transform.position = new Vector3(x, y, z);
                    } else {
                        pan = _spinDir * angle;
                        x = RETICLE_DISTANCE * Mathf.Sin(3.1415f * pan / 180.0f);
                        y = 0;
                        z = RETICLE_DISTANCE * Mathf.Cos(3.1415f * pan / 180.0f);
                        _reticle.transform.rotation = Quaternion.Euler(0.0f, pan, 0.0f);
                        _reticle.transform.position = new Vector3(x, y, z);
                    }

                    _reticle.SetActive(true);
                    _turnAngle = 0.0f;
                }
                break;
            case ExperimentState.TurnBack: // rotate (_turn is an amplitude)
                Debug.Log($"Turnback keyup {_inputHandler.KeyUp} {_KeyUpOld} keydown {_inputHandler.KeyDown} {_KeyDownOld} step {_turnStep}");
                if(_inputHandler.KeyUp) //if (Input.GetKey(KeyCode.UpArrow))
                {
                    if(_KeyUpOld)
                    {
                        _turnStep = Mathf.Min(TURN_STEP_MULTIPLIER * _turnStep, MAX_TURN_STEP);
                    } else {
                        _turnStep = MIN_TURN_STEP;
                    }
                    _KeyUpOld = true;
                    _turnAngle = Mathf.Min(_turnAngle + _turnStep, MAX_TURN_ANGLE);
                } else if(_inputHandler.KeyDown) {// if (Input.GetKey(KeyCode.DownArrow))
                    if(_KeyDownOld)
                    {
                        _turnStep = Mathf.Min(TURN_STEP_MULTIPLIER * _turnStep, MAX_TURN_STEP);
                    } else {
                        _turnStep = MIN_TURN_STEP;
                    }
                    _KeyDownOld = true;
                    _turnAngle = Mathf.Max(_turnAngle - _turnStep, MIN_TURN_ANGLE);
                } else {
                    _KeyUpOld = false;
                    _KeyDownOld = false;
                }

                if (_pitch)
                {
                    pan = 0.0f;
                    tilt = _spinDir * (_turn + _turnAngle);
                    _camera.transform.rotation = Quaternion.Euler(tilt, pan, 0.0f);

                    x = 0;
                    y = -RETICLE_DISTANCE * Mathf.Sin(3.1415f * tilt / 180.0f);
                    z = RETICLE_DISTANCE * Mathf.Cos(3.1415f * tilt / 180.0f);
                    _reticle.transform.rotation = Quaternion.Euler(tilt, 0.0f, 0.0f);
                    _reticle.transform.position = new Vector3(x, y, z);
                    Debug.Log($"Pitch angle {_turn} {_turnAngle}");
                } else {
                    pan = _spinDir * (_turn + _turnAngle);
                    tilt = 0.0f;
                    _camera.transform.rotation = Quaternion.Euler(tilt, pan, 0.0f);

                    x = RETICLE_DISTANCE * Mathf.Sin(3.1415f * pan / 180.0f);
                    y = 0;
                    z = RETICLE_DISTANCE * Mathf.Cos(3.1415f * pan / 180.0f);
                    _reticle.transform.rotation = Quaternion.Euler(0.0f, pan, 0.0f);
                    _reticle.transform.position = new Vector3(x, y, z);
                    Debug.Log($"Yaw angle {_turn} {_turnAngle}");
                }

                if(_inputHandler.TriggerPressed) 
                {
                    _responseLog.AddRotation(_cond, _turnStart, 180 - _turn, _pitch, _spinDir, _turnAngle);
                    _trackerLog.StopRecordingAndSave(Application.persistentDataPath + "/HeadTracking_rotation_" + startTime + "_" + _cond + ".txt");

                    _reticle.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                    _reticle.transform.position = new Vector3(0.0f, 0.0f, RETICLE_DISTANCE);
                    _reticle.SetActive(false);
                    _camera.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
                    _camera.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

                    if(_cond < NROTATION - 1)
                    {
                        _cond = _cond + 1;
                        _experimentState = ExperimentState.BeforeMotion;
                    } 
                    else
                    {
                        _responseLog.Dump(Application.persistentDataPath + "/Responses_rotation_" + startTime + ".txt", "cond, starttime, motion, rotation, pitch, spindir, inittarget, finaltarget");
                        _d.SetDialogElements("Completed", new string[] { "" });
                        _d.SetDialogInstructions("Press trigger to quit");
                        _dialog.SetActive(true);
                        _experimentState = ExperimentState.Done;
                    }
                }
                break;
            case ExperimentState.Done:
                if (_inputHandler.TriggerPressed)
                {
#if UNITY_EDITOR
                    EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                }
                break;
        }

    }
}
