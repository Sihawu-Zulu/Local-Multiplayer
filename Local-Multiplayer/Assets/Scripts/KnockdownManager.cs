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


    [Header("Fall Flat Fallback")]
    [SerializeField] private float fallRotateDuration  = 0.22f;  
    [SerializeField] private float fallRotateAngle = 90f;    
    [SerializeField] private float animWaitBeforeFall  = 0.15f;   

    [Header("Animation stuff")]
    private AnimationManager animatorScript;
    [SerializeField] private float getUpDuration = 1.2f;

    
    [Header("String Line Renderer")]
    [SerializeField] private LineRenderer stringLineRenderer;     
    [SerializeField] private float stringLineWidth = 0.04f;
    [SerializeField] private Color stringLineColorStart = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private Color stringLineColorEnd   = new Color(1f, 0.3f, 0.1f, 0.6f);
    [SerializeField] private float stringWobbleAmount = 0.06f;  
    [SerializeField] private float stringWobbleSpeed = 8f;


    [Header("Player Colours")]
[SerializeField] private Color p1Color = new Color(0.9f, 0.2f, 0.2f); 
[SerializeField] private Color p2Color = new Color(0.2f, 0.4f, 0.9f);  

    private Transform p1ArmTransform;
    private Transform p2ArmTransform;
    private ParticleSystem p1StringTrail;
    private ParticleSystem p2StringTrail;
    private PlayerArmMarker p1ArmMarker;
    private PlayerArmMarker p2ArmMarker;

   
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

    private int   downedPlayerID  = 0;
    private int   attackerPlayerID = 0;
    private int   mashCount  = 0;
    private float tugProgress = 0f;
    private float recoveryTimer= 0f;
    private bool  p1ArmGone = false;
    private bool  p2ArmGone = false;
    private bool  p1GetUpPressed = false;
    private bool  p2GetUpPressed = false;

    private Coroutine rollCoroutine = null;
    private Coroutine fallCoroutine  = null;
    private bool      isTuggingThisFrame = false;   

   
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
        UpdateStringLine();  
    }

    public void OnP1GetUp() => p1GetUpPressed = true;
    public void OnP2GetUp() => p2GetUpPressed = true;



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
            if (arm.PlayerID == 1) { p1ArmTransform = arm.transform; p1StringTrail = arm.StringTrail; p1ArmMarker = arm; }
            if (arm.PlayerID == 2) { p2ArmTransform = arm.transform; p2StringTrail = arm.StringTrail; p2ArmMarker = arm; }
        }

        
        if (stringLineRenderer != null)
        {
            stringLineRenderer.positionCount = 3; 
            stringLineRenderer.startWidth  = stringLineWidth;
            stringLineRenderer.endWidth   = stringLineWidth * 0.5f;
            stringLineRenderer.startColor = stringLineColorStart;
            stringLineRenderer.endColor = stringLineColorEnd;
            stringLineRenderer.enabled = false;
        }

        playersResolved = true;
    }

    private void OnDestroy()
    {
        if (p1Controller != null) p1Controller.OnGetUpEvent -= OnP1GetUp;
        if (p2Controller != null) p2Controller.OnGetUpEvent -= OnP2GetUp;
    }

  

    public void StartKnockdown(int downedID)
    {
        if (CurrentState != KnockdownState.None) return;

        downedPlayerID   = downedID;
        attackerPlayerID = downedID == 1 ? 2 : 1;
        mashCount = 0;
        tugProgress = 0f;
        recoveryTimer = 0f;
        CurrentState  = KnockdownState.Downed;
        p1GetUpPressed = false;
        p2GetUpPressed = false;

        GetController(downedPlayerID)?.SetMovementEnabled(false);
        GetCombat(downedPlayerID)?.SetCombatEnabled(false);
        GetPhysics(downedPlayerID)?.SetPhysicsEnabled(false);

      
        GetController(downedPlayerID)?.animationScript.PlayKnockDown();
        if (fallCoroutine != null) StopCoroutine(fallCoroutine);
        fallCoroutine = StartCoroutine(FallFlatFallback(downedID));

        ApplyKnockdownKnockback(downedID);

        AudioManager.Instance?.Play(AudioManager.Instance.knockdownFall, 1f, 0.05f);
        CameraShake.Instance?.Shake(knockdownShakeDuration, knockdownShakeMagnitude);
        GetVFX(downedID)?.PlayKnockdownDust();

        OnKnockdownStarted?.Invoke(downedID);
        
    }

 

    private void ApplyKnockdownKnockback(int downedID)
    {
        var downedCtrl   = GetController(downedID);
        var attackerCtrl = GetController(downedID == 1 ? 2 : 1);
        if (downedCtrl == null || attackerCtrl == null) return;

        Vector3 diff  = downedCtrl.transform.position - attackerCtrl.transform.position;
        diff.y = 0f;
        Vector3 dir   = diff.magnitude > 0.01f ? diff.normalized : Vector3.right;
        Vector3 force = (dir + Vector3.up * 0.3f).normalized * knockbackForce;

        downedCtrl.ApplyKnockback(force);
    }



    private IEnumerator FallFlatFallback(int playerID)
    {
        var ctrl = GetController(playerID);
        if (ctrl == null) yield break;

        Transform visual = ctrl.GetVisualSlot();
        if (visual == null) yield break;


        yield return new WaitForSeconds(animWaitBeforeFall);


        float currentZ = visual.localRotation.eulerAngles.z;
        bool animIsHandlingIt = currentZ > 15f && currentZ < 345f; 

        if (!animIsHandlingIt)
        {
            Debug.Log($"[Knockdown] P{playerID} knockdown anim not there doing to fall  fallback");

            Quaternion startRot = visual.localRotation;

          
            var attackerCtrl = GetController(attackerPlayerID);
            float dir = 1f;
            if (attackerCtrl != null)
                dir = ctrl.transform.position.x >= attackerCtrl.transform.position.x ? 1f : -1f;

            Quaternion endRot = Quaternion.Euler(0f, 0f, fallRotateAngle * dir);

            float elapsed = 0f;
            while (elapsed < fallRotateDuration)
            {
              
                if (CurrentState == KnockdownState.None) yield break;

                elapsed += Time.deltaTime;
                float t  = Mathf.SmoothStep(0f, 1f, elapsed / fallRotateDuration);
                visual.localRotation = Quaternion.Lerp(startRot, endRot, t);
                yield return null;
            }

            visual.localRotation = endRot;
        }
    }


    private void HandleMashInput()
    {
        bool pressed = downedPlayerID == 1 ? p1GetUpPressed : p2GetUpPressed;

        if (downedPlayerID == 1) p1GetUpPressed = false;
        else                     
        p2GetUpPressed = false;

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
        isTuggingThisFrame = isTugging; 

        
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

  
private void UpdateStringLine()
{
    if (stringLineRenderer == null) return;

    if (!isTuggingThisFrame)
    {
        stringLineRenderer.enabled = false;
        return;
    }

    var downedArmTransform   = downedPlayerID   == 1 ? p1ArmTransform : p2ArmTransform;
    var attackerArmTransform = attackerPlayerID == 1 ? p1ArmTransform : p2ArmTransform;


    Color armColor = downedPlayerID == 1 ? p1Color : p2Color;
    Color fadedEnd = new Color(armColor.r, armColor.g, armColor.b, 0.3f);


    stringLineRenderer.startColor = fadedEnd;   
    stringLineRenderer.endColor   = armColor;   

  
    Vector3 from = attackerArmTransform != null
        ? attackerArmTransform.position
        : GetController(attackerPlayerID).transform.position + Vector3.up * 0.8f;

   
    Vector3 to = downedArmTransform != null
        ? downedArmTransform.position
        : GetController(downedPlayerID).transform.position + Vector3.up * 0.5f;

    SetStringLinePositions(from, to);
    stringLineRenderer.enabled = true;
}

    private void SetStringLinePositions(Vector3 from, Vector3 to)
    {
       
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
            // Debug.Log($"[Knockdown] P{downedPlayerID} ran out of time");
            DetachArm();
        }
    }



    private void Recover()
    {
        if (CurrentState == KnockdownState.None) return;

        CurrentState = KnockdownState.Recovered;
        StopStringTrails();
        HideStringLine();

      
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

        // Hide the arm renderers
        var armMarker = downedPlayerID == 1 ? p1ArmMarker : p2ArmMarker;
        armMarker?.SetArmVisible(false);

        if (downedPlayerID == 1) p1ArmGone = true;
        else                     
        p2ArmGone = true;

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
            // Debug.Log($"[Knockdown] P{playerID} recovered!");
        }
        else
        {
            OnArmDetached?.Invoke(playerID);
            // Debug.Log($"[Knockdown] P{playerID} arm detached...... no more heavy attack!");
        }

        CurrentState = KnockdownState.None;
    }


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

        // Restore the arm renderers
        var armMarker = playerID == 1 ? p1ArmMarker : p2ArmMarker;
        armMarker?.SetArmVisible(true);
    }

    private void HideStringLine()
    {
        isTuggingThisFrame = false;
        if (stringLineRenderer != null)
            stringLineRenderer.enabled = false;
    }

 

    public void ResetKnockdown()
    {
        CurrentState = KnockdownState.None;
        downedPlayerID   = 0;
        attackerPlayerID = 0;
        mashCount= 0;
        tugProgress  = 0f;
        recoveryTimer= 0f;
        p1ArmGone = false;
        p2ArmGone  = false;

        StopStringTrails();
        HideStringLine();

        if (fallCoroutine != null) { StopCoroutine(fallCoroutine); fallCoroutine = null; }

        p1Combat?.SetHeavyAttackEnabled(true);
        p2Combat?.SetHeavyAttackEnabled(true);
        SnapArmBack(1);
        SnapArmBack(2);
        ResetVisual(1);
        ResetVisual(2);

       
    }

    public bool isArmGone(int playerID) => playerID == 1 ? p1ArmGone : p2ArmGone;



    private void StopStringTrails()
    {
        if (p1StringTrail != null) p1StringTrail.Stop();
        if (p2StringTrail != null) p2StringTrail.Stop();
        AudioManager.Instance?.StopStringPullLoop();
    }

    private MultiplayerPlayerController GetController(int id) => id == 1 ? p1Controller : p2Controller;
    private PlayerHealth GetHealth(int id) => id == 1 ? p1Health : p2Health;
    private CombatSystem GetCombat(int id)  => id == 1 ? p1Combat: p2Combat;
    private VoodooPhysicsLayer GetPhysics(int id) => id == 1 ? p1Physics: p2Physics;
    private CombatVFX GetVFX(int id)  => id == 1 ? p1VFX : p2VFX;
}