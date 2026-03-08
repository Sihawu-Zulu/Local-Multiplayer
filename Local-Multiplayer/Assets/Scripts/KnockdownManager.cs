using System.Collections;
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
    // [SerializeField] private float fallRotateDuration = 0.25f; 
    // [SerializeField] private float fallRotateAngle = 90f;   

    // [Header("Roll / Tumble Settings")]
    // [SerializeField] private float rollDuration = 0.18f;  // how long each roll takes
    // [SerializeField] private float rollAngle = 360f;   // full spin per mash
    // [SerializeField] private float rollMoveDistance = 0.4f;   // how far they slide per roll

    [Header("Animation stuff")]
    private AnimationManager animatorScript;
     [SerializeField] private float getUpDuration = 1.2f;

  
    private Transform p1ArmTransform;
    private Transform p2ArmTransform;
    private ParticleSystem p1StringTrail;
    private ParticleSystem p2StringTrail;

    // --- events ---
    public UnityEvent<int> OnKnockdownStarted;
    public UnityEvent<int> OnPlayerRecovered;
    public UnityEvent<int> OnArmDetached;
    public UnityEvent<float> OnTugProgressChanged;
    public UnityEvent<int> OnMashProgress;

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

    private int downedPlayerID = 0;
    private int attackerPlayerID = 0;
    private int mashCount = 0;
    private float tugProgress = 0f;
    private float recoveryTimer = 0f;
    private bool p1ArmGone = false;
    private bool p2ArmGone = false;
    private bool p1GetUpPressed = false;
    private bool p2GetUpPressed = false;

    private Coroutine rollCoroutine = null;

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

        // grab arm transforms and trails from the marker components on the prefabs
        var arms = FindObjectsByType<PlayerArmMarker>(FindObjectsSortMode.None);
        foreach (var arm in arms)
        {
            if (arm.PlayerID == 1) { p1ArmTransform = arm.transform; p1StringTrail = arm.StringTrail; }
            if (arm.PlayerID == 2) { p2ArmTransform = arm.transform; p2StringTrail = arm.StringTrail; }
        }

        playersResolved = true;
    }

    private void OnDestroy()
    {
        if (p1Controller != null) p1Controller.OnGetUpEvent -= OnP1GetUp;
        if (p2Controller != null) p2Controller.OnGetUpEvent -= OnP2GetUp;
    }

   
    // entry point

    public void StartKnockdown(int downedID)
    {
        if (CurrentState != KnockdownState.None) return;

        downedPlayerID = downedID;
        attackerPlayerID = downedID == 1 ? 2 : 1;
        mashCount = 0;
        tugProgress = 0f;
        recoveryTimer = 0f;
        CurrentState = KnockdownState.Downed;
        p1GetUpPressed = false;
        p2GetUpPressed = false;

        GetController(downedPlayerID)?.SetMovementEnabled(false);
        GetCombat(downedPlayerID)?.SetCombatEnabled(false);
        GetPhysics(downedPlayerID)?.SetPhysicsEnabled(false); 

        GetController(downedPlayerID)?.animationScript.PlayGetUp();

        // push the downed player away from the attacker then tip them flat
        ApplyKnockdownKnockback(downedID);

           GetController(downedPlayerID)?.animationScript.PlayKnockDown(); //using animation now not that shit transform shit
        //animatorScript.PlayKnockdown();
        // StartCoroutine(FallFlat(downedID));

        // juice on knockdown
        AudioManager.Instance?.Play(AudioManager.Instance.knockdownFall, 1f, 0.05f);
        CameraShake.Instance?.Shake(knockdownShakeDuration, knockdownShakeMagnitude);

        // vfx dust at the downed player's feet
        GetVFX(downedID)?.PlayKnockdownDust();

        OnKnockdownStarted?.Invoke(downedID);
        Debug.Log($"[Knockdown] P{downedID} is down");

    }

    // -------------------------------------------------------
    // knockback + fall flat
    // -------------------------------------------------------

    private void ApplyKnockdownKnockback(int downedID)
    {
        var downedCtrl = GetController(downedID);
        var attackerCtrl = GetController(downedID == 1 ? 2 : 1);
        if (downedCtrl == null || attackerCtrl == null) return;

        // direction away from the attacker, slight upward pop
        Vector3 diff = downedCtrl.transform.position - attackerCtrl.transform.position;
        diff.y = 0f;
        Vector3 dir = diff.magnitude > 0.01f ? diff.normalized : Vector3.right;
        Vector3 force = (dir + Vector3.up * 0.3f).normalized * knockbackForce;

        downedCtrl.ApplyKnockback(force);
    }

    // rotates the character visual slot 90° on z so they lie flat
    // private IEnumerator FallFlat(int playerID)
    // {
    //     var ctrl = GetController(playerID);
    //     if (ctrl == null) yield break;

    //     Transform visual = ctrl.GetVisualSlot();
    //     if (visual == null) yield break;


    //     Quaternion startRot = visual.localRotation;
    //     // fall in a random left/right direction for variety
    //     float dir = Random.value > 0.5f ? 1f : -1f;
    //     Quaternion endRot = Quaternion.Euler(0f, 0f, fallRotateAngle * dir);

    //     float elapsed = 0f;
    //     while (elapsed < fallRotateDuration)
    //     {
    //         elapsed += Time.deltaTime;
    //         visual.localRotation = Quaternion.Lerp(startRot, endRot, elapsed / fallRotateDuration);
    //         yield return null;
    //     }

    //     visual.localRotation = endRot;
    // }

    // -------------------------------------------------------
    // update loops
    // -------------------------------------------------------

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

        // // each mash does a little tumble roll
        // if (rollCoroutine != null) StopCoroutine(rollCoroutine);
        // rollCoroutine = StartCoroutine(TumbleRoll(downedPlayerID));

        // Debug.Log($"[Knockdown] P{downedPlayerID} mashed {mashCount}/{mashesRequired}");

        // // debug stubs for get-up animation triggers — replace with animator calls when ready
        // if (mashCount == 1)
        //     Debug.Log($"[Knockdown] P{downedPlayerID} ANIM: start struggling/rolling");

        // if (mashCount >= mashesRequired / 2)
        //     Debug.Log($"[Knockdown] P{downedPlayerID} ANIM: getting up — halfway there");

        // if (mashCount >= mashesRequired)
        // {
        //     Debug.Log($"[Knockdown] P{downedPlayerID} ANIM: stand up");
        //     Recover();
        // }
    }

    // spins the visual one full rotation on z in a random direction and slides them slightly
    // private IEnumerator TumbleRoll(int playerID)
    // {
    //     var ctrl = GetController(playerID);
    //     if (ctrl == null) yield break;

    //     Transform visual = ctrl.GetVisualSlot();
    //     if (visual == null) yield break;

    //     float dir = Random.value > 0.5f ? 1f : -1f;
    //     Quaternion startRot = visual.localRotation;
    //     Vector3 startPos = visual.localPosition;
    //     Vector3 targetPos = startPos + new Vector3(dir * rollMoveDistance, 0f, 0f);

    //     float elapsed = 0f;
    //     while (elapsed < rollDuration)
    //     {
    //         elapsed += Time.deltaTime;
    //         float t = elapsed / rollDuration;

    //         // spin adds on top of whatever the current flat rotation is
    //         visual.localRotation = startRot * Quaternion.Euler(0f, 0f, rollAngle * dir * t);
    //         visual.localPosition = Vector3.Lerp(startPos, targetPos, t);

    //         yield return null;
    //     }

    //     // settle at the new position, keep whatever z rotation we landed on
    //     visual.localPosition = targetPos;
    // }

    private void HandleTugInput()
    {
        // string pulling only happens while the player is on the floor
        if (CurrentState != KnockdownState.Downed) return;

        var attackerController = GetController(attackerPlayerID);
        if (attackerController == null) return;

        bool isTugging = attackerController.BlockHeld;

        // trail comes out of the downed player arm
        var trail = downedPlayerID == 1 ? p1StringTrail : p2StringTrail;
        if (trail != null)
        {
            if (isTugging && !trail.isPlaying) trail.Play();
            if (!isTugging && trail.isPlaying) trail.Stop();
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

        GetController(downedPlayerID)?.animationScript.PlayGetUp();
        StartCoroutine(ReEnableAfterGetUp(downedPlayerID, armDetached: false));

        // stand back up
        // StartCoroutine(StandUp(downedPlayerID, onComplete: () =>
        // {
        //     GetController(downedPlayerID)?.SetMovementEnabled(true);
        //     GetCombat(downedPlayerID)?.SetCombatEnabled(true);
        //     GetPhysics(downedPlayerID)?.SetPhysicsEnabled(true);
        //     SnapArmBack(downedPlayerID);
        //     AudioManager.Instance?.Play(AudioManager.Instance.standUpSuccess, 1f);
        //     OnPlayerRecovered?.Invoke(downedPlayerID);
        //     Debug.Log($"[Knockdown] P{downedPlayerID} recovered!");
        //     CurrentState = KnockdownState.None;
        // }));

    }

    // tweens the visual back upright before re-enabling control
    // private IEnumerator StandUp(int playerID, System.Action onComplete)
    // {
    //     var ctrl = GetController(playerID);
    //     if (ctrl == null) { onComplete?.Invoke(); yield break; }

    //     Transform visual = ctrl.GetVisualSlot();
    //     if (visual == null) { onComplete?.Invoke(); yield break; }

    //     Quaternion startRot = visual.localRotation;
    //     Quaternion endRot = Quaternion.identity;
    //     Vector3 startPos = visual.localPosition;

    //     float elapsed = 0f;
    //     while (elapsed < fallRotateDuration)
    //     {
    //         elapsed += Time.deltaTime;
    //         float t = elapsed / fallRotateDuration;
    //         visual.localRotation = Quaternion.Lerp(startRot, endRot, t);
    //         visual.localPosition = Vector3.Lerp(startPos, Vector3.zero, t);
    //         yield return null;
    //     }

    //     visual.localRotation = Quaternion.identity;
    //     visual.localPosition = Vector3.zero;

    //     onComplete?.Invoke();
    // }

    private void DetachArm()
    {
        if (CurrentState == KnockdownState.None) return;

        CurrentState = KnockdownState.ArmDetached;
        StopStringTrails();

        var armTransform = downedPlayerID == 1 ? p1ArmTransform : p2ArmTransform;
        if (armTransform != null)
            armTransform.localPosition = armDetachedOffset;

        if (downedPlayerID == 1) p1ArmGone = true;
        else                     p2ArmGone = true;

        GetCombat(downedPlayerID)?.SetHeavyAttackEnabled(false);

        // juice — arm detach is a big moment
        AudioManager.Instance?.Play(AudioManager.Instance.armDetach, 1f, 0.04f);
        CameraShake.Instance?.Shake(armDetachShakeDuration, armDetachShakeMagnitude);
        HitStop.Instance?.Freeze(armDetachHitStopDur);

        var armT = downedPlayerID == 1 ? p1ArmTransform : p2ArmTransform;
        if (armT != null) GetVFX(downedPlayerID)?.PlayArmDetach(armT.position);

        // still get up — just without heavy attack
        GetController(downedPlayerID)?.animationScript.PlayGetUp();
        StartCoroutine(ReEnableAfterGetUp(downedPlayerID, armDetached: true));
    }


    /// <summary>
    /// Waits for the GetUp animation to finish then re-enables the player.
    /// Set getUpDuration in the Inspector to match your GetUp clip length.
    /// </summary>
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

    
    // arm visual
    

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


    public void ResetKnockdown()
    {
        CurrentState = KnockdownState.None;
        downedPlayerID = 0;
        attackerPlayerID = 0;
        mashCount = 0;
        tugProgress = 0f;
        recoveryTimer = 0f;
        p1ArmGone = false;
        p2ArmGone = false;

        StopStringTrails();
        p1Combat?.SetHeavyAttackEnabled(true);
        p2Combat?.SetHeavyAttackEnabled(true);
        SnapArmBack(1);
        SnapArmBack(2);

        // make sure both visuals are upright
        // ResetVisual(1);
        // ResetVisual(2);

        Debug.Log("[Knockdown] reset");
    }

    public bool isArmGone (int playerID) => playerID == 1 ? p1ArmGone : p2ArmGone;

    // private void ResetVisual(int playerID)
    // {
    //     var ctrl = GetController(playerID);
    //     if (ctrl == null) return;
    //     Transform visual = ctrl.GetVisualSlot();
    //     if (visual == null) return;
    //     visual.localRotation = Quaternion.identity;
    //     visual.localPosition = Vector3.zero;
    // }

    // public bool IsArmGone(int playerID) => playerID == 1 ? p1ArmGone : p2ArmGone;

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
    private PlayerHealth GetHealth(int id) => id == 1 ? p1Health : p2Health;
    private CombatSystem GetCombat(int id) => id == 1 ? p1Combat : p2Combat;
    private VoodooPhysicsLayer GetPhysics(int id) => id == 1 ? p1Physics : p2Physics;
    private CombatVFX GetVFX(int id) => id == 1 ? p1VFX : p2VFX;
}