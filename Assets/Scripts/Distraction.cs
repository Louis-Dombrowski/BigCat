using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Distraction : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] public float strength = 1;

    [Header("State")]
    [SerializeField] public bool moving;
    
    public float Weight(Vector3 catPos)
    {
        float r = (catPos - transform.position).magnitude;
        return strength / r * r;
    }

    public void MarkAsMoving()
    {
        moving = true;
    }
}
