using System;
using Unity.VisualScripting;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [SerializeField] private float acceleration = 20f;
    Rigidbody rb;
    private float steerInput;
    private float throttleInput;

    [SerializeField] private float testSteer;
    [SerializeField] private float testThrottle;
    [SerializeField] private float turnDegrees = 90f;
    [Tooltip("Higher values make the car slide less, but move slower.")]
    [SerializeField] private float grip = 5f;

    private void FixedUpdate()
    {
        rb.AddForce(transform.forward * (acceleration * throttleInput), ForceMode.Acceleration);
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0,steerInput * turnDegrees * Time.fixedDeltaTime, 0));
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

    public void SetGrip(int num)
    {
        grip = num;
    }

    public void SetAcceleration(int num)
    {
        acceleration = num;
    }

    public void SetTurnDegrees(int degrees)
    {
        turnDegrees = degrees;
    }
}
