using UnityEngine;

public interface RacingDrivers 
{
    float RaceProgress { get; }
    int CurrentWaypoint { get; }
        int LapNumber { get; }
        float DistanceToWaypoint { get; }

        Transform DriverTransform { get; }
    void ReceiveHotspotInformation();
    void ResetDriver();

}
