using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ButtonInteraction : MonoBehaviour
{
    public UnityEvent buttonPressed;

    private void OnTriggerEnter(Collider other)
    {
        buttonPressed.Invoke();
    }
}
