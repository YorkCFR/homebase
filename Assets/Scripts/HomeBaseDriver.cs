using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.InputSystem;


/**
 * Create the basic experimental environment.
 * 
 * Copyright Michael Jenkin 2025, 2026
 * Version History
 * 
 * V2.5 - Updates after the 17th of Feb
 * V2.1 - ensure output even if no user input (?)
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

public class HomeBaseDriver : MonoBehaviour
{

    public GameObject AdjustableTarget;
    public GameObject FixedTarget;
    public GameObject CameraHolder;
    public GameObject HomeBase;
    public GameObject Dialog;

    private const int NSPHERES = 2400;                     // number of spheres (12,000)

    TopLevelMenu topLevelMenu;
    private bool _doingMenu = true;
    private Enums.Experiment _whichExperiment;
    private SphereField _sf = null;
    private long _startTime;

    /**
     * All of the non-scene texture objects are initially active (so they are easy to find). 
     * Get links to them, then make them inactive (except for the camera :-)
     **/
    private void GetGameObjects()
    {
        /*
        AdjustTarget = GameObject.Find("Target");
        _adjustTarget.SetActive(false);

        _reticle = GameObject.Find("Reticle");
        _reticle.SetActive(false);

        _camera = GameObject.Find("Camera Holder");
*/
 
        Dialog.SetActive(false);

        //_home = GameObject.Find("Homebase");
        //_home.SetActive(false);
    }
    
    void Start()
    {
        _startTime = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
        GetGameObjects();
        topLevelMenu = GetComponent<TopLevelMenu>();


        _sf = new SphereField(NSPHERES); // we regenerate locations as needed
        _sf.EnableHomeBaseDisplay();
        _doingMenu = true;
        //_whichExperiment = TopLevelMenu.Experiment.None;
    }

    /**
     * Quit the application
     **/
    private void QuitPlaying()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void DealWithMenu()
    {
        Enums.Experiment exp = topLevelMenu.DealWithMenu();
        switch(exp)
        {
            case Enums.Experiment.ControlForward:
                _whichExperiment = Enums.Experiment.ControlForward;
                _doingMenu = false;
                break;
            case Enums.Experiment.ControlBackward:
                _whichExperiment = Enums.Experiment.ControlBackward;
                _doingMenu = false;
                break;
            case Enums.Experiment.ControlRotation:
                _whichExperiment = Enums.Experiment.ControlRotation;
                _doingMenu = false;
                break;
            case Enums.Experiment.TriangleCompletion:
                _whichExperiment = Enums.Experiment.TriangleCompletion;
                _doingMenu = false;
                break;
            case Enums.Experiment.Waiting:
                break;
            case Enums.Experiment.Quit:
                QuitPlaying();
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {

        _sf.FLickerDisplay(0.0001f);

        if(_doingMenu) {
            DealWithMenu();
        } else
        {
            switch (_whichExperiment)
            {
                case Enums.Experiment.ControlForward:
                    LinearForward linearForward = GetComponent<LinearForward>();
                    linearForward.DoAdjustLinearTarget(_startTime, _sf);
                    break;
                case Enums.Experiment.ControlBackward:
                    LinearBackward linearBackward = GetComponent<LinearBackward>();
                    linearBackward.DoAdjustLinearTargetBackward(_startTime, _sf);
                    break;
                case Enums.Experiment.ControlRotation:
                    RotationControl rotationControl = GetComponent<RotationControl>();
                    rotationControl.DoRotationControl(_startTime, _sf);
                    break;
                case Enums.Experiment.TriangleCompletion:
                    TriangleCompletion triangleCompletion = GetComponent<TriangleCompletion>();
                    triangleCompletion.DoTriangleCompletion(_startTime, _sf);
                    break;
                default:
                    Debug.Log("EH?");
                    break;
            }
        }
    }
}
