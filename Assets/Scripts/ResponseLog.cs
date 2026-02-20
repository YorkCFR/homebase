using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

/**
 * Record and then dump out a set of responses
 *
 * Version
 * V2.0 Now its just a list of strings. This simplifies using it elsewhere
 * V1.0 borrowed from the TrialLog code from the SMUG project.
 *
 * Copyright (c) Michael Jenkin, 2025, 2026
 **/


public class ResponseLog
{

    private List<string> _log;


    public ResponseLog()
    {
        _log = new List<string>();
    }


    public void Add(string s)
    {
        _log.Add(s);
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