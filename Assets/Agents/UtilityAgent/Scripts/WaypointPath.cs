using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    [SerializeField] private int nextIndex = 1;
    [SerializeField] private int currentIndex = 0;
    //[SerializeField] private GameObject drivers;
    //[SerializeField] private List<CarController> driversList = new List<CarController>();
    private List<Transform> waypoints = new List<Transform>();


    public Vector3 NextWaypoint => transform.GetChild(nextIndex).position;

    public int CurrentIndex { get { return currentIndex; } }
    public int NextIndex { get { return nextIndex; } }


    private void Start()
    {
        Debug.Log(NextWaypoint);
        foreach (Transform child in transform)
        {
            waypoints.Add(child.transform);
        }
        //foreach(GameObject child in drivers.transform)
        //{
        //    driversList.Add(child.GetComponent<CarController>());
        //}
    }
    public Transform GetCurrentWaypoint(int currentIndex)
    {
        return waypoints[currentIndex];
    }
    public void WaypointReached(ref int currentIndex, ref int nextIndex)
    {
        currentIndex = nextIndex;
        nextIndex++;
        if (nextIndex >= waypoints.Count)
        {
            nextIndex = 0;
        }
    }
}
