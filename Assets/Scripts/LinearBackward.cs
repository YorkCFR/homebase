using UnityEngine;
using UnityEditor;

/**
 * Do the formward linear motion task. Much of this code was lifted from the 
 * first version.
 *
 * At the meeting of the 17th we decided on the following
 *
 * lliner motion backup
 *  - start with target in front of you by 5cm
 *  - press to start
 *  - moved 8m dist at 1.4m/s 
 *  - delay 200ms
 *  - target is extinguished
 *  - rot 180 degrees at 30 degrees/s (4 directions)
 *  - 200ms delay
 *  - pointer appears, dist from you is chosen uniformly between 4m-12m (record pos of pointer chosen)
 *  - dist is constrained to 0.5m and 32m
 *  - match target distance to the dist you moved
 *  - click when happy
 *
 *
 * Version History
 *      V1.0 - With lots of code stolen in the refactoring process
 *
 * Michael Jenkin, 2026
 **/

public class LinearBackward : MonoBehaviour
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
        MovingBackward,
        WaitToRotate,
        Turn,
        Wait,    
        AdjustTarget,
        Done
    };

    private const int NLINEAR = 4;                         // number of linear conditions
    private const float WAIT_TIME = 0.2f;                   // Wait time in sec
    private const float ROTATE_VEL = 30.0f;                 // rotational velocity in deg/sec
    private const float ROTATION = 180.0f;                  // how much to turn by
    private const float LINEAR_VEL = 1.4f;                  // linear velocity in m/s
    private const float INIT_TARGET_MIN = 4.0f;             // minimum target initial position
    private const float INIT_TARGET_MAX = 12.0f;            // maximum target initial position
    private const float TARGET_MIN = 0.5f;                  // mininmum target range
    private const float TARGET_MAX = 32.0f;                 // maximum target range
    private const float MIN_MOTION_STEP = 0.01f;            // motion step minimum
    private const float MAX_MOTION_STEP = 0.5f;            // motion step maximum
    private const float MOTION_STEP_MULTIPLIER = 1.1f;      // motion step multiplier
    private const float TARGET_DISTANCE = 8.0f;             // only one target distance
    private const float TARGET_START_DIST = 0.05f;          // target starts this far from you

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
    private bool _pitch;                                    // is this a pitch or yaw
    private bool _turnRight;                                // is this trial coded as to the left or right?
    private float _distance;                                // how far we want to move
    private float _targetDistance;                          // current target distance 
    private float _targetDistanceInit;                      // initial target distance

    float[][] _linear_conditions = new float[NLINEAR][];   // the conditions

    private float _waitStart, _waitStart2, _waitStart3, _backwardTime, _turnStart, _motionStart;
    private float _motionStep = MIN_MOTION_STEP;            // how big a step to make for a keypress (m)

    private bool _AstateOld = false;
    private bool _BstateOld = false;
  
    

    public void Start()
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

        float[] l1 = new float[3] {TARGET_DISTANCE, 1.0f, 1.0f};   // dist, pan/tilt, direction sign
        float[] l2 = new float[3] {TARGET_DISTANCE, 1.0f, -1.0f};  // dist, pan/tilt, direction sign
        float[] l3 = new float[3] {TARGET_DISTANCE, -1.0f, 1.0f};  // dist, pan/tilt, direction sign 
        float[] l4 = new float[3] {TARGET_DISTANCE, -1.0f, -1.0f}; // dist, pan/tilt, direciont sign
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
    }


    public void DoAdjustLinearTargetBackward(long startTime, SphereField sf)
    {
        float angle, dist, pan, tilt;

        Debug.Log("DoAdjustLienarTarget " + _experimentState);
        switch (_experimentState)
        {
            case ExperimentState.Initialize:
                _d.SetBackground(instructionMaterial); 
                _d.SetDialogElements("Backward Linear Motion", new string[] { "" });
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
            case ExperimentState.BeforeMotion: // waiting before condition
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

                    _experimentState = ExperimentState.WaitForBackwardTarget;
                    _camera.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
                    _camera.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                    _d.SetDialogElements("Backward Linear Motion", new string[] { "Condition " + (1+_cond) + "/" + NLINEAR });
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
                if(dist >= _distance)
                {
                    dist = TARGET_DISTANCE;
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
                if (angle >= ROTATION) // rotation is finished
                {
                    angle = ROTATION;
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
                if((Time.time - _waitStart3) >= WAIT_TIME)
                {
                    _experimentState = ExperimentState.AdjustTarget;
                    _targetDistanceInit = UnityEngine.Random.Range(INIT_TARGET_MIN, INIT_TARGET_MAX);
                    _targetDistance = _targetDistanceInit;
                    _reticle.transform.rotation = Quaternion.Euler(180.0f, 0.0f, 0.0f); // all answers make this the right rotation!
                    _reticle.transform.position = new Vector3(0.0f, 0.0f, -_distance - _targetDistanceInit);
                    _reticle.SetActive(true);
                }
                break;
            case ExperimentState.AdjustTarget: // adjust target task (A/B to move, Trigger to select
                if(_inputHandler.Astate) 
                {
                    if(_AstateOld)
                    {
                        _motionStep = Mathf.Min(MOTION_STEP_MULTIPLIER * _motionStep, MAX_MOTION_STEP);
                    } else
                    {
                        _motionStep = MIN_MOTION_STEP;
                    }
                    _AstateOld = true;
                    _BstateOld = false;
                    _targetDistance = Mathf.Min(_targetDistance - _motionStep, TARGET_MAX);
                }
                else if(_inputHandler.Bstate) 
                {
                    if(_BstateOld)
                    {
                        _motionStep = Mathf.Min(MOTION_STEP_MULTIPLIER * _motionStep, MAX_MOTION_STEP);
                    } else
                    {
                        _motionStep = MIN_MOTION_STEP;
                    }
                    _AstateOld = false;
                    _BstateOld = true;
                    _targetDistance = Mathf.Max(_targetDistance + _motionStep, TARGET_MIN);
                } 
                else
                {
                    _AstateOld = false;
                    _BstateOld = false;
                }

   
                _reticle.transform.position = new Vector3(0.0f, 0.0f, -_distance - _targetDistance);

                if(_inputHandler.TriggerPressed) 
                {
                    _responseLog.AddBackward(_cond, _waitStart, _distance, _pitch, _spinDir, _targetDistanceInit, _targetDistance); 
                    _trackerLog.StopRecordingAndSave(Application.persistentDataPath + "/HeadTracking_linear_backward_" + startTime + "_" + _cond + ".txt");
                    _reticle.SetActive(false);

                    _camera.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
                    _camera.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
                    _reticle.SetActive(false);

                    if(_cond < NLINEAR - 1)
                    {
                        _cond = _cond + 1;
                        _experimentState = ExperimentState.BeforeMotion;
                    } else {
                        _responseLog.Dump(Application.persistentDataPath + "/Responses_linear_backward_" + startTime + ".txt", "cond, starttime, motion, rotation, pitch, spindir, inittarget, finaltarget");
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
