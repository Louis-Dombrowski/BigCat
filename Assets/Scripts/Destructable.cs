using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using NUnit.Framework.Constraints;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class Destructable : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private int value = 0;
    [SerializeField] private float impulseInclination = 45f;
    [SerializeField] private float explosionForce = 40;
    [SerializeField] private float randomForce = 10;
    [SerializeField] private float despawnDelay = 30;
    [SerializeField] private Sfx.ClipId destroySound = Sfx.ClipId.BuildingCrumble;

    [Header("State")]
    [SerializeField] private List<Rigidbody> parts = new();
    [SerializeField] public bool hasBeenKicked = false;
    
    // Start is called before the first frame update
    void Start()
    {
        #if UNITY_EDITOR
        if (destroySound == Sfx.ClipId.Null)
        {
            Debug.LogWarning($"Destructable sound not set: {UnityEditor.Search.SearchUtils.GetHierarchyPath(gameObject)}");
        }
        #endif
        
        var colliders = GetComponentsInChildren<Collider>();
        foreach (var c in colliders)
        {
            var mesh = c.GetComponent<MeshCollider>();
            if (mesh != null) mesh.convex = true;
            
            c.GetOrAddComponent<DestructablePart>();

            // GetOrAddComponent is SO broken
            Rigidbody body = c.GetComponent<Rigidbody>();
            while (body == null)
            {
                body = c.AddComponent<Rigidbody>();
            }
            
            parts.Add(c.GetComponent<Rigidbody>());
            parts[^1].constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    public void Explode(Vector3 direction, Vector3 hitPoint)
    {
        if (hasBeenKicked) return; // In case it gets kicked twice in the same physics frame.
        
        GuiData.instance.score -= value;
        hasBeenKicked = true;
        
        Vector3 baseImpulse = direction;
        baseImpulse.y = Mathf.Tan(impulseInclination * Mathf.Deg2Rad);
        baseImpulse.Normalize();

        Quaternion forward = Quaternion.LookRotation(baseImpulse);
        
        Random.InitState(name.GetHashCode());
        foreach (var b in parts)
        {
            Quaternion randomRotation = forward * Quaternion.Euler(Random.Range(0, 180), Random.Range(0, 180), 0);

            b.constraints = RigidbodyConstraints.None;
            b.AddForce(baseImpulse * explosionForce + randomRotation * Vector3.forward * randomForce, ForceMode.Impulse);
         
            Destroy(b.GetComponent<DestructablePart>());
            b.AddComponent<Kickable>();
        }

        float volumeMultiplier = Mathf.Log(Mathf.Abs(value) + 1, 1_000_000);
        Sfx.PlaySound(destroySound, hitPoint, volumeMultiplier);
        
        Destroy(gameObject, despawnDelay);
    }
}
