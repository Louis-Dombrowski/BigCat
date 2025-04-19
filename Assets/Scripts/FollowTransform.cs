using System;
using UnityEngine;

public class FollowTransform : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 posOffset;
    [SerializeField] private Vector3 rotOffset;
    [SerializeField] private bool followRot = true;
    [SerializeField] private bool followPos = true;
    
    private void LateUpdate()
    {
        if (followPos) transform.position = target.position + posOffset;
        if (followRot) transform.rotation = target.rotation * Quaternion.Euler(rotOffset);
    }
}
