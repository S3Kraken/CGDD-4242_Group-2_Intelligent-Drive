using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    [SerializeField] private int nextIndex = 1;
    [SerializeField] private int currentIndex = 0;
    private List<Transform> waypoints = new List<Transform>();

    public Vector3 NextWaypoint => transform.GetChild(nextIndex).position;

    private void Start()
    {
        Debug.Log(NextWaypoint);
        foreach(Transform child in transform)
        {
            waypoints.Add(child.transform);
        }
    }
    public Transform GetCurrentWaypoint()
    {
            return waypoints[currentIndex];
    }
    public void WaypointReached()
    {
        currentIndex = nextIndex;
        nextIndex++;
        if (nextIndex >= waypoints.Count)
        {
            nextIndex = 0;
        }
    }
}
