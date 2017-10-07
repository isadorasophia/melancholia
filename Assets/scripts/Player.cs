using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Controller2D))]
public class Player : MonoBehaviour {

    /* Jump properties */
    [SerializeField] private float maxJumpHeight = 3.5f;
    [SerializeField] private float minJumpHeight = 1;

    [SerializeField]
    private float timeToJumpApex = .4f;

    /* Wall jump settings */
    [SerializeField] private Vector2 wallJumpClimb;
    [SerializeField] private Vector2 wallJumpOff;
    [SerializeField] private Vector2 wallLeap;

    [SerializeField] private float wallStickTime = .25f;
    private float timeToWallUnstick;

    [SerializeField]
    private float wallSlideMaxSpeed = 3;

    private float accelarationTimeAirborne = .2f;
    private float accelarationTimeGrounded = .1f;
    private float speed = 6;
    
    private float xVelocitySmoothing;

    /* Settings values */
    private float gravity;
    private float maxJumpVelocity;
    private float minJumpVelocity;
    
    private Vector2 velocity;

    private bool wallSliding;
    private int xWallDir;
    
    private Controller2D controller;
    private Vector2 directionalInput;

    void Start () {
        controller = GetComponent<Controller2D>();

        /* jump height = initial velocity * time + (accelaration * time^2) / 2
        /*  -> accelaration = jump height * 2 / time^2 */
        gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        maxJumpVelocity = Mathf.Abs(gravity * timeToJumpApex);

        /* vf^2 = 2 * acc * displacement */
        minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
    }

    void Update() {
        calculateVelocity();
        handleWallSliding();

        controller.move(velocity * Time.deltaTime, directionalInput);

        /* if we have anything in our way, nevermind */
        if (controller.collisions.above || controller.collisions.below) {
            if (controller.collisions.slidingDownMaxSlope) {
                velocity.y += controller.collisions.slopeNormal.y * -gravity * Time.deltaTime;
            } else {
                velocity.y = 0;
            }
        }
    }

    public void setDirectionalInput(Vector2 input) {
        directionalInput = input;
    }

    public void onJumpInputDown() {
        if (wallSliding) {
            if (xWallDir == directionalInput.x) {
                /* move on the same direction */
                velocity.x = -xWallDir * wallJumpClimb.x;
                velocity.y = wallJumpClimb.y;
            } else if (directionalInput.x == 0) {
                /* we just want to get rid of the wall */
                velocity.x = -xWallDir * wallJumpOff.x;
                velocity.y = wallJumpOff.y;
            } else {
                velocity.x = -xWallDir * wallLeap.x;
                velocity.y = wallLeap.y;
            }
        }

        if (controller.collisions.below) {
            if (controller.collisions.slidingDownMaxSlope) {
                /* not jumping against max slope */
                if (directionalInput.x != -Mathf.Sign(controller.collisions.slopeNormal.x)) {
                    velocity.x = maxJumpVelocity * controller.collisions.slopeNormal.x;
                    velocity.y = maxJumpVelocity * controller.collisions.slopeNormal.y;
                } else {
                    velocity.x = controller.collisions.slopeNormal.x * wallJumpClimb.x;
                    velocity.y = wallJumpClimb.y;
                }
            } else {
                velocity.y = maxJumpVelocity;
            }
        }
    }

    public void onJumpInputUp() {
        velocity.y = Mathf.Min(velocity.y, minJumpVelocity);
    }

    private void calculateVelocity() {
        float xTargetVelocity = directionalInput.x * speed;

        velocity.x = Mathf.SmoothDamp(velocity.x, xTargetVelocity, ref xVelocitySmoothing,
            controller.collisions.below ? accelarationTimeGrounded : accelarationTimeAirborne);
        velocity.y += gravity * Time.deltaTime;
    }

    private void handleWallSliding() {
        xWallDir = controller.collisions.left ? -1 : 1;
        wallSliding = false;

        if ((controller.collisions.left || controller.collisions.right) && !controller.collisions.below && velocity.y < 0) {
            wallSliding = true;

            velocity.y = Mathf.Max(velocity.y, -wallSlideMaxSpeed);

            if (timeToWallUnstick > 0) {
                xVelocitySmoothing = 0;
                velocity.x = 0;

                if (directionalInput.x != xWallDir && directionalInput.x != 0) {
                    timeToWallUnstick -= Time.deltaTime;
                } else {
                    timeToWallUnstick = wallStickTime;
                }
            } else {
                timeToWallUnstick = wallStickTime;
            }
        }
    }
}
