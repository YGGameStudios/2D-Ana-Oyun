using System.Collections;
using UnityEngine;

public class LeapChargeAoeSkill : Skill
{
    [Header("Leap")]

    public float leapTravelTime = 0.6f;
    public float returnTravelTime = 0.6f;
    public bool faceLeapDirection = true;

    [Header("Charge & Damage")]
    public float chargeDuration = 3f;
    public int damageAmount = 20;

    [Header("Landing")]
    [Range(0f, 1f)]
    public float landingFactor = 0.85f; // 1 = tam oyuncu, 0.85 = biraz önüne
    // İniş ofseti otomatik hesaplanır; ekstra ayar değişkenleri yok

    [Header("Ground Check")]
    public LayerMask groundLayers = ~0;
    public float groundRayDistance = 3f;
    public float groundProbeDepth = 20f; // uzun tek ray ile boşluk/zemin kontrolü

    [Header("Arc")]
    public float arcHeight = 1.5f; // zıplama sırasında yukarı parabol

    [Header("Knockback")]
    public float knockbackRadius = 3f;
    public float knockbackForce = 8f;
    public LayerMask knockbackLayers = ~0; // all by default
    public float knockbackKinematicDisplacement = 1f;

    [Header("Manual Test Trigger")]
    public bool manualTrigger = true;
    public KeyCode manualKey = KeyCode.R;

    [Header("Auto Use")]
    public bool autoUse = false;
    public float autoInterval = 8f;
    public string autoTargetTag = "Player";

    [Header("Charge VFX")]
    public GameObject chargeEffectPrefab;
    public Vector3 chargeEffectLocalOffset = Vector3.zero;

    [Header("Movement Collision")]
    public bool disableCollisionsDuringMove = true;

    [Header("Debug Gizmos")]
    public bool debugGizmos = true;
    public Color gizmoStartColor = new Color(0.2f, 0.8f, 1f, 0.9f);
    public Color gizmoDestColor = new Color(0.2f, 1f, 0.2f, 0.9f);
    public Color gizmoPathColor = new Color(1f, 1f, 0.2f, 0.9f);
    public Color gizmoRayColor = new Color(1f, 0.6f, 0.2f, 0.9f);
    public Color gizmoHitColor = new Color(1f, 0.2f, 0.2f, 0.9f);
    public Color gizmoKnockbackColor = new Color(1f, 0.2f, 1f, 0.9f);
    public Color gizmoProbeColor = new Color(0.9f, 0.4f, 1f, 0.9f);

    private bool _isRunning;
    private Rigidbody2D _rb;
    private Collider2D[] _colliders;
    private GameObject _chargeEffectInstance;
    private bool _knockbackQueued;
    private Vector2 _knockbackCenter;
    private bool _manualQueued;
    private float _halfHeight;
    private float _nextAutoTime;

    // Debug state
    private Vector3 _debugStart;
    private Vector3 _debugDest;
    private Vector3 _debugReturnStart;
    private Vector3 _debugReturnDest;
    private Vector2 _debugRayOrigin;
    private bool _debugHasHit;
    private Vector2 _debugHitPoint;
    private Vector2 _debugProbeOrigin;
    private bool _debugProbeHasHit;
    private Vector2 _debugProbeHitPoint;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _colliders = GetComponents<Collider2D>();
        // collider'lardan yarım yükseklik (merkezden tabana) hesapla
        _halfHeight = 0f;
        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] == null) continue;
                Bounds b = _colliders[i].bounds;
                if (b.extents.y > _halfHeight) _halfHeight = b.extents.y;
            }
        }
    }

    private void Update()
    {
        if (manualTrigger && Input.GetKeyDown(manualKey))
        {
            _manualQueued = true;
        }
    }

    private void FixedUpdate()
    {
        if (autoUse && !_isRunning && Time.time >= _nextAutoTime)
        {
            Transform t = FindTargetByTag();
            bool used = false;
            if (t != null)
            {
                used = TryUseOn(t);
            }
            if (!used)
            {
                used = TryUse();
            }
            if (used)
            {
                _nextAutoTime = Time.time + autoInterval;
            }
        }
        
        if (_manualQueued)
        {
            _manualQueued = false;
            Transform player = FindTargetByTag();
            if (!TryUseOn(player))
            {
                TryUse();
            }
        }
        if (_knockbackQueued)
        {
            _knockbackQueued = false;
            DoKnockback(_knockbackCenter);
        }
    }

    protected override bool Use()
    {
        if (_isRunning) return false;
        Vector3 startPos = transform.position;
        Transform player = FindPlayer();
        if (player == null) return false;

        Vector3 targetPos = player.position;

        Vector3 toPlayer = targetPos - startPos;
        float factor = Mathf.Clamp01(landingFactor);
        Vector3 dest = new Vector3(startPos.x + toPlayer.x * factor, startPos.y + toPlayer.y * factor, startPos.z);
        // Long probe: ensure there is ground below destination
        Vector2 probeOrigin = new Vector2(dest.x, dest.y + groundProbeDepth * 0.5f);
        RaycastHit2D probe = Physics2D.Raycast(probeOrigin, Vector2.down, groundProbeDepth, groundLayers);
        _debugProbeOrigin = probeOrigin;
        _debugProbeHasHit = probe.collider != null;
        _debugProbeHitPoint = probe.point;
        if (!_debugProbeHasHit)
        {
            _debugStart = startPos;
            _debugDest = dest;
            return false;
        }

        // Ground-aware landing: clamp center to at least groundY + halfHeight
        Vector2 rayOrigin = new Vector2(dest.x, dest.y + groundRayDistance * 0.5f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, groundRayDistance, groundLayers);
        if (hit.collider != null)
        {
            float desiredCenterY = hit.point.y + _halfHeight;
            if (dest.y < desiredCenterY) dest.y = desiredCenterY;
            _debugHasHit = true;
            _debugHitPoint = hit.point;
        }
        else
        {
            _debugHasHit = false;
        }
        _debugRayOrigin = rayOrigin;
        _debugStart = startPos;
        _debugDest = dest;
        StartCoroutine(Execute(startPos, dest, player));
        return true;
    }

    protected override bool UseOn(Transform target)
    {
        if (_isRunning) return false;
        Vector3 startPos = transform.position;
        Transform player = target != null ? target : FindPlayer();
        if (player == null) return false;

        Vector3 targetPos = player.position;

        Vector3 toPlayer = targetPos - startPos;
        float factor = Mathf.Clamp01(landingFactor);
        Vector3 dest = new Vector3(startPos.x + toPlayer.x * factor, startPos.y + toPlayer.y * factor, startPos.z);
        // Long probe: ensure there is ground below destination
        Vector2 probeOrigin = new Vector2(dest.x, dest.y + groundProbeDepth * 0.5f);
        RaycastHit2D probe = Physics2D.Raycast(probeOrigin, Vector2.down, groundProbeDepth, groundLayers);
        _debugProbeOrigin = probeOrigin;
        _debugProbeHasHit = probe.collider != null;
        _debugProbeHitPoint = probe.point;
        if (!_debugProbeHasHit)
        {
            _debugStart = startPos;
            _debugDest = dest;
            return false;
        }

        Vector2 rayOrigin = new Vector2(dest.x, dest.y + groundRayDistance * 0.5f);
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.down, groundRayDistance, groundLayers);
        if (hit.collider != null)
        {
            float desiredCenterY = hit.point.y + _halfHeight;
            if (dest.y < desiredCenterY) dest.y = desiredCenterY;
            _debugHasHit = true;
            _debugHitPoint = hit.point;
        }
        else
        {
            _debugHasHit = false;
        }
        _debugRayOrigin = rayOrigin;
        _debugStart = startPos;
        _debugDest = dest;
        StartCoroutine(Execute(startPos, dest, player));
        return true;
    }

    private IEnumerator Execute(Vector3 start, Vector3 dest, Transform player)
    {
        _isRunning = true;

        if (faceLeapDirection)
        {
            FaceDirection(dest - start);
        }

        if (disableCollisionsDuringMove) SetCollidersEnabled(false);
        yield return MoveOverTime(start, dest, leapTravelTime, arcHeight);
        if (disableCollisionsDuringMove) SetCollidersEnabled(true);

        // After landing, look at the target if available
        Transform p2 = player != null ? player : FindTargetByTag();
        if (p2 != null) FaceDirection(p2.position - transform.position);

        StartChargeVfx();
        float t = 0f;
        while (t < chargeDuration)
        {
            t += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        StopChargeVfx();

        // Queue knockback to be processed in FixedUpdate, then apply direct damage to player
        _knockbackCenter = transform.position;
        _knockbackQueued = true;
        DamagePlayer(player);

        if (disableCollisionsDuringMove) SetCollidersEnabled(false);
        // Return with a parabolic descent as well
        _debugReturnStart = transform.position;
        _debugReturnDest = start;
        yield return MoveOverTime(transform.position, start, returnTravelTime, arcHeight);
        if (disableCollisionsDuringMove) SetCollidersEnabled(true);

        // After returning, face target again if available
        Transform p3 = player != null ? player : FindTargetByTag();
        if (p3 != null)
        {
            FaceDirection(p3.position - transform.position);
        }

        _isRunning = false;
        ArmCooldown();
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugGizmos) return;

        // Path and endpoints
        Gizmos.color = gizmoStartColor;
        Gizmos.DrawWireSphere(_debugStart, 0.15f);
        Gizmos.color = gizmoDestColor;
        Gizmos.DrawWireSphere(_debugDest, 0.15f);
        Gizmos.color = gizmoPathColor;
        Gizmos.DrawLine(_debugStart, _debugDest);

        // Forward arc preview
        DrawParabola(_debugStart, _debugDest, arcHeight, gizmoPathColor);

        // Return arc preview
        DrawParabola(_debugReturnStart, _debugReturnDest, arcHeight, gizmoPathColor);

        // Ground ray and hit
        Gizmos.color = gizmoRayColor;
        Vector3 rayEnd = new Vector3(_debugRayOrigin.x, _debugRayOrigin.y, transform.position.z) + Vector3.down * groundRayDistance;
        Gizmos.DrawLine(new Vector3(_debugRayOrigin.x, _debugRayOrigin.y, transform.position.z), rayEnd);
        if (_debugHasHit)
        {
            Gizmos.color = gizmoHitColor;
            Gizmos.DrawWireSphere(new Vector3(_debugHitPoint.x, _debugHitPoint.y, transform.position.z), 0.12f);
        }

        // Knockback radius at current position
        Gizmos.color = gizmoKnockbackColor;
        Gizmos.DrawWireSphere(transform.position, knockbackRadius);
    }

    private void DrawParabola(Vector3 from, Vector3 to, float arc, Color color)
    {
        if (from == Vector3.zero && to == Vector3.zero) return;
        Gizmos.color = color;
        const int steps = 16;
        Vector3 prev = from;
        for (int i = 1; i <= steps; i++)
        {
            float u = i / (float)steps;
            Vector3 p = Vector3.Lerp(from, to, u);
            float yOffset = arc > 0f ? (4f * arc * u * (1f - u)) : 0f;
            p.y += yOffset;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
    }

    private IEnumerator MoveOverTime(Vector3 from, Vector3 to, float duration, float arc)
    {
        if (duration <= 0f)
        {
            if (_rb != null)
            {
                _rb.MovePosition(new Vector2(to.x, to.y));
            }
            else
            {
                transform.position = to;
            }
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            float u = t / duration;
            Vector3 basePos = Vector3.Lerp(from, to, u);
            float yOffset = arc > 0f ? (4f * arc * u * (1f - u)) : 0f; // parabolik tepe
            Vector2 nextPos2D = new Vector2(basePos.x, basePos.y + yOffset);
            if (_rb != null)
            {
                _rb.MovePosition(nextPos2D);
                t += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
            else
            {
                transform.position = new Vector3(nextPos2D.x, nextPos2D.y, transform.position.z);
                t += Time.deltaTime;
                yield return null;
            }
        }
        if (_rb != null)
        {
            _rb.MovePosition(new Vector2(to.x, to.y));
        }
        else
        {
            transform.position = to;
        }
    }

    private void DamagePlayer(Transform player)
    {
        if (player == null)
        {
            player = FindPlayer();
        }
        if (player != null)
        {
            player.gameObject.SendMessage("TakeDamage", damageAmount, SendMessageOptions.DontRequireReceiver);
        }
    }

    private void DoKnockback(Vector3 center)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, knockbackRadius, knockbackLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null) continue;
            if (hits[i].gameObject == this.gameObject) continue;
            Rigidbody2D rb = hits[i].attachedRigidbody;
            Vector2 dir = (hits[i].bounds.center - center).normalized;
            if (rb != null)
            {
                if (rb.bodyType == RigidbodyType2D.Dynamic)
                {
                    rb.AddForce(dir * knockbackForce, ForceMode2D.Impulse);
                }
                else if (rb.bodyType == RigidbodyType2D.Kinematic)
                {
                    // Displace kinematic bodies directly
                    rb.MovePosition(rb.position + dir * knockbackKinematicDisplacement);
                }
            }
            else
            {
                // No rigidbody: fallback to small transform displacement
                hits[i].transform.position += (Vector3)(dir * knockbackKinematicDisplacement);
            }
        }
    }

    private void SetCollidersEnabled(bool enabled)
    {
        if (_colliders == null || _colliders.Length == 0) return;
        for (int i = 0; i < _colliders.Length; i++)
        {
            if (_colliders[i] != null) _colliders[i].enabled = enabled;
        }
    }

    private void StartChargeVfx()
    {
        if (chargeEffectPrefab == null) return;
        if (_chargeEffectInstance != null) return;
        _chargeEffectInstance = Object.Instantiate(chargeEffectPrefab, transform);
        _chargeEffectInstance.transform.localPosition = chargeEffectLocalOffset;
    }

    private void StopChargeVfx()
    {
        if (_chargeEffectInstance != null)
        {
            Object.Destroy(_chargeEffectInstance);
            _chargeEffectInstance = null;
        }
    }

    private void FaceDirection(Vector3 dir)
    {
        if (dir.sqrMagnitude < 0.0001f) return;
        Vector3 ls = transform.localScale;
        if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y))
        {
            if (ls.x == 0f) ls.x = 1f;
            ls.x = Mathf.Abs(ls.x) * Mathf.Sign(dir.x);
            transform.localScale = ls;
        }
        else
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }

    private Transform FindPlayer()
    {
        GameObject p = GameObject.FindWithTag("Player");
        return p != null ? p.transform : null;
    }

    private Transform FindTargetByTag()
    {
        if (string.IsNullOrEmpty(autoTargetTag)) return FindPlayer();
        GameObject t = GameObject.FindWithTag(autoTargetTag);
        if (t != null) return t.transform;
        return FindPlayer();
    }
}


