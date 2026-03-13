using UnityEngine;
using UnityEngine.Video;
using UnityEditor;
using System;
using UnityEngine.InputSystem;


/**
 * Create the basic experimental environment.
 * 
 * Copyright Michael Jenkin 2025, 2026
 * Version History
 * 
 * V3.0 - updates after the 9th of March
 * V2.0 - Updates after the 17th of Feb
 * V1.8 - ensure output even if no user input (?)
 * V1.7 - updated triangle completion task
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
    public GameObject MovieScreen;

    private const int NSPHERES = 3600;                     // number of spheres (12,000)
    private const float FLICKER_PROB = 0.001f;             // flicker probability (0.0001 originally)

    private enum AllControlState
    {
        Backward,
        Rotate,
        Forward
    };

    private AllControlState _allControlState;

    TopLevelMenu topLevelMenu;
    private bool _doingMenu = true;
    private Enums.Experiment _whichExperiment;
    private SphereField _sf = null;
    private long _startTime;

    private bool _moviePlaying = false;


    
    void Start()
    {
        _startTime = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
        topLevelMenu = GetComponent<TopLevelMenu>();


        _sf = new SphereField(NSPHERES); // we regenerate locations as needed
        _sf.EnableHomeBaseDisplay();
        _doingMenu = true;
        Dialog.SetActive(true);
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
        //Debug.Log($"Deal with Menu {exp}");
        switch(exp)
        {
            case Enums.Experiment.ControlForward:
                _whichExperiment = Enums.Experiment.ControlForward;
                _doingMenu = false;
                 _startTime = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
                break;
            case Enums.Experiment.ControlBackward:
                _whichExperiment = Enums.Experiment.ControlBackward;
                _doingMenu = false;
                 _startTime = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
                break;
            case Enums.Experiment.ControlRotation:
                _whichExperiment = Enums.Experiment.ControlRotation;
                _doingMenu = false;
                 _startTime = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
                break;
            case Enums.Experiment.TriangleCompletion:
                _whichExperiment = Enums.Experiment.TriangleCompletion;
                _doingMenu = false;
                _startTime = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
                 break;
            case Enums.Experiment.Tutorial:
                _doingMenu = false;
                _startTime = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
                _whichExperiment = Enums.Experiment.Tutorial;
                break;
            case Enums.Experiment.ControlAll:
                _doingMenu = false;
                _startTime = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
                _whichExperiment = Enums.Experiment.ControlAll;
                _allControlState = AllControlState.Backward;
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
        LinearForward linearForward = GetComponent<LinearForward>();
        LinearBackward linearBackward = GetComponent<LinearBackward>();
        RotationControl rotationControl = GetComponent<RotationControl>();
        TriangleCompletion triangleCompletion = GetComponent<TriangleCompletion>();

        _sf.FLickerDisplay(FLICKER_PROB);

        if(_doingMenu) {
            DealWithMenu();
        } 
        else
        {
            switch (_whichExperiment)
            {
                case Enums.Experiment.ControlForward:
                    
                    if(linearForward.DoAdjustLinearTarget(_startTime, _sf)) {
                        linearForward.Restart();
                        topLevelMenu.Reset();
                        _doingMenu = true;
                    }
                    break;
                case Enums.Experiment.ControlBackward:
                    if(linearBackward.DoAdjustLinearTargetBackward(_startTime, _sf)) {
                        linearBackward.Restart();
                        topLevelMenu.Reset();
                        _doingMenu = true;
                    }
                    break;
                case Enums.Experiment.ControlRotation:
                    if(rotationControl.DoRotationControl(_startTime, _sf)){
                        rotationControl.Restart();
                        topLevelMenu.Reset();
                        _doingMenu = true;
                    }
                    break;
                case Enums.Experiment.TriangleCompletion:
                    Debug.Log("Doing triangle completion");
                    if(triangleCompletion.DoTriangleCompletion(_startTime, _sf)){
                        triangleCompletion.Restart();
                        topLevelMenu.Reset();
                        _doingMenu = true;
                    }
                    break;
                case Enums.Experiment.ControlAll:
                    Debug.Log($"Doing ControlALl {_allControlState}");
                    switch (_allControlState)
                    {
                        case AllControlState.Backward:
                            Debug.Log("doing backward");
                            if(linearBackward.DoAdjustLinearTargetBackward(_startTime, _sf)) {
                                linearBackward.Restart();
                                _allControlState = AllControlState.Rotate;
                                Debug.Log("Finished, off to rotate");
                            }
                            break;
                        case AllControlState.Rotate:
                            Debug.Log("Doing rotate");
                            if(rotationControl.DoRotationControl(_startTime, _sf)){
                                rotationControl.Restart();
                                _allControlState = AllControlState.Forward;
                            }
                            break;
                        case AllControlState.Forward:
                            Debug.Log("Doing forward");
                            if(linearForward.DoAdjustLinearTarget(_startTime, _sf)) {
                                linearForward.Restart();
                                topLevelMenu.Reset();
                                _doingMenu = true;
                            }
                            break;
                        default:
                            Debug.Log("No one to talk to me");
                            break;
                    }
                    break;
                case Enums.Experiment.Tutorial:
                    VideoPlayer video = MovieScreen.GetComponent<VideoPlayer>();
                    if(!_moviePlaying)
                    {
                        MovieScreen.SetActive(true);
                        video.Stop();
                        video.frame = 0;
                        video.Play();
                        _moviePlaying = true;
                    } else
                    {
                        if((video.frame > 0) && !video.isPlaying)
                        {
                            MovieScreen.SetActive(false);
                            topLevelMenu.Reset();
                            _doingMenu = true;
                            _moviePlaying = false;
                        }
                    }
                    break;
                default:
                    Debug.Log("EH?");
                    break;
            }
        }
    }
}
