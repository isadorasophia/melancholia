using UnityEngine;
using System.Collections;

public class Controller2D : RaycastController {

    public CollisionInfo collisions;

    [SerializeField]
    private float maxSlopeAngle = 80;

    [HideInInspector]
    public Vector2 playerInput;

    public override void Start() {
        base.Start();

        collisions.faceDirection = 1;
    }

    public void move(Vector2 moveAmount, bool standingOnPlatform) {
        move(moveAmount, Vector2.zero, standingOnPlatform);
    }

    public void move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false) {
        /* update our settings according to our physics */
        updateRaycastOrigins();

        collisions.reset();
        collisions.moveAmountOld = moveAmount;
        playerInput = input;
        
        /* are we descending? */
        if (moveAmount.y < 0) {
            descendSlope(ref moveAmount);
        }

        if (moveAmount.x != 0) {
            collisions.faceDirection = (int) Mathf.Sign(moveAmount.x);
        }

        /* check for any collisions, if appropriate */
        horizontalCollisions(ref moveAmount);

        if (moveAmount.y != 0) {
            verticalCollisions(ref moveAmount);
        }

        /* finally, move ourselves! */
        transform.Translate(moveAmount);

        if (standingOnPlatform) {
            collisions.below = true;
        }
    }

    /**
     * <summary>
     * Limit our movement by checking for any horizontal collisions
     * </summary>
     * */
    void horizontalCollisions(ref Vector2 moveAmount) {
        /* current moveAmount status */
        float xDirection = collisions.faceDirection;
        float length = Mathf.Abs(moveAmount.x) + skinWidth;

        if (Mathf.Abs(moveAmount.x) < skinWidth) {
            length = skinWidth * 2;
        }

        /* detect a possible collision for each of our rays */
        for (int i = 0; i < horizontalRayCount; i++) {
            /* draw our ray collision reach */
            Vector2 rayOrigin = xDirection == -1 ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
            rayOrigin += Vector2.up * (horizontalRaySpacing * i);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * xDirection, length, collisionMask);
            Debug.DrawRay(rayOrigin, Vector2.right * xDirection, Color.red);

            /* did we hit something? */
            if (hit) {
                
                if (hit.distance == 0) {
                    continue;
                }

                /* are we in a valid slope? */
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (i == 0 && slopeAngle <= maxSlopeAngle) {
                    /* we are actually climbing */
                    if (collisions.descendingSlope) {
                        collisions.descendingSlope = false;
                        moveAmount = collisions.moveAmountOld;
                    }

                    float distanceToSlopeStart = 0;

                    /* is this a new slope? set our collision distance straight */
                    if (slopeAngle != collisions.slopeAngleOld) {
                        distanceToSlopeStart = hit.distance - skinWidth;
                        moveAmount.x -= distanceToSlopeStart * xDirection;
                    }

                    climbSlope(ref moveAmount, slopeAngle, hit.normal);
                    
                    /* reset out collision distance */
                    moveAmount.x += distanceToSlopeStart * xDirection;
                }

                /* we have a legit collision */
                if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle) {
                    moveAmount.x = (hit.distance - skinWidth) * xDirection;
                    length = hit.distance;

                    /* if we were climbing, correct our y moveAmount */
                    if (collisions.climbingSlope) {
                        moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
                    }

                    collisions.left = xDirection == -1;
                    collisions.right = xDirection == 1;
                }
            }
        }
    }

    /**
     * <summary>
     * Limit our movement by checking for any vertical collisions
     * </summary>
     * */
    void verticalCollisions(ref Vector2 moveAmount) {
        /* current moveAmount status */
        float yDirection = Mathf.Sign(moveAmount.y);
        float length = Mathf.Abs(moveAmount.y) + skinWidth;

        /* detect a possible collision for each of our rays */
        for (int i = 0; i < verticalRayCount; i++) {
            /* draw our ray collision reach */
            Vector2 rayOrigin = yDirection == -1 ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);

            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * yDirection, length, collisionMask);
            Debug.DrawRay(rayOrigin, Vector2.up * yDirection, Color.red);

            /* did we (legit) hit something? */
            if (hit) {
                /* can we pass it? */
                if (hit.collider.tag == "Through") {
                    /* moving up - ignore it */
                    if (yDirection == 1 || hit.distance == 0) {
                        continue;
                    }

                    if (collisions.fallingThroughPlatform) {
                        continue;
                    }

                    if (playerInput.y == -1) {
                        collisions.fallingThroughPlatform = true;
                        
                        /* wait for half a second to reset */
                        Invoke("resetFallingThroughPlatform", .25f);
                        continue;
                    }
                }

                moveAmount.y = (hit.distance - skinWidth) * yDirection;
                length = hit.distance;

                /* if we were climbing, correct our x moveAmount */
                if (collisions.climbingSlope) {
                    moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);
                }

                collisions.below = yDirection == -1;
                collisions.above = yDirection == 1;
            }
        }

        /* if we were climbing, fix our x moveAmount */
        if (collisions.climbingSlope) {
            /* current x moveAmount status */
            float xDirection = Mathf.Sign(moveAmount.x);
            length = Mathf.Abs(moveAmount.x) + skinWidth;

            Vector2 rayOrigin = (xDirection == -1? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * moveAmount.y;
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin,Vector2.right * xDirection,length,collisionMask);

            if (hit) {
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

                /* if we were climbing something else, fix our x moveAmount to stop */
                if (slopeAngle != collisions.slopeAngle) {
                    moveAmount.x = (hit.distance - skinWidth) * xDirection;

                    collisions.slopeAngle = slopeAngle;
                    collisions.slopeNormal = hit.normal;
                }
            }
        }
    }

    /**
     * <summary>
     * Adjust moveAmount to climb a slope
     * </summary>
     * */
    void climbSlope(ref Vector2 moveAmount, float slopeAngle, Vector2 sloperNormal) {
        /* current moveAmount status */
        float moveDistance = Mathf.Abs(moveAmount.x);
        float yVelocity = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

        /* if we are not jumping */
        if (moveAmount.y <= yVelocity) {
            moveAmount.y = yVelocity;
            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);

            /* set status */
            collisions.below = true;
            collisions.climbingSlope = true;
            collisions.slopeAngle = slopeAngle;
            collisions.slopeNormal = sloperNormal;
        }
    }

    /**
     * <summary>
     * Adjust moveAmount to descend a slope
     * </summary>
     * */
    void descendSlope(ref Vector2 moveAmount) {

        /* deal with falls */
        RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast (raycastOrigins.bottomLeft, Vector2.down, 
            Mathf.Abs (moveAmount.y) + skinWidth, collisionMask);
        RaycastHit2D maxSlopeHitRight = Physics2D.Raycast (raycastOrigins.bottomRight, Vector2.down, 
            Mathf.Abs (moveAmount.y) + skinWidth, collisionMask);

        if (maxSlopeHitLeft ^ maxSlopeHitRight) {
            slideDownMaxSlope(maxSlopeHitLeft, ref moveAmount);
            slideDownMaxSlope(maxSlopeHitRight, ref moveAmount);
        }

        /* if we aren't sliding down... */
        if (!collisions.slidingDownMaxSlope) {
            /* current moveAmount status */
            float xDirection = Mathf.Sign(moveAmount.x);

            /* draw hit ray from below - as we may not detect slope from vertical movement */
            Vector2 rayOrigin = xDirection == -1 ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
            RaycastHit2D hit = Physics2D.Raycast (rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

            if (hit) {
                /* do we have a valid slope? */
                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle) {
                    /* if we are descending */
                    if (Mathf.Sign(hit.normal.x) == xDirection) {
                        if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x)) {
                            /* set our velocities accordingly */
                            float moveDistance = Mathf.Abs(moveAmount.x);
                            moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
                            moveAmount.y -= Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

                            collisions.slopeAngle = slopeAngle;
                            collisions.descendingSlope = true;
                            collisions.below = true;
                            collisions.slopeNormal = hit.normal;
                        }
                    }
                }
            }
        }
    }

    void slideDownMaxSlope(RaycastHit2D hit, ref Vector2 moveAmount) {
        if (hit) {
            float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);

            if (slopeAngle > maxSlopeAngle) {
                moveAmount.x = Mathf.Sign(hit.normal.x) * (Mathf.Abs(moveAmount.y) - hit.distance) / 
                    Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

                collisions.slopeAngle = slopeAngle;
                collisions.slidingDownMaxSlope = true;
                collisions.slopeNormal = hit.normal;
            }
        }
    }

    void resetFallingThroughPlatform() {
        collisions.fallingThroughPlatform = false;
    }

    /***
     * Helper structures
     * **/
    public struct CollisionInfo {
        public bool above, below;
        public bool left, right;

        public bool climbingSlope;
        public bool descendingSlope;
        public bool slidingDownMaxSlope;

        public float slopeAngle, slopeAngleOld;
        public Vector2 slopeNormal;
        public Vector2 moveAmountOld;

        public int faceDirection;
        public bool fallingThroughPlatform;

        public void reset() {
            above = below = false;
            left = right = false;

            climbingSlope = descendingSlope = false;
            slidingDownMaxSlope = false;

            slopeNormal = Vector2.zero;

            slopeAngleOld = slopeAngle;
            slopeAngle = 0;
        }
    }

}
