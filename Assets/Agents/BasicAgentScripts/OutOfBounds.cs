using UnityEngine;

public class OutOfBounds : MonoBehaviour
{
    private RaceManager raceManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        raceManager = FindFirstObjectByType<RaceManager>();
    }

    private void OnTriggerEnter(Collider other)
    {
        RacingDrivers driver = other.GetComponentInParent<RacingDrivers>();

        if (driver != null)
        {
            raceManager.ResetDriver(driver);
        }
    }
}
