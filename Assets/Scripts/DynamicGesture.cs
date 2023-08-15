using System.Collections.Generic;
using UnityEngine;

//Scriptable object containing a series of StaticGesture, and the time to execute them for the detection to be valid
[CreateAssetMenu(fileName = "DynamicGesture", menuName = "ScriptableObjects/DynamicGesture", order = 2)]
public class DynamicGesture : Gesture
{
    [SerializeField]
    public List<StaticGesture> orderedKeyFrames;
    public float execTime;
}
