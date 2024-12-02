using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Stopwatch
{
    public enum State
    {
        Idle,
        Counting,
        Finished
    }
    
    public float length;
    public float time;
    public float progress;
    public State state;
    
    public void Start()
    {
        if (state == State.Counting)
        {
            Debug.LogWarning("Tried to start already-counting stopwatch");
            return;
        }

        time = 0;
        state = State.Counting;
    }
    
    public void Tick()
    {
        time += Time.deltaTime;
        progress = Mathf.Clamp01(time / length);
        
        if (time >= length)
        {
            state = State.Finished;
        }
    }

    public float Progress()
    {
        return time / length;
    }
}
