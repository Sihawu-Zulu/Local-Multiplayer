using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class KnockdownManager : MonoBehaviour
{
    [Header("Recovery Settings")]
    [SerializeField] private int mashesRequired = 15;
    [SerializeField] private float recoveryTimeLimit = 6f;

    [Header("Tug Of War Settings")]
    [SerializeField] private float tugDamagePerSecond = 5f;
    [SerializeField] private float tugProgressPerSecond = 0.15f;
    [SerializeField] private float tugDecayPerSecond = 0.08f;

    [Header("Arm Settings")]
    [SerializeField] private Vector3 armDetachedOffset = new Vector3(0.5f, -0.3f, 0f);

    [Header("Juice Settings")]
    [SerializeField] private float knockdownShakeMagnitude = 0.2f;
    [SerializeField] private float knockdownShakeDuration = 0.25f;
    [SerializeField] private float armDetachShakeMagnitude = 0.35f;
    [SerializeField] private float armDetachShakeDuration = 0.3f;
    [SerializeField] private float armDetachHitStopDur = 0.12f;

    [Header("Knockback / Fall Settings")]
    [SerializeField] private float knockbackForce = 12f;

    // --- fallback fall flat if knockdown anim doesnt play ---
    [Header("Fall Flat Fallback")]
    [SerializeField] private float fallRotateDuration  = 0.22f;   // how long the tip-over takes
    [SerializeField] private float fallRotateAngle     = 90f;     // degrees to tip (keep at 90)
    [SerializeField] private float animWaitBeforeFall  = 0.15f;   // short window to let anim start before fallback kicks in

    [Header("Animation stuff")]
    private AnimationManager animatorScript;
    [SerializeField] private float getUpDuration = 1.2f;

    // --- string line renderer ---
    [Header("String Line Renderer")]
    [SerializeField] private LineRenderer stringLineRenderer;     // assign in Inspector on this GO
    [SerializeField] private float stringLineWidth      = 0.04f;
    [SerializeField] private Color stringLineColorStart = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color stringLineColorEnd   = new Color(1f, 0.3f, 0.1f, 0.6f);
    [SerializeField] private float stringWobbleAmount   = 0.06f;  // subtle wobble mid-point
    [SerializeField] private float stringWobbleSpeed    = 8f;

    private Transform p1ArmTransform;
    private Transform p2ArmTransform;
    private ParticleSystem p1StringTrail;
    private ParticleSystem p2StringTrail;

    // --- events ---
    public UnityEvent<int>   OnKnockdownStarted;
    public UnityEvent<int>   OnPlayerRecovered;
    public UnityEvent<int>   OnArmDetached;
    public UnityEvent<float> OnTugProgressChanged;
    public UnityEvent<int>   OnMashProgress;

    // --- refs ---
    private MultiplayerPlayerController p1Controller;
    private MultiplayerPlayerController p2Controller;
    private PlayerHealth p1Health;
    private PlayerHealth p2Health;
    private CombatSystem p1Combat;
    private CombatSystem p2Combat;
    private VoodooPhysicsLayer p1Physics;
    private VoodooPhysicsLayer p2Physics;
    private CombatVFX p1VFX;
    private CombatVFX p2VFX;
    private bool playersResolved = false;

    // --- state ---
    public KnockdownState CurrentState { get; private set; } = KnockdownState.None;

    private int   downedPlayerID    = 0;
    private int   attackerPlayerID  = 0;
    private int   mashCount         = 0;
    private float tugProgress       = 0f;
    private float recoveryTimer     = 0f;
    private bool  p1ArmGone         = false;
    private bool  p2ArmGone         = false;
    private bool  p1GetUpPressed    = false;
    private bool  p2GetUpPressed    = false;

    private Coroutine rollCoroutine     = null;
    private Coroutine fallCoroutine     = null;
    private bool      isTuggingThisFrame = false;   // read in Update, written in HandleTugInput

    // -------------------------------------------------------

    private void Update()
    {
        if (!playersResolved)
        {
            TryResolvePlayerReferences();
            return;
        }

        if (CurrentState != KnockdownState.Downed && CurrentState != KnockdownState.TugOfWar) return;

        HandleMashInput();
        HandleTugInput();
        TickRecoveryTimer();
        UpdateStringLine();  // draw/hide line renderer each frame
    }

    public void OnP1GetUp() => p1GetUpPressed = true;
    public void OnP2GetUp() => p2GetUpPressed = true;

    // -------------------------------------------------------
    // resolve refs
    // -------------------------------------------------------

    private void TryResolvePlayerReferences()
    {
        var controllers = FindObjectsByType<MultiplayerPlayerController>(FindObjectsSortMode.None);

        foreach (var c in controllers)
        {
            if (c.PlayerID == 1) { p1Controller = c; p1Health = c.GetComponent<PlayerHealth>(); p1Combat = c.GetComponent<CombatSystem>(); p1Physics = c.GetComponent<VoodooPhysicsLayer>(); p1VFX = c.GetComponent<CombatVFX>(); }
            if (c.PlayerID == 2) { p2Controller = c; p2Health = c.GetComponent<PlayerHealth>(); p2Combat = c.GetComponent<CombatSystem>(); p2Physics = c.GetComponent<VoodooPhysicsLayer>(); p2VFX = c.GetComponent<CombatVFX>(); }
        }

        if (p1Controller == null || p2Controller == null) return;

        p1Controller.OnGetUpEvent += OnP1GetUp;
        p2Controller.OnGetUpEvent += OnP2GetUp;

        var arms = FindObjectsByType<PlayerArmMarker>(FindObjectsSortMode.None);
        foreach (var arm in arms)
        {
            if (arm.PlayerID == 1) { p1ArmTransform = arm.transform; p1StringTrail = arm.StringTrail; }
            if (arm.PlayerID == 2) { p2ArmTransform = arm.transform; p2StringTrail = arm.StringTrail; }
        }

        // set up line renderer appearance once
        if (stringLineRenderer != null)
        {
            stringLineRenderer.positionCount = 3; // start, mid wobble point, end
            stringLineRenderer.startWidth    = stringLineWidth;
            stringLineRenderer.endWidth      = stringLineWidth * 0.5f;
            stringLineRenderer.startColor    = stringLineColorStart;
            stringLineRenderer.endColor      = stringLineColorEnd;
            stringLineRenderer.enabled       = false;
        }

        playersResolved = true;
    }

    private void OnDestroy()
    {
        if (p1Controller != null) p1Controller.OnGetUpEvent -= OnP1GetUp;
        if (p2Controller != null) p2Controller.OnGetUpEvent -= OnP2GetUp;
    }

    // -------------------------------------------------------
    // entry point
    // -------------------------------------------------------

    public void StartKnockdown(int downedID)
    {
        if (CurrentState != KnockdownState.None) return;

        downedPlayerID   = downedID;
        attackerPlayerID = downedID == 1 ? 2 : 1;
        mashCount        = 0;
        tugProgress      = 0f;
        recoveryTimer    = 0f;
        CurrentState     = KnockdownState.Downed;
        p1GetUpPressed   = false;
        p2GetUpPressed   = false;

        GetController(downedPlayerID)?.SetMovementEnabled(false);
        GetCombat(downedPlayerID)?.SetCombatEnabled(false);
        GetPhysics(downedPlayerID)?.SetPhysicsEnabled(false);

        // play knockdown anim then start the fallback timer in parallel
        GetController(downedPlayerID)?.animationScript.PlayKnockDown();
        if (fallCoroutine != null) StopCoroutine(fallCoroutine);
        fallCoroutine = StartCoroutine(FallFlatFallback(downedID));

        ApplyKnockdownKnockback(downedID);

        AudioManager.Instance?.Play(AudioManager.Instance.knockdownFall, 1f, 0.05f);
        CameraShake.Instance?.Shake(knockdownShakeDuration, knockdownShakeMagnitude);
        GetVFX(downedID)?.PlayKnockdownDust();

        OnKnockdownStarted?.Invoke(downedID);
        Debug.Log($"[Knockdown] P{downedID} is down");
    }

    // -------------------------------------------------------
    // knockback
    // -------------------------------------------------------

    private void ApplyKnockdownKnockback(int downedID)
    {
        var downedCtrl   = GetController(downedID);
        var attackerCtrl = GetController(downedID == 1 ? 2 : 1);
        if (downedCtrl == null || attackerCtrl == null) return;

        Vector3 diff  = downedCtrl.transform.position - attackerCtrl.transform.position;
        diff.y        = 0f;
        Vector3 dir   = diff.magnitude > 0.01f ? diff.normalized : Vector3.right;
        Vector3 force = (dir + Vector3.up * 0.3f).normalized * knockbackForce;

        downedCtrl.ApplyKnockback(force);
    }

    // -------------------------------------------------------
    // fall flat fallback
    // waits a short window to let the knockdown animation start playing.
    // if the visual slot z-rotation hasn't changed meaningfully after that window
    // (i.e. the anim isn't driving it), we rotate it 90° ourselves.
    // -------------------------------------------------------

    private IEnumerator FallFlatFallback(int playerID)
    {
        var ctrl = GetController(playerID);
        if (ctrl == null) yield break;

        Transform visual = ctrl.GetVisualSlot();
        if (visual == null) yield break;

        // give the anim a moment to start
        yield return new WaitForSeconds(animWaitBeforeFall);

        // check if the animation is already tilting the character
        // if z rotation is still near upright, the anim hasn't kicked in — do it manually
        float currentZ = visual.localRotation.eulerAngles.z;
        bool animIsHandlingIt = currentZ > 15f && currentZ < 345f; // outside ~15° of upright

        if (!animIsHandlingIt)
        {
            Debug.Log($"[Knockdown] P{playerID} knockdown anim not detected — applying fall flat fallback");

            Quaternion startRot = visual.localRotation;

            // fall direction: away from attacker (same logic used for knockback)
            var attackerCtrl = GetController(attackerPlayerID);
            float dir = 1f;
            if (attackerCtrl != null)
                dir = ctrl.transform.position.x >= attackerCtrl.transform.position.x ? 1f : -1f;

            Quaternion endRot = Quaternion.Euler(0f, 0f, fallRotateAngle * dir);

            float elapsed = 0f;
            while (elapsed < fallRotateDuration)
            {
                // stop if the state has already ended (e.g. player recovered instantly)
                if (CurrentState == KnockdownState.None) yield break;

                elapsed += Time.deltaTime;
                float t  = Mathf.SmoothStep(0f, 1f, elapsed / fallRotateDuration);
                visual.localRotation = Quaternion.Lerp(startRot, endRot, t);
                yield return null;
            }

            visual.localRotation = endRot;
        }
    }

    // -------------------------------------------------------
    // update loops
    // -------------------------------------------------------

    private void HandleMashInput()
    {
        bool pressed = downedPlayerID == 1 ? p1GetUpPressed : p2GetUpPressed;

        if (downedPlayerID == 1) p1GetUpPressed = false;
        else                     p2GetUpPressed = false;

        if (!pressed) return;

        mashCount++;
        OnMashProgress?.Invoke(mashCount);

        AudioManager.Instance?.Play(AudioManager.Instance.mashGetUp, 0.6f, 0.12f);
        Debug.Log($"[Knockdown] P{downedPlayerID} mashed {mashCount}/{mashesRequired}");

        if (mashCount >= mashesRequired)
            Recover();
    }

    private void HandleTugInput()
    {
        if (CurrentState != KnockdownState.Downed) return;

        var attackerController = GetController(attackerPlayerID);
        if (attackerController == null) return;

        bool isTugging = attackerController.BlockHeld;
        isTuggingThisFrame = isTugging; // expose to UpdateStringLine

        // particle trail on downed player's arm
        var trail = downedPlayerID == 1 ? p1StringTrail : p2StringTrail;
        if (trail != null)
        {
            if (isTugging  && !trail.isPlaying) trail.Play();
            if (!isTugging && trail.isPlaying)  trail.Stop();
        }

        if (isTugging)
        {
            tugProgress += tugProgressPerSecond * Time.deltaTime;
            GetHealth(downedPlayerID)?.TakeDamage(tugDamagePerSecond * Time.deltaTime);
            AudioManager.Instance?.StartStringPullLoop();
        }
        else
        {
            tugProgress -= tugDecayPerSecond * Time.deltaTime;
            AudioManager.Instance?.StopStringPullLoop();
        }

        tugProgress = Mathf.Clamp01(tugProgress);
        OnTugProgressChanged?.Invoke(tugProgress);
        UpdateArmPosition(downedPlayerID, tugProgress);

        if (tugProgress >= 1f)
            DetachArm();
    }

    // -------------------------------------------------------
    // line renderer — drawn from attacker hand toward downed arm
    // -------------------------------------------------------

    private void UpdateStringLine()
    {
        if (stringLineRenderer == null) return;

        if (!isTuggingThisFrame)
        {
            stringLineRenderer.enabled = false;
            return;
        }

        // find arm transforms
        var downedArmTransform    = downedPlayerID    == 1 ? p1ArmTransform : p2ArmTransform;
        var attackerArmTransform  = attackerPlayerID  == 1 ? p1ArmTransform : p2ArmTransform;

        if (downedArmTransform == null || attackerArmTransform == null)
        {
            // fall back: use player root positions
            var downedCtrl   = GetController(downedPlayerID);
            var attackerCtrl = GetController(attackerPlayerID);
            if (downedCtrl == null || attackerCtrl == null) return;

            SetStringLinePositions(
                attackerCtrl.transform.position + Vector3.up * 0.8f,
                downedCtrl.transform.position   + Vector3.up * 0.5f
            );
        }
        else
        {
            SetStringLinePositions(attackerArmTransform.position, downedArmTransform.position);
        }

        stringLineRenderer.enabled = true;
    }

    private void SetStringLinePositions(Vector3 from, Vector3 to)
    {
        // mid-point with a small wobble so it looks like a dangling string, not a laser
        Vector3 mid     = (from + to) * 0.5f;
        float   wobbleX = Mathf.Sin(Time.time * stringWobbleSpeed) * stringWobbleAmount;
        float   wobbleY = Mathf.Cos(Time.time * stringWobbleSpeed * 0.7f) * stringWobbleAmount;
        mid += new Vector3(wobbleX, wobbleY, 0f);

        stringLineRenderer.SetPosition(0, from);
        stringLineRenderer.SetPosition(1, mid);
        stringLineRenderer.SetPosition(2, to);
    }

    private void TickRecoveryTimer()
    {
        recoveryTimer += Time.deltaTime;

        if (recoveryTimer >= recoveryTimeLimit)
        {
            Debug.Log($"[Knockdown] P{downedPlayerID} ran out of time");
            DetachArm();
        }
    }

    // -------------------------------------------------------
    // outcomes
    // -------------------------------------------------------

    private void Recover()
    {
        if (CurrentState == KnockdownState.None) return;

        CurrentState = KnockdownState.Recovered;
        StopStringTrails();
        HideStringLine();

        // reset the fall flat rotation before playing get up
        ResetVisual(downedPlayerID);

        GetController(downedPlayerID)?.animationScript.PlayGetUp();
        StartCoroutine(ReEnableAfterGetUp(downedPlayerID, armDetached: false));
    }

    private void DetachArm()
    {
        if (CurrentState == KnockdownState.None) return;

        CurrentState = KnockdownState.ArmDetached;
        StopStringTrails();
        HideStringLine();

        var armTransform = downedPlayerID == 1 ? p1ArmTransform : p2ArmTransform;
        if (armTransform != null)
            armTransform.localPosition = armDetachedOffset;

        if (downedPlayerID == 1) p1ArmGone = true;
        else                     p2ArmGone = true;

        GetCombat(downedPlayerID)?.SetHeavyAttackEnabled(false);

        AudioManager.Instance?.Play(AudioManager.Instance.armDetach, 1f, 0.04f);
        CameraShake.Instance?.Shake(armDetachShakeDuration, armDetachShakeMagnitude);
        HitStop.Instance?.Freeze(armDetachHitStopDur);

        var armT = downedPlayerID == 1 ? p1ArmTransform : p2ArmTransform;
        if (armT != null) GetVFX(downedPlayerID)?.PlayArmDetach(armT.position);

        ResetVisual(downedPlayerID);

        GetController(downedPlayerID)?.animationScript.PlayGetUp();
        StartCoroutine(ReEnableAfterGetUp(downedPlayerID, armDetached: true));
    }

    private IEnumerator ReEnableAfterGetUp(int playerID, bool armDetached)
    {
        yield return new WaitForSeconds(getUpDuration);

        GetController(playerID)?.SetMovementEnabled(true);
        GetCombat(playerID)?.SetCombatEnabled(true);
        GetPhysics(playerID)?.SetPhysicsEnabled(true);

        if (!armDetached)
        {
            SnapArmBack(playerID);
            AudioManager.Instance?.Play(AudioManager.Instance.standUpSuccess, 1f);
            OnPlayerRecovered?.Invoke(playerID);
            Debug.Log($"[Knockdown] P{playerID} recovered!");
        }
        else
        {
            OnArmDetached?.Invoke(playerID);
            Debug.Log($"[Knockdown] P{playerID} arm detached — no more heavy attack!");
        }

        CurrentState = KnockdownState.None;
    }

    // -------------------------------------------------------
    // visual helpers
    // -------------------------------------------------------

    // snaps the visual slot back upright before get-up anim plays
    private void ResetVisual(int playerID)
    {
        var ctrl = GetController(playerID);
        if (ctrl == null) return;
        Transform visual = ctrl.GetVisualSlot();
        if (visual == null) return;
        visual.localRotation = Quaternion.identity;
    }

    private void UpdateArmPosition(int playerID, float progress)
    {
        var armTransform = playerID == 1 ? p1ArmTransform : p2ArmTransform;
        if (armTransform == null) return;
        armTransform.localPosition = Vector3.Lerp(Vector3.zero, armDetachedOffset, progress);
    }

    private void SnapArmBack(int playerID)
    {
        var armTransform = playerID == 1 ? p1ArmTransform : p2ArmTransform;
        if (armTransform != null)
            armTransform.localPosition = Vector3.zero;
    }

    private void HideStringLine()
    {
        isTuggingThisFrame = false;
        if (stringLineRenderer != null)
            stringLineRenderer.enabled = false;
    }

    // -------------------------------------------------------

    public void ResetKnockdown()
    {
        CurrentState     = KnockdownState.None;
        downedPlayerID   = 0;
        attackerPlayerID = 0;
        mashCount        = 0;
        tugProgress      = 0f;
        recoveryTimer    = 0f;
        p1ArmGone        = false;
        p2ArmGone        = false;

        StopStringTrails();
        HideStringLine();

        if (fallCoroutine != null) { StopCoroutine(fallCoroutine); fallCoroutine = null; }

        p1Combat?.SetHeavyAttackEnabled(true);
        p2Combat?.SetHeavyAttackEnabled(true);
        SnapArmBack(1);
        SnapArmBack(2);
        ResetVisual(1);
        ResetVisual(2);

        Debug.Log("[Knockdown] reset");
    }

    public bool isArmGone(int playerID) => playerID == 1 ? p1ArmGone : p2ArmGone;

    // -------------------------------------------------------
    // helpers
    // -------------------------------------------------------

    private void StopStringTrails()
    {
        p1StringTrail?.Stop();
        p2StringTrail?.Stop();
        AudioManager.Instance?.StopStringPullLoop();
    }

    private MultiplayerPlayerController GetController(int id) => id == 1 ? p1Controller : p2Controller;
    private PlayerHealth                GetHealth(int id)     => id == 1 ? p1Health      : p2Health;
    private CombatSystem                GetCombat(int id)     => id == 1 ? p1Combat       : p2Combat;
    private VoodooPhysicsLayer          GetPhysics(int id)    => id == 1 ? p1Physics      : p2Physics;
    private CombatVFX                   GetVFX(int id)        => id == 1 ? p1VFX          : p2VFX;
}