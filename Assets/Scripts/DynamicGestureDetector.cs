using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Class handling detection of dynamic gestures
public class DynamicGestureDetector : MonoBehaviour
{
    public List<DynamicGesture> dynamicGestures;
    public Dictionary<DynamicGesture, float> runningTimers;
    public Dictionary<DynamicGesture, int> reachedKeyFrame;

    public UnityEvent<DynamicGesture> onRecognized;
    // Start is called before the first frame update
    void Start()
    {
        ResetAllDetections();   
    }

    // Update is called once per frame
    void Update()
    {
        List<DynamicGesture> timersToRemove = new List<DynamicGesture>();
        List<DynamicGesture> timersToAdvance = new List<DynamicGesture>();
        foreach (var timer in runningTimers)
        {
            if (timer.Value > timer.Key.execTime)
            {
                timersToRemove.Add(timer.Key); //marking expired timers (not removing them during the iteration)
            }
            else
            {
                timersToAdvance.Add(timer.Key); //marking other timers (not doing it during the iteration)
            }
        }
        foreach (var dynamicGesture in timersToRemove) //removing expired timers
        {
            runningTimers.Remove(dynamicGesture);
        }
        foreach (var dynamicGesture in timersToAdvance) //advancing other timers
        {
            runningTimers[dynamicGesture] += Time.deltaTime;

        }
    }

    public void keyFrameRecognized(StaticGesture gesture)
    {
        foreach (var dynamicGesture in dynamicGestures)
        {
            int nextKeyFrame = reachedKeyFrame[dynamicGesture] + 1;

            if (dynamicGesture.orderedKeyFrames[nextKeyFrame].gestureName == gesture.gestureName)
            {
                //first frame
                if (nextKeyFrame == -1)
                {
                    //start timer
                    runningTimers[dynamicGesture] = 0f;
                    // advance keyFrameNumber
                    reachedKeyFrame[dynamicGesture] = 0; //reached first keyframe
                }
                //last frame
                else if (nextKeyFrame == dynamicGesture.orderedKeyFrames.Count - 1)
                {
                    //Gesture recognition event
                    onRecognized?.Invoke(dynamicGesture);
                    //Remove timer
                    runningTimers.Remove(dynamicGesture);
                    //remove reachedKeyFrame
                    reachedKeyFrame[dynamicGesture] = -1;
                    Debug.Log(dynamicGesture.gestureName);
                }
                //intermediate frame
                else
                {
                    reachedKeyFrame[dynamicGesture]++;
                }
            }            
        }
    }

    // Resets all timers and all counts of keyframes
    public void ResetAllDetections()
    {
        runningTimers = new Dictionary<DynamicGesture, float>();
        reachedKeyFrame = new Dictionary<DynamicGesture, int>();
        foreach (var dynamicGesture in dynamicGestures)
        {
            reachedKeyFrame[dynamicGesture] = -1;
        }
    }
}
