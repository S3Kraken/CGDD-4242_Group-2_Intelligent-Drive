using System;
using Unity.VisualScripting;
using UnityEngine;

public class CarController : MonoBehaviour
{
    [SerializeField] private float acceleration = 20f;
    Rigidbody rb;
    private float steerInput;
    private float throttleInput;
    private float boostMultiplier = 1.0f;
    private float boostTimer;

    [SerializeField] private float turnDegrees = 90f;
    [SerializeField] private float grip = 5f;

    //Top speed with no boost (acceleration balanced by linear damping)
    public float CruiseSpeed => acceleration / rb.linearDamping;
    //Turning speed at full steering, in radians per second
    public float TurnRate => turnDegrees * Mathf.Deg2Rad;

    private void FixedUpdate()
    {
        float currentAcceleration = acceleration;
        if (boostTimer > 0f)
        {
            currentAcceleration *= boostMultiplier;
            boostTimer -= Time.fixedDeltaTime;
        }

        rb.AddForce(transform.forward * (currentAcceleration * throttleInput), ForceMode.Acceleration);
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0, steerInput * turnDegrees * Time.fixedDeltaTime, 0));

        float sidewaysSpeed = Vector3.Dot(rb.linearVelocity, transform.right);
        rb.AddForce(transform.right * -(sidewaysSpeed * grip), ForceMode.Acceleration);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        //Debug.Log(CruiseSpeed);
    }

    public void Drive(float steer, float throttle)
    {
        steerInput = steer;
        throttleInput = throttle;
    }

    public void ApplyBoost(float vBoost, float duration)
    {
        boostMultiplier = vBoost / CruiseSpeed;
        boostTimer = duration;
    }
}
