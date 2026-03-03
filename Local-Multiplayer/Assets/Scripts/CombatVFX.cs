using UnityEngine;

// attach to each player - holds vfx prefab/particle references
// called by combatsystem and knockdownmanager when impacts happen
// uses world-space position of the hit so particles spawn at the right spot

public class CombatVFX : MonoBehaviour
{
    [Header("Hit VFX")]
    public ParticleSystem lightHitBurst;    // small pop on light hit
    public ParticleSystem heavyHitBurst;    // bigger burst on heavy hit
    public ParticleSystem blockSpark;       // spark when a hit is blocked

    [Header("Knockdown VFX")]
    public ParticleSystem knockdownDust;    // dust cloud when player hits the floor

    [Header("Arm Detach VFX")]
    public ParticleSystem armDetachBurst;   // thread explosion when arm pops off

    // -------------------------------------------------------

    // called with the world position of the opponent so the burst spawns at the impact point
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

    // -------------------------------------------------------

    private void PlayAt(ParticleSystem ps, Vector3 pos)
    {
        if (ps == null) return;
        ps.transform.position = pos;
        ps.Play();
    }
}
