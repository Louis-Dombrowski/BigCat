using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class CatDetector : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private UnityEvent onEnter;
    [SerializeField] private UnityEvent onExit;

    [Header("State")]
    [SerializeField] private int catPartsInTrigger = 0;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Cat"))
        {
            if(catPartsInTrigger == 0) onEnter.Invoke();
            catPartsInTrigger++;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Cat"))
        {
            catPartsInTrigger--;
            if(catPartsInTrigger == 0) onExit.Invoke();
        }
    }
}
