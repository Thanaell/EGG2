using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivitySpotController : MonoBehaviour
{
    // Public vars
    public HandsAndAnimators hands;
    public SHOWING_TECHNIQUE currentShowTechnique;
    public GameObject triggerArea;
    public string animationName;

    // Used animators and hands
    private Animator usedAnimator;
    private GameObject usedHand;

    // Trigger area vars
    private bool isInside;
    private bool isAnim;

    // Lerp vars
    private bool isPreparingLerp;
    private bool isLerping;
    private float lerpStartTime;
    private List<Vector3> lerpStartPositions;
    private List<Quaternion> lerpStartRotations;
    private List<Vector3> lerpEndPositions;
    private List<Quaternion> lerpEndRotations;

    // Animation coroutine vars
    private float nextAnimPlayTimestamp;

    // Customizable vars
    private float delayBetweenAnimations = 3.5f;
    private float lerpDurationAfterShow = 0.2f;

    private void Start()
    {
        isInside = false;
        isAnim = false;

        isPreparingLerp = false;
        isLerping = false;
        lerpStartTime = -1f;

        nextAnimPlayTimestamp = -1f;

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

    private void Update()
    {
        // Hand is in the activity and not in animation
        if (isInside && !isAnim)
        {
            // Start coroutine when we reach the next play timestamp
            if (Time.time > nextAnimPlayTimestamp)
            {
                StartCoroutine(SetNextAnimPlayTimestamp());
            }
        }
    }

    private void LateUpdate()
    {
        // Preparing the lerp when the override animation ends
        if (isPreparingLerp)
        {
            isPreparingLerp = false;

            isLerping = true;
            lerpStartTime = Time.time;

            lerpStartPositions = new List<Vector3>();
            lerpStartRotations = new List<Quaternion>();
            lerpEndPositions = new List<Vector3>();
            lerpEndRotations = new List<Quaternion>();

            // Setting both positions and rotations from the end of animation to the currently tracked hand
            OVRSkeleton overrideSkeleton = hands.overrideHand.GetComponent<OVRSkeleton>();
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
        if (isLerping)
        {
            if (Time.time > lerpStartTime + lerpDurationAfterShow)
            {
                isLerping = false;
            }
            else
            {
                // How far into the lerp we are
                float lerpProgression = (Time.time - lerpStartTime) / lerpDurationAfterShow;

                // Lerping every bones according to the progress
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

    // Method called to clean after the animation
    public void FinishAnimation()
    {
        isAnim = false;
        usedAnimator.enabled = false;

        usedHand.SetActive(false);
        hands.mainHandRenderer.enabled = true;
    }

    // Method called OnTriggerEnter of the activity
    public void PlayActivity()
    {
        isInside = true;
    }

    // Method called OnTriggerExit of the activity
    public void StopActivity()
    {
        isInside = false;

        // Kill coroutine to prevent weird delayed behaviours
        StopAllCoroutines();

        isPreparingLerp = false;
        isLerping = false;

        FinishAnimation();
        nextAnimPlayTimestamp = -1;
    }

    private IEnumerator SetNextAnimPlayTimestamp()
    {
        usedAnimator.enabled = true;
        usedHand.SetActive(true);
        if (currentShowTechnique == SHOWING_TECHNIQUE.OVERRIDE_HAND)
        {
            hands.mainHandRenderer.enabled = false;
        }

        usedAnimator.Play(animationName);

        yield return new WaitForEndOfFrame();

        float currentClipLength = usedAnimator.GetCurrentAnimatorStateInfo(0).length;
        nextAnimPlayTimestamp = Time.time + currentClipLength + delayBetweenAnimations;

        // Initiating lerp just before the end of animation play
        yield return new WaitForSeconds(currentClipLength - 0.05f);

        if (currentShowTechnique == SHOWING_TECHNIQUE.OVERRIDE_HAND)
        {
            isPreparingLerp = true;
        }

        yield return new WaitForSeconds(0.05f);

        if (isInside)
        {
            FinishAnimation();
        }
    }
}
