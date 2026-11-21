using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class Leaderboard : MonoBehaviour
{
        [SerializeField] private TMP_Text[] leaderboardSlots;

        public void UpdateLeaderboard(List<RacingDrivers> drivers)
        {
            List<RacingDrivers> leaderboard = drivers.OrderByDescending(driver =>(driver.LapNumber - 1) + driver.RaceProgress).ToList();

           
            for (int i = 0; i < leaderboardSlots.Length; i++)
            {
                if (i < leaderboard.Count)
                {
                    RacingDrivers driver = leaderboard[i];

                    string driverName = driver.DriverTransform.name;

                    leaderboardSlots[i].text =
                        $"{i + 1}. {driverName} ";
                }
                else
                {
                    leaderboardSlots[i].text = $"{i + 1}";
                }
            }
        }
    }
