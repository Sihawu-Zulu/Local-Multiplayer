using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
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


    public void PlayJump()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
        {
            animator.SetBool(AnimationBools[i], false);
        }
        animator.SetBool(AnimationBools[0], true);
    }

    public void PlayIdle()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
        {
            animator.SetBool(AnimationBools[i], false);
        }
        animator.SetBool(AnimationBools[1], true);
    }

    public void PlayRun()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
        {
            animator.SetBool(AnimationBools[i], false);
        }
        animator.SetBool(AnimationBools[2], true);
    }

    public void PlayKnockDown()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
        {
            animator.SetBool(AnimationBools[i], false);
        }
        animator.SetBool(AnimationBools[3], true);
    }

    public void PlayKnockdown()
    {
        animator.SetBool("Knowdown", true);
    }

    // public void PlayKnockdownRoll()
    // {
    //     animator.SetBool("KnockdownRoll", true);
    // }

    public void PlayGetup()
    {
        animator.SetBool("Knockdown", false);
    }

    public void PlayLightAttack()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
        {
            animator.SetBool(AnimationBools[i], false);
        }
        animator.SetBool(AnimationBools[4], true);
    }

    public void PlayGetUp()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
        {
            animator.SetBool(AnimationBools[i], false);
        }
        animator.SetBool(AnimationBools[5], true);
    }

    public void PlayTakeDamage()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
        {
            animator.SetBool(AnimationBools[i], false);
        }
        animator.SetBool(AnimationBools[6], true);
    }

    public void PlayGrab()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
        {
            animator.SetBool(AnimationBools[i], false);
        }
        animator.SetBool(AnimationBools[7], true);
    }

    public void PlayHeavyAttack()
    {
        for (int i = 0; i < AnimationBools.Count; i++)
        {
            animator.SetBool(AnimationBools[i], false);
        }
        animator.SetBool(AnimationBools[8], true);
    }
}
