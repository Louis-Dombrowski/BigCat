using System.Collections.Generic;
using Unity.TubeRenderer;
using UnityEngine;

public class YarnBall : BaseCard
{
    [System.Serializable]
    private struct YarnPoint
    {
        public int idx;
        public float velocity;
    }
    
    [Header("Parts")]
    [SerializeField] private SphereCollider ball;
    [SerializeField] private TubeRenderer yarn;
    [SerializeField] private Transform ballModel;
    [SerializeField] private Distraction distraction;
    [Header("Properties")]
    [SerializeField] private GameObject poofVFX;
    [SerializeField] private float turnAngle = 15;
    [SerializeField] private float maxSegmentLength = 3;
    [SerializeField] private FloatRange distractionWeightRange;
    [SerializeField] private float radiusStep = 0.125f;
    [SerializeField] private float radius = 1.5f;
    [SerializeField] private float maxLengthPerRadius = 50f;
    [SerializeField] private FloatRange initialRadiusRange = new();
    [Header("State")]
    [SerializeField] private float maxLength;
    [SerializeField] private FloatRange radiusRange;
    [SerializeField] private float yarnLength = 0f;
    [SerializeField] private Vector3 prevPos;
    [SerializeField] private Vector3 prevDir;
    [SerializeField] private List<YarnPoint> simulatedPoints = new();
    [SerializeField] private bool ballExists = true;
    
    void Start()
    {
        transform.rotation = Quaternion.identity; // orientation doesnt matter for this object, and it messes with the tube renderer

        radiusRange.max = radius;
        maxLength = maxLengthPerRadius * radius;
        
        SetRadius(radius);
        
        prevPos = ball.transform.position;
        prevDir = Vector3.zero;
        
        AddYarnPoint(prevPos);
        AddYarnPoint(prevPos);
    }

    void FixedUpdate()
    {
        if (ballExists)
        {
            yarn.transform.localPosition = -transform.position;
            //                                     \/ This only approximates the extra length, it will likely cause slight jumps
            float percentSize = 1 - (yarnLength + (ball.transform.position - yarn.positions[^1]).magnitude) / maxLength;
            float radius = radiusRange.Lerp(percentSize);
            SetRadius(radius);
            distraction.strength = distractionWeightRange.Lerp(percentSize);

            if (percentSize <= 0)
            {
                Instantiate(poofVFX, ball.transform.position, Quaternion.identity, transform);
                Destroy(ball.gameObject);
                ballExists = false;
                return;
            }

            Vector3 yarnPoint = ball.transform.position + Vector3.down * radius;
            // Make the end of the string track the ball
            yarn.positions[^1] = yarnPoint;

            // Add new points if necessary
            Vector3 delta = ball.transform.position - prevPos;
            bool tooLong = delta.magnitude > maxSegmentLength;
            bool turnsTooSharp = prevDir != Vector3.zero &&
                                 Vector3.Dot(prevDir, delta.normalized) < Mathf.Cos(turnAngle * Mathf.Deg2Rad);

            if (tooLong || turnsTooSharp)
            {
                AddYarnPoint(yarnPoint);
                prevDir = ball.transform.position - prevPos;
                yarnLength += prevDir.magnitude;
                prevDir.Normalize();

                prevPos = ball.transform.position;
            }
        }

        if (!ballExists && simulatedPoints.Count == 0) Destroy(this);
        
        SimulatePoints();
    }

    private void SimulatePoints()
    {
        //                                   \/ Dont simulate the last point, since it's pinned to the sphere
        for (int i = simulatedPoints.Count - 2; i >= 0; i--)
        {
            var p = simulatedPoints[i];

            // Dont continue simulating this point if it has hit the ground. Ground colliders should (hopefully) be static.
            var overlaps = Physics.OverlapSphere(yarn.positions[p.idx], yarn.startWidth, LayerMask.GetMask("Ground"));
            if (overlaps.Length > 0)
            {
                simulatedPoints.RemoveAt(i);
                continue;
            }

            p.velocity += Physics.gravity.y * Time.deltaTime;
            yarn.positions[p.idx] += Vector3.up * (p.velocity * Time.deltaTime);
            simulatedPoints[i] = p;
        }
    }

    private void AddYarnPoint(Vector3 p)
    {
        List<Vector3> oldPoints = new(yarn.positions);
        oldPoints.Add(p);
        yarn.SetPositions(oldPoints.ToArray());

        var simData = new YarnPoint();
        simData.idx = oldPoints.Count - 1;
        simData.velocity = 0;
        simulatedPoints.Add(simData);
    }

    private void SetRadius(float r)
    {
        ballModel.localScale = Vector3.one * r * 2;
        if(ball != null) ball.radius = r;
    }

    public override void ChangeProperty(int delta)
    {
        radius += delta * radiusStep;
        radius = initialRadiusRange.ClampWithin(radius);
        SetRadius(radius);
    }

    public override void DisablePlayFunctionality()
    {
        Destroy(ball.GetComponent<Rigidbody>());
        Destroy(ball);
        ballExists = false;
    }
}
