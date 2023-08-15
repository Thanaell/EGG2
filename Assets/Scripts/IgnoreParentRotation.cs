using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IgnoreParentRotation : MonoBehaviour
{
    private HAND_STATE currentState;
    Quaternion previousRotation;
    // Start is called before the first frame update
    void Start()
    {
        currentState=HAND_STATE.TRACKING;
        previousRotation=this.transform.rotation;
    }

    public void updateState(HAND_STATE state){
        currentState=state;
        Debug.Log(state);
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState==HAND_STATE.ANIM){
             transform.rotation = previousRotation;
        }        
        previousRotation=this.transform.rotation;
    }
    
}
