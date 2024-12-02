using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Toggleable : MonoBehaviour
{
    public enum State
    {
        On,
        Off
    }
    
    [Header("Properties")]
    [SerializeField] private UnityEvent turnOn = new();
    [SerializeField] private UnityEvent turnOff = new();
    [Header("State")]
    [SerializeField] private State state = State.Off;

    void Start()
    {
        // Initialize the state on startup
        if (state == State.On)
        {
            turnOn.Invoke();
        }
        else
        {
            turnOff.Invoke();
        }
    }

    public void Toggle()
    {
        if (state == State.Off)
        {
            turnOn.Invoke();
            state = State.On;
        }
        else
        {
            turnOff.Invoke();
            state = State.Off;
        }
    }
}
