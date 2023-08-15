using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections;
using System;

// Tentative of Dynamic Time Warping algorithm between an AnimationClip and a series of object positions
// WARNING : NOT FUNCTIONAL
public class DWTAlgo : MonoBehaviour
{
    public OVRSkeleton skeleton;
    public AnimationClip recordedGesture;
    public float threshold = 0.1f;
    private List<List<Vector3>> recordedGestures;
    private List<List<Vector3>> liveGestures;
    private bool isRecording = false;
    private float elapsedTime = 0.0f;


    void Start()
    {
        liveGestures = new List<List<Vector3>>();
        // When the Oculus hand had his time to initialize hand, with a simple coroutine i start a delay of
        // a function to initialize the script
        StartCoroutine(DelayRoutine(2.5f, Initialize));
    }

    // Coroutine used for delay some function
    public IEnumerator DelayRoutine(float delay, Action actionToDo)
    {
        yield return new WaitForSeconds(delay);
        actionToDo.Invoke();
    }

    public void Initialize()
    {
        // Check the function for know what it does
        recordedGestures = GetGestureDataFromAnimationClip(recordedGesture);
        Debug.Log(recordedGestures.Count);
        Debug.Log(recordedGestures[0].Count);
    }

    System.Collections.IEnumerator WaitForBones()
    {
        // Wait until the OVRSkeleton.Bones list is populated
        yield return new WaitUntil(() => skeleton.Bones.Count > 0);    
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            isRecording = true;
            liveGestures.Clear();
            elapsedTime = 0.0f;
        }

        if (Input.GetKey(KeyCode.Alpha2))
        {
            isRecording = false;
            float dwtDistance = GetDWTdistance(recordedGestures, liveGestures);
            Debug.Log(dwtDistance);
            if (dwtDistance < threshold)
            {
                Debug.Log("Gesture recognized");
            }
        }

        if (isRecording)
        {
            List<Vector3> frame = new List<Vector3>();
            foreach (var bone in skeleton.Bones)
            {
                frame.Add(bone.Transform.position);
            }
            liveGestures.Add(frame);
            elapsedTime += Time.deltaTime;
        }
    }

    private float GetDWTdistance(List<List<Vector3>> A, List<List<Vector3>> B)
    {
        int n = A.Count;
        int m = B.Count;
        float[,] DWT = new float[n + 1, m + 1];

        for (int i = 0; i <= n; i++)
        {
            for (int j = 0; j <= m; j++)
            {
                DWT[i, j] = Mathf.Infinity;
            }
        }
        DWT[0, 0] = 0;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                float cost = Vector3.Distance(A[i - 1][0], B[j - 1][0]);
                DWT[i, j] = cost + Mathf.Min(DWT[i - 1, j], DWT[i, j - 1], DWT[i - 1, j - 1]);
            }
        }

        return DWT[n, m];
    }

    private List<List<Vector3>> GetGestureDataFromAnimationClip(AnimationClip clip)
    {
        List<List<Vector3>> bonePositions = new List<List<Vector3>>();
        // Get the transforms of the bones in the right hand
        List<Transform> rightHandBones = new List<Transform>();
        int boneCount = 24; //normal number of bones
        Debug.Log(skeleton.Bones.Count);
        for (int i = 0; i < boneCount; i++)
        {
            var bone = skeleton.Bones[i];
            rightHandBones.Add(bone.Transform);

        }

        // Sample the animation clip at each frame and store the local position of each bone
        for (int i = 0; i < clip.length * clip.frameRate; i++)
        {
            float time = i / clip.frameRate;
            clip.SampleAnimation(gameObject, time);

            List<Vector3> frameBonePositions = new List<Vector3>();
            foreach (Transform bone in rightHandBones)
            {
                frameBonePositions.Add(bone.localPosition);
            }
            bonePositions.Add(frameBonePositions);
        }
        for (int i = 0; i<24; i++)
        {
            Debug.Log(bonePositions[0][i]);
            
        }
        return bonePositions;
    }

}
