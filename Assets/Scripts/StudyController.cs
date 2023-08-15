
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Unity.VisualScripting.Dependencies.NCalc;

public enum SHOWING_TECHNIQUE
{
    GHOST_HAND, EXTERNAL_HAND, OVERRIDE_HAND
}

public enum STUDY_STEP
{    
    IDLE, SHOW_TECHNIQUE, FIRST_PERFORM, REPETITIONS
}


//Custom classes to map objects in editor in a collapsable way
[System.Serializable]
public class HandsAndAnimators
{
    public Animator overrideAnimator;
    public Animator externalAnimator;
    public Animator ghostAnimator;

    public GameObject mainHand;
    public SkinnedMeshRenderer mainHandRenderer;

    public GameObject externalHand;
    public GameObject ghostHand;
    public GameObject overrideHand;
}

[System.Serializable]
public class UI_Elements
{
    public Image detectionMarker;
    public TMP_Text repetionsCounterText;
    public TMP_Text instructionsText;

    public GameObject showButton;
    public GameObject tryButton;
    public GameObject repeatButton;
}


// Mapping of all used gestures with a string 
// The study is loaded from a JsonFile with the strings. 
// We cans choose which gesture to match with wich string directly in the editor
[System.Serializable]
public struct Mapping
{
    public string refName;
    public Gesture gesture;
}

/// <summary>
///  Main class controlling what happens in the study
///  The user must perform hand gestures that are shown to them in different ways (showing techniques)
/// </summary>
public class StudyController : MonoBehaviour
{
    public int participantNumber = -1;
    //Each modality for a single participant contains the 3 showing techniques in a random order
    public int modalityNumber = -1; //1, 2, 3. 

    // Elements to map in the editor
    public HandsAndAnimators hands;
    public UI_Elements UI;
    public List<Mapping> gestureMapping;

    // Main study modality and progress variables
    private List<Gesture> gestures;
    private StudyStory studyStory;
    private SHOWING_TECHNIQUE showingTechnique;
    private STUDY_STEP studyStep;

    private bool isTraining;
    private bool isAnim;
    private bool isPreparingLerp;
    private bool isLerping;
    private bool isExpectingGesture;
    private bool isFirstPerformDone;

    private int maxRepetitions = 10;
    private int currentRepetition;
    private int currentGestureIndex;
    private int showGestureRepeats;

    private GameObject usedHand;
    private Animator usedAnimator;

    private Gesture currentExpectedGesture;

    // Various timers 
    private float nextStaticGestureDetectionTimestamp;
    private float gestureTimeout;
    private float neutralTimeout;
    private float nextAnimPlayTimestamp;
    private float lerpStartTime;

    // Variables used for logging
    private float timestampStartNewGesture;
    private float timestampStartFirstPerform;
    private float timestampStartRepetitions;
    private int numberSuccessWhileShow;
    private int numberGestureAskedWhileTry;
    private int numberSuccessWhileTry;
    private int numberSuccessWhileRepeat;

    // List used for Lerps of Main Hand
    private List<Vector3> lerpStartPositions;
    private List<Quaternion> lerpStartRotations;
    private List<Vector3> lerpEndPositions;
    private List<Quaternion> lerpEndRotations;

   
    // The two loggers (hand is frame by frame, main is compiled data)
    public HandLogger handLogger;
    public MainDataLogger mainDataLogger;

    // Customisable vars (eg. waiting time between show tech)
    private float delayBetweenAnimations = 3.5f;
    private float delayBetweenStaticDetection = 2f;

    private float neutralTimeoutDelay = 4f;
    private float staticGestureTimeoutDelay = 3f;

    private float lerpDurationAfterShow = 0.2f;

    // Events
    public UnityEvent<List<StaticGesture>> gestureChanged;
    public UnityEvent stateChanged;

    void Start()
    {
        isTraining = true;
        isAnim = false;
        isPreparingLerp = false;
        isLerping = false;
        isExpectingGesture = false;
        isFirstPerformDone = false;

        currentRepetition = 0;
        currentGestureIndex = 0;
        showGestureRepeats = 0;

        if(participantNumber == -1 || modalityNumber == -1)
        {
            Debug.Log("ERROR Participant or modality number not set");
            Application.Quit();
        }

        // Creating log files with timeStamp
        double timeSinceUnixEpoch = Math.Floor((DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds);
        handLogger.CreateStreamWriter("./StudyLogs/Hand_Participant" + participantNumber.ToString() + "_Modality" + modalityNumber.ToString() + "_" + timeSinceUnixEpoch.ToString() + ".csv");
        mainDataLogger.CreateStreamWriter("./StudyLogs/Main_Participant" + participantNumber.ToString() + "_Modality" + modalityNumber.ToString() + "_" + timeSinceUnixEpoch.ToString() + ".csv");

        nextStaticGestureDetectionTimestamp = -1f;
        gestureTimeout = 0f;
        neutralTimeout = 0f;
        nextAnimPlayTimestamp = 0f;

        gestures = new List<Gesture>();

        studyStep = STUDY_STEP.IDLE;

        // Loading gestures from JsonFile, depending on participant and modality numbers
        studyStory = JsonLoader.loadStudyStory("./Study1Story.json");
        Modality currentModality = studyStory.Participants[participantNumber - 1].Modalities[modalityNumber - 1];
        switch(currentModality.ShowTechnique)
        {
            case "OVERRIDE":
                showingTechnique = SHOWING_TECHNIQUE.OVERRIDE_HAND;
                usedAnimator = hands.overrideAnimator;
                usedHand = hands.overrideHand;
                break;
            case "GHOST":
                showingTechnique = SHOWING_TECHNIQUE.GHOST_HAND;
                usedAnimator = hands.ghostAnimator;
                usedHand = hands.ghostHand;
                break;
            case "EXTERNAL":
                showingTechnique = SHOWING_TECHNIQUE.EXTERNAL_HAND;
                usedAnimator = hands.externalAnimator;
                usedHand = hands.externalHand;
                break;
        }

        gestures.Add(FindGesture(currentModality.GestureTraining));
        gestures.Add(FindGesture(currentModality.GestureStatic));
        gestures.Add(FindGesture(currentModality.GestureShort));
        gestures.Add(FindGesture(currentModality.GestureLong));

        // Starting first phase of the study
        StartIdle();
    }

    void Update()
    {
        // Logging each frame except in IDLE
        if (currentGestureIndex <= 4 && studyStep!=STUDY_STEP.IDLE){
            HandLog();
        }

        // Starting animation if in first phase and wait delay expired
        if (studyStep == STUDY_STEP.SHOW_TECHNIQUE && nextAnimPlayTimestamp < Time.time)
        {
            isAnim = true;

            if (showingTechnique == SHOWING_TECHNIQUE.OVERRIDE_HAND)
            {
                hands.mainHandRenderer.enabled = false;
            }
            usedHand.SetActive(true);
            usedAnimator.Play(currentExpectedGesture.gestureName);
            // Start anim coroutiine
            StartCoroutine(SetNextAnimPlayTimestamp());
        }

        // If one of the 2 performing phases and delay expired, go back to appropriate instruction (neutral or perform the gesture)
        if (studyStep == STUDY_STEP.REPETITIONS || studyStep == STUDY_STEP.FIRST_PERFORM)
        {
            if(isExpectingGesture && Time.time > gestureTimeout)
            {
                if(studyStep == STUDY_STEP.REPETITIONS)
                {
                    currentRepetition++;
                }

                OnGestureEnd(false); // back to neutral without detection
            }

            if(!isExpectingGesture && Time.time > neutralTimeout)
            {
                OnNeutralEnd(); // back to performing
            }

            // if we reached max repetitions, end of this cycle, go back to idle
            if(currentRepetition >= maxRepetitions)
            {
                StartIdle();
            } 
        }
    }

    // Late Update because we're touching animations
    // Handles lerp between end of animation and tracked position when we show the gesture through Override
    private void LateUpdate()
    {
        // If we need to lerp (only in SHOWING_TECHNIQUE.OVERRIDE)
        if (isPreparingLerp)
        {
            isPreparingLerp = false;

            isLerping = true;
            lerpStartTime = Time.time;

            lerpStartPositions = new List<Vector3>();
            lerpStartRotations = new List<Quaternion>();
            lerpEndPositions = new List<Vector3>();
            lerpEndRotations = new List<Quaternion>();

            OVRSkeleton overrideSkeleton = hands.overrideHand.GetComponent<OVRSkeleton>();
            // Setting both positions and rotations from the end of animation to the currently tracked hand
            foreach (OVRBone bone in overrideSkeleton.Bones)
            {
                lerpStartPositions.Add(new Vector3(bone.Transform.localPosition.x, bone.Transform.localPosition.y, bone.Transform.localPosition.z));
                lerpStartRotations.Add(new Quaternion(bone.Transform.localRotation.x, bone.Transform.localRotation.y, bone.Transform.localRotation.z, bone.Transform.localRotation.w));
            }

            OVRSkeleton mainSkeleton = hands.mainHand.GetComponent<OVRSkeleton>();
            foreach (OVRBone bone in mainSkeleton.Bones)
            {
                lerpEndPositions.Add(new Vector3(bone.Transform.localPosition.x, bone.Transform.localPosition.y, bone.Transform.localPosition.z));
                lerpEndRotations.Add(new Quaternion(bone.Transform.localRotation.x, bone.Transform.localRotation.y, bone.Transform.localRotation.z, bone.Transform.localRotation.w));
            }
        }

        // If lerp is active, actually lerping in position and rotation until the main hand is back to its actual tracked position
        if (showingTechnique == SHOWING_TECHNIQUE.OVERRIDE_HAND && studyStep == STUDY_STEP.SHOW_TECHNIQUE && isLerping)
        {
            if (Time.time > lerpStartTime + lerpDurationAfterShow)
            {
                isLerping = false;
            }
            else
            {
                float lerpProgression = (Time.time - lerpStartTime) / lerpDurationAfterShow;

                int boneIndex = 0;
                OVRSkeleton mainSkeleton = hands.mainHand.GetComponent<OVRSkeleton>();
                foreach (OVRBone bone in mainSkeleton.Bones)
                {
                    bone.Transform.localPosition = Vector3.Lerp(lerpStartPositions[boneIndex], lerpEndPositions[boneIndex], lerpProgression);
                    bone.Transform.localRotation = Quaternion.Lerp(lerpStartRotations[boneIndex], lerpEndRotations[boneIndex], lerpProgression);

                    boneIndex++;
                }
            }
        }
    }

    // Function called when we're done expecting a gesture (whether it was recognized or not)
    private void OnGestureEnd(bool isGestureRecognized)
    {
        isExpectingGesture = false;

        // Updating UI
        UI.repetionsCounterText.text = currentRepetition.ToString() + "/10"; // text only active in STUDYSTEP.REPETITIONS
        UI.instructionsText.text = "Go to neutral position";

        //Updating UI and log depending on gesture detection or nor

        if (isGestureRecognized)
        {
            UI.detectionMarker.color = Color.green;
            if (studyStep == STUDY_STEP.FIRST_PERFORM)
            {
                numberSuccessWhileTry++;
            }
            else if (studyStep == STUDY_STEP.REPETITIONS)
            {
                numberSuccessWhileRepeat++;
            }
        }
        else
        {
            UI.detectionMarker.color = Color.red;
        }
        // setting timeout for neutral position phase
        neutralTimeout = Time.time + neutralTimeoutDelay;
        stateChanged.Invoke(); // reset detector
    }

    //Function called when we're done with asking for neutral position
    private void OnNeutralEnd()
    {
        if (studyStep == STUDY_STEP.FIRST_PERFORM)
        {
            numberGestureAskedWhileTry++;
        }

        isExpectingGesture = true;

        UI.instructionsText.text = "Perform the gesture";
        UI.detectionMarker.color = Color.red;

        //Defining max delay before we come back from gesture asking to neutral askiing

        if (currentExpectedGesture is DynamicGesture)
        {
            // depending on exec time for dynamic gestures
            gestureTimeout = Time.time + ((DynamicGesture)currentExpectedGesture).execTime;
        }
        else
        {
            // fixed through variable for static gestures
            gestureTimeout = Time.time + staticGestureTimeoutDelay;
        }
        stateChanged.Invoke(); // resetting detector
    }

    private void HandLog(string detectedGestureName="n/a")
    {
        handLogger.WriteDataToCSV(participantNumber, modalityNumber, showingTechnique, Time.time, isTraining, isLerping, studyStep, isAnim,
             currentRepetition, showGestureRepeats, currentExpectedGesture.gestureName, detectedGestureName); //handlogger knows by itself the hand position
                
    }

    // Idle is the first phase (before the left button press, nothing happens for the user
    public void StartIdle()
    {
        isPreparingLerp = false;
        isLerping = false;

        // Logging previous results if study has started
        if (currentGestureIndex != 0 && currentGestureIndex < 5)
        {
            mainDataLogger.WriteDataToCSV(participantNumber, modalityNumber, showingTechnique, isTraining, showGestureRepeats, currentExpectedGesture.gestureName, timestampStartFirstPerform - timestampStartNewGesture, timestampStartRepetitions - timestampStartFirstPerform, numberSuccessWhileShow, numberGestureAskedWhileTry, numberSuccessWhileTry, numberSuccessWhileRepeat);
        }

        // Couting cycles through the study (the user goes through 4 gestures)
        currentGestureIndex++;

        
        if (currentGestureIndex >= 5) // End of this modality
        {
            studyStep = STUDY_STEP.IDLE;
            
            UI.instructionsText.text = "Please remove the headset";

            UI.showButton.transform.parent.gameObject.SetActive(false);
            UI.tryButton.transform.parent.gameObject.SetActive(false);
            UI.repeatButton.transform.parent.gameObject.SetActive(false);
        }
        else // Setting up variables for new modality
        {
            studyStep = STUDY_STEP.IDLE;
            currentRepetition = 0;
            showGestureRepeats = 0;

            timestampStartNewGesture = -1f;
            timestampStartFirstPerform = -1f;
            numberSuccessWhileShow = 0;
            numberGestureAskedWhileTry = 0;
            numberSuccessWhileTry = 0;
            numberSuccessWhileRepeat = 0;

            nextStaticGestureDetectionTimestamp = -1f;

            // The first gesture is training
            if (currentGestureIndex > 1)
            {
                isTraining = false;
            }

            isFirstPerformDone = false;

            // Changing UI
            UI.detectionMarker.enabled = false;
            UI.detectionMarker.color = Color.red;

            UI.repetionsCounterText.enabled = false;
            UI.repetionsCounterText.text = "0/10";

            UI.instructionsText.text = "Press the first button to start";

            UI.showButton.GetComponent<MeshRenderer>().material.color = Color.red;
            UI.tryButton.GetComponent<MeshRenderer>().material.color = Color.grey;
            UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.grey;

            // Updating expected gesture
            currentExpectedGesture = gestures[currentGestureIndex - 1];

            // Updating StaticGestureDetector from current expected gesture through events
            List<StaticGesture> gestureList = new List<StaticGesture>();
            if (currentExpectedGesture is StaticGesture) // if static, send the gesture directly
            {           
                gestureList.Add((StaticGesture)currentExpectedGesture);
            }
            else
            {
                gestureList = ((DynamicGesture)currentExpectedGesture).orderedKeyFrames; // if dynamix, send each frame of the gesture
            }
            gestureChanged.Invoke(gestureList);
        }
        
    }
    // First phase. The user watches the gesture
    public void StartShowTechnique()
    {
        stateChanged.Invoke();
        if(studyStep != STUDY_STEP.REPETITIONS)
        {
            // Setting the timestamp only the first time the button is pressed
            if(timestampStartNewGesture < 0f)
            {
                timestampStartNewGesture = Time.time;
            }

            studyStep = STUDY_STEP.SHOW_TECHNIQUE;
            showGestureRepeats++;

            UI.instructionsText.text = "Watch the gesture until you understand it";

            UI.showButton.GetComponent<MeshRenderer>().material.color = Color.grey;
            UI.tryButton.GetComponent<MeshRenderer>().material.color = Color.red;
            UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.grey;

            // Remove detection square on show
            UI.detectionMarker.enabled = false;

            // Enabling animator to show the gesture
            usedAnimator.enabled = true;
            nextAnimPlayTimestamp = Time.time;
        }
    }

    // Second phase. The user must reproduce the gesture they saw
    // This phase alternats between Neutral Position and Perform the Gesture
    public void StartFirstPerform()
    {
        stateChanged.Invoke();
        if(studyStep == STUDY_STEP.SHOW_TECHNIQUE)
        {
            isPreparingLerp = false;
            isLerping = false;

            // Setting the timestamp only the first time the button is pressed
            if (timestampStartFirstPerform < 0f)
            {
                timestampStartFirstPerform = Time.time;
            }

            studyStep = STUDY_STEP.FIRST_PERFORM;
            isAnim = false;
            
            //Update UI
            UI.detectionMarker.enabled = true;

            UI.showButton.GetComponent<MeshRenderer>().material.color = Color.red;
            UI.tryButton.GetComponent<MeshRenderer>().material.color = Color.grey;

            if (isFirstPerformDone)
            {
                UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.red;
            }
            else
            {
                UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.grey;
            }

            //Disabling animator and showing hand
            usedAnimator.enabled = false;
            usedHand.SetActive(false);

            // Re-enabling main hand if showing technique is override
            if (showingTechnique == SHOWING_TECHNIQUE.OVERRIDE_HAND)
            {
                hands.mainHandRenderer.enabled = true;
            }

            // Start by asking for neutral position
            OnGestureEnd(false);
        }
    }

    // Third phase
    // The user must perform the gesture 10 times, without possibility to go back
    // Alternates betwwen neutral position and perform the gesture
    public void StartRepetitions(bool isCalledFromKeypad = false)
    {
        stateChanged.Invoke();
        if(studyStep == STUDY_STEP.FIRST_PERFORM && (isFirstPerformDone || isCalledFromKeypad))
        {
            isPreparingLerp = false;
            isLerping = false;

            timestampStartRepetitions = Time.time;

            studyStep = STUDY_STEP.REPETITIONS;

            //Update UI
            UI.repetionsCounterText.enabled = true;

            UI.showButton.GetComponent<MeshRenderer>().material.color = Color.grey;
            UI.tryButton.GetComponent<MeshRenderer>().material.color = Color.grey;
            UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.grey;

            //Start on neutral position
            OnGestureEnd(false);
        }
    }

    // Called through event by the detector when a gesture (static or dynamic) is detected
    // Detectors only look at the main hand, never the used hand (the one used to show gestures)
    public void OnRecognizeEvent(Gesture detectedGesture)
    {
        // We ignore gestures detected when a lerp is happening
        if (isLerping)
        {
            return;
        }

        Debug.Log(detectedGesture.gestureName);

        // If we detected the correct gesture, and it's a static one, create a delay before next allowed detection
        // Prevent detection spamming for static gestures
        if (detectedGesture.gestureName == currentExpectedGesture.gestureName && currentExpectedGesture is StaticGesture)
        {
            if (Time.time > nextStaticGestureDetectionTimestamp)
            {
                nextStaticGestureDetectionTimestamp = Time.time + delayBetweenStaticDetection;
            }
            else
            {
                return;
            }
        }

        // Updating log variables and UI correctly depending on show technique and study step
        switch (studyStep)
        {
            case STUDY_STEP.IDLE:
                break;
            case STUDY_STEP.SHOW_TECHNIQUE:
                HandLog(detectedGesture.gestureName);
                if (detectedGesture.gestureName == currentExpectedGesture.gestureName)
                {
                    numberSuccessWhileShow++;
                }
                break;
            case STUDY_STEP.FIRST_PERFORM:
                if (isExpectingGesture)
                {
                    HandLog(detectedGesture.gestureName);
                    if (detectedGesture.gestureName == currentExpectedGesture.gestureName)
                    {
                        isFirstPerformDone = true;
                        UI.repeatButton.GetComponent<MeshRenderer>().material.color = Color.red;
                        // Go back to neutral position, knowing we detected a gesture
                        OnGestureEnd(true);
                    }
                }
                break;
            case STUDY_STEP.REPETITIONS:
                if (isExpectingGesture)
                {
                    HandLog(detectedGesture.gestureName);
                    if (detectedGesture.gestureName == currentExpectedGesture.gestureName)
                    {
                        currentRepetition++;
                        // Go back to neutral position, knowing we detected a gesture
                        OnGestureEnd(true);
                    }
                }
                break;
        }
    }

    // Find gesture in Gesture Mapping
    private Gesture FindGesture(string gestureRef)
    {
        foreach(Mapping mapping in gestureMapping)
        {
            if(mapping.refName == gestureRef)
            {
                return mapping.gesture;
            }
        }

        Debug.Log("Gesture ref was not found, when did you last question your life choices?"); // Yex, when ?

        return null;
    }

    // Coroutine handling a pause between animation plays in the show technique phase
    // Also prepares lerp to smoothly go back to tracking at the end of animation
    private IEnumerator SetNextAnimPlayTimestamp()
    {
        yield return new WaitForEndOfFrame();

        float currentClipLength = usedAnimator.GetCurrentAnimatorStateInfo(0).length;
        nextAnimPlayTimestamp = Time.time + currentClipLength + delayBetweenAnimations;

        // Initiating lerp just before the end of animation play
        yield return new WaitForSeconds(currentClipLength - 0.05f);

        if (showingTechnique == SHOWING_TECHNIQUE.OVERRIDE_HAND && studyStep==STUDY_STEP.SHOW_TECHNIQUE)
        {
            isPreparingLerp = true;
        }

        yield return new WaitForSeconds(0.05f);

        // Disabling animation play and going back to tracking
        isAnim = false;
        usedHand.SetActive(false);
        hands.mainHandRenderer.enabled = true;
    }
}
