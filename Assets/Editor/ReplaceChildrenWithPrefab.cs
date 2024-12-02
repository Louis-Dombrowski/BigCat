using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Have you made a terrible mistake?
// Prefabify your children!
// Hopefully your scene is sorted nicely :3
// :333333333333

[ExecuteAlways]
public class ReplaceChildrenWithPrefab : MonoBehaviour
{
    public GameObject prefab;
    public bool theBigRedButton = false;
    public bool numberChildren = false;
    
    // Update is called once per frame
    void Update()
    {
        if (theBigRedButton)
        {
            theBigRedButton = false;
            List<(Vector3 p, Quaternion r, Vector3 s)> positions = new();
            for(int i = 0; i < transform.childCount; i++)
            {
                var t = transform.GetChild(i);
                positions.Add(new(t.position, t.rotation, t.localScale));
            }
            
            for(int i = 0; i < transform.childCount; i++)
            {
                DestroyImmediate(transform.GetChild(i).gameObject);
            }
            
            foreach (var p in positions)
            {
                var t = (PrefabUtility.InstantiatePrefab(prefab, transform) as GameObject).transform;
                t.position = p.p;
                t.rotation = p.r;
                t.localScale = p.s;
            }
        }

        if (numberChildren)
        {
            numberChildren = false;

            for (int i = 0; i < transform.childCount; i++)
            {
                transform.GetChild(i).name += $" ({i})";
            }
        }
    }
}
