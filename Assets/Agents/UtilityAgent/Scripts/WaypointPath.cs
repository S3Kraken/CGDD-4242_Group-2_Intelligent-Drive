using System;
using UnityEngine;

public class WaypointPath : MonoBehaviour
{
    [SerializeField] private int nextIndex = 0;
    public Vector3 NextWaypoint => transform.GetChild(nextIndex).position;

    private void Start()
    {
        Debug.Log(NextWaypoint);
    }
}
