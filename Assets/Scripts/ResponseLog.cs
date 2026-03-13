using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

/**
 * Record and then dump out a set of responses
 *
 * Version History
 * V3.0 - version based on March 9th revisions
 * V2.0 - version based on Feb 17th revisions
 * V1.0 - lifted from the original monolithic version prior to refactoring.
 *
 * Copyright (c) Michael Jenkin, 2025, 2026
 **/


public class ResponseLog
{

    private GameObject _reticle, _camera_holder;

    private List<string> _log;


    public ResponseLog()
    {
        _log = new List<string>();
        _camera_holder = GameObject.Find("Camera Holder");
        HomeBaseDriver driver = _camera_holder.GetComponent<HomeBaseDriver>();
        _reticle = driver.AdjustableTarget;
    }


    public void Add(string s)
    {
        s = s + $", {_camera_holder.transform.position.x}, {_camera_holder.transform.position.y}, {_camera_holder.transform.position.z}, " +
                    $"{_camera_holder.transform.rotation.x}, {_camera_holder.transform.rotation.y}, {_camera_holder.transform.rotation.z}, {_camera_holder.transform.rotation.w}, " +
                    $"{_reticle.transform.position.x}, {_reticle.transform.position.y}, {_reticle.transform.position.z}, " +
                    $"{_reticle.transform.rotation.x}, {_reticle.transform.rotation.y}, {_reticle.transform.rotation.z}, {_reticle.transform.rotation.w}";
        _log.Add(s);
    }

    public void AddTriangle(int cond, float backStart, float l1, float turn, bool pitch, float spinDir, float l2, float targetDistanceInit, float targetDistance, float angle)
    {
        string s;
        s = $"{cond}, {backStart}, {l1}, {turn}, {pitch}, {spinDir}, {l2}, {targetDistanceInit}, {targetDistance}, {angle}";
        Add(s);
    }
    
    public void AddRotation(int cond, float turnStart, float rotation, bool pitch, float spinDir, float response)
    {
        string s;

        s = $"{cond}, {turnStart}, {rotation}, {pitch}, {spinDir}, {response}";
        Add(s);
    }

    public void AddBackward(int cond, float waitStart, float distance, bool pitch, float spinDir, 
                    float targetDistanceInit, float targetDistance)
    {
        string s;

        s = $"{cond}, {waitStart}, {distance}, {pitch}, {spinDir}, {targetDistanceInit}, {targetDistance}";
        Add(s);
    }
   public void AddForward(int cond, float turnStart, float distance, float rotation, bool pitch, float spinDir, 
                   float targetDistanceInit, float targetDistance)
    {
        string s;

        s = $"{cond}, {turnStart}, {distance}, {rotation}, {pitch}, {spinDir}, {targetDistanceInit}, {targetDistance}";
        Add(s);
    } 
    public void Dump(string fileName, string header)
    {
        Debug.Log("Writing to " + fileName);
        try
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.WriteLine(header);
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
    }
}