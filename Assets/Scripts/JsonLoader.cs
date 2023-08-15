using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;



// Custom classes to deserialize Json into Participants and Modalities (guide for study)
[System.Serializable]
public class Modality
{
    public string ShowTechnique;
    public string GestureTraining;
    public string GestureStatic;
    public string GestureShort;
    public string GestureLong;
}

[System.Serializable]
public class Participant
{
    public List<Modality> Modalities;
}


[System.Serializable]
public class StudyStory
{
    public List<Participant> Participants;
}

public static class JsonLoader 
{
    public static StudyStory loadStudyStory(string path)
    {
        StreamReader reader = new StreamReader(path);
        string jsonStudyStory = reader.ReadToEnd();
        StudyStory studyStory = JsonUtility.FromJson<StudyStory>(jsonStudyStory);
        return studyStory;
    }
}
