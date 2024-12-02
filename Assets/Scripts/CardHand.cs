using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Util;

public class CardHand : MonoBehaviour
{
    private delegate void GizmoDrawCall();
    enum CursorState
    {
        Free,
        CarryingCard
    }
    
    [Serializable]
    struct CardSet
    {
        public GameObject prefab;
        public int count;
    }
    
    [Header("Parts")]
    [SerializeField] private new Camera camera;
    [SerializeField] private Transform handParent;
    [SerializeField] private Transform debugCube;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private Transform focusPoint;
    [SerializeField] private Transform cursorPos;
    [Header("Properties")]
    [SerializeField] private float handDistance = 2;
    [SerializeField] private float arcHeight = 0.5f;
    [SerializeField] private float downwardOffset = 3;
    [SerializeField] private FloatRange handRange = new();
    [SerializeField] private float zRange = 0.25f;
    [SerializeField] private List<CardSet> initialHand = new();
    [Tooltip("The distance a card rises when hovered over")]
    [SerializeField] private float hoverDistance = 0.5f;
    [SerializeField] private float cardReturnHeight = 1f;
    [Header("State")]
    [SerializeField] private List<Card> hand = new();
    [FormerlySerializedAs("selectedCard")]
    [Tooltip("An index of -1 means there is no card selected")]
    [SerializeField] private int hoveredCard = -1;
    [Tooltip("In the local space of the camera")]
    [SerializeField] private Rect viewPlane;
    [Tooltip("In the local space of the camera")]
    [SerializeField] private Rect cardReturnArea;
    [SerializeField] private CursorState cursorState;
    [SerializeField] private bool holdingInteract = false;
    [SerializeField] private Card heldCard;
    
    private void Start()
    {
        cameraController = GetComponent<CameraController>();
        
        // Initialize cards in hand
        // Instantiate them all
        foreach (var set in initialHand)
        {
            for(int i = 0; i < set.count; i++)
            {
                var card = Instantiate(set.prefab, handParent).GetComponent<Card>();
                card.transform.name += i; // They need to have distinct names
                card.approx.Initialize(new Vector2(Mathf.PI / 2, 0)); // dimension 0 is the angular position, dimension 1 is the radial focus position
                hand.Add(card);
            }
        }
        // Spread them out evenly
        for (int i = 0; i < hand.Count; i++)
        {
            var pos = handRange.Lerp(1 - (float)i / (float)(hand.Count - 1));
            
            var card = hand[i];
            card.approx.Initialize(2, new [] { pos, 0 });
            hand[i] = card;
        }
    }
    
    // Make sure to run after the camera has moved
    private void LateUpdate()
    {
        float handArcAngle; // Radians
        float handArcRadius;
        Vector3 handCenter; // World space
        // Also updates viewPlane and cardReturnArea
        {
            float halfHeight = handDistance * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2);
            float halfWidth = handDistance * Mathf.Tan(Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect) * Mathf.Deg2Rad / 2);

            handArcRadius = halfWidth * halfWidth / (2 * arcHeight);
            if (Mathf.Abs(halfWidth / handArcRadius) > 1) handArcRadius = 2 * halfWidth; // ensure it's big enough to keep arcsine from exploding
            
            float toArcCenter = -halfHeight - Mathf.Sqrt(handArcRadius * handArcRadius - halfWidth * halfWidth);
            if (float.IsNaN(toArcCenter)) toArcCenter = 1;

            handArcAngle = 2 * Mathf.Asin(halfWidth / handArcRadius);
            handCenter = camera.transform.TransformPoint(0, toArcCenter - downwardOffset, handDistance);

            viewPlane.x = -halfWidth;
            viewPlane.y = -halfHeight;
            viewPlane.width = 2 * halfWidth;
            viewPlane.height = 2 * halfHeight;

            cardReturnArea = viewPlane;
            cardReturnArea.height = arcHeight + cardReturnHeight;
        }
        
        // Make the hand of cards follow in front of the camera and orient correctly
        handParent.position = handCenter;
        handParent.rotation = Quaternion.LookRotation(-camera.transform.forward);
        
        // Update card particle simulation
        // These are floats to avoid integer division issues
        float numPoints = hand.Count + (hoveredCard >= 0 ? 2 : 0);
        float currentPoint = 0;
        for(int i = 0; i < hand.Count; i++)
        {
            // Find this cards current target position

            float targetRadius = handArcRadius;
            if (i == hoveredCard)
            {
                targetRadius += hoverDistance;
                currentPoint++; // * see below
            }

            float targetPosition; // Percent pos
            if (numPoints == 1) targetPosition = 0.5f;
            else targetPosition = 1 - currentPoint / (numPoints - 1);
            targetPosition = handRange.Lerp(targetPosition); // remap it within the position range of the hand
            
            // Update positions:
            var card = hand[i];
            var posData = card.approx.Update(Time.deltaTime, new[] { targetPosition, targetRadius });
            hand[i] = card;
            
            // Position the transforms:
            float angle = posData[0] * handArcAngle + (Mathf.PI - handArcAngle) / 2;
            var toCard = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);

            Vector3 zFightingMargin = zRange / handRange.Size() * posData[0] * Vector3.forward;
            card.transform.localPosition = posData[1] * toCard + zFightingMargin;
            card.transform.localRotation = Quaternion.LookRotation(Vector3.forward, toCard);

            currentPoint += (i == hoveredCard ? 2 : 1); // * Dont allocate the points around this card to their own cards
        }
        
        UpdateCursor();
    }

    public void UpdateCursor()
    {
        var input = InputHandler.PollCardHandInput();
        
        Vector3 rayDir = PixelPosToRayDirection(input.MousePos);
        
        bool mouseHitTerrain = Physics.Raycast(
            camera.transform.position,
            rayDir,
            out var terrainHit,
            1000f,
            LayerMask.GetMask(new[]{"Ground", "Scenery"}),
            QueryTriggerInteraction.Ignore
        );
        if (mouseHitTerrain)
        {
            cursorPos.position = terrainHit.point;
            cursorPos.rotation = Quaternion.LookRotation(terrainHit.normal, camera.transform.forward);
        }
        
        if (cursorState == CursorState.Free) // If the mouse is free, Check if the mouse is hovering over any cards
        {
            if (cameraController.cursorIsOccupied) // The user is manipulating the camera, we don't want card animations distracting them
            {
                hoveredCard = -1;
                return;
            }
            
            bool mouseHitCard = Physics.Raycast(
                camera.transform.position,
                rayDir,
                out var cardHit,
                1000f, // Can't be kept small, this looks for cards on terrain outside of the hand as well
                LayerMask.GetMask("Card"),
                QueryTriggerInteraction.Ignore
            );
            
            if (mouseHitCard && cardHit.collider.TryGetComponent<Card>(out var card)) // If the mouse found a card
            {
                hoveredCard = hand.FindIndex((other) => { return card == other; }); // Get its index and make it hover ( * -1 if the card isnt in the hand)

                if (input.Interact && !holdingInteract) // If the user clicks when a card is currently beneath the cursor
                {
                    if (hoveredCard == -1) // and the card is not in the hand, pick it up
                    {
                        heldCard = card;
                        heldCard.shouldUpdateCard = true;
                        cursorState = CursorState.CarryingCard;
                    }
                    else // and the card is in the hand, remove it and bring it to the focus point
                    {
                        // Unparent the card and prepare it for controlling itself
                        RemoveHoveredCard();
                        heldCard.approx.prevTarget = new[] { focusPoint.position.x, focusPoint.position.y, focusPoint.position.z };
                    }
                    // The logic later sets the card's target
                }
            }
            else // If the mouse isnt over a card, reset hoveredCard. If the mouse is over a card outside of the hand, this will be -1 already, see above *
            {
                hoveredCard = -1;
            }
        }
        
        if (cursorState == CursorState.CarryingCard)
        {
            // Ignores Z
            Vector2 cursorLocalPos = input.MousePos; // pixels
            cursorLocalPos /= new Vector2(Screen.width, Screen.height); // uv (0-1)
            cursorLocalPos = new( // local
                cursorLocalPos.x * viewPlane.width +  viewPlane.x,
                cursorLocalPos.y * viewPlane.height + viewPlane.y
            );

            gizmoDrawCalls.Clear();
            gizmoDrawCalls.Add(() =>
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(camera.transform.TransformPoint(cursorLocalPos.x, cursorLocalPos.y, handDistance), 0.05f);
            });
            
            bool inCardReturn =
                cardReturnArea.x < cursorLocalPos.x && cursorLocalPos.x < cardReturnArea.xMax
             && cardReturnArea.y < cursorLocalPos.x && cursorLocalPos.y < cardReturnArea.yMax;

            if (inCardReturn) heldCard.SetTarget(focusPoint);
            else if(input.Interact) heldCard.SetTarget(cursorPos);
            
            if (!input.Interact) // If the user releases left click
            {
                if (inCardReturn) AddHeldCard(); // Add it back to the hand
                else // Drop the card and stop tracking it
                {
                    heldCard.shouldUpdateCard = false;
                    heldCard = null;
                    cursorState = CursorState.Free;
                }
            }
        }

        holdingInteract = input.Interact; // Setting up holdingInteract to be true on the next frame
    }
    private Vector3 PixelPosToRayDirection(Vector2 pixelPos)
    {
        Vector2 uvPos = pixelPos / new Vector2(Screen.width, Screen.height);
        Vector3 localPos = new (
            uvPos.x * viewPlane.width + viewPlane.x,
            uvPos.y * viewPlane.height + viewPlane.y,
            handDistance
        );
        Vector3 worldPos = camera.transform.TransformPoint(localPos);
        Vector3 toPlane = worldPos - camera.transform.position;

        return toPlane.normalized;
    }

    void RemoveHoveredCard()
    {
        var card = hand[hoveredCard];
        hand.RemoveAt(hoveredCard);
        card.transform.SetParent(GameObject.Find("EditModeAssets").transform, true);
        card.approx.Initialize(card.transform.position);
        card.rotApprox.Initialize(card.transform.rotation);
        card.ExitHand();
        heldCard = card;
        hoveredCard = -1;
        cursorState = CursorState.CarryingCard;
    }

    void AddHeldCard()
    {
        heldCard.transform.SetParent(handParent, true);
        heldCard.approx.Initialize(new Vector2(0.5f, 0)); // percent angle and radius
        heldCard.shouldFollowTarget = false;
        heldCard.EnterHand();
        hand.Add(heldCard);
        heldCard = null;
        cursorState = CursorState.Free;
    }
    
    private void DrawRectInFrontOfCamera(Rect r)
    {
        Gizmos.DrawLineStrip(new[] {
        camera.transform.TransformPoint(r.xMin, r.yMin, handDistance),
        camera.transform.TransformPoint(r.xMin, r.yMax, handDistance),
        camera.transform.TransformPoint(r.xMax, r.yMax, handDistance),
        camera.transform.TransformPoint(r.xMax, r.yMin, handDistance)
        }, true);
    }
    
    private List<GizmoDrawCall> gizmoDrawCalls = new();
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        float halfHeight = handDistance * Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad / 2);
        float halfWidth = handDistance * Mathf.Tan(Camera.VerticalToHorizontalFieldOfView(camera.fieldOfView, camera.aspect) * Mathf.Deg2Rad / 2);
        float arcRadius = halfWidth * halfWidth / (2 * arcHeight);
        
        float toArcCenter = -halfHeight - Mathf.Sqrt(arcRadius * arcRadius - halfWidth * halfWidth);
        
        float arcAngle = 2 * Mathf.Asin(halfWidth / arcRadius);
        Vector3 arcCenter = camera.transform.TransformPoint(0, toArcCenter - downwardOffset, handDistance);
        
        Gizmos.color = Color.red;
        DrawRectInFrontOfCamera(viewPlane);
        Gizmos.color = Color.blue;
        DrawRectInFrontOfCamera(cardReturnArea);
        
        const int CirclePoints = 10;
        var arcPoints = new Vector3[CirclePoints + 1];
        for (int i = 0; i < CirclePoints; i++)
        {
            float angle = (float)i / (float)(CirclePoints - 1) * arcAngle;
            angle += (Mathf.PI - arcAngle) / 2;

            arcPoints[i] = handParent.TransformPoint(arcRadius * Mathf.Cos(angle), arcRadius * Mathf.Sin(angle), 0);
        }
        arcPoints[^1] = arcCenter;
        
        Gizmos.color = Color.magenta;
        Gizmos.DrawLineStrip(arcPoints, true);

        foreach (var c in gizmoDrawCalls) c();
    }
}
