using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class Kickable : MonoBehaviour
{
    [Header("Properties")]
    [SerializeField] private float kickForce = 5;
    [SerializeField] private float randomForce = 0.2f;
    [SerializeField] private UnityEvent onKick = new();
    
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Cat"))
        {
            var cat = other.gameObject.GetComponentInParent<CatController>();
            
            Vector3 baseImpulse = cat.ExplosionDirection();
            baseImpulse.Normalize();

            Random.InitState(name.GetHashCode());
            Quaternion forward = Quaternion.LookRotation(baseImpulse);
            Quaternion randomRotation = forward * Quaternion.Euler(Random.Range(0, 180), Random.Range(0, 180), 0);

            Vector3 impulse = baseImpulse * kickForce + randomRotation * Vector3.forward * randomForce;

            GetComponent<Rigidbody>().AddForce(impulse, ForceMode.Impulse);

            onKick.Invoke();
        }
    }
}
