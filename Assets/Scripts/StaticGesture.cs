using System.Collections.Generic;
using UnityEngine;

// Scriptable object storing poses of hand and fingers
[CreateAssetMenu(fileName = "StaticGesture", menuName = "ScriptableObjects/StaticGesture", order = 1)]
public class StaticGesture : Gesture
{
    [SerializeField]
    public List<Vector3> fingerDatas;
    public float threshold = 0.1f;
}
