using System.Collections;
using UnityEngine;

// Charge while inching toward target, perform rapid barrages, allow parry/dodge window to stun,
// then leap to target and perform an unavoidable final attack.
public class LeapAdvanceBarrageSkill : Skill
{
    [Header("Refs")]
    public Rigidbody2D rb;
    public Transform owner; // boss root

    [Header("Targeting")]
    public string targetTag = "Player";
    public float minApproachDistance = 3.5f;
    public float maxApproachDistance = 8f;
    [Tooltip("Skill only starts if target distance is within this range.")]
    public float activateMinDistance = 0f;
    public float activateMaxDistance = 12f;

    [Header("Charge\n& Approach")]
    public float chargeDuration = 2.0f;
    public float approachSpeed = 3.0f;

    [Header("Barrage")]
    public int barrageCount = 6; // rapid attacks during charge window
    public float barrageInterval = 0.2f;
    public float barrageRange = 1.2f;
    public int requiredSuccessfulDodges = 3; // if player dodges/parries >= this, boss gets stunned
    public KeyCode playerDodgeKey = KeyCode.E; // temporary input hook to simulate dodge

    [Header("Leap & Final")]
    public float leapForce = 22f;
    public float leapUpward = 4f;
    public float finalAttackRange = 1.5f;
    public bool landNextToTarget = true; // place boss near target instead of just applying velocity
    public float landSideOffset = 1.2f;  // distance to keep from target on landing
    public bool loopCombat = true; // after final attack, restart sequence if target still valid

    [Header("FX/Hooks (optional)")]
    public GameObject barrageVfx;
    public GameObject finalVfx;

    [Header("Upright")]
    public bool keepUpright = true; // keep boss upright (no tilt)
    public Transform uprightTransform; // which transform to keep upright (defaults to owner)

    [Header("Return/Anchor")]
    public Transform returnPoint; // optional general anchor for this skill if needed later

    private bool _running;
    private bool _stunned;
    private int _dodges;

    private Transform _cachedTarget;

    void Awake()
    {
        if (owner == null) owner = transform;
        if (rb == null) rb = GetComponentInParent<Rigidbody2D>();
        if (uprightTransform == null) uprightTransform = owner;
        if (rb != null)
        {
            rb.freezeRotation = true;
            rb.angularVelocity = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        if (keepUpright)
        {
            SetUpright();
        }
    }

    void LateUpdate()
    {
        if (!keepUpright) return;
        MaintainUpright();
    }

    protected override bool UseOn(Transform target)
    {
        if (_running) return false;
        if (target == null) target = FindTargetByTag();
        if (target == null) return false;
        // Distance gating: only activate if within configured range
        Transform ownerTf = owner != null ? owner : transform;
        float distToTarget = Vector2.Distance(ownerTf.position, target.position);
        if (distToTarget < activateMinDistance || distToTarget > activateMaxDistance)
        {
            return false;
        }
        _cachedTarget = target;
        StartCoroutine(RunSequence());
        return true;
    }

    protected override bool Use()
    {
        return UseOn(FindTargetByTag());
    }

    private IEnumerator RunSequence()
    {
        _running = true;
        while (true)
        {
            // Validate target and activation
            if (_cachedTarget == null) _cachedTarget = FindTargetByTag();
            if (!IsWithinActivationRange()) break;

            _stunned = false;
            _dodges = 0;

            float endCharge = Time.time + chargeDuration;
            float nextBarrage = Time.time;
            int shotsDone = 0;

            while (Time.time < endCharge)
            {
                if (keepUpright) MaintainUpright();

                // Approach slightly toward the target during charge
                if (_cachedTarget != null)
                {
                    Vector2 toTarget = (Vector2)(_cachedTarget.position - owner.position);
                    float dist = toTarget.magnitude;
                    if (dist > minApproachDistance)
                    {
                        float allowed = Mathf.Min(approachSpeed * Time.deltaTime, Mathf.Max(0f, dist - minApproachDistance));
                        Vector2 step = toTarget.normalized * allowed;
                        owner.position += (Vector3)step;
                    }
                }

                // Barrage window
                if (Time.time >= nextBarrage && shotsDone < barrageCount)
                {
                    DoBarrageAttack();
                    shotsDone++;
                    nextBarrage = Time.time + barrageInterval;
                }

                // Simulate dodge/parry via key (integration point for your dodge system)
                if (Input.GetKeyDown(playerDodgeKey))
                {
                    _dodges++;
                    if (_dodges >= requiredSuccessfulDodges)
                    {
                        _stunned = true;
                        break;
                    }
                }

                yield return null;
            }

            if (_stunned)
            {
                yield return StartCoroutine(DoStun());
                if (!loopCombat) break;
                else { yield return null; continue; }
            }

            // Leap to target and perform unavoidable final attack (stay near target, do not return)
            DoLeapToTarget();
            if (keepUpright) MaintainUpright();
            yield return new WaitForSeconds(0.15f);
            DoFinalAttackUnavoidable();

            if (!loopCombat) break;
            // brief delay before restarting
            yield return null;
        }
        _running = false;
    }

    private Transform FindTargetByTag()
    {
        GameObject p = GameObject.FindWithTag(targetTag);
        return p != null ? p.transform : null;
    }

    private bool IsWithinActivationRange()
    {
        if (_cachedTarget == null) return false;
        Transform ownerTf = owner != null ? owner : transform;
        float dist = Vector2.Distance(ownerTf.position, _cachedTarget.position);
        if (dist < activateMinDistance || dist > activateMaxDistance) return false;
        return true;
    }

    private void DoBarrageAttack()
    {
        if (barrageVfx != null)
        {
            Instantiate(barrageVfx, owner.position, Quaternion.identity);
        }
        // Here you can raycast/overlap to damage if within barrageRange
        // kept simple: visual + hook point
    }

    private IEnumerator DoStun()
    {
        float stunTime = 1.25f;
        float end = Time.time + stunTime;
        Vector2 origVel = rb != null ? rb.velocity : Vector2.zero;
        if (rb != null) rb.velocity = Vector2.zero;
        while (Time.time < end)
        {
            yield return null;
        }
        if (rb != null) rb.velocity = origVel;
    }

    private void DoLeapToTarget()
    {
        Vector2 dir = Vector2.right;
        Vector3 destPos = owner.position;
        if (_cachedTarget != null)
        {
            dir = (Vector2)(_cachedTarget.position - owner.position).normalized;
            if (landNextToTarget)
            {
                // Land slightly next to target (stay close, don't overlap)
                Vector3 targetPos = _cachedTarget.position;
                Vector3 sideOffset = (Vector3)(dir.normalized * Mathf.Max(0.1f, landSideOffset));
                destPos = targetPos - sideOffset;
            }
        }

        if (landNextToTarget)
        {
            if (rb != null)
            {
                rb.MovePosition(new Vector2(destPos.x, destPos.y));
                rb.velocity = Vector2.zero;
            }
            else
            {
                owner.position = destPos;
            }
        }
        else
        {
            if (rb != null)
            {
                Vector2 vel = dir * leapForce + Vector2.up * leapUpward;
                rb.velocity = vel;
            }
        }

        if (rb != null) rb.angularVelocity = 0f;
        if (keepUpright) SetUpright();
    }

    private void DoFinalAttackUnavoidable()
    {
        if (finalVfx != null)
        {
            Object.Instantiate(finalVfx, owner.position, Quaternion.identity);
        }
        // Mark as unavoidable: you can route to your combat system with a flag that ignores dodge/parry
        // e.g., CombatSystem.Instance.DoAttack(owner, target, damage, ignoreParry: true, ignoreDodge: true);
    }

    private void MaintainUpright()
    {
        if (rb != null)
        {
            rb.angularVelocity = 0f;
            rb.freezeRotation = true;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.SetRotation(0f);
        }
        SetUpright();
    }

    private void SetUpright()
    {
        Transform t = uprightTransform != null ? uprightTransform : owner;
        if (t == null) return;
        t.rotation = Quaternion.Euler(0f, 0f, 0f);
    }
}


