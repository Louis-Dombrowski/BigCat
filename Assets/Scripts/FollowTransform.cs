using System;
using UnityEngine;

public class FollowTransform : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 posOffset;
    [SerializeField] private Quaternion rotOffset;
    
    private void LateUpdate()
    {
        transform.position = target.position;
        transform.rotation = target.rotation;
    }
}
