using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class RaceManager : MonoBehaviour
{
    [SerializeField] private Leaderboard leaderboardUI;
    public MonoBehaviour[] driverObjects;

    private List<RacingDrivers> drivers = new List<RacingDrivers>();

    private Dictionary<RacingDrivers, float> driverProgress =
        new Dictionary<RacingDrivers, float>();

    [SerializeField] private int raceLaps = 3;

    public int RaceLaps { get { return raceLaps; } }

    void Start()
    {
        InitializeDrivers();
    }
    void InitializeDrivers()
    {
        foreach (MonoBehaviour driverObject in driverObjects)
        {
            RacingDrivers driver = driverObject as RacingDrivers;

            if (driver != null)
            {
                drivers.Add(driver);
                driverProgress.Add(driver, 0f);
            }
            else
            {
                Debug.LogWarning(driverObject.name + " does not implement RaceDriver.");
            }
        }
    }

    // Continuously receives progress information from all drivers.
    void Update()
    {
        foreach (RacingDrivers driver in drivers)
        {
            ReceiveDriverProgress(driver);
        }

        UpdateLeaderboard();
        leaderboardUI.UpdateLeaderboard(drivers);
    }

    // Receives waypoint, distance, and lap information from a driver.
    void ReceiveDriverProgress(RacingDrivers driver)
    {
        int waypoint = driver.CurrentWaypoint;
        int lap = driver.LapNumber;
        float distance = driver.DistanceToWaypoint;

        // Calculates approximate progress through the race.
        float progress = (lap - 1) * 1000f + waypoint * 10f
                         - distance;

        driverProgress[driver] = progress;
    }

    void UpdateLeaderboard()
    {
        List<RacingDrivers> leaderboard = drivers
        .OrderByDescending(driver =>
            (driver.LapNumber - 1) + driver.RaceProgress)
        .ToList();

        for (int i = 0; i < leaderboard.Count; i++)
        {
            Debug.Log((i + 1) + ". " +
                      leaderboard[i].DriverTransform.name);
            Debug.Log("1st Place: " + leaderboard[0].DriverTransform.name);
        }
    }

    public void ShareHotspotInformation()
    {
        foreach (RacingDrivers driver in drivers)
        {
            driver.ReceiveHotspotInformation();
        }
    }
    public void ResetDriver(RacingDrivers driver)
    {
        if (driver != null)
        {
            driver.ResetDriver();
        }
    }
}