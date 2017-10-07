using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {
    
    public Controller2D target;

    [SerializeField]
    private Vector2 focusAreaSize;

    [SerializeField] private float lookAheadDistX;
    [SerializeField] private float verticalOffset;

    [SerializeField] private float verticalSmoothTime;
    [SerializeField] private float lookSmoothTimeX;

    private FocusArea focusArea;

    private float currentLookAheadX;
    private float targetLookAheadX;
    private float lookAheadDirX;
    private float smoothLookVelocityX;

    private float smoothVelocityY;

    private bool lookAheadStopped;

    private void Start() {
        focusArea = new FocusArea(target.collider.bounds, focusAreaSize);
    }

    /* execute after player movement */
    private void LateUpdate() {
        focusArea.update(target.collider.bounds);

        Vector2 focusPosition = focusArea.center + Vector2.up * verticalOffset;

        if (focusArea.velocity.x != 0) {
            lookAheadDirX = Mathf.Sign(focusArea.velocity.x);

            if (Mathf.Sign(target.playerInput.x) == Mathf.Sign(focusArea.velocity.x) 
                && target.playerInput.x != 0) {
                targetLookAheadX = lookAheadDirX * lookAheadDistX;
                lookAheadStopped = false;

            } else {
                if (!lookAheadStopped) {
                    targetLookAheadX = currentLookAheadX + (lookAheadDirX * lookAheadDistX - currentLookAheadX) / 4f;
                    lookAheadStopped = true;
                } 
            }
        }

        focusPosition.y = Mathf.SmoothDamp(transform.position.y, focusPosition.y, ref smoothVelocityY, verticalSmoothTime);

        currentLookAheadX = Mathf.SmoothDamp(currentLookAheadX, targetLookAheadX, ref smoothLookVelocityX, lookSmoothTimeX);
        focusPosition += Vector2.right * currentLookAheadX;
        
        transform.position = (Vector3) focusPosition + Vector3.forward * -10;
    }

    private void OnDrawGizmos() {
        Gizmos.color = new Color(1, 0, 0, .5f);
        Gizmos.DrawCube(focusArea.center, focusAreaSize);
    }

    struct FocusArea {
        public Vector2 center;
        public Vector2 velocity; 

        private float left, right;
        private float top, bottom;

        public FocusArea(Bounds targetBounds, Vector2 size) {
            left = targetBounds.center.x - size.x / 2;
            right = targetBounds.center.x + size.x / 2;

            bottom = targetBounds.min.y;
            top = targetBounds.min.y + size.y;

            velocity = Vector2.zero;
            center = new Vector2((left + right) / 2, (top + bottom) / 2);
        }

        public void update(Bounds targetBounds) {
            /* horizontal movement */
            float xShift = 0;
            if (targetBounds.min.x < left) {
                xShift = targetBounds.min.x - left;
            } else if (targetBounds.max.x > right) {
                xShift = targetBounds.max.x - right;
            }

            left += xShift;
            right += xShift;

            /* vertical movement */
            float yShift = 0;
            if (targetBounds.min.y < bottom) {
                yShift = targetBounds.min.y - bottom;
            } else if (targetBounds.max.y > top) {
                yShift = targetBounds.max.y - top;
            }

            top += yShift;
            bottom += yShift;
            
            /* update center */
            center = new Vector2((left + right) / 2, (top + bottom) / 2);
            velocity = new Vector2(xShift, yShift);
        }
    }

}
