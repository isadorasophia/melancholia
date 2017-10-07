using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : RaycastController {

    [SerializeField]
    private LayerMask passengerMask;

    [SerializeField]
    private float speed;

    [SerializeField]
    [Range(0, 2)]
    private float easeAmount;

    [SerializeField]
    private bool cyclic;

    [SerializeField]
    private float waitTime;

    private int fromWaypointIndex;
    private float percentBetweenWaypoints;
    private float nextMoveTime;

    [SerializeField]
    private Vector3[] localWaypoints;
    private Vector3[] globalWaypoints;

    private List<PassengerMovement> passengerMovement;
    private Dictionary<Transform, Controller2D> passengerDictionary;

    public override void Start() {
        base.Start();

        passengerDictionary = new Dictionary<Transform, Controller2D>();
        globalWaypoints = new Vector3[localWaypoints.Length];

        /* set our global waypoints */
        for (int i = 0; i < localWaypoints.Length; ++i) {
            globalWaypoints[i] = localWaypoints[i] + transform.position;
        }
    }

    void Update() {
        updateRaycastOrigins();

        Vector3 velocity = calculatePlatformMovement();
        calculatePassengerMovement(velocity);

        movePassengers(true);
        transform.Translate(velocity);
        movePassengers(false);
    }

    Vector3 calculatePlatformMovement() {
        /* are we at a wait time? */
        if (Time.time < nextMoveTime || globalWaypoints.Length == 0) {
            return Vector3.zero;
        }

        /* dont let we overflow */
        fromWaypointIndex %= globalWaypoints.Length;
        int toWaypointIndex = (fromWaypointIndex + 1) % globalWaypoints.Length;

        float distanceBetweenWaypoints = Vector3.Distance(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex]);
        percentBetweenWaypoints += Time.deltaTime * speed/distanceBetweenWaypoints;
        percentBetweenWaypoints = Mathf.Clamp01(percentBetweenWaypoints);

        float easedPercentBetweenWaypoints = ease(percentBetweenWaypoints);

        Vector3 newPos = Vector3.Lerp(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex], easedPercentBetweenWaypoints);

        if (percentBetweenWaypoints >= 1) {
            percentBetweenWaypoints = 0;
            ++fromWaypointIndex;

            if (!cyclic) {
                if (fromWaypointIndex >= globalWaypoints.Length - 1) {
                    fromWaypointIndex = 0;
                    System.Array.Reverse(globalWaypoints);
                }
            }

            nextMoveTime = Time.time + waitTime;
        }

        return newPos - transform.position;
    }

    void movePassengers(bool beforeMovePlatform) {
        foreach (PassengerMovement passenger in passengerMovement) {
            if (!passengerDictionary.ContainsKey(passenger.transform)) {
                passengerDictionary.Add(passenger.transform, 
                    passenger.transform.GetComponent<Controller2D>());
            }

            if (passenger.moveBeforePlatform == beforeMovePlatform) {
                passengerDictionary[passenger.transform].move(passenger.velocity, passenger.standingOnPlatform);
            }
        }
    }

    void calculatePassengerMovement(Vector3 velocity) {

        HashSet<Transform> movePassangers = new HashSet<Transform>();
        passengerMovement = new List<PassengerMovement>();

        float xDirection = Mathf.Sign(velocity.x);
        float yDirection = Mathf.Sign(velocity.y);

        /* vertical moving platform */
        if (velocity.y > 0) {
            float length = Mathf.Abs(velocity.y) + skinWidth;

            /* detect if we have any passenger */
            for (int i = 0; i < verticalRayCount; i++) {
                Vector2 rayOrigin = yDirection == -1 ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
                rayOrigin += Vector2.right * verticalRaySpacing * i;

                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * yDirection, length, passengerMask);

                if (hit && hit.distance != 0) {
                    if (!movePassangers.Contains(hit.transform)) {
                        movePassangers.Add(hit.transform);

                        float xPush = yDirection == 1 ? velocity.x : 0;
                        float yPush = velocity.y - (hit.distance - skinWidth) * yDirection;

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(xPush, yPush), 
                            yDirection == 1, true));
                    }
                }
            }
        }

        /* horizontal moving platform */
        if (velocity.x != 0) {
            float length = Mathf.Abs(velocity.x) + skinWidth;

            /* detect if we hit any passenger */
            for (int i = 0; i < horizontalRayCount; i++) {
                Vector2 rayOrigin = xDirection == -1 ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
                rayOrigin += Vector2.up * (horizontalRaySpacing * i);

                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * xDirection, length, passengerMask);

                if (hit && hit.distance != 0) {
                    if (!movePassangers.Contains(hit.transform)) {
                        movePassangers.Add(hit.transform);

                        float xPush = velocity.x - (hit.distance - skinWidth) * xDirection;
                        float yPush = -skinWidth;

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(xPush, yPush),
                            false, true));
                    }
                }
            }
        }

        /* passenger on top of a horizontally or downward moving platform */
        if (yDirection == -1 || velocity.y == 0 && velocity.x != 0) {
            float length = skinWidth * 2;

            /* detect if we have any passenger */
            for (int i = 0; i < verticalRayCount; i++) {
                Vector2 rayOrigin = raycastOrigins.topLeft + Vector2.right * verticalRaySpacing * i;
                RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up, length, passengerMask);

                if (hit && hit.distance != 0) {
                    if (!movePassangers.Contains(hit.transform)) {
                        movePassangers.Add(hit.transform);

                        float xPush = velocity.x;
                        float yPush = velocity.y;

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(xPush, yPush),
                            true, false));
                    }
                }
            }
        }
    }
    
    /**
     * <summary>
     * Ease an amount of movement based on a percentage x
     * </summary>
     * */
    float ease(float x) {
        float a = easeAmount+1;
        return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
    }

    private void OnDrawGizmos() {
        if (localWaypoints != null) {
            Gizmos.color = Color.green;
            float size = .3f;

            for (int i = 0; i < localWaypoints.Length; ++i) {
                /* only move waypoints if we are editing our settings */
                Vector3 globalWaypointPos = Application.isPlaying ? globalWaypoints[i] : localWaypoints[i] + transform.position;

                Gizmos.DrawLine(globalWaypointPos - Vector3.up * size, globalWaypointPos + Vector3.up * size);
                Gizmos.DrawLine(globalWaypointPos - Vector3.left * size, globalWaypointPos + Vector3.left * size);
            }
        }
    }

    /***
     * Helper structures
     * **/
    struct PassengerMovement {
        public Transform transform;
        public Vector3 velocity;

        public bool standingOnPlatform;
        public bool moveBeforePlatform;

        public PassengerMovement(Transform _transform, Vector3 _velocity, 
            bool _standingOnPlatform, bool _moveBeforePlatform) {
            transform = _transform;
            velocity = _velocity;
            standingOnPlatform = _standingOnPlatform;
            moveBeforePlatform = _moveBeforePlatform;
        }
    }
}
