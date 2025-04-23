using System;
using System.Collections.Generic;
using UnityEngine;
using Util;

[ExecuteAlways]
public class BoneRenderer : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private bool render = true;
    [SerializeField] private bool reloadBoneHierarchy = false;
    [Header("State")]
    private List<Transform> bones = new();
    
    // Update is called once per frame
    void Update()
    {
        if (reloadBoneHierarchy)
        {
            reloadBoneHierarchy = false;
            bones.Clear();
            bones.Add(transform);
    
            // Unroll tree into a list
            for (int b = 0; b < bones.Count; b++)
            {
                for (int c = 0; c < bones[b].childCount; c++)
                {
                    bones.Add(bones[b].GetChild(c));
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!render) return;

        foreach (var t in bones)
        {
            if(t != transform) ArrowGizmo.Draw(t.parent.position, t.position - t.parent.position, Color.magenta);
            ArrowGizmo.Draw(t.position, t.forward / 2, Color.blue);
        }
    }
}
