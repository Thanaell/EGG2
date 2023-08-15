using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System;
using Unity.VisualScripting;

//Logger for hand positions each frame (except when study phase is STUDYSTEP.IDLE)
public class HandLogger : MonoBehaviour
{

    [Header("Hand Skeleton")]
    public OVRSkeleton skeleton;
    // List of bones took from the OVRSkeleton
    private List<OVRBone> fingerbones = null;

    private StreamWriter writer;

    void Start()
    {
        // When the Oculus hand had his time to initialize hand, with a simple coroutine i start a delay of
        // a function to initialize the script
        StartCoroutine(DelayRoutine(2f, Initialize));
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
        SetSkeleton();
    }
    public void SetSkeleton()
    {
        // Populate the private list of fingerbones from the current hand we put in the skeleton
        fingerbones = new List<OVRBone>(skeleton.Bones);
    }

    private void OnApplicationQuit()
    {
        writer.Close();
    }

    public void CreateStreamWriter(string path)
    {
        writer = new StreamWriter(path);

        // Add first line with columns names
        string line = "participantNb;modalityNb;showTechnique;timeStamp;isTraining;isLerping;studyStep;isAnim;currentRepetitionNb;showGestureRepeats;currentExpectedGestureName;detectedGestureName;jointsPositions";
        writer.WriteLine(line);
        writer.Flush();
    }

    public void WriteDataToCSV(
        int participantNumber, 
        int modalityNumber,
        SHOWING_TECHNIQUE showTechnique, 
        float timeStamp, 
        bool isTraining,
        bool isLerping,
        STUDY_STEP studyStep,
        bool isAnim,
        int currentRepetitionNumber,
        int showGestureRepeats,
        string currentExpectedGestureName,
        string detectedGestureName
        )
    {
        if (fingerbones == null)
        {
            return;
        }

        // we create also a new list of Vector 3
        List<Vector3> data = new List<Vector3>();

        // with a foreach we go through every bone we set at the start
        // in the list of fingerbones
        foreach (var bone in fingerbones)
        {
            // and we will going to populate the list of Vector3 we done before with all the trasform found in the fingerbones
            // the fingers positions are in base at the hand Root
            data.Add(skeleton.transform.InverseTransformPoint(bone.Transform.position));
        }

        string line = "";
        line += string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};",
                   participantNumber, modalityNumber, showTechnique, timeStamp, isTraining, isLerping, studyStep, isAnim, 
                   currentRepetitionNumber, showGestureRepeats, currentExpectedGestureName, detectedGestureName);
        foreach (Vector3 bonePosition in data)
        {
            line += string.Format("{0};{1};{2};",
                    bonePosition.x,bonePosition.y,bonePosition.z);
        }
        writer.WriteLine(line);
        writer.Flush();
    }
}
