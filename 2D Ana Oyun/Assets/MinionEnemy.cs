using UnityEngine;

public class MinionEnemy : BaseEnemy
{
    [Header("Minion Settings")]
    public GameObject fireballPrefab;
    public float fireballCooldown = 1.5f;
    public float shootRange = 8f;
    
    [Header("Attachment System")]
    public LayerMask attachableLayers = -1; // Hangi layer'lara yapışabilir
    
    [Header("Wall Seeking (when not attached)")]
    [Tooltip("Duvar arama yarıçapı")] public float wallSeekRadius = 10f;
    [Tooltip("Duvara doğru giderken yatay hız")] public float horizontalSeekSpeed = 3.5f;
    [Tooltip("Duvara yakınken zıplama kuvveti")] public float hopForce = 7f;
    [Tooltip("Zıplama tekrar süresi")] public float hopCooldown = 0.9f;
    [Tooltip("Duvara yakınlık ray mesafesi")] public float attachRayDistance = 0.35f;
    [Tooltip("Zıplama tetiklemek için duvara olan mesafe eşiği")] public float hopTriggerDistance = 1.25f;
    [Tooltip("Yan duvarı aramak için sağ/sol ray mesafesi")] public float sideCheckDistance = 6f;
    [Tooltip("Anlık yapışma için yatay kısa ray mesafesi")] public float sideAttachRayDistance = 0.25f;
    
    private float lastFireballTime = 0f;
    private bool isAttached = false;
    private bool isFlying = true;
    private Rigidbody2D rb;
    private float lastHopTime = -999f;
    private float suppressHopUntilTime = 0f;
    
    protected override void Start()
    {
        base.Start();
        
        enemyName = "Wall Minion";
        maxHealth = 30f;
        currentHealth = maxHealth;
        moveSpeed = 0f; // Hareket etmez
        
        // Rigidbody2D'yi al
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();
        rb.isKinematic = false;
        if (rb.gravityScale <= 0f) rb.gravityScale = 1f; // Uçuşta yerçekimi olsun ki aşağı süzülsün
        
        // Player'ı bul
        target = GameObject.FindWithTag("Player")?.transform;
        
        // Başlangıçta uçuyor
        isFlying = true;
        isAttached = false;
    }
    
    protected override void Update()
    {
        // Yapıştıysa saldır
        if (isAttached && canAttack && target != null)
        {
            AttackPlayer();
        }
        else if (!isAttached)
        {
            HandleWallSeekingAndAttach();
        }
    }
    
    private void AttachToSurface()
    {
        isFlying = false;
        isAttached = true;
        
        // Rigidbody'yi durdur
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;
        
        Debug.Log($"{enemyName} attached to surface!");
        
        // Player'a doğru bak
        if (target != null)
        {
            Vector2 direction = ((Vector2)target.position - (Vector2)transform.position);
            if (direction.x > 0)
                transform.localScale = new Vector3(1, 1, 1);
            else if (direction.x < 0)
                transform.localScale = new Vector3(-1, 1, 1);
        }
    }
    
    private void AttackPlayer()
    {
        // Player menzilde mi kontrol et
        float distanceToPlayer = Vector2.Distance(transform.position, target.position);
        
        if (distanceToPlayer <= shootRange)
        {
            // Ateş topu at
            if (Time.time - lastFireballTime >= fireballCooldown)
            {
                ShootAtPlayer();
                lastFireballTime = Time.time;
            }
        }
        
        // Player'a doğru bak
        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position);
        if (direction.x > 0)
            transform.localScale = new Vector3(1, 1, 1);
        else if (direction.x < 0)
            transform.localScale = new Vector3(-1, 1, 1);
    }

    private void HandleWallSeekingAndAttach()
    {
        // Öncelik: sağ/sol yatay ray ile yan duvar arama (üstte dururken de yana kayıp yapışsın)
        Vector2 selfPos2D = transform.position;
        RaycastHit2D hitRight = Physics2D.Raycast(selfPos2D, Vector2.right, sideCheckDistance, attachableLayers);
        RaycastHit2D hitLeft  = Physics2D.Raycast(selfPos2D, Vector2.left,  sideCheckDistance, attachableLayers);

        bool hasSide = hitRight.collider != null || hitLeft.collider != null;
        if (hasSide)
        {
            // En yakın yana hedefle
            float distR = hitRight.collider != null ? Mathf.Abs(hitRight.point.x - selfPos2D.x) : float.MaxValue;
            float distL = hitLeft.collider  != null ? Mathf.Abs(hitLeft.point.x  - selfPos2D.x) : float.MaxValue;
            int horizontalSign = distR < distL ? +1 : -1;

            // Yatay sürüklenme
            Vector2 currentVel = rb.velocity;
            currentVel.x = horizontalSeekSpeed * horizontalSign;
            rb.velocity = currentVel;

            // Yönünü çevir
            if (horizontalSign > 0) transform.localScale = new Vector3(1, 1, 1);
            else transform.localScale = new Vector3(-1, 1, 1);

            // Yakınsa hafif zıpla (üstte takılı kalmayı engelle)
            float horizDist = Mathf.Min(distR, distL);
            if (horizDist <= hopTriggerDistance && Time.time >= lastHopTime + hopCooldown && Time.time >= suppressHopUntilTime)
            {
                rb.AddForce(Vector2.up * hopForce, ForceMode2D.Impulse);
                lastHopTime = Time.time;
            }

            // Yatay kısa ray ile anlık temas kontrolü -> yapış
            Vector2 sideDir = horizontalSign > 0 ? Vector2.right : Vector2.left;
            RaycastHit2D sideTouch = Physics2D.Raycast(selfPos2D, sideDir, sideAttachRayDistance, attachableLayers);
            if (sideTouch.collider != null)
            {
                AttachToSurface();
                return;
            }
        }
        else
        {
            // Yedek: en yakın noktaya doğru hareket (mevcut yaklaşım)
            Vector2? nearestPointNullable = FindNearestAttachablePoint(selfPos2D, wallSeekRadius, out Collider2D nearestCollider);
            if (nearestPointNullable.HasValue)
            {
                Vector2 nearestPoint = nearestPointNullable.Value;
                Vector2 dirToWall = (nearestPoint - selfPos2D);
                float horizontalDir = Mathf.Sign(Mathf.Abs(dirToWall.x) < 0.001f ? 0.001f : dirToWall.x);

                Vector2 currentVel = rb.velocity;
                currentVel.x = horizontalSeekSpeed * horizontalDir;
                rb.velocity = currentVel;

                if (horizontalDir > 0) transform.localScale = new Vector3(1, 1, 1);
                else transform.localScale = new Vector3(-1, 1, 1);

                float distToWall = dirToWall.magnitude;
                if (distToWall <= hopTriggerDistance && Time.time >= lastHopTime + hopCooldown && Time.time >= suppressHopUntilTime)
                {
                    rb.AddForce(Vector2.up * hopForce, ForceMode2D.Impulse);
                    lastHopTime = Time.time;
                }

                // Yakın temas kontrolü
                Vector2 sideDir = new Vector2(Mathf.Sign(horizontalDir), 0f);
                RaycastHit2D nearSide = Physics2D.Raycast(selfPos2D, sideDir, attachRayDistance, attachableLayers);
                if (nearSide.collider != null)
                {
                    AttachToSurface();
                }
            }
        }
    }

    private Vector2? FindNearestAttachablePoint(Vector2 origin, float radius, out Collider2D nearestCollider)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, radius, attachableLayers);
        nearestCollider = null;
        if (hits == null || hits.Length == 0)
            return null;

        float bestDist = float.MaxValue;
        Vector2 bestPoint = origin;
        foreach (var col in hits)
        {
            // En yakın nokta
            Vector2 p = col.ClosestPoint(origin);
            float d = Vector2.SqrMagnitude(p - origin);
            if (d < bestDist)
            {
                bestDist = d;
                bestPoint = p;
                nearestCollider = col;
            }
        }
        return bestPoint;
    }
    
    private void ShootAtPlayer()
    {
        if (fireballPrefab == null || target == null || !isAttached) return;
        
        // Player'a doğru ateş topu at
        Vector2 direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
        
        GameObject fireball = Instantiate(fireballPrefab, transform.position + (Vector3)direction * 0.5f, Quaternion.identity);
        
        Rigidbody2D fireballRb = fireball.GetComponent<Rigidbody2D>();
        if (fireballRb == null) fireballRb = fireball.AddComponent<Rigidbody2D>();
        
        fireballRb.velocity = direction * 6f;
        
        ProjectileController projectile = fireball.GetComponent<ProjectileController>();
        if (projectile == null) projectile = fireball.AddComponent<ProjectileController>();
        
        projectile.damage = 15f;
        projectile.caster = this;
        
        Destroy(fireball, 4f);
        
        Debug.Log($"{enemyName} shot fireball at player!");
    }
    
    // Çarpışma kontrolü - yapışma için
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Sadece uçuyorken ve henüz yapışmamışken kontrol et
        if (isFlying && !isAttached)
        {
            // Belirtilen layer'lara çarptı mı kontrol et
            if (((1 << collision.gameObject.layer) & attachableLayers) != 0)
            {
                // Duvar temasını doğrula: temas normalinin x bileşeni büyükse (yaklaşık yatay), yüzey diktir -> duvar
                bool isWallLike = false;
                if (collision.contactCount > 0)
                {
                    Vector2 n = collision.GetContact(0).normal;
                    isWallLike = Mathf.Abs(n.x) > Mathf.Abs(n.y);
                }
                if (isWallLike)
                {
                    Debug.Log($"{enemyName} collided with {collision.gameObject.name} (Layer: {collision.gameObject.layer}) - Attaching!");
                    AttachToSurface();
                }
                else
                {
                    // Üst yüzeye indi: zıplamak yerine en yakın yan kenara doğru kaydır ve zıplamayı kısa süre bastır
                    Bounds b = collision.collider.bounds;
                    float distToLeft = Mathf.Abs(transform.position.x - b.min.x);
                    float distToRight = Mathf.Abs(b.max.x - transform.position.x);
                    int horizontalSign = distToLeft < distToRight ? -1 : +1;

                    Vector2 vel = rb.velocity;
                    vel.y = Mathf.Min(vel.y, 0f); // yukarı zıplamayı kes
                    vel.x = horizontalSeekSpeed * horizontalSign;
                    rb.velocity = vel;

                    suppressHopUntilTime = Time.time + 0.8f; // önce yana kay

                    // Hemen yanda duvar varsa anında yapış
                    Vector2 sideDir = horizontalSign > 0 ? Vector2.right : Vector2.left;
                    RaycastHit2D sideTouch = Physics2D.Raycast(transform.position, sideDir, sideAttachRayDistance, attachableLayers);
                    if (sideTouch.collider != null)
                    {
                        AttachToSurface();
                    }
                }
            }
            else
            {
                Debug.Log($"{enemyName} collided with {collision.gameObject.name} (Layer: {collision.gameObject.layer}) - Not attachable layer, continuing flight");
            }
        }
    }

    // Trigger tabanlı duvar alanlarına girince yapışma (örn. trigger collider kullanan duvarlar)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isFlying && !isAttached)
        {
            if (((1 << other.gameObject.layer) & attachableLayers) != 0)
            {
                Debug.Log($"{enemyName} entered trigger {other.gameObject.name} (Layer: {other.gameObject.layer}) - Attaching!");
                AttachToSurface();
            }
        }
    }
    
    protected override void Die()
    {
        Debug.Log($"{enemyName} destroyed!");
        
        // Ölüm efekti eklenebilir buraya
        
        base.Die();
    }
    
    // Gizmo çizimi
    private void OnDrawGizmosSelected()
    {
        // Shoot range
        Gizmos.color = isAttached ? Color.red : new Color(1f, 0.5f, 0f); // Orange color
        Gizmos.DrawWireSphere(transform.position, shootRange);
        
        // Flying state indicator
        if (isFlying)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }

        // Duvar arama yarıçapı
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wallSeekRadius);
    }
}
