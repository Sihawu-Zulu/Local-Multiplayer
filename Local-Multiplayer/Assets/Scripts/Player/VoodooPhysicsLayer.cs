using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(MultiplayerPlayerController))]
public class VoodooPhysicsLayer : MonoBehaviour
{
    [Header("Body Sway / Lean")]
    [SerializeField] private float leanAngle        = 12f;    // max z rotation when running
    [SerializeField] private float leanSmoothing    = 8f;     // how snappy the lean is
    [SerializeField] private float swayAmplitude    = 0.06f;  // side-to-side wobble while moving
    [SerializeField] private float swayFrequency    = 7f;     // speed of the wobble cycle

    [Header("Overshoot & Settle")]
    [SerializeField] private float overshootAmount  = 0.12f;  // how far the body overshoots on stop
    [SerializeField] private float overshootDecay   = 9f;     // spring decay speed

    [Header("Landing Squash & Bounce")]
    [SerializeField] private float squashAmount     = 0.28f;  // how much y scale squashes on land
    [SerializeField] private float squashDuration   = 0.08f;  // time to reach peak squash
    [SerializeField] private float bounceDecay      = 6f;     // spring back speed after squash

    [Header("Limb Lag")]
    [SerializeField] private float limbLagSmoothing = 4f;     // lower = more lag/slop on limbs


    [Header("Limb Lag Transforms ")]
    [SerializeField] private float limbLagPositionScale = 0.3f;  // how much limbs offset relative to body

    // -------------------------------------------------------

    private MultiplayerPlayerController controller;
    private Transform visualSlot;

    // body state
    private float   currentLean         = 0f;
    private float   swayTimer           = 0f;
    private Vector3 overshootVelocity   = Vector3.zero;
    private Vector3 overshootOffset     = Vector3.zero;
    private Vector3 lastMoveDir         = Vector3.zero;
    private Vector3 currentMoveDir      = Vector3.zero;
    private bool    wasMoving           = false;

    // landing state
    private bool    wasGrounded         = false;
    private float   currentSquash       = 1f;
    private float   squashVelocity      = 0f;
    private Coroutine squashCoroutine;

    // limb lag
    private List<LimbLagEntry> limbs = new List<LimbLagEntry>();

    private class LimbLagEntry
    {
        public Transform t;
        public Vector3   laggedLocalPos;
        public Vector3   lagVelocity;
    }

    // base visual position before any offsets — so effects stack cleanly
    private Vector3 baseVisualLocalPos;
    private bool    physicsEnabled = true;

    // -------------------------------------------------------

    private void Awake()
    {
        controller = GetComponent<MultiplayerPlayerController>();
    }

    private void Start()
    {
        visualSlot = controller.GetVisualSlot();

        if (visualSlot == null)
        {
      
            enabled = false;
            return;
        }

        baseVisualLocalPos = visualSlot.localPosition;
        ResolveLimbs();
    }

    // find any LimbLagMarker children so we don't need inspector wiring
    private void ResolveLimbs()
    {
        var markers = GetComponentsInChildren<LimbLagMarker>();
        foreach (var m in markers)
        {
            limbs.Add(new LimbLagEntry
            {
                t            = m.transform,
                laggedLocalPos = m.transform.localPosition,
                lagVelocity  = Vector3.zero
            });
        }

      
    }

    // -------------------------------------------------------

    private void Update()
    {
        if (!physicsEnabled || visualSlot == null) return;

        // read current movement from the controller's exposed move direction
        Vector3 moveDir = controller.GetMoveDirection();
        bool isMoving   = moveDir.magnitude > 0.05f;
        bool isGrounded = controller.IsGrounded;

        HandleLanding(isGrounded);
        HandleLean(moveDir, isMoving);
        HandleSway(isMoving);
        HandleOvershoot(moveDir, isMoving);
        ApplyBodyTransform();
        HandleLimbLag(moveDir);

        wasMoving   = isMoving;
        wasGrounded = isGrounded;
        lastMoveDir = moveDir;
    }

    // -------------------------------------------------------
    // lean — tilts body in movement direction
    // -------------------------------------------------------

    private void HandleLean(Vector3 moveDir, bool isMoving)
    {
        float targetLean = isMoving ? -moveDir.x * leanAngle : 0f;
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSmoothing);
    }

    // -------------------------------------------------------
    // sway — subtle side wobble while moving
    // -------------------------------------------------------

    private void HandleSway(bool isMoving)
    {
        if (isMoving)
            swayTimer += Time.deltaTime * swayFrequency;
        else
            swayTimer = Mathf.Lerp(swayTimer, 0f, Time.deltaTime * 4f);  // gently settle sway
    }

    // -------------------------------------------------------
    // overshoot — body carries forward momentum, springs back on stop
    // -------------------------------------------------------

    private void HandleOvershoot(Vector3 moveDir, bool isMoving)
    {
        if (isMoving)
        {
            // body leans into movement direction slightly
            Vector3 target = moveDir * overshootAmount;
            overshootOffset = Vector3.SmoothDamp(overshootOffset, target,
                                                 ref overshootVelocity, 1f / overshootDecay);
        }
        else if (wasMoving && !isMoving)
        {
            // just stopped — carry the last direction as overshoot then spring back
            overshootVelocity += lastMoveDir * overshootAmount * 2f;
        }

        if (!isMoving)
        {
            // spring back to zero
            overshootOffset = Vector3.SmoothDamp(overshootOffset, Vector3.zero,
                                                 ref overshootVelocity, 1f / overshootDecay);
        }

        currentMoveDir = moveDir;
    }

    // -------------------------------------------------------
    // landing squash
    // -------------------------------------------------------

    private void HandleLanding(bool isGrounded)
    {
        if (isGrounded && !wasGrounded)
        {
            // just landed
            if (squashCoroutine != null) StopCoroutine(squashCoroutine);
            squashCoroutine = StartCoroutine(SquashRoutine());
        }

        // always lerp squash back toward 1 via spring
        if (currentSquash != 1f)
            currentSquash = Mathf.SmoothDamp(currentSquash, 1f, ref squashVelocity, 1f / bounceDecay);
    }

    private IEnumerator SquashRoutine()
    {
        float target   = 1f - squashAmount;
        float elapsed  = 0f;
        float startVal = currentSquash;

        // squash down fast
        while (elapsed < squashDuration)
        {
            elapsed       += Time.deltaTime;
            currentSquash  = Mathf.Lerp(startVal, target, elapsed / squashDuration);
            yield return null;
        }

        currentSquash = target;
        // bounce back is handled by the spring in HandleLanding
    }

    // -------------------------------------------------------
    // apply everything to the visual slot
    // -------------------------------------------------------

    private void ApplyBodyTransform()
    {
        // sway offset on x
        float swayX = Mathf.Sin(swayTimer) * swayAmplitude;

        // combine all offsets — keep z at base so knockdown rotation isn't overwritten
        Vector3 finalPos = baseVisualLocalPos
                         + new Vector3(swayX + overshootOffset.x, overshootOffset.y, 0f);

        visualSlot.localPosition = finalPos;

        // lean + existing z rotation (knockdown uses z rotation — add on top of it)
        Quaternion baseLean   = Quaternion.Euler(0f, 0f, currentLean);
        Quaternion currentRot = visualSlot.localRotation;

        // only apply lean if not currently in a knockdown rotation
        // we detect this by checking if the existing z rot is significant
        float existingZ = currentRot.eulerAngles.z;
        bool knockedDown = existingZ > 45f && existingZ < 315f;

        if (!knockedDown)
            visualSlot.localRotation = Quaternion.Lerp(currentRot, baseLean, Time.deltaTime * leanSmoothing);

        // squash & stretch on y scale only
        Vector3 s = visualSlot.localScale;
        visualSlot.localScale = new Vector3(s.x, currentSquash * Mathf.Abs(s.x) > 0 ? currentSquash : s.y, s.z);
    }

    // -------------------------------------------------------
    // limb lag — limbs trail behind the body's movement
    // -------------------------------------------------------

    private void HandleLimbLag(Vector3 moveDir)
    {
        foreach (var limb in limbs)
        {
            if (limb.t == null) continue;

            // target is a slight offset opposite to movement direction — creates the drag effect
            Vector3 lagTarget = -moveDir * limbLagPositionScale;

            limb.laggedLocalPos = Vector3.SmoothDamp(
                limb.laggedLocalPos,
                lagTarget,
                ref limb.lagVelocity,
                1f / limbLagSmoothing
            );

            limb.t.localPosition = limb.laggedLocalPos;
        }
    }

    // -------------------------------------------------------
    // enable / disable — called by knockdownmanager so effects
    // don't fight with the knockdown rotation coroutines
    // -------------------------------------------------------

    public void SetPhysicsEnabled(bool enabled)
    {
        physicsEnabled = enabled;

        if (!enabled && visualSlot != null)
        {
            // reset offsets so knockdown starts from a clean state
            overshootOffset   = Vector3.zero;
            overshootVelocity = Vector3.zero;
            currentLean       = 0f;
            swayTimer         = 0f;
        }
    }
}
