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

    TopLevelMenu topLevelMenu;

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
        GetGameObjects();
        Debug.Log("Getting toplevelmenu");
        topLevelMenu = GetComponent<TopLevelMenu>();
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
        TopLevelMenu.Experiment exp = topLevelMenu.DealWithMenu();
        switch(exp)
        {
            case TopLevelMenu.Experiment.ControlForward:
                Debug.Log("*** do Control Forward");
                break;
            case TopLevelMenu.Experiment.ControlBackward:
                Debug.Log("*** do Control Forward");
                break;
            case TopLevelMenu.Experiment.ControlRotation:
                break;
            case TopLevelMenu.Experiment.TriangleCompletion:
                break;
            case TopLevelMenu.Experiment.Waiting:
                break;
            case TopLevelMenu.Experiment.Quit:
                QuitPlaying();
                break;
        }

        
        Debug.Log("Menu state is " + exp);
    }

    // Update is called once per frame
    void Update()
    {
        DealWithMenu();
    }
}
