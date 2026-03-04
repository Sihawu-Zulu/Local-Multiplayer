using UnityEngine;



public class CombatVFX : MonoBehaviour
{
    private ParticleSystem lightHitBurst;
    private ParticleSystem heavyHitBurst;
    private ParticleSystem blockSpark;
    private ParticleSystem knockdownDust;
    private ParticleSystem armDetachBurst;

    private bool resolved = false;

    
    private void TryResolve()
    {
        if (resolved) return;

        var marker = GetComponentInChildren<CharacterVFXMarker>();
        if (marker == null) return;

        lightHitBurst   = marker.lightHitBurst;
        heavyHitBurst   = marker.heavyHitBurst;
        blockSpark      = marker.blockSpark;
        knockdownDust   = marker.knockdownDust;
        armDetachBurst  = marker.armDetachBurst;
        resolved        = true;

     
    }

    

    public void PlayLightHit(Vector3 worldPos)
    {
        TryResolve();
        PlayAt(lightHitBurst, worldPos);
    }

    public void PlayHeavyHit(Vector3 worldPos)
    {
        TryResolve();
        PlayAt(heavyHitBurst, worldPos);
    }

    public void PlayBlock(Vector3 worldPos)
    {
        TryResolve();
        PlayAt(blockSpark, worldPos);
    }

    public void PlayKnockdownDust()
    {
        TryResolve();
        PlayAt(knockdownDust, transform.position);
    }

    public void PlayArmDetach(Vector3 armWorldPos)
    {
        TryResolve();
        PlayAt(armDetachBurst, armWorldPos);
    }



    private void PlayAt(ParticleSystem ps, Vector3 pos)
    {
        if (ps == null) return;
        ps.transform.position = pos;
        ps.Play();
    }
}