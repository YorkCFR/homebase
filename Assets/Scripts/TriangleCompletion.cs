using UnityEngine;
using UnityEditor;

/**
 * Triangle completion experiment
 *
 * Homing
 *  - start with target in front of you by 5cm
 *  - press trigger to start
 *  - move back a FIXED dist (8m) at const velo- 1.4m/s
 *  - delay- 200ms
 *  - target is extinguished
 * - viewer rotates at constant velocity 30 degrees a sec??
 * - delay 200ms
 * - move forward leg const velocity 4 or 8m
 * - pointer appears on torso fixed, dist from you is chosen uniformly between 4m-12m (record pos of pointer chosen)
 * - can move in dist and rot
 * - dist is constrained to 0.5m and 32m
 * - point to start
 * - click when aligned
 *
 * Version History
 * V2.0 - major refactoring
 * V1.0 - from the monolithic version
 *
 * Copyright (c) Michael Jenkin 2025, 2026.
 **/

public class TriangleCompletion : MonoBehaviour
{

    public Material defaultDialogMaterial;
    public Material instructionMaterial;

    private enum ExperimentState
    {
        Initialize, 
        Setup,
        BeforeMotion,
        WaitForBackwardTarget,
        WaitToMove,
        WaitToRotate,
        MovingBackward,
        Wait,
        Turn,
        LegTwo,
        PlaceTarget,
        Done
 
    };

    private const int NTRIANG = 24;                         // number of triangle completion conditions
    private const float TARGET_START_DIST = 0.05f;          // target starts this far from you
    private const float WAIT_TIME = 0.2f;                   // Wait time in sec
    private const float ROTATE_VEL = 30.0f;                 // rotational velocity in deg/sec
    private const float LINEAR_VEL = 1.4f;                  // linear velocity in m/s


    private const float MIN_TURN_ANGLE = -180.0f;           // minimm turn angle
    private const float MAX_TURN_ANGLE = 180.0f;            // maximumm turn angle
    private const float MIN_TURN_STEP = 0.1f;               // minimum turn step (deg)
    private const float MAX_TURN_STEP = 2.0f;               // maximm turn step (deg)   
    private const float TURN_STEP_MULTIPLIER = 1.01f;       // turn step multiplier
    private float _turnStep = MIN_TURN_STEP;                // step size for turn keypress (deg)

    private const float START_MIN_RETICLE_DISTANCE = 2.0f;  // minimum distance for reticle
    private const float START_MAX_RETICLE_DISTANCE = 16.0f; // maximum distance for reticle    
    private const float MIN_RETICLE_DISTANCE = 0.5f;       // minimum target initial position
    private const float MAX_RETICLE_DISTANCE = 32.0f;      // maximum target initial position
    private const float MIN_MOTION_STEP = 0.01f;            // motion step minimum
    private const float MAX_MOTION_STEP = 0.5f;            // motion step maximum
    private const float MOTION_STEP_MULTIPLIER = 1.1f;      // motion step multiplier
    private float _motionStep = MIN_MOTION_STEP;            // how big a step to make for a keypress (m)

    private Dialog _d;                                      // Dialog interface
    private GameObject _reticle = null;
    private GameObject _target = null;
    private GameObject _camera = null;
    private GameObject _dialog = null;

    private InputHandler _inputHandler;
    private ExperimentState _experimentState = ExperimentState.Initialize;    // current state of the experiment
    private ResponseLog _responseLog = new ResponseLog();   // the response log
    private HeadTrackerLog _trackerLog;

    private int _cond;                                      // current condition number (starts from 0)
    private float _spinDir = 1.0f;                          // spin direction
    private bool _pitch;                                    // is this a pitch or yaw
    private bool _turnRight;                                // is this trial coded as to the left or right?
    private float _length1;                                 // length of first leg
    private float _length2;                                 // length of the second leg
    private float _turn;                                    // unsigned amount to turn
    private float _directionAngle;                          // relative angle chosen by participant
    private float _directionDistance;                       // relative distance chosen by participant
    private float _directionDistanceInit;                   // initial direction distance


    float[][] _triangle_conditions = new float[NTRIANG][];
    private float _waitStart, _backwardTime, _waitStart2, _waitStart3, _turnStart, _motion2Start;

    private bool _KeyUpOld = false;
    private bool _KeyDownOld = false;
    private bool _AstateOld = false;
    private bool _BstateOld = false;

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
        float[] c1  = new float[5] { 8.0f, 8.0f, 165.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c2  = new float[5] { 8.0f, 8.0f, 150.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c3  = new float[5] { 8.0f, 8.0f, 135.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c4  = new float[5] { 8.0f, 4.0f, 165.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c5  = new float[5] { 8.0f, 4.0f, 150.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c6  = new float[5] { 8.0f, 4.0f, 135.0f, 1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c7  = new float[5] { 8.0f, 8.0f, 165.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c8  = new float[5] { 8.0f, 8.0f, 150.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c9  = new float[5] { 8.0f, 8.0f, 135.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c10  = new float[5] { 8.0f, 4.0f, 165.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c11  = new float[5] { 8.0f, 4.0f, 150.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c12  = new float[5] { 8.0f, 4.0f, 135.0f, 1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c13 = new float[5] { 8.0f, 8.0f, 165.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c14  = new float[5] {8.0f, 8.0f, 150.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c15 = new float[5] { 8.0f, 8.0f, 135.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c16 = new float[5] { 8.0f, 4.0f, 165.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c17 = new float[5] { 8.0f, 4.0f, 150.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c18 = new float[5] { 8.0f, 4.0f, 135.0f, -1.0f, 1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c19 = new float[5] { 8.0f, 8.0f, 165.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c20 = new float[5] { 8.0f, 8.0f, 150.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c21 = new float[5] { 8.0f, 8.0f, 135.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        float[] c22  = new float[5] { 8.0f, 4.0f, 165.0f,-1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c23  = new float[5] { 8.0f, 4.0f, 150.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2
        float[] c24  = new float[5] { 8.0f, 4.0f, 135.0f, -1.0f, -1.0f }; // l1, l2, theta, pan/tilt, dir1/dir2

        _triangle_conditions[0] = c1; _triangle_conditions[1] = c2; _triangle_conditions[2] = c3;
        _triangle_conditions[3] = c4; _triangle_conditions[4] = c5; _triangle_conditions[5] = c6;
        _triangle_conditions[6] = c7; _triangle_conditions[7] = c8; _triangle_conditions[8] = c9;
        _triangle_conditions[9] = c10; _triangle_conditions[10] = c11; _triangle_conditions[11] = c12;

        _triangle_conditions[12] = c13; _triangle_conditions[13] = c14; _triangle_conditions[14] = c15;
        _triangle_conditions[15] = c16; _triangle_conditions[16] = c17; _triangle_conditions[17] = c18;
        _triangle_conditions[18] = c19; _triangle_conditions[19] = c20; _triangle_conditions[20] = c21;
        _triangle_conditions[21] = c22; _triangle_conditions[22] = c23; _triangle_conditions[23] = c24;

        for(int i = 0; i < 10*NTRIANG; i++)
        {
            int index1 = UnityEngine.Random.Range(0, NTRIANG);
            int index2 = UnityEngine.Random.Range(0, NTRIANG);
            float[] z = _triangle_conditions[index1];
            _triangle_conditions[index1] = _triangle_conditions[index2];
            _triangle_conditions[index2] = z;
        }

    }

    // Update is called once per frame
   public void DoTriangleCompletion(long startTime, SphereField sf)
    {
        float dist, angle, x, y, z, pan, tilt;

        Debug.Log("Do Triangle Completion " + _experimentState);
        switch (_experimentState)
        {
            case ExperimentState.Initialize:
                _d.SetBackground(instructionMaterial); 
                _d.SetDialogElements("Triangle Completion", new string[] { "" });
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

                if (_cond < NTRIANG)
                {
                    _length1 = _triangle_conditions[_cond][0];
                    _length2 = _triangle_conditions[_cond][1];
                    _turn = 180.0f - _triangle_conditions[_cond][2];
                    _pitch = _triangle_conditions[_cond][3] > 0;
                    _turnRight = _triangle_conditions[_cond][4] > 0;

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

                    _experimentState = ExperimentState.WaitForBackwardTarget;
                    _camera.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
                    _camera.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                    _d.SetDialogElements("Triangle Completion", new string[] { "Condition " + (1+_cond) + "/" + NTRIANG });
                    _d.SetDialogInstructions("Press trigger to start");
                    _dialog.SetActive(true);
                    _trackerLog.StartRecording();
                }
                break;
            case ExperimentState.WaitForBackwardTarget:
                if(_inputHandler.TriggerPressed) 
                {
                    _target.transform.position = new Vector3(0.0f,0.0f, TARGET_START_DIST);
                    _target.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                    _dialog.SetActive(false);
                    _target.SetActive(true);
                    _waitStart = Time.time;
                    _experimentState = ExperimentState.WaitToMove;
                }
                break;
            case ExperimentState.WaitToMove:
                if((Time.time - _waitStart) >= WAIT_TIME) {
                    _backwardTime = Time.time;
                    _experimentState = ExperimentState.MovingBackward;
                }
                break;
            case ExperimentState.MovingBackward:
                dist = LINEAR_VEL * (Time.time - _backwardTime);
                if(dist >= _length1)
                {
                    dist = _length1;
                    _experimentState = ExperimentState.WaitToRotate;
                    _waitStart2 = Time.time;
                }
                _camera.transform.position = new Vector3(0.0f, 0.0f, -dist);
                break;
            case ExperimentState.WaitToRotate:
                if((Time.time - _waitStart2) >= WAIT_TIME)
                {
                    _target.SetActive(false);
                    _experimentState = ExperimentState.Turn;
                    _turnStart = Time.time;
                }
                break;
            case ExperimentState.Turn:
                angle = ROTATE_VEL * (Time.time - _turnStart);
                if (angle >= _turn) // rotation is finished
                {
                    angle = _turn;
                    _waitStart3 = Time.time;
                    _experimentState = ExperimentState.Wait;
                }

                // execute the turn
                if (_pitch)
                {
                    pan = 0.0f;
                    tilt = _spinDir * angle;
                    _camera.transform.rotation = Quaternion.Euler(tilt, pan, 0.0f);
                }
                else
                {
                    pan = _spinDir * angle;
                    tilt = 0.0f;
                    _camera.transform.rotation = Quaternion.Euler(tilt, pan, 0.0f);
                }
                break;
            case ExperimentState.Wait:
                 if((Time.time - _waitStart2) >= WAIT_TIME)
                {
                    _target.SetActive(false);
                    _experimentState = ExperimentState.LegTwo;
                    _motion2Start = Time.time;
                }
                break;
            case ExperimentState.LegTwo: // drive forward along leg2
                Debug.Log("Legtwo");
                dist = LINEAR_VEL * (Time.time - _motion2Start);
                if(dist >= _length2)
                    dist = _length2;

                // update camera position
                if (_pitch)
                {
                    x = 0;
                    y = -dist * Mathf.Sin(3.1415f * _spinDir * _turn / 180.0f);
                    z = dist * Mathf.Cos(3.1415f * _spinDir * _turn / 180.0f) - _length1;
                }
                else
                {
                    x = dist * Mathf.Sin(3.1415f * _spinDir * _turn / 180.0f);
                    y = 0;
                    z = dist * Mathf.Cos(3.1415f * _spinDir * _turn / 180.0f) - _length1;
                }
                _camera.transform.position = new Vector3(x, y, z);

                // at the end of the motion
                if (dist >= _length2) // got to the stimulus distance for leg 2
                {
                    _experimentState = ExperimentState.PlaceTarget;
                    _directionDistance = UnityEngine.Random.Range(START_MIN_RETICLE_DISTANCE, START_MAX_RETICLE_DISTANCE);
                    _directionDistanceInit = _directionDistance;

                    if (_pitch)
                    {
                        tilt = _spinDir * _turn;
                        pan = 0.0f;
                        x = 0;
                        y = -_directionDistance * Mathf.Sin(3.1415f * tilt / 180.0f);
                        z = _directionDistance * Mathf.Cos(3.1415f * tilt / 180.0f);
                        _reticle.transform.rotation = Quaternion.Euler(tilt, 0.0f, 0.0f);
                        _reticle.transform.position = new Vector3(x + _camera.transform.position.x, y + _camera.transform.position.y, z + _camera.transform.position.z);
                    } else {
                        pan = _spinDir * _turn;
                        tilt = 0.0f;
                        x = _directionDistance * Mathf.Sin(3.1415f * pan / 180.0f);
                        y = 0;
                        z = _directionDistance * Mathf.Cos(3.1415f * pan / 180.0f);
                        _reticle.transform.rotation = Quaternion.Euler(tilt, pan, 0.0f);
                        _reticle.transform.position = new Vector3(x + _camera.transform.position.x, y + _camera.transform.position.y, z + _camera.transform.position.z);
                    }

                    _reticle.SetActive(true);
                    _directionAngle = 0.0f;
                }
                break;
            case ExperimentState.PlaceTarget: // point in direction to goal location
                Debug.Log("TargetDirection");

                // deal with rotation changes
                if(_inputHandler.KeyUp) { 
                    if(_KeyUpOld)
                    {
                        _turnStep = Mathf.Min(TURN_STEP_MULTIPLIER * _turnStep, MAX_TURN_STEP);
                    } 
                    else 
                    {
                        _turnStep = MIN_TURN_STEP;
                    }
                    _KeyUpOld = true;
                    _directionAngle = Mathf.Min(_directionAngle + _turnStep, MAX_TURN_ANGLE);
                } 
                else if(_inputHandler.KeyDown) { //if (Input.GetKey(KeyCode.LeftArrow))
                    if(_KeyDownOld)
                    {
                        _turnStep = Mathf.Min(TURN_STEP_MULTIPLIER * _turnStep, MAX_TURN_STEP);
                    } 
                    else 
                    {
                        _turnStep = MIN_TURN_STEP;
                    }
                    _KeyDownOld = true;
                    _directionAngle = Mathf.Max(_directionAngle - _turnStep, MIN_TURN_ANGLE);
                } 
                else 
                {
                    _KeyUpOld = false;
                    _KeyDownOld = false;
                }

                // deal with distance change
                if(_inputHandler.Astate) 
                {
                    if(_AstateOld)
                    {
                        _motionStep = Mathf.Min(MOTION_STEP_MULTIPLIER * _motionStep, MAX_MOTION_STEP);
                    } 
                    else
                    {
                        _motionStep = MIN_MOTION_STEP;
                    }
                    _AstateOld = true;
                    _BstateOld = false;
                    _directionDistance = Mathf.Max(_directionDistance + _motionStep, MIN_RETICLE_DISTANCE);

                } 
                else if(_inputHandler.Bstate) {
                    if(_BstateOld)
                    {
                        _motionStep = Mathf.Min(MOTION_STEP_MULTIPLIER * _motionStep, MAX_MOTION_STEP);
                    } 
                    else
                    {
                        _motionStep = MIN_MOTION_STEP;
                    }
                    _AstateOld = false;
                    _BstateOld = true;
                    _directionDistance = Mathf.Min(_directionDistance - _motionStep, MAX_RETICLE_DISTANCE);
                } else {
                    _AstateOld = false;
                    _BstateOld = false;
                }

                // rotate the camera based on the update
                if (_pitch)
                {
                    pan = 0.0f;
                    tilt = _spinDir * (_turn + _directionAngle);
                    _camera.transform.rotation = Quaternion.Euler(tilt, pan, 0.0f);
                    x = 0;
                    y = -_directionDistance * Mathf.Sin(3.1415f * tilt / 180.0f);
                    z = _directionDistance * Mathf.Cos(3.1415f * tilt / 180.0f);
                    _reticle.transform.rotation = Quaternion.Euler(tilt, pan, 0.0f);
                    _reticle.transform.position = new Vector3(x + _camera.transform.position.x, y + _camera.transform.position.y, z + _camera.transform.position.z);
                    Debug.Log($"Pitch angle {_turn} {_directionAngle}");
                } else {
                    pan = _spinDir * (_turn + _directionAngle);
                    tilt = 0.0f;
                    _camera.transform.rotation = Quaternion.Euler(tilt, pan, 0.0f);
                    x = _directionDistance * Mathf.Sin(3.1415f * pan / 180.0f);
                    y = 0;
                    z = _directionDistance * Mathf.Cos(3.1415f * pan / 180.0f);
                    _reticle.transform.rotation = Quaternion.Euler(tilt, pan, 0.0f);
                    _reticle.transform.position = new Vector3(x + _camera.transform.position.x, y + _camera.transform.position.y, z + _camera.transform.position.z);
                    Debug.Log($"Yaw angle {_turn} {_directionAngle}");
                }

                // deal with selection
                if(_inputHandler.TriggerPressed) //if (Input.GetKeyDown("x"))
                {
                    _reticle.SetActive(false);
                    _responseLog.AddTriangle(_cond, _backwardTime, _length1, 180.0f - _turn, _pitch, _spinDir, _length2, _directionDistanceInit, _directionDistance, _directionAngle);
                    _trackerLog.StopRecordingAndSave(Application.persistentDataPath + "/HeadTracking_triangle_completion_" + startTime + "_" + _cond + ".txt");

                    if (_cond < NTRIANG - 1)
                    {
                        _cond = _cond + 1;
                        _experimentState = ExperimentState.BeforeMotion;
                    }
                    else
                    {
                        _responseLog.Dump(Application.persistentDataPath + "/Responses_triangle_" + startTime + ".txt", "cond, backtime, len1, angle, pitch, spindir, len2, dirinit, dirfinal, anglefinal");
                        _camera.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
                        _camera.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                        _d.SetDialogElements("Completed", new string[] { "" });
                        _d.SetDialogInstructions("Press trigger to quit");
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
