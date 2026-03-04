using UnityEngine;

// i must remember to attach to each player 


public class CombatVFX : MonoBehaviour
{
    [Header("Hit VFX")]
    public ParticleSystem lightHitBurst;    
    public ParticleSystem heavyHitBurst;    
    public ParticleSystem blockSpark;       
    [Header("Knockdown VFX")]
    public ParticleSystem knockdownDust;    

    [Header("Arm Detach VFX")]
    public ParticleSystem armDetachBurst;   // thread explosion when arm pops off

 
    public void PlayLightHit(Vector3 worldPos)
    {
        PlayAt(lightHitBurst, worldPos);
    }

    public void PlayHeavyHit(Vector3 worldPos)
    {
        PlayAt(heavyHitBurst, worldPos);
    }

    public void PlayBlock(Vector3 worldPos)
    {
        PlayAt(blockSpark, worldPos);
    }

    public void PlayKnockdownDust()
    {
        // dust spawns at the players own feet
        PlayAt(knockdownDust, transform.position);
    }

    public void PlayArmDetach(Vector3 armWorldPos)
    {
        PlayAt(armDetachBurst, armWorldPos);
    }

   

    private void PlayAt(ParticleSystem ps, Vector3 pos)
    {
        if (ps == null) return;
        ps.transform.position = pos;
        ps.Play();
    }
}
