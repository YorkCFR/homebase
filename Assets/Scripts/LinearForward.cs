using UnityEngine;
using UnityEditor;

/**
 * Do the formward linear motion task. Much of this code was lifted from the 
 * first version.
 *
 *  At the meeting of 9th of March (ony things that impact this component)
 *  - components are run in sequence
 *  - insert two practice trials
 *  - more complete headers in the output file
 *  - updated reticle
 *  - updated instruction cards
 *  - updated conditions
 *
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
 *      - click when matching distance moved
 *
 *
 * Version History
 * V3.0 - version based on March 9th revisions
 * V2.0 - version based on Feb 17th revisions
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
        Setup,
        BeforeMotion,
        WaitForAdjustTarget,
        Turn,
        Wait,
        MoveForward,        
        AdjustTarget,
        Done
    };

    private const int NLINEAR = 16;                         // number of linear conditions
    private const int NPRACTICE = 2;                        // Number of practice conditions
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
    private ResponseLog _responseLog;                      // the response log
    private HeadTrackerLog _trackerLog;
    
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
    
    private float _motionStep = MIN_MOTION_STEP;            // how big a step to make for a keypress (m)

   

     float[][] _linear_conditions = new float[NLINEAR+NPRACTICE][];   // the conditions

    private bool _AstateOld = false;
    private bool _BstateOld = false;


    public void Start()
    {
        _responseLog = new ResponseLog();
        HomeBaseDriver driver = GetComponent<HomeBaseDriver>();
        _dialog = driver.Dialog;
        _d = _dialog.GetComponent<Dialog>();
        _experimentState = ExperimentState.Initialize;
        _camera = GameObject.Find("Camera Holder");
        _reticle = driver.AdjustableTarget;
        _inputHandler = _camera.GetComponent<InputHandler>();
        ConstructConditions();
        _trackerLog = GetComponent<HeadTrackerLog> ();

        Debug.Log($"tracker {_trackerLog==null}");

    }


    private void ConstructConditions()
    {
        _linear_conditions[0] = new float[3] {3.0f, 1.0f, 1.0f};   // dist, pan/tilt, direction sign
        _linear_conditions[1] = new float[3] {3.0f, 1.0f, -1.0f};  // dist, pan/tilt, direction sign
        _linear_conditions[2] = new float[3] {3.0f, -1.0f, 1.0f};  // dist, pan/tilt, direction sign 
        _linear_conditions[3] = new float[3] {3.0f, -1.0f, -1.0f}; // dist, pan/tilt, direciont sign
        _linear_conditions[4] = new float[3] {5.0f, 1.0f, 1.0f};   // dist, pan/tilt, direction sign
        _linear_conditions[5] = new float[3] {5.0f, 1.0f, -1.0f};  // dist, pan/tilt, direction sign
        _linear_conditions[6] = new float[3] {5.0f, -1.0f, 1.0f};  // dist, pan/tilt, direction sign 
        _linear_conditions[7] = new float[3] {5.0f, -1.0f, -1.0f}; // dist, pan/tilt, direciont sign
        _linear_conditions[8] = new float[3] {7.0f, 1.0f, 1.0f};   // dist, pan/tilt, direction sign
        _linear_conditions[9] = new float[3] {7.0f, 1.0f, -1.0f};  // dist, pan/tilt, direction sign
        _linear_conditions[10] = new float[3] {7.0f, -1.0f, 1.0f};  // dist, pan/tilt, direction sign 
        _linear_conditions[11] = new float[3] {7.0f, -1.0f, -1.0f}; // dist, pan/tilt, direciont sign
        _linear_conditions[12] = new float[3] {9.0f, 1.0f, 1.0f};   // dist, pan/tilt, direction sign
        _linear_conditions[13] = new float[3] {9.0f, 1.0f, -1.0f};  // dist, pan/tilt, direction sign
        _linear_conditions[14] = new float[3] {9.0f, -1.0f, 1.0f};  // dist, pan/tilt, direction sign 
        _linear_conditions[15] = new float[3] {9.0f, -1.0f, -1.0f}; // dist, pan/tilt, direciont sign
        _linear_conditions[16] = new float[3];
        _linear_conditions[17] = new float[3];

        float[] z = new float[3];
        for(int i = 0; i < NLINEAR*10; i++)
        {
            int index1 = UnityEngine.Random.Range(0, NLINEAR);
            int index2 = UnityEngine.Random.Range(0, NLINEAR);
            for(int j=0;j<3;j++) 
                z[j] = _linear_conditions[index1][j];
            for(int j=0;j<3;j++) 
                _linear_conditions[index1][j] = _linear_conditions[index2][j];
            for(int j=0;j<3;j++)
                _linear_conditions[index2][j] = z[j];
        }

        float[] p1 = new float[3] {6.0f, -1.0f, -1.0f};   // dist, pan/tilt, direction sign
        float[] p2 = new float[3] {8.0f, 1.0f, 1.0f};   // dist, pan/tilt, direction sign

        // slide the real conditions back and insert the practice conditions
        for(int i=(NLINEAR-1); i >= 0; i--)
        {
            for(int j=0;j<3;j++)
                _linear_conditions[i+NPRACTICE][j] = _linear_conditions[i][j];
        }

        for(int i=0; i<3; i++)
        {
            _linear_conditions[0][i] = p1[i];
            _linear_conditions[1][i] = p2[i];
        }

        for(int i = 0; i < NLINEAR + NPRACTICE; i++)
        {
            for(int j=0;j<3;j++)
                Debug.Log(_linear_conditions[i][j]);
        }
    }

    public bool DoAdjustLinearTarget(long startTime, SphereField sf)
    {
        float motion, angle, x, y, z, pan, tilt;

        Debug.Log("DoAdjustLienarTarget " + _experimentState + " condition " + _cond);
        switch (_experimentState)
        {
            case ExperimentState.Initialize:

                _d.SetBackground(instructionMaterial); 
                _d.SetDialogElements("", new string[] { "" });
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

                if (_cond < (NLINEAR+NPRACTICE))
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
                    _d.SetDialogElements("Forward Linear Motion", new string[] { "Condition " + (1+_cond) + "/" + (NLINEAR+NPRACTICE) });
                    _d.SetDialogInstructions("Press trigger to start");
                    _dialog.SetActive(true);
                    _trackerLog.StartRecording();
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
                angle = ROTATE_VEL * (Time.time - _turnStart);
                if (angle >= ROTATION) // rotation is finished
                {
                    angle = ROTATION;
                    _waitStart = Time.time;
                    _experimentState = ExperimentState.Wait;
                    Debug.Log("We haev turned " + ROTATION);
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
                if((Time.time - _waitStart) > WAIT_TIME)
                {
                    _experimentState = ExperimentState.MoveForward;
                    _motionStart = Time.time;
                    Debug.Log("we have waited long enough");
                }
                break;
            case ExperimentState.MoveForward: // moving in pointing direction

                // move the camera
                motion = LINEAR_VEL * (Time.time - _motionStart);
                Debug.Log("Moving forward " + motion + " " + _distance);

                if(motion >= _distance)
                    motion = _distance;

                // update camera position
                if (_pitch)
                {
                    x = 0;
                    y = -motion * Mathf.Sin(3.1415f * _spinDir * ROTATION  / 180.0f);
                    z = motion * Mathf.Cos(3.1415f * _spinDir * ROTATION  / 180.0f);
                }
                else
                {
                    x = motion * Mathf.Sin(3.1415f * _spinDir * ROTATION  / 180.0f);
                    y = 0;
                    z = motion * Mathf.Cos(3.1415f * _spinDir * ROTATION  / 180.0f);
                }
                _camera.transform.position = new Vector3(x, y, z);

                // got to where we want to go
                if (motion >= _distance) // got to the stimulus distance 
                {
                    motion = _distance;
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
                    if(_AstateOld)
                    {
                        _motionStep = Mathf.Min(MOTION_STEP_MULTIPLIER * _motionStep, MAX_MOTION_STEP);
                    } else
                    {
                        _motionStep = MIN_MOTION_STEP;
                    }
                    _AstateOld = true;
                    _BstateOld = false;
                    _targetDistance = Mathf.Max(_targetDistance - _motionStep, TARGET_MIN);
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
                    _targetDistance = Mathf.Min(_targetDistance + _motionStep, TARGET_MAX);
                } 
                else
                {
                    _AstateOld = false;
                    _BstateOld = false;
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
                    Debug.Log("Trigger pressed");
                    _responseLog.AddForward(_cond, _turnStart, _distance, ROTATION, _pitch, _spinDir, _targetDistanceInit, _targetDistance); 
                    Debug.Log("response logged");
                    Debug.Log(Application.persistentDataPath);
                    Debug.Log($"tracker {_trackerLog==null}");
                    _trackerLog.StopRecordingAndSave(Application.persistentDataPath + "/HeadTracking_linear_formward_" + startTime + "_" + _cond + ".txt");
                    Debug.Log("tracker logged");
                    _reticle.SetActive(false);
                    Debug.Log("reticle off");

                    _camera.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
                    _camera.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);

                    if(_cond < (NLINEAR + NPRACTICE - 1))
                    {
                        _cond = _cond + 1;
                        _experimentState = ExperimentState.BeforeMotion;
                        Debug.Log($"going back for next condition {_cond}");
                    } else {
                        Debug.Log("saving data");
                        _responseLog.Dump(Application.persistentDataPath + "/Responses_linear_forward_" + startTime + ".txt",
                        "cond, starttime, targetd,  rotation, pitch, spinDir, inittarget, finaltarget, cam pos x, cam pos y, cam pos z, cam rot x, cam rot y, cam rot z, cam rot w, reticle pos x, reticle pos y, reticle pos z, reticle rot x, reticle rot y, reticle rot z, reticle rot w");
                        Debug.Log($"Output is in {Application.persistentDataPath}");
                        _d.SetDialogElements("Completed", new string[] { "" });
                        _d.SetDialogInstructions("Press trigger to quit");
                        _dialog.SetActive(true);
                        _experimentState = ExperimentState.Done;
                    }

                }
                break;
            case ExperimentState.Done:
                if (_inputHandler.TriggerPressed) {
                    _dialog.SetActive(false);
                    return true;
                }

                break;
        }
        return false;
    }
}
