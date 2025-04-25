using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
using Util;

public class CatController : MonoBehaviour
{
	public enum StepState
	{
		Grounded,
		Stepping
	};

	public enum InterestState
	{
		Distracted,
		Bored,
		Roaming,
		Startled,
		Running,
		LevelComplete
	};

	public struct Waypoint
	{
		public Vector2 pos;
		public bool turning;
	}
	
	public static void Startle(Vector3 position)
	{
		if (instance == null)
		{
			Debug.LogError("Tried to startle cat while it was not initialized");
			return;
		}
		
		instance.fearSource = position;
		instance.interestState = InterestState.Startled;
	}
	private static CatController instance = null;
	
	[Header("Body Parts")]
	[Tooltip("The final destination of the cat. When it reaches this, the level ends.")]
	[SerializeField] private Transform destination;
	[SerializeField] private Transform target;
	[SerializeField] private Transform bodyCenterOfMass;
	[SerializeField] private Transform body;
	[SerializeField] private Transform neck;
	[SerializeField] private Transform head;
	[SerializeField] private Transform front;
	[SerializeField] private Transform back;
	[SerializeField] private CatLeg frontRightLeg;
	[SerializeField] private CatLeg backRightLeg;
	[SerializeField] private CatLeg backLeftLeg;
	[SerializeField] private CatLeg frontLeftLeg;
	[SerializeField] private List<LegIK> rigIK;
	[Header("Properties")]
	[SerializeField] private float stepTime = 1f;
	[SerializeField] private float gallopTime = 0.4f;
	[SerializeField] private float turnaroundTime = 0.1f;
	[SerializeField] private float secondStepDelay = 0.1f;
	[SerializeField] private float bobHeight = 0.1f;
	[Tooltip("Degrees")]
	[SerializeField] private float turnAngle = 15;
	[SerializeField] private FloatRange straightStrideLength;
	[SerializeField] private FloatRange turningStrideLength;
	[SerializeField] private float lookAngle = 15;
	[SerializeField] private float interestThreshold = 0.05f;
	[SerializeField] private float proximityThreshold = 0.01f;
	[Tooltip("How far to run when startled")]
	[SerializeField] private float runDistance = 50;
	[Tooltip("The height that the cat idles above the ground")]
	[SerializeField] private float height = 2.2f;
	[SerializeField] private Approximator heightApprox;

	[Header("State")]
	[SerializeField] private bool stepParity = false;
	[SerializeField] private bool recalculatePath = true;
	[Tooltip("Previous global position of `body`")]
	[SerializeField] private List<Waypoint> pathWaypoints;
	[SerializeField] private QuadraticBezier currentPath;
	[Tooltip("Rotation at the beginning of this step")]
	[SerializeField] private Quaternion prevRotation;
	[SerializeField] private Quaternion tgtQuat;
	[SerializeField] private StepState stepState = StepState.Grounded;
	[SerializeField] private Stopwatch stepStopwatch = new();
	[SerializeField] private QuaternionApproximator lookApprox;
	[SerializeField] public HashSet<Distraction> visibleDistractions = new();
	[SerializeField] private Distraction targetDistraction = null;
	[SerializeField] private InterestState interestState = InterestState.Bored;
	[SerializeField] private Vector3 fearSource = Vector3.zero;
	
	void Start()
	{
		lookApprox.Initialize(Quaternion.identity);
		heightApprox.Initialize(body.position.y);
		
		if (instance != null)
		{
			Debug.LogWarning("Tried to instantiate two cats at the same time");
		}
		instance = this;
		
		// Transfer the cat gameobject's rotation onto the body
		// This has to be done in Start, because the legs find their offsets in Awake
		Quaternion bodyRot = transform.rotation;
		transform.rotation = Quaternion.identity;
		body.rotation = bodyRot;

		// Initialize paw positions
		var legs = GetComponentsInChildren<CatLeg>();
		foreach(var l in legs) l.TeleportHome();

		rigIK = new(GetComponentsInChildren<LegIK>());
	}
	
	void FixedUpdate()
	{
		foreach (var l in rigIK)
		{
			l.catDirection = body.rotation;
		}
		
		if (interestState == InterestState.LevelComplete) return;
		
		if (interestState == InterestState.Startled) // Begin running
		{
			Vector3 runDirection = (head.transform.position - fearSource).normalized;
			targetDistraction = null;
			target.position = fearSource + runDirection * runDistance;
			recalculatePath = true;
			interestState = InterestState.Running;
			gizmoDrawCalls["StartleArrow"] = () =>
			{
				ArrowGizmo.Draw(fearSource, runDirection * runDistance, Color.cyan);
			};
		}
		if (interestState == InterestState.Running) // Stop running once you've reached your target
		{
			if (Distance2D(body.position, target.position) < straightStrideLength.max * 2) interestState = InterestState.Bored;
		}
		
		if(interestState != InterestState.Startled && interestState != InterestState.Running) // Don't update interests while scared
		{
			UpdateInterest();
		}
		
		LookToTarget();
		
		if (interestState == InterestState.Bored) // Roam to a point
		{
			target.position = FindRoamTarget();
			recalculatePath = true;
			interestState = InterestState.Roaming;
		}

		// Update highscore
		if (interestState == InterestState.Roaming)
		{
			if (Distance2D(body.position, target.position) < straightStrideLength.max)
			{
				GuiData.instance.UpdateHighScore();
				interestState = InterestState.LevelComplete;
				Sfx.PlaySound(Sfx.ClipId.LevelComplete);
			}
		} 
		
		if (recalculatePath && stepState == StepState.Grounded) // Update the path when the cat isn't in the middle of a step
		{
			pathWaypoints = FindPath();
			recalculatePath = false;
		}
		
		if(stepState == StepState.Grounded && pathWaypoints.Count > 0) // Begin animating towards the next point
		{
			Vector3 prevWaypoint = body.position;
			
			prevRotation = body.rotation;
			
			Vector3 nextWaypoint = pathWaypoints[0].pos;
			nextWaypoint = new(nextWaypoint.x, prevWaypoint.y, nextWaypoint.y);

			bool turning = pathWaypoints[0].turning;
			Vector3 delta = nextWaypoint - prevWaypoint;
			
			float startHeight = backLeftLeg.TargetPawHeight() + backRightLeg.TargetPawHeight() + frontRightLeg.TargetPawHeight() + frontLeftLeg.TargetPawHeight();
			startHeight /= 4;
			startHeight += height;
			
			float angle = Vector3.SignedAngle(body.forward, nextWaypoint - prevWaypoint, Vector3.up);
			tgtQuat = Quaternion.Euler(0, angle, 0);
			
			stepState = StepState.Stepping;

			// figure out the amount of time each stride should take
			bool gallopGait = false;
			float t;
			if (interestState == InterestState.Running)
			{
				if(turning) t = turnaroundTime;
				else
				{
					t = gallopTime;
					gallopGait = true;
				}
			}
			else
			{
				t = stepTime;
			}

			stepStopwatch.length = t;
			stepStopwatch.Start();
			
			// Set up all the leg motions in advance
			if (gallopGait)
			{
				if (stepParity)
				{
					frontLeftLeg .StartCoroutine(frontLeftLeg .AnimateStep(delta, 0, t));
					frontRightLeg  .StartCoroutine(backLeftLeg  .AnimateStep(delta, secondStepDelay, t));
				}
				else
				{
					backLeftLeg.StartCoroutine(frontRightLeg.AnimateStep(delta, 0, t));
					backRightLeg .StartCoroutine(backRightLeg .AnimateStep(delta, secondStepDelay, t));	
				}
			}
			else
			{
				if (stepParity)
				{
					frontLeftLeg .StartCoroutine(frontLeftLeg .AnimateStep(delta, 0, t));
					backLeftLeg  .StartCoroutine(backLeftLeg  .AnimateStep(delta, secondStepDelay, t));
				}
				else
				{
					frontRightLeg.StartCoroutine(frontRightLeg.AnimateStep(delta, 0, t));
					backRightLeg .StartCoroutine(backRightLeg .AnimateStep(delta, secondStepDelay, t));	
				}
			}
			stepParity = !stepParity;
			
			float endHeight = backLeftLeg.TargetPawHeight() + backRightLeg.TargetPawHeight() + frontRightLeg.TargetPawHeight() + frontLeftLeg.TargetPawHeight();
			endHeight /= 4;
			endHeight += height;
			
			float midHeight = (startHeight + endHeight) / 2 + Mathf.Lerp(0, bobHeight, delta.magnitude / straightStrideLength.max);

			// Set up the path to animate
			currentPath.p1 = prevWaypoint;
			currentPath.p1.y = startHeight;
			currentPath.p2 = prevWaypoint + delta / 2;
			currentPath.p2.y = midHeight;
			currentPath.p3 = nextWaypoint;
			currentPath.p3.y = endHeight;
		}
		
		if (stepState == StepState.Stepping) // Animate the cat moving
		{
			stepStopwatch.Tick();

			float percentThroughHalf = stepStopwatch.progress * 2;
			bool halfwayThrough = false;
			if (percentThroughHalf > 1)
			{
				halfwayThrough = true;
				percentThroughHalf -= 1;
			}

			if (!halfwayThrough)
			{
				front.localRotation = Quaternion.Slerp(Quaternion.identity, tgtQuat, percentThroughHalf);
			}
			else
			{
				front.localRotation = Quaternion.Slerp(tgtQuat, Quaternion.identity, percentThroughHalf);
				body.localRotation = prevRotation * Quaternion.Slerp(Quaternion.identity, tgtQuat, percentThroughHalf);
			}

			float percent = stepStopwatch.progress;

			//print((body.position - currentPath.Position(percent)).magnitude);
			
			body.position = currentPath.Position(percent);

			if (percent >= 1)
			{
				stepState = StepState.Grounded;
				pathWaypoints.RemoveAt(0);
			}
		}
	}

	public Vector3 ExplosionDirection()
	{
		return (currentPath.p3 - currentPath.p1).normalized;
	}

	private void UpdateInterest()
	{
		// If the current distraction has become boring, leave it
		if (targetDistraction != null)
		{
			bool uninteresting = targetDistraction.strength < interestThreshold;

			Vector2 bodyPos = new(body.transform.position.x, body.transform.position.z);
			Vector2 tgtPos = new(targetDistraction.transform.position.x, targetDistraction.transform.position.z);
			bool tooClose = (bodyPos - tgtPos).magnitude < proximityThreshold;

			if (uninteresting || tooClose)
			{
				targetDistraction.strength = 0; // Make it so the cat won't return to the distraction
				
				targetDistraction = null;
				interestState = InterestState.Bored;
			}
		}
		
		// If theres no target distraction, try to find a new one
		if (targetDistraction == null)
		{
			// Remove distractions that have been destroyed
			visibleDistractions.RemoveWhere((d) => d == null);
			
			float highestWeight = 0;
			foreach (var d in visibleDistractions)
			{
				float w = d.strength;
				if (w < highestWeight || w < interestThreshold) continue;

				highestWeight = w;
				targetDistraction = d;
			}

			if (targetDistraction != null)
			{
				target.position = targetDistraction.transform.position;
				interestState = InterestState.Distracted;
				recalculatePath = true;
			}
			else if(interestState != InterestState.Roaming)
			{
				interestState = InterestState.Bored; // Make the cat bored if its distraction is destroyed
			}
		}

		if (targetDistraction != null && targetDistraction.moving)
		{
			target.position = targetDistraction.transform.position;
			recalculatePath = true;
		} 
	}

	private void LookToTarget()
	{
		Vector3 toTarget = (target.position - neck.position);
		Quaternion towardsTarget = Quaternion.LookRotation(body.InverseTransformDirection(toTarget.normalized)); // Local rotation
		
		Quaternion lookDir = Quaternion.RotateTowards(Quaternion.identity, towardsTarget, lookAngle);

		if (toTarget.magnitude > 3)
		{
			Quaternion look = lookApprox.Update(Time.deltaTime, lookDir);
			neck.localRotation = look;
			head.localRotation = look;
		}
	}
	private float Distance2D(Vector3 a, Vector3 b)
	{
		float dx = a.x - b.x;
		float dz = a.z - b.z;
		return Mathf.Sqrt(dx * dx + dz * dz);
	}
	
	Vector3 FindRoamTarget()
	{
		return destination.position;
	}
	
	/// <returns>
	/// a list of waypoints along a path to the `target` transform
	/// (x, y) => (x, 0, y)
	/// </returns>
	private List<Waypoint> FindPath()
	{
		List<Waypoint> waypoints = new();

		Vector2 fwd = new (body.forward.x, body.forward.z); // unit vector
		Vector2 pos = new (body.position.x, body.position.z); // world pos
		Vector2 tgt = new (target.position.x, target.position.z); // world pos
		
		bool needsToTurn = Vector2.Angle(tgt - pos, fwd) > turnAngle;

		// Turn with a wide radius
		if (needsToTurn)
		{
			// Direction around the turning circle the cat will be travelling
			// -1 for CW, 1 for CCW, will never be zero if needs to turn is true
			int direction = Mathf.RoundToInt(Mathf.Sign(Vector3.Cross(fwd, tgt - pos).z));
			
			// Find a circle from step length and turn angle:
			// https://www.desmos.com/calculator/yk1d7kuoph
			Vector2 center; // world pos
			float r;
			#region
			{
				float sinHalfA = Mathf.Sin(turnAngle * Mathf.Deg2Rad / 2);
				float cosHalfA = Mathf.Cos(turnAngle * Mathf.Deg2Rad / 2);
				
				Vector2 P = tgt - pos;
				P.ComplexMultiplyBy(new(fwd.y, fwd.x)); // Orient P so that +Y is forward
				
				// `direction` handles flipping the circle across the y axis.
				// This keeps everything in Q1 and Q4 until it needs mirroring later.
				P.x = Mathf.Abs(P.x);
				
				float sqrtExp = Mathf.Sqrt(2 - 2 * Mathf.Cos(turnAngle * Mathf.Deg2Rad));

				float l =
					(P.x * P.x + P.y * P.y)
					/ (2 * P.x * cosHalfA - 2 * P.y * sinHalfA)
					* sqrtExp;
				l -= 0.01f; // Shrink it slightly to avoid floating point error
				
				// If the tgt point is SUPER close to the cat's current position, dont bother pathing to it
				if (l < turningStrideLength.min) return new();
				l = turningStrideLength.ClampWithin(l);

				r = l / sqrtExp;

				Vector2 centerDirection = new Vector2(cosHalfA * -direction, -sinHalfA);
				centerDirection.ComplexMultiplyBy(new(fwd.y, -fwd.x)); // Yes, this is supposed to be different than the other rotations vector. No, don't ask me why.
				
				center = pos + r * centerDirection;

				// Draw turning circle
				gizmoDrawCalls["TurningCircle"] = () => {
					Vector3[] circlePoints = new Vector3[36];
					for (int i = 0; i < circlePoints.Length; i++)
					{
						float a = 2 * Mathf.PI * i / circlePoints.Length;

						Vector2 p = center + r * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
						circlePoints[i] = new(p.x, 2, p.y);
					}
					Gizmos.color = Color.magenta;
					Gizmos.DrawLineStrip(circlePoints, true);
				};
			}
			#endregion

			// Find the point on the circle whose tangent points towards tgt
			// https://www.desmos.com/calculator/r589ykdirc
			Vector2 endpoint; // relative to `center`
			#region

			{
				Vector2 T = tgt - center;
				
				// The previous step ensures that the target point is farther from the center of the circle than its radius
				float L = Mathf.Sqrt(T.x * T.x + T.y * T.y - r * r);
				float k = T.x * T.x + T.y * T.y - L * L + r * r;

				// Because of the early return when the step length becomes too small,
				// A, B, and C are never zero.
				float A = 4 * (T.x * T.x + T.y * T.y);
				float B = -4 * k * T.y;
				float C = k * k - 4 * T.x * T.x * r * r;

				Vector2[] possiblePoints = new Vector2[4]; // Relative to center
				
				// Always real, because of the radius shrinking/early return from before.
				float discriminant = Mathf.Sqrt(B * B - 4 * A * C);
				possiblePoints[0].y = (-B + discriminant) / (2 * A);
				possiblePoints[1].y = (-B - discriminant) / (2 * A);
				possiblePoints[2].y = possiblePoints[0].y;
				possiblePoints[3].y = possiblePoints[1].y;

				for (int i = 0; i < possiblePoints.Length; i++)
				{
					float y = possiblePoints[i].y;
					possiblePoints[i].x = Mathf.Sqrt(r * r - y * y);
				}
				possiblePoints[2].x *= -1;
				possiblePoints[3].x *= -1;

				// Narrow down to the correct point
				List<int> options = new();

				// Remove the incorrect branches of the sqrts
				List<(float error, int idx)> errors = new();
				for (int i = 0; i < possiblePoints.Length; i++)
				{
					Vector2 p = possiblePoints[i];

					errors.Add(new(Mathf.Abs(-p.x / p.y - (p.y - T.y) / (p.x - T.x)), i));
				}
				errors.Sort((a, b) => Mathf.RoundToInt(Mathf.Sign(a.error - b.error)));
				options.Add(errors[0].idx);
				options.Add(errors[1].idx);

				// Figure out which point requires the cat to travel backwards
				Vector2 p1 = possiblePoints[options[0]];
				Vector2 p2 = possiblePoints[options[1]];
				if (Vector3.Cross(p1, p2).z * direction < 0) options.RemoveAt(0);
				else options.RemoveAt(1);
				
				// Draw all the point candidates in red, and the chosen point in black
				gizmoDrawCalls["TurningCircleCandidatePoints"] = () => {
					Gizmos.color = Color.red;
					foreach (var p in possiblePoints)
					{
						Gizmos.DrawSphere(new(p.x + center.x, 2, p.y + center.y), 0.1f);
					}
					Gizmos.color = Color.black;
					foreach (var o in options)
					{
						Vector2 p = possiblePoints[o] + center;
						Gizmos.DrawSphere(new(p.x, 2, p.y), 0.1f);
					}
				};

				endpoint = possiblePoints[options[0]];
			}
			#endregion
			
			// Plot the circular portion of the path:
			#region
			{
				float angleDiff = Vector2.SignedAngle(pos - center, endpoint) * Mathf.Deg2Rad;
				if (angleDiff * direction < 0) // Remap angleDiff to 0-360
				{
					angleDiff = direction * 2 * Mathf.PI + angleDiff;
				}

				int circleSteps = Mathf.CeilToInt(Mathf.Abs(angleDiff) / (turnAngle * Mathf.Deg2Rad));
				float toTurn = angleDiff / circleSteps; // The actual angle being turned (The ceil should keep it less than turnAngle)

				float startAngle = Vector2.SignedAngle(Vector2.right, pos - center) * Mathf.Deg2Rad;
				for (int i = 1; i <= circleSteps; i++)
				{
					float a = startAngle + toTurn * i;
					Waypoint wp = new();
					wp.pos = center + r * new Vector2(Mathf.Cos(a), Mathf.Sin(a));
					wp.turning = true;
					waypoints.Add(wp);
				}
			}
			#endregion
		}
		
		// Plot the straight section:
		bool done = false;
		int iterations = 0;
		while (!done && iterations < 100)
		{
			Vector2 back = waypoints.Count == 0 ? new(pos.x, pos.y) : waypoints[^1].pos;

			Vector3 toTgt = tgt - back;

			float dist;
			if (toTgt.magnitude < straightStrideLength.max)
			{
				dist = toTgt.magnitude;
				done = true;
			}
			else dist = straightStrideLength.max;

			Vector2 path = toTgt.normalized * dist;
			Vector2 next = back + path;

			Waypoint wp = new();
			wp.pos = next;
			wp.turning = false;
			waypoints.Add(wp);
			
			iterations++;
		}
		
		// Draw the path
		List<Vector3> pathPoints = new();
		pathPoints.Add(new(pos.x, 3, pos.y));
		foreach(var w in waypoints) pathPoints.Add(new (w.pos.x, 3, w.pos.y));
		gizmoDrawCalls["CatPath"] = () =>
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawLineStrip(pathPoints.ToArray(), false);
			Gizmos.color = Color.red;
			foreach (var p in pathPoints) Gizmos.DrawSphere(p, 0.15f);
		};
		
		return waypoints;
	}
	
	private Dictionary<string, UnityAction> gizmoDrawCalls = new();
	private void OnDrawGizmos()
	{
		if (!Application.isPlaying) return;

		foreach (var c in gizmoDrawCalls) c.Value();
	}
}
