/**
 * Create a field of spheres of different radii in one of two planes. 
 *
 * 
 * Copyright Michael Jenkin 2025, 2026
 * Version History
 * 
 * V4.0 - yet another substantive re-write based on changes in the experiment 
 * V3.0 - yet anothr substantive re-write of how to generate the random field.
 * V2.0 - substantive update based on revised experimental structure
 * V1.0 - based on the OSC software
 **/
using UnityEngine;
using System;

public class SphereField {
    private const float MIN_SPHERE_DIAMETER = 0.4f;
    private const float MAX_SPHERE_DIAMETER = 1.2f;
    public const float PLANE_OFFSET = (3.3f / 2.0f); // 
    private const float MAX_MAX_PLANE_DIST = 100.0f;

    private class Blob {
        public float X;
        public float Z;
        public float Y;
        public GameObject Obj;
    }


    private Blob[] _blobs;
    private int _n;
    private bool _pitch;

    /**
     * Create N spheres in the volume defined above. We will now create n balls ...
     */
    public SphereField(int n)
    {
        _n = n;
        _pitch = true;
        _blobs = new Blob[n];
        UnityEngine.Object sphere = Resources.Load("TexturedSphere01");
        for (int i = 0; i < _n; i++)
        {
            _blobs[i] = new Blob();
            _blobs[i].Obj = (GameObject)UnityEngine.Object.Instantiate(sphere);
            MakeOneBLob(i);
        }
        Debug.Log("Blobs created");
    }

    public void RePaint(bool pitch)
    {
        _pitch = pitch;
        for(int i = 0; i < _n; i++)
        {
            bool active = _blobs[i].Obj.activeSelf;
            MakeOneBLob(i);
            _blobs[i].Obj.SetActive(active);
        }
    }

    public void DestroyGameObjects()
    {
        for (int j = 0; j < _n; j++)
        {
            _blobs[j].Obj.SetActive(false);
            UnityEngine.Object.Destroy(_blobs[j].Obj);
        }
    }

    /**
     * Enable the display
     */
    public void EnableHomeBaseDisplay()
    {
        for (int i = 0; i < _n; i++)
        {
            _blobs[i].Obj.SetActive(true);
        }
    }

    /**
     * Flicker the display
     */
    public void FLickerDisplay(float chance)
    {
        for (int i = 0; i < _n; i++)
        {
            float coin = UnityEngine.Random.Range(0.0f, 1.0f);
            if(coin < chance)
            {
                bool active = _blobs[i].Obj.activeSelf;
                MakeOneBLob(i);
                _blobs[i].Obj.SetActive(active);
            }

        }
    }

    /**
     * Create one blob
     */
    private void MakeOneBLob(int i)
    {
         // all spheres are in a plane with a given radius
        float p = UnityEngine.Random.Range(-MAX_MAX_PLANE_DIST, MAX_MAX_PLANE_DIST);
        float q = UnityEngine.Random.Range(-MAX_MAX_PLANE_DIST, MAX_MAX_PLANE_DIST);
        float r = UnityEngine.Random.Range(MIN_SPHERE_DIAMETER, MAX_SPHERE_DIAMETER);
        float d;
            
        float pic = UnityEngine.Random.Range(0.0f, 1.0f);
        if(pic < 0.5)
        {
            d = PLANE_OFFSET + MAX_SPHERE_DIAMETER/2;
        } else {
            d = -PLANE_OFFSET - MAX_SPHERE_DIAMETER/2;
        }

        float x, y, z;
        // put them there
        if (_pitch)
        {
            x = d;
            y = p;
            z = q;
        } else {
            x = p;
            y = d;
            z = q;
        }

        // define the blob
        _blobs[i].X = x;
        _blobs[i].Y = y;
        _blobs[i].Z = z;
        _blobs[i].Obj.transform.position = new Vector3(_blobs[i].X, _blobs[i].Y, _blobs[i].Z);
        _blobs[i].Obj.transform.localScale = new Vector3(r, r, r);
        _blobs[i].Obj.transform.rotation = UnityEngine.Random.rotationUniform;
        _blobs[i].Obj.SetActive(false);
    }

}
