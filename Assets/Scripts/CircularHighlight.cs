using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways]
public class CircularHighlight : MonoBehaviour
{
    [FormerlySerializedAs("radius")]
    [Header("Properties")]
    [SerializeField] public float diameter;
    [SerializeField] public float height;
    
    // Update is called once per frame
    void Update()
    {
        transform.localScale = new(diameter, height, diameter);
    }
}
