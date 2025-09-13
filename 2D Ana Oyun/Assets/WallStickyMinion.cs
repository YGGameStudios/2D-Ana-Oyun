using System.Collections;
using UnityEngine;

public class WallStickyMinion : MonoBehaviour
{
    public enum MinionState
    {
        Idle,
        Launched,
        SeekingWall,
        Stuck,
        Returning
    }

    [Header("Setup")]
    public Rigidbody2D rb;
    public LayerMask wallMask;
    public Transform boss;
    public Transform player;

    [Header("Movement Speeds")]
    public float launchSpeed = 12f;
    public float seekWallSpeed = 8f;
    public float returnSpeed = 14f;

    [Header("Behavior Timers")]
    public float maxFreeFlyTime = 0.75f;
    public float operateDuration = 5.5f; // time to shoot while stuck
    public float returnAfterSeconds = 6f; // hard cap: return to boss regardless of state

    [Header("Wall Seeking")]
    public float wallSnapDistance = 0.25f;
    public float wallSearchRadius = 10f;

    [Header("Shooting")]
    public GameObject bulletPrefab; // optional; if null, will skip shooting
    public float bulletSpeed = 18f;
    public float fireInterval = 0.5f;
    public float bulletLifetime = 3f;

    private MinionState _state = MinionState.Idle;
    private float _stateStartTime;
    private float _spawnTime;
    private Vector2 _launchDirection;
    private bool _stuckHasStarted;
    private Coroutine _shootRoutine;
    private Collider2D[] _colliders;
    private float _noCollisionUntil;
    private bool _flightCollisionsEnabled;

    void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f; // never fall
            rb.freezeRotation = true;
        }
        _colliders = GetComponentsInChildren<Collider2D>(includeInactive: true);
    }

    void OnEnable()
    {
        _state = MinionState.Idle;
        _stuckHasStarted = false;
        _spawnTime = Time.time;
        _shootRoutine = null;
        _flightCollisionsEnabled = false;
    }

    public void Launch(Vector2 direction, Transform ownerBoss)
    {
        boss = ownerBoss;
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }

        _launchDirection = direction.normalized;
        _state = MinionState.Launched;
        _stateStartTime = Time.time;
        _spawnTime = Time.time;
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f; // keep no gravity during flight
            rb.velocity = _launchDirection * launchSpeed;
        }

        // Short grace: disable all collisions (triggers only) to avoid clipping boss/nearby
        _noCollisionUntil = Time.time + 0.25f;
        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null) _colliders[i].isTrigger = true;
            }
        }
        _flightCollisionsEnabled = false;
    }

    void Update()
    {
        // Global lifetime cap: always return after returnAfterSeconds
        if (_state != MinionState.Returning && Time.time - _spawnTime >= returnAfterSeconds)
        {
            BeginReturnToBoss();
        }

        // After initial grace, enable flight collisions with world, ignoring player (and boss)
        if ((_state == MinionState.Launched || _state == MinionState.SeekingWall) && !_flightCollisionsEnabled)
        {
            if (Time.time >= _noCollisionUntil)
            {
                SetupCollisionsForFlight();
                // Also ignore boss during flight to avoid interaction on exit
                if (boss != null)
                {
                    var bossCols = boss.GetComponentsInChildren<Collider2D>(includeInactive: true);
                    if (_colliders != null && bossCols != null)
                    {
                        foreach (var mc in _colliders)
                        {
                            if (mc == null) continue;
                            foreach (var bc in bossCols)
                            {
                                if (bc == null) continue;
                                Physics2D.IgnoreCollision(mc, bc, true);
                            }
                        }
                    }
                }
                _flightCollisionsEnabled = true;
            }
        }

        switch (_state)
        {
            case MinionState.Launched:
                UpdateLaunched();
                break;
            case MinionState.SeekingWall:
                UpdateSeekingWall();
                break;
            case MinionState.Stuck:
                UpdateStuck();
                break;
            case MinionState.Returning:
                UpdateReturning();
                break;
        }
    }

    void UpdateLaunched()
    {
        if (Time.time - _stateStartTime > maxFreeFlyTime)
        {
            EnterSeekingWall();
        }
    }

    void EnterSeekingWall()
    {
        _state = MinionState.SeekingWall;
        _stateStartTime = Time.time;
    }

    void UpdateSeekingWall()
    {
        // Find closest wall point using a set of radial raycasts
        Vector2 origin = transform.position;
        float bestDist = float.MaxValue;
        Vector2 bestPoint = origin;
        Vector2 bestNormal = Vector2.zero;
        const int rays = 24;
        for (int i = 0; i < rays; i++)
        {
            float angle = (360f / rays) * i * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            RaycastHit2D hit = Physics2D.Raycast(origin, dir, wallSearchRadius, wallMask);
            if (hit.collider != null)
            {
                float d = hit.distance;
                if (d < bestDist)
                {
                    bestDist = d;
                    bestPoint = hit.point;
                    bestNormal = hit.normal;
                }
            }
        }

        if (bestDist < float.MaxValue)
        {
            // Move towards bestPoint
            Vector2 toPoint = (bestPoint - (Vector2)transform.position);
            Vector2 move = toPoint.normalized * seekWallSpeed;
            if (rb != null)
            {
                rb.velocity = move;
            }
            // Snap when close
            if (toPoint.magnitude <= wallSnapDistance)
            {
                StickToWall(bestNormal);
            }
            else
            {
                // Extra safety: short ray ahead to snap early when skimming along edges
                Vector2 aheadDir = (rb != null && rb.velocity.sqrMagnitude > 0.0001f) ? rb.velocity.normalized : toPoint.normalized;
                float snapProbe = Mathf.Max(0.2f, wallSnapDistance * 1.5f);
                RaycastHit2D nearHit = Physics2D.Raycast((Vector2)transform.position, aheadDir, snapProbe, wallMask);
                if (nearHit.collider != null)
                {
                    StickToWall(nearHit.normal);
                }
            }
        }
        else
        {
            // No wall found; gently slow down
            if (rb != null)
            {
                rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, 0.02f);
            }
        }
    }

    void StickToWall(Vector2 normal)
    {
        _state = MinionState.Stuck;
        _stateStartTime = Time.time;
        _stuckHasStarted = false; // used to start shooting coroutine once
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Orient so that forward faces away from wall normal
        float angle = Mathf.Atan2(-normal.y, -normal.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Fire immediately once on stick to ensure at least one shot
        TryShootAtPlayer();
    }

    void UpdateStuck()
    {
        if (!_stuckHasStarted)
        {
            _stuckHasStarted = true;
            _shootRoutine = StartCoroutine(ShootAtPlayerForDuration());
        }

        if (Time.time - _stateStartTime >= operateDuration)
        {
            BeginReturnToBoss();
        }
    }

    IEnumerator ShootAtPlayerForDuration()
    {
        float end = Time.time + operateDuration;
        while (Time.time < end && _state == MinionState.Stuck)
        {
            TryShootAtPlayer();
            yield return new WaitForSeconds(fireInterval);
        }
    }

    void TryShootAtPlayer()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (bulletPrefab == null || player == null) return;
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        GameObject b = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        Rigidbody2D brb = b.GetComponent<Rigidbody2D>();
        if (brb == null)
        {
            brb = b.AddComponent<Rigidbody2D>();
        }
        brb.gravityScale = 0f;
        brb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        brb.interpolation = RigidbodyInterpolation2D.Interpolate;
        brb.velocity = dir * bulletSpeed;
        // Ensure projectile uses trigger collider to avoid hard collisions
        Collider2D bc = b.GetComponent<Collider2D>();
        if (bc == null)
        {
            CircleCollider2D cc = b.AddComponent<CircleCollider2D>();
            cc.radius = 0.1f;
            cc.isTrigger = true;
        }
        else
        {
            bc.isTrigger = true;
        }
        if (bulletLifetime > 0f)
        {
            Destroy(b, bulletLifetime);
        }
    }

    void BeginReturnToBoss()
    {
        _state = MinionState.Returning;
        _stateStartTime = Time.time;
        if (_shootRoutine != null)
        {
            StopCoroutine(_shootRoutine);
            _shootRoutine = null;
        }
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.angularVelocity = 0f;
            rb.gravityScale = 0f;
        }
        // Avoid colliding with anything while returning (only trigger with boss)
        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null) _colliders[i].isTrigger = true;
            }
        }
        // Re-enable collisions with player so trigger with boss can be detected if boss is parent of player
        if (player != null)
        {
            var playerCols = player.GetComponentsInChildren<Collider2D>(includeInactive: true);
            if (_colliders != null && playerCols != null)
            {
                foreach (var mc in _colliders)
                {
                    if (mc == null) continue;
                    foreach (var pc in playerCols)
                    {
                        if (pc == null) continue;
                        Physics2D.IgnoreCollision(mc, pc, false);
                    }
                }
            }
        }
    }

    void UpdateReturning()
    {
        if (boss == null)
        {
            // No boss to return to; destroy self
            Destroy(gameObject);
            return;
        }

        Vector2 toBoss = (Vector2)boss.position - (Vector2)transform.position;
        if (rb != null)
        {
            rb.velocity = toBoss.normalized * returnSpeed;
        }
        if (toBoss.magnitude <= 0.3f)
        {
            // Reached boss
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (_state == MinionState.Returning)
        {
            if (IsBossTransform(collision.collider != null ? collision.collider.transform : null))
            {
                CompleteReturnToBoss();
            }
            return;
        }

        if (_state == MinionState.Launched || _state == MinionState.SeekingWall)
        {
            // Treat any collision as a valid sticking surface to avoid missing due to mask/layer issues
            Vector2 n = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector2.up;
            StickToWall(n);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore any triggers during initial launch no-collision window
        if (Time.time < _noCollisionUntil) return;
        if (_state == MinionState.Returning )
        {
            if (IsBossTransform(other != null ? other.transform : null))
            {
                CompleteReturnToBoss();
            }
        }
    }

    private void CompleteReturnToBoss()
    {
        if (boss != null)
        {
            transform.position = boss.position;
        }
        Destroy(gameObject);
    }
    
    private bool IsBossTransform(Transform t)
    {
        if (boss == null || t == null) return false;
        if (t == boss) return true;
        return t.IsChildOf(boss);
    }

    private void SetupCollisionsForFlight()
    {
        // Ensure our colliders are solid (not triggers) so we interact with environment
        if (_colliders != null)
        {
            for (int i = 0; i < _colliders.Length; i++)
            {
                if (_colliders[i] != null) _colliders[i].isTrigger = false;
            }
        }

        // Ignore collisions with player only
        if (player == null)
        {
            GameObject p = GameObject.FindWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player != null)
        {
            var playerCols = player.GetComponentsInChildren<Collider2D>(includeInactive: true);
            if (_colliders != null && playerCols != null)
            {
                foreach (var mc in _colliders)
                {
                    if (mc == null) continue;
                    foreach (var pc in playerCols)
                    {
                        if (pc == null) continue;
                        Physics2D.IgnoreCollision(mc, pc, true);
                    }
                }
            }
        }
    }
    private IEnumerator MoveProjectileTransform(Transform proj, Vector2 dir, float speed, float lifetime)
    {
        float elapsed = 0f;
        while (proj != null && elapsed < lifetime)
        {
            proj.Translate((Vector3)(dir * speed * Time.deltaTime), Space.World);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
