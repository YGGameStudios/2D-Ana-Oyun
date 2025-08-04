using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Player Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float moveSpeed = 5f;
    public float jumpForce = 7f;
    
    [Header("Combat System")]
    public bool hasDashAbility = false;
    public float dashSpeed = 15f;
    public float dashDuration = 0.3f;
    private bool isDashing = false;
    
    [Header("Parry System")]
    public float parryWindow = 0.5f; // Parry için zaman penceresi
    private bool isParrying = false;
    private bool isBlocking = false;
    private float parryTimer = 0f;
    
    [Header("Simple Controls")]
    public bool canMove = true;
    public bool canJump = true;
    public bool canTakeDamage = true;
    public bool canInteract = true;
    
    [Header("References")]
    private Rigidbody2D rb;
    private bool isGrounded = false;
    
    private void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        
        // Rigidbody2D yoksa ekle
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
    }
    
    private void Update()
    {
        HandleMovement();
        HandleJump();
        HandleInteraction();
        HandleCombat();
        CheckGrounded();
        
        // Parry timer güncelle
        if (isParrying)
        {
            parryTimer -= Time.deltaTime;
            if (parryTimer <= 0)
            {
                isParrying = false;
            }
        }
    }
    
    private void HandleMovement()
    {
        if (!canMove || isDashing) return;
        
        // A/D veya Left/Right ile yatay hareket (2D)
        float horizontal = Input.GetAxis("Horizontal");
        
        Vector2 movement = new Vector2(horizontal, 0f);
        
        if (movement.magnitude > 0)
        {
            // Hareket et (2D)
            transform.position += (Vector3)movement * moveSpeed * Time.deltaTime;
            
            // Sprite'ı hareket yönüne çevir
            if (horizontal > 0)
                transform.localScale = new Vector3(1, transform.localScale.y, 1); // Sağa bak
            else if (horizontal < 0)
                transform.localScale = new Vector3(-1, transform.localScale.y, 1); // Sola bak
        }
    }
    
    private void HandleCombat()
    {
        // L1 - Üst parry/block
        if (Input.GetKeyDown(KeyCode.Q)) // L1 yerine Q
        {
            StartParry("upper");
        }
        else if (Input.GetKey(KeyCode.Q))
        {
            isBlocking = true;
        }
        else if (Input.GetKeyUp(KeyCode.Q))
        {
            isBlocking = false;
        }
        
        // R1 - Alt parry/block  
        if (Input.GetKeyDown(KeyCode.E)) // R1 yerine E
        {
            StartParry("lower");
        }
        else if (Input.GetKey(KeyCode.E))
        {
            isBlocking = true;
        }
        else if (Input.GetKeyUp(KeyCode.E))
        {
            isBlocking = false;
        }
        
        // Dash (boss öldürüldükten sonra aktif olur)
        if (hasDashAbility && Input.GetKeyDown(KeyCode.LeftShift) && !isDashing)
        {
            StartDash();
        }
    }
    
    private void StartParry(string parryType)
    {
        isParrying = true;
        parryTimer = parryWindow;
        Debug.Log($"Player started {parryType} parry!");
    }
    
    private void StartDash()
    {
        if (isDashing) return;
        
        StartCoroutine(ExecuteDash());
    }
    
    private System.Collections.IEnumerator ExecuteDash()
    {
        isDashing = true;
        float dashTimer = 0f;
        Vector2 dashDirection = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
        
        while (dashTimer < dashDuration)
        {
            transform.position += (Vector3)dashDirection * dashSpeed * Time.deltaTime;
            dashTimer += Time.deltaTime;
            yield return null;
        }
        
        isDashing = false;
        Debug.Log("Dash completed!");
    }
    
    private void HandleJump()
    {
        if (!canJump || !isGrounded) return;
        
        if (Input.GetKeyDown(KeyCode.Space))
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            Debug.Log("Player jumped!");
        }
    }
    
    private void HandleInteraction()
    {
        if (!canInteract) return;
        
        // F tuşu ile etkileşim
        if (Input.GetKeyDown(KeyCode.F))
        {
            InteractWithNearbyObjects();
        }
    }
    
    private void InteractWithNearbyObjects()
    {
        // Yakındaki enemy'lerle etkileşim (2D)
        Collider2D[] nearbyObjects = Physics2D.OverlapCircleAll(transform.position, 3f);
        
        foreach (var obj in nearbyObjects)
        {
            BaseEnemy enemy = obj.GetComponent<BaseEnemy>();
            if (enemy != null)
            {
                Debug.Log($"Interacting with {enemy.enemyName}");
                
                // Enemy kontrollerini değiştir
                enemy.SetCanMove(!enemy.canMove);
                Debug.Log($"{enemy.enemyName} movement: {enemy.canMove}");
            }
        }
    }
    
    private void CheckGrounded()
    {
        // Zemin kontrolü - 2D raycast
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, 1.1f);
        isGrounded = hit.collider != null;
    }
    
    public void TakeDamage(float damage, string attackType = "normal")
    {
        if (!canTakeDamage) return;
        
        float finalDamage = damage;
        
        // Parry kontrolü
        if (isParrying)
        {
            Debug.Log("Perfect parry! No damage taken!");
            // Boss'a parry feedback gönder
            BaseEnemy boss = FindObjectOfType<BaseEnemy>();
            if (boss != null && boss.name.Contains("Boss"))
            {
                boss.OnPlayerParry();
            }
            return;
        }
        
        // Block kontrolü (sadece normal saldırılar için)
        if (isBlocking && attackType != "unblockable")
        {
            finalDamage = damage * 0.3f; // %30 hasar al
            Debug.Log($"Blocked! Reduced damage: {finalDamage}");
        }
        
        currentHealth -= finalDamage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        Debug.Log($"Player took {finalDamage} damage! Health: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void UnlockDash()
    {
        hasDashAbility = true;
        Debug.Log("Player unlocked dash ability!");
    }
    
    private void Die()
    {
        Debug.Log("Player died!");
        // Ölüm işlemleri
    }
    
    // Kontrol metodları - external scriptlerden çağrılabilir
    public void SetCanMove(bool value) 
    { 
        canMove = value; 
        Debug.Log($"Player movement: {canMove}");
    }
    
    public void SetCanJump(bool value) 
    { 
        canJump = value; 
        Debug.Log($"Player jump: {canJump}");
    }
    
    public void SetCanTakeDamage(bool value) 
    { 
        canTakeDamage = value; 
        Debug.Log($"Player can take damage: {canTakeDamage}");
    }
    
    public void SetCanInteract(bool value) 
    { 
        canInteract = value; 
        Debug.Log($"Player interaction: {canInteract}");
    }
    
    // Hızlı kontrol metodları
    public void ToggleMovement() { SetCanMove(!canMove); }
    public void ToggleJump() { SetCanJump(!canJump); }
    public void ToggleInvincibility() { SetCanTakeDamage(!canTakeDamage); }
    public void ToggleInteraction() { SetCanInteract(!canInteract); }
    
    // Debug için gizmo çiz
    private void OnDrawGizmosSelected()
    {
        // Etkileşim aralığı (2D)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 3f);
        
        // Zemin kontrolü (2D)
        Gizmos.color = isGrounded ? Color.red : Color.yellow;
        Gizmos.DrawRay(transform.position, Vector2.down * 1.1f);
    }
}
