using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivitySpotController : MonoBehaviour
{
    public HandsAndAnimators hands;

    public SHOWING_TECHNIQUE currentShowTechnique;
    public GameObject triggerArea;
    public string animationName;

    private Animator usedAnimator;
    private GameObject usedHand;

    private void Start()
    {
        switch (currentShowTechnique)
        {
            case SHOWING_TECHNIQUE.OVERRIDE_HAND:
                usedAnimator = hands.overrideAnimator;
                usedHand = hands.overrideHand;
                break;
            case SHOWING_TECHNIQUE.GHOST_HAND:
                usedAnimator = hands.ghostAnimator;
                usedHand = hands.ghostHand;
                break;
            case SHOWING_TECHNIQUE.EXTERNAL_HAND:
                usedAnimator = hands.externalAnimator;
                usedHand = hands.externalHand;
                break;
        }
    }

    public void PlayActivity()
    {
        Debug.Log("Playing activity");

        usedAnimator.enabled = true;
        usedHand.SetActive(true);
        if(currentShowTechnique == SHOWING_TECHNIQUE.OVERRIDE_HAND)
        {
            hands.mainHandRenderer.enabled = false;
        }

        usedAnimator.Play(animationName);
    }

    public void StopActivity()
    {
        usedAnimator.enabled = false;
        usedHand.SetActive(false);

        hands.mainHandRenderer.enabled = true;
    }
}
