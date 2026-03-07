using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    [SerializeField] private List<string> AnimationBools;
    public Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        PlayIdle();
    }

    // clears every bool in the list then sets the one at [index] true
    // keeps the animator state machine clean — only ever one state active
    private void SetOnlyBool(int index)
    {
        for (int i = 0; i < AnimationBools.Count; i++)
            animator.SetBool(AnimationBools[i], false);

        if (index >= 0 && index < AnimationBools.Count)
            animator.SetBool(AnimationBools[index], true);
    }

    public void PlayJump() => SetOnlyBool(0);
    public void PlayIdle() => SetOnlyBool(1);
    public void PlayRun() => SetOnlyBool(2);
    public void PlayKnockDown() => SetOnlyBool(3);

    public void PlayLightAttack() => SetOnlyBool(4);
    public void PlayGetUp() => SetOnlyBool(5);
    public void PlayTakeDamage() => SetOnlyBool(6);
    public void PlayGrab() => SetOnlyBool(7);
    public void PlayHeavyAttack() => SetOnlyBool(8);

    // kept for any external callers that used the old name
    public void PlayKnockdown() => PlayKnockDown();
    public void PlayGetup() => PlayGetUp();
}