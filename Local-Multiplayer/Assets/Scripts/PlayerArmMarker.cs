using UnityEngine;

public class PlayerArmMarker : MonoBehaviour
{
    public int             PlayerID;
    public ParticleSystem  StringTrail;


    private Renderer[] renderers;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
    }

    public void SetArmVisible(bool visible)
    {
        foreach (var r in renderers)
            r.enabled = visible;
    }
}