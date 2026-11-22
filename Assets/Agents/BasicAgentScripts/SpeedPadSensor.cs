using UnityEngine;

public class SpeedPadSensor : MonoBehaviour
{
    private RaceManager raceManager;
    private SpeedPad pad;

    public bool IsAvailable
    {
        get { return pad != null && pad.IsAvailable; }
    }

    void Awake()
    {
        pad = GetComponent<SpeedPad>();
    }

    void Start()
    {
        raceManager = FindFirstObjectByType<RaceManager>();
    }

    //SpeedPad calls this when a car picks up the boost
    public void PadCollected(CarController car)
    {
        if (raceManager == null)
        {
            return;
        }

        RacingDrivers driver = car.GetComponent<RacingDrivers>();

        if (driver != null)
        {
            raceManager.ReportPadCollected(pad, driver);
        }
    }
}
