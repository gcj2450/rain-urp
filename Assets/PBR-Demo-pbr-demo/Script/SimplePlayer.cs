﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerState
{
    Ground,
    Air,
    Water,
}
public class SimplePlayer : MonoBehaviour {
    public float MaxMoveSpeed = 10;
    public float GroundAcceleration = 10;
    public float AirAcceleration = 5;
    public float AimSensity = 0.1f;
    public float JumpHeight = 1.5f;
    public float JumpTime = 1;
    public float WallJumpVelocity = 1;
    Vector2 targetVelocity;
    Vector2 velocity;
    public bool WallContact = false;
    public Vector3 WallContactNormal;
    public PlayerState State = PlayerState.Ground;

	// Use this for initialization
	void Start () {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnEnable()
    {
    }

    // Update is called once per frame
    void Update () {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if(Cursor.lockState == CursorLockMode.Locked)
        {
            var camera = transform.Find("MainCamera");
            Vector2 aim = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            aim.Scale(new Vector2(AimSensity, -AimSensity));
            transform.Rotate(0, aim.x, 0, Space.Self);
            camera.Rotate(aim.y, 0, 0, Space.Self);
            camera.localEulerAngles = new Vector3(camera.localEulerAngles.x, camera.localEulerAngles.y, camera.localEulerAngles.z);
        }

        float jumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(Physics.gravity.y) * JumpHeight);
        if(this.State == PlayerState.Ground)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                var rigidbody = GetComponent<Rigidbody>();
                rigidbody.velocity = new Vector3(rigidbody.velocity.x, jumpVelocity, rigidbody.velocity.z);
                State = PlayerState.Air;
            }
        }
        else if (this.State == PlayerState.Air)
        {
            if(WallContact && Input.GetKeyDown(KeyCode.Space))
            {
                var jumpDir = (Vector3.up + WallContactNormal.normalized).normalized;
                var v = jumpDir * WallJumpVelocity;
                velocity = v.ToVector2XZ();
                GetComponent<Rigidbody>().velocity = velocity.ToVector3XZ(v.y);
            }
        }
    }
    

    private void FixedUpdate()
    {
        WallContact = false;
        WallContactNormal = Vector3.zero;
        if(JumpTime != 0 && JumpTime!=float.NaN)
        {
            float g = 2 * JumpHeight / Mathf.Pow(JumpTime / 2, 2);
            Physics.gravity = Vector3.down * g;
        }
        if (this.State== PlayerState.Ground)
        {
            Vector2 movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            var worldMovement = transform.localToWorldMatrix.MultiplyVector(movement.ToVector3XZ()).ToVector2XZ();
            targetVelocity = worldMovement.normalized * MaxMoveSpeed;

            var rigidbody = GetComponent<Rigidbody>();
            var dv = targetVelocity - velocity;
            var acc = dv / Time.fixedDeltaTime;
            acc = Mathf.Clamp(acc.magnitude, 0, GroundAcceleration) * acc.normalized;
            velocity += acc * Time.fixedDeltaTime;
            rigidbody.velocity = velocity.ToVector3XZ(rigidbody.velocity.y);
            //rigidbody.AddForce(acc.ToVector3XZ() * Time.fixedDeltaTime, ForceMode.VelocityChange);
        }
        else if (this.State == PlayerState.Air)
        {
            Vector2 movement = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            var worldMovement = transform.localToWorldMatrix.MultiplyVector(movement.ToVector3XZ()).ToVector2XZ();
            targetVelocity = worldMovement.normalized * MaxMoveSpeed;

            var rigidbody = GetComponent<Rigidbody>();
            var dv = targetVelocity - velocity;
            var acc = dv / Time.fixedDeltaTime;
            acc = Mathf.Clamp(acc.magnitude, 0, AirAcceleration) * acc.normalized;
            velocity += acc * Time.fixedDeltaTime;
            rigidbody.velocity = velocity.ToVector3XZ(rigidbody.velocity.y);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        WallContact = true;
    }

    private void OnCollisionStay(Collision collision)
    {
        foreach(var contract in collision.contacts)
        {
            if(contract.thisCollider.gameObject.name == "Body")
            {
                WallContact = true;
                WallContactNormal = contract.normal;
                Debug.DrawLine(contract.point, contract.point + 5 * contract.normal, Color.blue);
            }
            if (contract.thisCollider.gameObject.name == "Foot" && collision.impulse.y > 0)
            {
                State = PlayerState.Ground;
            }
        }
    }
}
