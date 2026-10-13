using System;
using Unity.VisualScripting;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [field: SerializeField]
    public float acceleration { get; private set; } = 20f;
    Rigidbody rb;
    private float steerInput;
    private float throttleInput;

    [field: SerializeField]
    public float turnDegrees { get; private set; } = 90f;
    [Tooltip("Higher values make the car slide less, but move slower.")]

    [field: SerializeField]
    public float grip { get; private set; } = 5f;

    private void FixedUpdate()
    {
        rb.AddForce(transform.forward * (acceleration * throttleInput), ForceMode.Acceleration);
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0, steerInput * turnDegrees * Time.fixedDeltaTime, 0));
        float sidewaysSpeed = Vector3.Dot(rb.linearVelocity, transform.right);
        rb.AddForce(transform.right * -(sidewaysSpeed * grip), ForceMode.Acceleration);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Drive(float steer, float throttle)
    {
        steerInput = steer;
        throttleInput = throttle;
    }

    private void Update()
    {
        //Drive(testSteer, testThrottle);
    }

    public void SetGrip(float num)
    {
        grip = num;
    }

    public void SetAcceleration(float num)
    {
        acceleration = num;
    }

    public void SetTurnDegrees(float degrees)
    {
        turnDegrees = degrees;
    }
}
