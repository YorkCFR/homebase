using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

/**
 * Log head and CameraHolder state
 *
 * Version Log
 *    V1.0 - initial version.
 * Copyright Michael Jenkin, 2026
 **/

public class HeadTrackerLog : MonoBehaviour
{
    private GameObject _camera, _camera_holder;
    private List<string> _log;
    private bool _logging = false;

    void Start()
    {
        _camera_holder = GameObject.Find("Camera Holder");
        _camera = GameObject.Find("Main Camera");
    }

    public void StartRecording()
    {
        _log = new List<string>();
        _logging = true;
    }

    public void StopRecordingAndSave(string fileName)
    {
        Debug.Log("Writing to " + fileName);
        try
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                foreach (string p in _log)
                {
                    sw.WriteLine(p);
                }
                sw.Close();
            }
        }
        catch (Exception e)
        {
            Debug.Log("There was an exception " + e);
        }
        _logging = false;
        _log = new List<string>();
    }
    // Update is called once per frame
    void Update()
    {
        if(_logging) {
            string s = $"{Time.time}, {_camera_holder.transform.position.x}, {_camera_holder.transform.position.y}, {_camera_holder.transform.position.z}, " +
                                  $"{_camera_holder.transform.rotation.x}, {_camera_holder.transform.rotation.y}, {_camera_holder.transform.rotation.z}, {_camera_holder.transform.rotation.w}, " +
                                  $"{_camera.transform.position.x}, {_camera.transform.position.y}, {_camera.transform.position.z}, " +
                                  $"{_camera.transform.rotation.x}, {_camera.transform.rotation.y}, {_camera.transform.rotation.z}, {_camera.transform.rotation.w}";
            _log.Add(s);
        }
    }
}
