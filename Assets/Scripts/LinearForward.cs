using UnityEngine;
using UnityEditor;

/**
 * Do the formward linear motion task. Much of this code was lifted from the 
 * first version.
 *
 * At the meeting of the 17th we decided on the following
 *
 * liner motion forward
 *      - press to start
 *      - rotate in yaw or pitch by CURRENTLY 45 degrees (mean of others)
 *      - 200ms delay
 *      - translate in direction facing in 1.4m/s to 4- 12m in 2m increments
 *      - stop
 *      - pointer appears, dist from you is chosen uniformly between 4m-12m (record pos of pointer chosen)
 *      - dist is constrained to 0.5m and 32m
 *      - move to the dist you moved
 *      - click when happy
 *
 *
 * Version History
 * V1.0 - lifted from the original monolithic version prior to refactoring.
 *
 * Michael Jenkin, 2026
 **/

public class LinearForward : MonoBehaviour
{
    public Material defaultDialogMaterial;
    public Material instructionMaterial;

    private enum ExperimentState
    {
        Initialize, 
        Instructions,
        Setup,
        BeforeMotion,
        WaitForAdjustTarget,
        Turn,
        Wait,
        MoveForward,        
        AdjustTarget,
        Done
    };

    private const int NLINEAR = 20;                         // number of linear conditions
    private const float WAIT_TIME = 0.2f;                   // Wait time in sec
    private const float ROTATE_VEL = 30.0f;                 // rotational velocity in deg/sec
    private const float ROTATION = 45.0f;                   // how much to turn by
    private const float LINEAR_VEL = 1.4f;                  // linear velocity in m/s
    private const float INIT_TARGET_MIN = 4.0f;             // minimum target initial position
    private const float INIT_TARGET_MAX = 12.0f;            // maximum target initial position
    private const float TARGET_MIN = 0.5f;                  // mininmum target range
    private const float TARGET_MAX = 32.0f;                 // maximum target range
    private const float MIN_MOTION_STEP = 0.01f;            // motion step minimum
    private const float MAX_MOTION_STEP = 0.5f;            // motion step maximum
    private const float MOTION_STEP_MULTIPLIER = 1.1f;      // motion step multiplier
 

    private Dialog _d;                                      // Dialog interface
    private GameObject _dialog;                             // Dialog Gameobject
    private GameObject _camera;
    private GameObject _reticle;
    private InputHandler _inputHandler;
    private ExperimentState _experimentState = ExperimentState.Initialize;    // current state of the experiment
    private ResponseLog _responseLog = new ResponseLog();   // the response log
    
    private int _cond;                                      // current condition number (starts from 0)
    private float _spinDir = 1.0f;                          // spin direction
    private bool _pitch;                                    // is this a pitch or yaw
    private bool _turnRight;                                // is this trial coded as to the left or right?
    private float _distance;                                // how far we want to move
    private float _targetDistance;                          // current target distance 
    private float _targetDistanceInit;                      // initial target distance
  

    private float _turnStart;                               // time when turn starts
    private float _waitStart;                               // time when waiting period starts
    private float _motionStart;                             // time when motion starts
    private float _angle;                                   // how much we have turned in deg
    private float _motion;                                  // how far we have moved in m
    private float _motionStep = MIN_MOTION_STEP;            // how big a step to make for a keypress (m)

   

     float[][] _linear_conditions = new float[NLINEAR][];   // the conditions


    public void Start()
    {
        Debug.Log("Linear forward started");
        HomeBaseDriver driver = GetComponent<HomeBaseDriver>();
        _dialog = driver.Dialog;
        _d = _dialog.GetComponent<Dialog>();
        _experimentState = ExperimentState.Initialize;
        _camera = GameObject.Find("Camera Holder");
        _reticle = driver.AdjustableTarget;
        _inputHandler = _camera.GetComponent<InputHandler>();
        ConstructConditions();
        Debug.Log("Linear forward start completed");
    }


    private void ConstructConditions()
    {
        int i = 0;
        for(int dist=4; dist<=12; dist+=2) {
            float[] l1 = new float[3] {(float)dist, 1.0f, 1.0f};   // dist, pan/tilt, direction sign
            float[] l2 = new float[3] {(float)dist, 1.0f, -1.0f};  // dist, pan/tilt, direction sign
            float[] l3 = new float[3] {(float)dist, -1.0f, 1.0f};  // dist, pan/tilt, direction sign 
            float[] l4 = new float[3] {(float)dist, -1.0f, -1.0f}; // dist, pan/tilt, direciont sign
            _linear_conditions[i++] = l1;
            _linear_conditions[i++] = l2;
            _linear_conditions[i++] = l3;
            _linear_conditions[i++] = l4;
        }
        for(i = 0; i < NLINEAR*10; i++)
        {
            int index1 = UnityEngine.Random.Range(0, NLINEAR);
            int index2 = UnityEngine.Random.Range(0, NLINEAR);
            float[] z = _linear_conditions[index1];
            _linear_conditions[index1] = _linear_conditions[index2];
            _linear_conditions[index2] = z;
        }
    }

    public void DoAdjustLinearTarget(long startTime, SphereField sf)
    {
        float _motion, x, y, z, pan, tilt;
        Renderer r;

        Debug.Log("DoAdjustLienarTarget " + _experimentState);
        switch (_experimentState)
        {
            case ExperimentState.Initialize:

                _d.SetBackground(instructionMaterial); 
                _d.SetDialogElements("Forward Linear Motion", new string[] { "" });
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

                if (_cond < NLINEAR)
                {
                    _distance = _linear_conditions[_cond][0];
                    _pitch = _linear_conditions[_cond][1] > 0;
                    _turnRight = _linear_conditions[_cond][2] > 0;

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

                    _experimentState = ExperimentState.WaitForAdjustTarget;
                    _camera.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
                    _camera.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                    _d.SetDialogElements("Forward Linear Motion", new string[] { "Condition " + (1+_cond) + "/" + NLINEAR });
                    _d.SetDialogInstructions("Press trigger to start");
                    _dialog.SetActive(true);
                }
                break;
            case ExperimentState.WaitForAdjustTarget:
                if(_inputHandler.TriggerPressed) 
                {
                    _dialog.SetActive(false);
                    _turnStart = Time.time;
                    _experimentState = ExperimentState.Turn;
                }
                break;
            case ExperimentState.Turn:
                _angle = ROTATE_VEL * (Time.time - _turnStart);
                if (_angle >= ROTATION) // rotation is finished
                {
                    _angle = ROTATION;
                    _waitStart = Time.time;
                    _experimentState = ExperimentState.Wait;
                    Debug.Log("We haev turned " + ROTATION);
                }

                // execute the turn
                if (_pitch)
                {
                    pan = 0.0f;
                    tilt = _spinDir * _angle;
                    _camera.transform.rotation = Quaternion.Euler(tilt, pan, 0.0f);
                }
                else
                {
                    pan = _spinDir * _angle;
                    tilt = 0.0f;
                    _camera.transform.rotation = Quaternion.Euler(tilt, pan, 0.0f);
                }
                break;
            case ExperimentState.Wait:
            Debug.Log("*** WAITING ***");
                if((Time.time - _waitStart) > WAIT_TIME)
                {
                    _experimentState = ExperimentState.MoveForward;
                    _motionStart = Time.time;
                    Debug.Log("we have waited long enough");
                }
                break;
            case ExperimentState.MoveForward: // moving in pointing direction

                // move the camera
                _motion = LINEAR_VEL * (Time.time - _motionStart);
                Debug.Log("Moving forward " + _motion + " " + _distance);

                if(_motion >= _distance)
                    _motion = _distance;

                // update camera position
                if (_pitch)
                {
                    x = 0;
                    y = -_motion * Mathf.Sin(3.1415f * _spinDir * ROTATION  / 180.0f);
                    z = _motion * Mathf.Cos(3.1415f * _spinDir * ROTATION  / 180.0f);
                }
                else
                {
                    x = _motion * Mathf.Sin(3.1415f * _spinDir * ROTATION  / 180.0f);
                    y = 0;
                    z = _motion * Mathf.Cos(3.1415f * _spinDir * ROTATION  / 180.0f);
                }
                _camera.transform.position = new Vector3(x, y, z);

                // got to where we want to go
                if (_motion >= _distance) // got to the stimulus distance 
                {
                    _motion = _distance;
                    _experimentState = ExperimentState.AdjustTarget;
                    _targetDistanceInit = UnityEngine.Random.Range(INIT_TARGET_MIN, INIT_TARGET_MAX);
                    _targetDistance = _targetDistanceInit;
                    if (_pitch)
                    {
                        x = 0;
                        y = -_targetDistance * Mathf.Sin(3.1415f * _spinDir * ROTATION / 180.0f);
                        z = _targetDistance * Mathf.Cos(3.1415f * _spinDir * ROTATION / 180.0f);
                        _reticle.transform.rotation = Quaternion.Euler(_spinDir * ROTATION, 0.0f, 0.0f);
                        _reticle.transform.position = new Vector3(x + _camera.transform.position.x, y + _camera.transform.position.y, z + _camera.transform.position.z);
                    } else {
                        x = _targetDistance * Mathf.Sin(3.1415f * _spinDir * ROTATION / 180.0f);
                        y = 0;
                        z = _targetDistance * Mathf.Cos(3.1415f * _spinDir * ROTATION / 180.0f);
                        _reticle.transform.rotation = Quaternion.Euler(0.0f, _spinDir * ROTATION, 0.0f);
                        _reticle.transform.position = new Vector3(x + _camera.transform.position.x, y + _camera.transform.position.y, z + _camera.transform.position.z);
                    }

                    _reticle.SetActive(true);
                }
                break;
            case ExperimentState.AdjustTarget: // adjust target task (A/B to move, Trigger to select
                if(_inputHandler.Astate) 
                {
                    if(_inputHandler.AstateOld)
                    {
                        _motionStep = Mathf.Min(MOTION_STEP_MULTIPLIER * _motionStep, MAX_MOTION_STEP);
                    } else
                    {
                        _motionStep = MIN_MOTION_STEP;
                    }
                    _inputHandler.AstateOld = true;
                    _inputHandler.BstateOld = false;
                    _targetDistance = Mathf.Min(_targetDistance + _motionStep, TARGET_MAX);
                }
                else if(_inputHandler.Bstate) 
                {
                    if(_inputHandler.BstateOld)
                    {
                        _motionStep = Mathf.Min(MOTION_STEP_MULTIPLIER * _motionStep, MAX_MOTION_STEP);
                    } else
                    {
                        _motionStep = MIN_MOTION_STEP;
                    }
                    _inputHandler.AstateOld = false;
                    _inputHandler.BstateOld = true;
                    _targetDistance = Mathf.Max(_targetDistance - _motionStep, TARGET_MIN);
                } 
                else
                {
                    _inputHandler.AstateOld = false;
                    _inputHandler.BstateOld = false;
                }

                if (_pitch)
                {
                    x = 0;
                    y = -_targetDistance * Mathf.Sin(3.1415f * _spinDir * ROTATION / 180.0f);
                    z = _targetDistance * Mathf.Cos(3.1415f * _spinDir * ROTATION / 180.0f);
                    _reticle.transform.rotation = Quaternion.Euler(_spinDir * ROTATION, 0.0f, 0.0f);
                    _reticle.transform.position = new Vector3(x + _camera.transform.position.x, y + _camera.transform.position.y, z + _camera.transform.position.z);
                } else {
                    x = _targetDistance * Mathf.Sin(3.1415f * _spinDir * ROTATION / 180.0f);
                    y = 0;
                    z = _targetDistance * Mathf.Cos(3.1415f * _spinDir * ROTATION / 180.0f);
                    _reticle.transform.rotation = Quaternion.Euler(0.0f, _spinDir * ROTATION, 0.0f);
                    _reticle.transform.position = new Vector3(x + _camera.transform.position.x, y + _camera.transform.position.y, z + _camera.transform.position.z);
                }

                if(_inputHandler.TriggerPressed) 
                {
                    _responseLog.Add(_cond, _turnStart, _distance, ROTATION, _pitch, _spinDir, _targetDistanceInit, _targetDistance); 
                    _reticle.SetActive(false);

                    _camera.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
                    _camera.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

                    if(_cond < NLINEAR - 1)
                    {
                        _cond = _cond + 1;
                        _experimentState = ExperimentState.BeforeMotion;
                    } else {
                        _responseLog.Dump(Application.persistentDataPath + "/Responses_linear_forward_" + startTime + ".txt", "cond, starttime, motion, rotation, pitch, spindir, inittarget, finaltarget");
                        Debug.Log($"Output is in {Application.persistentDataPath}");
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
