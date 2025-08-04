using UnityEngine;

public class MinionEnemy : BaseEnemy
{
    [Header("Minion Settings")]
    public GameObject fireballPrefab;
    public float fireballCooldown = 1.5f;
    public float shootRange = 8f;
    
    [Header("Attachment System")]
    public LayerMask attachableLayers = -1; // Hangi layer'lara yapışabilir
    
    private float lastFireballTime = 0f;
    private bool isAttached = false;
    private bool isFlying = true;
    private Rigidbody2D rb;
    
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
                Debug.Log($"{enemyName} collided with {collision.gameObject.name} (Layer: {collision.gameObject.layer}) - Attaching!");
                AttachToSurface();
            }
            else
            {
                Debug.Log($"{enemyName} collided with {collision.gameObject.name} (Layer: {collision.gameObject.layer}) - Not attachable layer, continuing flight");
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
    }
}
