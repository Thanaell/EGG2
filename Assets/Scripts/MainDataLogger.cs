using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using static OVRPlugin;

//Logging compiled data (timers, booleans per phase, numbers of gesture detections)
public class MainDataLogger : MonoBehaviour
{
    private StreamWriter writer;

    private void OnApplicationQuit()
    {
        writer.Close();
    }

    public void CreateStreamWriter(string path)
    {
        writer = new StreamWriter(path);

        // Add first line with columns names
        string line = "participantNb;modalityNb;showTechnique;gestureName;isTraining;showGestureRepeats;timeBeforeTriggerSecondPhase;timeBeforeTriggerThirdPhase;nbOfSuccessInFirstPhase;nbGestureAskedWhileTry;nbOfSuccessInSecondPhase;nbOfSuccessInThirdPhase";

        writer.WriteLine(line);
        writer.Flush();
    }

    public void WriteDataToCSV(
        int participantNumber,
        int modalityNumber,
        SHOWING_TECHNIQUE showTechnique,
        bool isTraining,
        int showGestureRepeats,
        string gestureName,
        float timeBeforeTriggerSecondPhase,
        float timeBeforeTriggerThirdPhase,
        int numberOfSuccessInFirstPhase,
        int numberGestureAskedWhileTry,
        int numberOfSuccessInSecondPhase,
        int numberOfSuccessInThirdPhase
    )
    {
        string line = "";

        line += string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};",
                   participantNumber, modalityNumber, showTechnique, gestureName, isTraining,
                   showGestureRepeats, timeBeforeTriggerSecondPhase, timeBeforeTriggerThirdPhase,
                   numberOfSuccessInFirstPhase, numberGestureAskedWhileTry,
                   numberOfSuccessInSecondPhase, numberOfSuccessInThirdPhase);

        writer.WriteLine(line);
        writer.Flush();
    }
}
