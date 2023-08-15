using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerAreaHandler : MonoBehaviour
{
    public UnityEvent areaEntered;
    public UnityEvent areaExited;

    private void OnTriggerEnter(Collider other)
    {
        areaEntered.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        areaExited.Invoke();
    }
}
