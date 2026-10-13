using System.Collections;
using UnityEngine;

public class SpeedPad : MonoBehaviour
{
    [SerializeField] private float vBoost = 60f;    //Speed while boosted, m/s
    [SerializeField] private float tBoost = 2f;     //Boost duration, seconds
    [SerializeField] private float cooldown = 8f;   //Seconds before the pad works again

    public float VBoost => vBoost;
    public float TBoost => tBoost;
    public bool IsAvailable { get; private set; } = true;

    private Renderer rend;

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        SetColor(Color.green);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsAvailable)
        {
            return;
        }

        CarController car = other.GetComponent<CarController>();
        if (car == null)
        {
            return;
        }

        car.ApplyBoost(vBoost, tBoost);

        UtilityAgent agent = other.GetComponent<UtilityAgent>();
        if (agent != null)
        {
            agent.OnPadCollected(this);
        }
        //Debug.Log("SPEED!");
        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        IsAvailable = false;
        SetColor(Color.gray);
        yield return new WaitForSeconds(cooldown);
        IsAvailable = true;
        SetColor(Color.green);
    }

    private void SetColor(Color c)
    {
        if (rend != null)
        {
            rend.material.color = c;
        }
    }
}