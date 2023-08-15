using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputController : MonoBehaviour
{
    public StudyController studyController;
    
    //Change state through keyboard input for testing
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Keypad0))
        {
            studyController.StartIdle();
        }

        if (Input.GetKeyDown(KeyCode.Keypad1))
        {
            studyController.StartShowTechnique();
        }

        if (Input.GetKeyDown(KeyCode.Keypad2))
        {
            studyController.StartFirstPerform();
        }

        if (Input.GetKeyDown(KeyCode.Keypad3))
        {
            studyController.StartRepetitions(true);
        }
    }
}
