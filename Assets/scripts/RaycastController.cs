using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class RaycastController : MonoBehaviour {

    internal const float skinWidth = .015f;
    private const float distBetweenRays = .25f;

    [SerializeField]
    internal LayerMask collisionMask;

    /* raycasting&collision settings */
    internal int horizontalRayCount;
    internal int verticalRayCount;

    internal float horizontalRaySpacing;
    internal float verticalRaySpacing;

    internal RaycastOrigins raycastOrigins;
    internal new BoxCollider2D collider;
    
    public virtual void Awake() {
        collider = GetComponent<BoxCollider2D>();
    }

    public virtual void Start() {
        calculateRaySpacing();
    }

    internal void updateRaycastOrigins() {
        /* set our boundary */
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
        raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);

        raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
        raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
    }

    internal void calculateRaySpacing() {
        /* set our boundary */
        Bounds bounds = collider.bounds;
        bounds.Expand(skinWidth * -2);

        float boundsWidth = bounds.size.x;
        float boundsHeight = bounds.size.y;

        verticalRayCount = Mathf.RoundToInt(boundsWidth / distBetweenRays);
        horizontalRayCount = Mathf.RoundToInt(boundsHeight/distBetweenRays);

        horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
        verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
    }

    /***
     * Helper structures
     * **/
    internal struct RaycastOrigins {
        public Vector2 topLeft, topRight;
        public Vector2 bottomLeft, bottomRight;
    }
}
