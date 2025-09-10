using System.Collections.Generic;
using UnityEngine;

public class BaseEnemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    public string enemyName = "Enemy";
    public float maxHealth = 100f;
    public float currentHealth;
    public float moveSpeed = 3f;
    
    [Header("Simple Controls")]
    public bool canMove = true;
    public bool canAttack = true;
    public bool canTakeDamage = true;
    
    [Header("Abilities")]
    public List<EnemyAbility> abilities = new List<EnemyAbility>();
    
    [Header("References")]
    public Transform target;
    
    protected virtual void Awake()
    {
        currentHealth = maxHealth;
    }
    
    protected virtual void Start()
    {
        // Oyuncu referansını bul
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            target = player.transform;
    }
    
    protected virtual void Update()
    {
        if (!canMove || target == null) return;
        
        Vector2 direction = GetDirectionToTarget();
        MoveTowards(direction);
        UpdateSpriteFacing(direction);
        TryAttack();
    }
    
    protected virtual Vector2 GetDirectionToTarget()
    {
        return ((Vector2)target.position - (Vector2)transform.position).normalized;
    }
    
    protected virtual void MoveTowards(Vector2 direction)
    {
        transform.position += (Vector3)direction * moveSpeed * Time.deltaTime;
    }
    
    protected virtual void UpdateSpriteFacing(Vector2 direction)
    {
        if (direction.x > 0)
            transform.localScale = new Vector3(1, 1, 1); // Sağa bak
        else if (direction.x < 0)
            transform.localScale = new Vector3(-1, 1, 1); // Sola bak
    }
    
    protected virtual bool IsInAttackRange()
    {
        return target != null && Vector2.Distance(transform.position, target.position) < 2f;
    }
    
    protected virtual void TryAttack()
    {
        if (canAttack && IsInAttackRange())
        {
            Attack();
        }
    }
    
    
    protected virtual void InitializeAbilities()
    {
        // Alt sınıflarda override edilecek
    }
    
    protected virtual void Attack()
    {
        if (!canAttack) return;
        
        // İlk kullanılabilir yeteneği kullan
        foreach (var ability in abilities)
        {
            if (ability != null && ability.CanUse())
            {
                ability.Use(this, target);
                break;
            }
        }
    }
    
    public virtual void TakeDamage(float damage)
    {
        if (!canTakeDamage) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    protected virtual void Die()
    {
        Destroy(gameObject);
    }
    
    // Basit yetenek aktarım sistemi
    public void CopyAbilityFrom(BaseEnemy otherEnemy, int abilityIndex)
    {
        if (otherEnemy == null || abilityIndex >= otherEnemy.abilities.Count || abilityIndex < 0) 
            return;
        
        EnemyAbility abilityToCopy = otherEnemy.abilities[abilityIndex];
        if (abilityToCopy != null)
        {
            abilities.Add(abilityToCopy.Clone());
            Debug.Log($"{enemyName} copied {abilityToCopy.abilityName} from {otherEnemy.enemyName}");
        }
    }
    
    // Parry sistemi için - alt sınıflarda override edilebilir
    public virtual void OnPlayerParry()
    {
        // Varsayılan davranış - alt sınıflar override edecek
        Debug.Log($"{enemyName} was parried!");
    }
}
