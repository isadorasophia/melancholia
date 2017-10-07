using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Player))]
public class PlayerInput : MonoBehaviour {

    private Player player;

    private void Start () {
        player = GetComponent<Player>();
    }

    private void Update() {
        Vector2 directionalInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        player.setDirectionalInput(directionalInput);

        if (Input.GetButtonDown("Jump")) {
            player.onJumpInputDown();
        }

        if (Input.GetButtonUp("Jump")) {
            player.onJumpInputUp();
        }
    }
}
