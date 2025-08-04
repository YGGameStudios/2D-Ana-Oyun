using UnityEngine;

[CreateAssetMenu(fileName = "FireballAbility", menuName = "Enemy Abilities/Fireball")]
public class FireballAbility : EnemyAbility
{
    [Header("Fireball Settings")]
    public GameObject fireballPrefab;
    public float projectileSpeed = 10f;
    
    protected override void ExecuteAbility(BaseEnemy caster, Transform target)
    {
        if (target == null) return;
        
        // Ateş topu yaratıp fırlat (2D)
        Vector2 direction = ((Vector2)target.position - (Vector2)caster.transform.position).normalized;
        
        if (fireballPrefab != null)
        {
            GameObject fireball = Instantiate(fireballPrefab, caster.transform.position + (Vector3)direction, Quaternion.identity);
            
            // 2D Rigidbody hareket
            Rigidbody2D rb = fireball.GetComponent<Rigidbody2D>();
            if (rb == null)
                rb = fireball.AddComponent<Rigidbody2D>();
            
            rb.velocity = direction * projectileSpeed;
            
            // Projectile component ekle
            ProjectileController projectile = fireball.GetComponent<ProjectileController>();
            if (projectile == null)
                projectile = fireball.AddComponent<ProjectileController>();
            
            projectile.damage = damage;
            projectile.caster = caster;
            
            // 5 saniye sonra yok et
            Destroy(fireball, 5f);
        }
        
        Debug.Log($"{caster.enemyName} used Fireball!");
    }
}
