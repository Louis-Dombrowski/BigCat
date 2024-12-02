using System.Collections;
using System.Collections.Generic;
using NUnit.Framework.Constraints;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

public class Destructable : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private int value = 0;
    [SerializeField] private float impulseInclination = 45f;
    [SerializeField] private float explosionForce = 40;
    [SerializeField] private float randomForce = 10;
    [SerializeField] private float despawnDelay = 30;
    
    [Header("State")]
    [SerializeField] private List<Rigidbody> parts = new();
    [SerializeField] private bool hasBeenKicked = false;
    
    // Start is called before the first frame update
    void Start()
    {
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

    public void Explode(Vector3 direction)
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

        Destroy(gameObject, despawnDelay);
    }
}
