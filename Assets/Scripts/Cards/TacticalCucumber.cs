using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TacticalCucumber : BaseCard
{
    [Header("Parts")]
    [SerializeField] private Rigidbody cucumber;
    [SerializeField] private Toggleable[] lights;
    [SerializeField] private SphereCollider lightRadius;
    [SerializeField] private SphereCollider activationRadius;
    [SerializeField] private CircularHighlight activationRadiusHighlight;
    
    [Header("Properties")]
    [SerializeField] private float launchHeight = 10f;
    [SerializeField] private float baseRadius = 4f;
    [SerializeField] private FloatRange radiusRange = new();
    [SerializeField] private float radiusStep = 0.125f;
    
    // Start is called before the first frame update
    void Start()
    {
        // Initialize the cucumber
        cucumber.constraints = RigidbodyConstraints.FreezeAll;
        cucumber.isKinematic = true;
    }

    public void ToggleLights()
    {
        foreach (var l in lights) l.Toggle();
    }

    public void Launch()
    {
        if (!cucumber.isKinematic) return;
        
        CatController.Startle(transform.position);
        StartCoroutine(AnimateFlight());
    }

    private IEnumerator AnimateFlight()
    {
        float g = Mathf.Abs(Physics.gravity.y);
        float a = -g / 2;
        float b = Mathf.Sqrt(2 * g * launchHeight);
        float maxT = -b / (2 * a);

        Vector3 initialPos = cucumber.transform.localPosition;
        float t = 0;
        while (t < maxT)
        {
            t += Time.deltaTime;

            float height = a * t * t + b * t;
            initialPos.y = height;
            cucumber.transform.localPosition = initialPos;
            
            yield return null;
        }

        Random.InitState(name.GetHashCode());
        
        cucumber.constraints = RigidbodyConstraints.None;
        cucumber.isKinematic = false;
        cucumber.AddTorque(Random.onUnitSphere, ForceMode.Impulse);
    }

    public override void ChangeProperty(int delta)
    {
        baseRadius += delta * radiusStep;
        baseRadius = radiusRange.ClampWithin(baseRadius);

        activationRadius.radius = baseRadius;
        lightRadius.radius = baseRadius * 2;
        activationRadiusHighlight.diameter = 2 * baseRadius;
    }

    public override void DisablePlayFunctionality()
    {
        lights = new Toggleable[0];
        lightRadius.gameObject.SetActive(false);
        activationRadius.gameObject.SetActive(false);
    }

    public override void EnableEditFunctionality()
    {
        activationRadiusHighlight.gameObject.SetActive(true);
    }
}
