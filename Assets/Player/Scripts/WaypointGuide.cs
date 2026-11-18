using UnityEngine;

public class WaypointGuide : MonoBehaviour
{
    private WaypointPath waypointPath;
    private PlayerMovement player;
    [SerializeField] private float rotationSpeed = 10f;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        waypointPath = FindFirstObjectByType<WaypointPath>();
        player = FindFirstObjectByType<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        if (waypointPath != null)
        {
            Transform target = waypointPath.GetCurrentWaypoint(player.CurrentWaypointIndex);

            if (target != null)
            {
                Vector3 direction = target.position - transform.position;
                direction.y = 0f;

                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    Quaternion finalRotation = targetRotation * Quaternion.Euler(0f, 90f, 90f);
                    transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, rotationSpeed * Time.deltaTime);
                }
            }

        }
    }
}
