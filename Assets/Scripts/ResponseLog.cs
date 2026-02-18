using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using UnityEngine;

/**
 * Record and then dump out a set of responses
 *
 * V1.0 borrowed from the TrialLog code from the SMUG project.
 *
 * Copyright (c) Michael Jenkin, 2025
 **/


public class ResponseLog
{
    public const int LINEAR_TARGET = 0;
    public const int ROTATIONAL_TARGET = 1;
    public const int WHERE_DID_I_GO = 2;
    private class DataPoint
    {
        public float l1;                                // the length of the first leg
        public float l2;                                // the length of the second length
        public float theta;                             // angle in degrees between the two legs
        public float panTilt;                           // is it pan (+1) or tilt (-1)
        public float dir;                               // is it to the right/up (+1) or to the left/down (-1)
        public int whichExperiment;                     // one of ADJUST_TARGET or WHERE_DID_I_GO
        public float distance1, distance2, thetaResp;   // responses for the ADJUST_TARGET version
        public float direction, distance;               // response for the WHERE_DID_I_GO version
        public float time;                              // the time of this data collection session


        public DataPoint(int whichExperiment, float time, float l1, float l2, float theta, float panTilt, float dir, float distance1, float distance2, float thetaResp, float direction, float distance)
        {
            this.whichExperiment = whichExperiment;
            this.time = time;
            this.l1 = l1;
            this.l2 = l2;
            this.theta = theta;
            this.panTilt = panTilt;
            this.dir = dir;
            this.distance1 = distance1;
            this.distance2 = distance2;
            this.thetaResp = thetaResp;
            this.direction = direction;
            this.distance = distance;
        }

        public override string ToString()
        {
            return this.whichExperiment + ", " + this.time + ", " + this.l1 + ", " + this.l2 + ", " + this.theta + ", " +
                   this.panTilt + ", " + this.dir + ", " + this.distance1 + ", " + this.distance2 + ", " + this.thetaResp + ", " +
                   this.direction + ", " + this.distance;
        }
    }

    private List<DataPoint> _log;


    public ResponseLog()
    {
        _log = new List<DataPoint>();
        Debug.Log("Response log created");
    }

    public void Add(int whichExperiment, float time, float l1, float l2, float theta, float panTilt, float dir, float distance1, float distance2, float thetaResp, float direction, float distance)
    {
        DataPoint p = new DataPoint(whichExperiment, time, l1, l2, theta, panTilt, dir, distance1, distance2, thetaResp, direction, distance);
        Debug.Log("Adding " + p.ToString());
        _log.Add(p);
        Debug.Log($"list has length {_log.Count}");
        for(int i=0;i<_log.Count;i++)
        {
            Debug.Log(_log[i]);
        }
    }

    public void Dump(string fileName, string header)
    {
        Debug.Log("Writing to " + fileName);
        try
        {
            using (StreamWriter sw = new StreamWriter(fileName))
            {
                sw.WriteLine(header);
                foreach (DataPoint p in _log)
                {
                    Debug.Log(p.ToString());
                    sw.WriteLine(p.ToString());
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