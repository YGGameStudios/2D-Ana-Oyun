using UnityEngine;

public class ProjectileController : MonoBehaviour
{
    public float damage = 10f;
    public BaseEnemy caster;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Oyuncuya çarptı mı?
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                // Boss mu minion mu kontrol et
                string attackType = "normal";
                if (caster != null && caster.name.Contains("Boss"))
                {
                    attackType = "boss";
                }
                
                player.TakeDamage(damage, attackType);
            }
            
            DestroyProjectile();
        }
        // Düşmana çarptı mı ve kendi değil mi?
        else if (other.CompareTag("Enemy") && other.GetComponent<BaseEnemy>() != caster)
        {
            BaseEnemy enemy = other.GetComponent<BaseEnemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
            
            DestroyProjectile();
        }
        // Zemine veya duvara çarptı mı?
        else if (other.CompareTag("Ground") || other.CompareTag("Wall"))
        {
            DestroyProjectile();
        }
    }
    
    private void DestroyProjectile()
    {
        // Patlama efekti eklenebilir
        Destroy(gameObject);
    }
}
