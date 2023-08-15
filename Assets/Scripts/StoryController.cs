using UnityEngine;

public enum HAND_STATE
{
    ANIM, TRACKING
}

// Playing animation over main hand (constantly)
public class StoryController : MonoBehaviour
{
    public string animationName;
    public Animator overrideAnimator;

    public GameObject overrideHand;

    void Start()
    {
        overrideAnimator.enabled = true;
        overrideAnimator.Play(animationName);
    }
}
