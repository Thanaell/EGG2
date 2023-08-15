using UnityEngine;
using UnityEditor.Animations;
using UnityEditor;

// Records animations with what happens on objetcToRecord
public class RecordAnimationClip : MonoBehaviour
{
    public AnimationClip clip;
    private GameObject objectToRecord;
    private GameObjectRecorder m_Recorder;
    public string objectName;
    private int frameCount = 0;
    bool isRecording = false;
    void findObject()
    {
        objectToRecord = GameObject.Find(objectName);
    }

    void LateUpdate()
    {

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!isRecording)
            {
                isRecording = true;
            }
            else
            {
                Save();
            }

        }
        if (isRecording)
        {
            if (clip == null)
                return;

            if (objectToRecord == null)
            {
                findObject();
                if (objectToRecord == null)
                    return;
                else
                {
                    Debug.Log("found Object");
                    // Create recorder and record the script GameObject.
                    m_Recorder = new GameObjectRecorder(objectToRecord);
                    // Bind all the Transforms on the GameObject and all its children.
                    m_Recorder.BindComponentsOfType<Transform>(objectToRecord, true);
                }
            }
            else
            {
                // Take a snapshot and record all the bindings values for this frame.
                m_Recorder.TakeSnapshot(Time.deltaTime);
                frameCount++;

            }
        }
    }

    void Save()
    {
        if (clip == null)
            return;

        if (m_Recorder.isRecording)
        {
            // Save the recorded session to the clip.
            m_Recorder.SaveToClip(clip);
            Debug.Log(frameCount);
        }
    }

}