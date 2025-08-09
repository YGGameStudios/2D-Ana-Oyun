using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossEnemy : BaseEnemy
{
    [Header("Boss Settings")]
    public Vector3 startPosition; // Boss'un başlangıç pozisyonu
    public Transform playerTarget;
    
    [Header("Boss Stats")]
    public float phase1Health = 100f;
    public float phase2Health = 70f;
    public float phase3Health = 40f;
    
    [Header("Phase 1 - Ranged")]
    public GameObject fireballPrefab;
    public float fireballCooldown = 2f;
    public int fireballCount = 5; // Kaç ateş topu atacak
    
    [Header("Phase 2 - Melee + Minions")]
    public GameObject minionPrefab;
    public float minionSpawnCooldown = 8f;
    
    [Header("Phase 3 - Enhanced")]
    public int comboRequiredForStun = 3; // Kaç başarılı parry gerekli
    
    // Boss state
    private int currentPhase = 1;
    private bool isStunned = false;
    private bool isCharging = false;
    private bool isSpinning = false;
    private float lastFireballTime = 0f;
    private float lastMinionSpawnTime = 0f;
    private int currentComboCount = 0;
    
    // Attack patterns
    private string[] meleeAttacks = { "upper", "lower", "spin" };
    private int fireballsShot = 0;
    private string currentMeleeAnim = ""; // aktif oynayan melee anim referansı

    // ÜST VE ALT MELEE SALDIRILARI
    [Header("Melee Attack Settings")] 
    // Windup kaldırıldı: doğrudan saldırı
    public float upperAttackRange = 3f;
    public float lowerAttackRange = 3f;
    public float upperAttackDamage = 35f;
    public float lowerAttackDamage = 30f;
    public float meleeReturnToIdleDelay = 0.1f; // Saldırı bitince idle'a dönmeden önce ufak bekleme

    [Header("Animation")] 
    [SerializeField] private Animator animator; // Boss animator
    [SerializeField] private string idleAnim = "BossIdle";
    [SerializeField] private string hurtAnim = "BossHurt";
    [SerializeField] private string deathAnim = "BossDeath";

    protected override void Start()
    {
        base.Start();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("BossEnemy: Animator not found.");
            }
        }
        
        enemyName = "Boss Enemy";
        maxHealth = phase1Health;
        currentHealth = maxHealth;
        
        // Başlangıç pozisyonunu kaydet
        if (startPosition == null)
            startPosition = transform.position;
            
        playerTarget = GameObject.FindWithTag("Player")?.transform;
        
        StartPhase1();
    }

    protected override void Update()
    {
        if (isStunned || !canMove) return;

        // Manuel faz geçiş kontrolü (test için)
        HandleManualPhaseControls();

        CheckPhaseTransition();

        switch (currentPhase)
        {
            case 1:
                Phase1Behavior();
                break;
            case 2:
                Phase2Behavior();
                break;
            case 3:
                Phase3Behavior();
                break;
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            SpawnMinions();
         }
    }
    
    private void HandleManualPhaseControls()
    {
        // 1 tuşu - Faz 1'e geç
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            ForcePhase(1);
        }
        // 2 tuşu - Faz 2'ye geç
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            ForcePhase(2);
        }
        // 3 tuşu - Faz 3'e geç
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            ForcePhase(3);
        }
        // R tuşu - Boss'u resetle (full HP)
        else if (Input.GetKeyDown(KeyCode.R))
        {
            ResetBoss();
        }
        // K tuşu - Boss'u öldür (test için)
        else if (Input.GetKeyDown(KeyCode.K))
        {
            KillBoss();
        }
    }
    
    private void ForcePhase(int phaseNumber)
    {
        if (phaseNumber == currentPhase) return;
        
        Debug.Log($"MANUALLY SWITCHING TO PHASE {phaseNumber}!");
        
        // Tüm coroutine'leri durdur
        StopAllCoroutines();
        isStunned = false;
        isCharging = false;
        isSpinning = false;
        
        switch (phaseNumber)
        {
            case 1:
                StartPhase1();
                break;
            case 2:
                StartPhase2();
                break;
            case 3:
                StartPhase3();
                break;
        }
    }
    
    private void ResetBoss()
    {
        Debug.Log("BOSS RESET!");
        
        // Tüm coroutine'leri durdur
        StopAllCoroutines();
        
        // Boss'u resetle
        currentHealth = maxHealth;
        isStunned = false;
        isCharging = false;
        isSpinning = false;
        currentComboCount = 0;
        fireballsShot = 0;
        
        // Faz 1'e dön
        StartPhase1();
        
        // Başlangıç pozisyonuna git
        transform.position = startPosition;
    }
    
    private void KillBoss()
    {
        Debug.Log("BOSS KILLED (TEST)!");
        currentHealth = 0;
        Die();
    }
    
    private void CheckPhaseTransition()
    {
        float healthPercentage = (currentHealth / maxHealth) * 100f;
        
        if (currentPhase == 1 && healthPercentage <= 70f)
        {
            StartPhase2();
        }
        else if (currentPhase == 2 && healthPercentage <= 40f)
        {
            StartPhase3();
        }
    }
    
    // ========== PHASE 1 ==========
    private void StartPhase1()
    {
        currentPhase = 1;
        fireballsShot = 0;
        Debug.Log("BOSS PHASE 1 STARTED!");
        
        // Başlangıç pozisyonuna git
        transform.position = startPosition;
    }
    
    private void Phase1Behavior()
    {
        // Sürekli ateş topu at
        if (Time.time - lastFireballTime >= fireballCooldown)
        {
            ShootFireball();
            lastFireballTime = Time.time;
            fireballsShot++;
            
            // Belirli sayıda ateş topu attıktan sonra dash yap
            if (fireballsShot >= fireballCount)
            {
                StartCoroutine(DashToPlayerAndMelee());
                fireballsShot = 0;
            }
        }
    }
    
    private void ShootFireball()
    {
        if (fireballPrefab == null || playerTarget == null) return;
        
        // Random yöne ateş topu at (player'ın genel yönüne ama tam değil)
        Vector2 playerDirection = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
        Vector2 randomDirection = playerDirection + new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.3f, 0.3f));
        
        GameObject fireball = Instantiate(fireballPrefab, transform.position + (Vector3)randomDirection, Quaternion.identity);
        
        Rigidbody2D rb = fireball.GetComponent<Rigidbody2D>();
        if (rb == null) rb = fireball.AddComponent<Rigidbody2D>();
        
        rb.velocity = randomDirection.normalized * 8f;
        
        ProjectileController projectile = fireball.GetComponent<ProjectileController>();
        if (projectile == null) projectile = fireball.AddComponent<ProjectileController>();
        
        projectile.damage = 25f;
        projectile.caster = this;
        
        Destroy(fireball, 5f);
        
        Debug.Log("Boss shot fireball!");
    }
    
    private IEnumerator DashToPlayerAndMelee()
    {
        if (playerTarget == null) yield break;
        
        Debug.Log("Boss dashing to player!");
        
        // Player'a doğru dash yap
        Vector2 dashDirection = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
        dashDirection.y += 0.6f; // Biraz yukarıya doğru git
        dashDirection = dashDirection.normalized; // Normalize et
        float dashSpeed = 12f;
        float dashDuration = 1.3f;
        float dashTimer = 0f;
        
        while (dashTimer < dashDuration)
        {
            transform.position += (Vector3)dashDirection * dashSpeed * Time.deltaTime;
            dashTimer += Time.deltaTime;
            yield return null;
        }
        
        // Yakın saldırılar yap
        yield return StartCoroutine(PerformMeleeCombo());
        
        // Başlangıç pozisyonuna dön
        yield return StartCoroutine(ReturnToStartPosition());
    }
    
    private IEnumerator PerformMeleeCombo()
    {
        int attackCount = Random.Range(2, 3); // Basit tut: 2 saldırı
        for (int i = 0; i < attackCount; i++)
        {
            // Rastgele üst / alt
            bool doUpper = Random.value > 0.5f;
            if (doUpper)
                yield return StartCoroutine(PerformUpperAttack());
            else
                yield return StartCoroutine(PerformLowerAttack());

            if (isStunned) break;
            yield return new WaitForSeconds(0.4f);
        }
    }

    private IEnumerator PerformUpperAttack()
    {
        PlayMeleeAnimation("BossUpperAttack");
        yield return StartCoroutine(ResolveMeleeHit(upperAttackRange, upperAttackDamage, true));
        if (meleeReturnToIdleDelay > 0f) yield return new WaitForSeconds(meleeReturnToIdleDelay);
        PlayMeleeAnimation(idleAnim);
    }

    private IEnumerator PerformLowerAttack()
    {
        PlayMeleeAnimation("BossLowerAttack");
        yield return StartCoroutine(ResolveMeleeHit(lowerAttackRange, lowerAttackDamage, false));
        if (meleeReturnToIdleDelay > 0f) yield return new WaitForSeconds(meleeReturnToIdleDelay);
        PlayMeleeAnimation(idleAnim);
    }

    private IEnumerator ResolveMeleeHit(float range, float damage, bool isUpper)
    {
        // Basit dairesel mesafe kontrolü
        if (playerTarget != null && Vector2.Distance(transform.position, playerTarget.position) <= range)
        {
            PlayerController player = playerTarget.GetComponent<PlayerController>();
            if (player != null)
            {
                string hitType = isUpper ? "upper" : "lower";
                player.TakeDamage(damage, hitType);
            }
        }
        yield return null;
    }

    private void PlayMeleeAnimation(string animName)
    {
        if (currentMeleeAnim == animName) return;
        currentMeleeAnim = animName;
        if (animator != null)
        {
            animator.Play(animName);
        }
        else
        {
            Debug.Log($"(No Animator) Would play anim: {animName}");
        }
    }

    // Eski generic melee attack fonksiyonları (PerformMeleeAttack / PerformSpinAttack) kullanılıyorsa
    // bunları artık iki parçalı sisteme geçiş için basitleştiriyoruz veya ileride tamamen kaldırılabilir
    
    private IEnumerator PerformSpinAttack(string attackType = "spin", bool isFullSpinAttack = true)
    {
        if (isFullSpinAttack)
        {
            // Gerçek spin attack - uzun şarj süresli
            Debug.Log("Boss charging spin attack!");
            isCharging = true;
            
            // 3 saniye şarj
            yield return new WaitForSeconds(3f);
            
            if (isStunned) yield break;
            
            Debug.Log("Boss executing spin attack!");
            isSpinning = true;
            
            // Spin saldırısı - savunulamaz
            float spinDuration = 2f;
            float spinTimer = 0f;
            float spinRadius = 4f;
            
            while (spinTimer < spinDuration)
            {
                // Boss'u döndür
                transform.Rotate(0f, 0f, 720f * Time.deltaTime); // Çok hızlı dönüş
                
                // Player spin aralığında mı kontrol et
                if (Vector2.Distance(transform.position, playerTarget.position) <= spinRadius)
                {
                    PlayerController player = playerTarget.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        player.TakeDamage(50f, "unblockable");
                    }
                }
                
                spinTimer += Time.deltaTime;
                yield return null;
            }
            
            isCharging = false;
            isSpinning = false;
        }
        else
        {
            // Normal melee attack - döner saldırı
            Debug.Log($"Boss performing spinning {attackType} attack!");
            
            float attackDuration = 0.8f;
            float rotationSpeed = 360f; // Saniyede 360 derece dönüş
            float elapsed = 0f;
            
            while (elapsed < attackDuration)
            {
                // Boss'u döndür
                transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Player'a hasar ver
            if (Vector2.Distance(transform.position, playerTarget.position) <= 3f)
            {
                PlayerController player = playerTarget.GetComponent<PlayerController>();
                if (player != null)
                {
                    player.TakeDamage(30f, attackType);
                }
            }
        }
    }
    
    private IEnumerator ReturnToStartPosition()
    {
        Debug.Log("Boss returning to start position!");
        
        Vector2 returnDirection = ((Vector2)startPosition - (Vector2)transform.position).normalized;
        float returnSpeed = 8f;
        
        while (Vector2.Distance(transform.position, startPosition) > 0.5f)
        {
            transform.position += (Vector3)returnDirection * returnSpeed * Time.deltaTime;
            returnDirection = ((Vector2)startPosition - (Vector2)transform.position).normalized;
            yield return null;
        }
        
        transform.position = startPosition;
    }
    
    // ========== PHASE 2 ==========
    private void StartPhase2()
    {
        currentPhase = 2;
        Debug.Log("BOSS PHASE 2 STARTED!");
        // Phase 2 animasyonu buraya eklenebilir
    }
    
    private void Phase2Behavior()
    {
        // Phase 1 davranışları + minion spawn
        Phase1Behavior();
        
        // Minion spawn
        if (Time.time - lastMinionSpawnTime >= minionSpawnCooldown)
        {
            SpawnMinions();
            lastMinionSpawnTime = Time.time;
        }
    }
    
    private void SpawnMinions()
    {
        if (minionPrefab == null) return;
        
        // 2-3 minion spawn yap
        int minionCount = Random.Range(2, 4);
        
        for (int i = 0; i < minionCount; i++)
        {
            // Boss'un yanında spawn et
            Vector3 spawnOffset = new Vector3(Random.Range(-1f, 1f), Random.Range(0.5f, 1.5f), 0);
            Vector3 spawnPosition = transform.position + spawnOffset;
            
            GameObject minion = Instantiate(minionPrefab, spawnPosition, Quaternion.identity);
            
            // Boss ile minion arasında collision'ı kapat
            Collider2D bossCollider = GetComponent<Collider2D>();
            Collider2D minionCollider = minion.GetComponent<Collider2D>();
            if (bossCollider != null && minionCollider != null)
            {
                Physics2D.IgnoreCollision(bossCollider, minionCollider, true);
                Debug.Log("Boss-Minion collision ignored");
            }
            
            // Diğer minion'larla da collision'ı kapat
            MinionEnemy[] existingMinions = FindObjectsOfType<MinionEnemy>();
            foreach (MinionEnemy existingMinion in existingMinions)
            {
                if (existingMinion.gameObject != minion) // Kendisi ile değil
                {
                    Collider2D existingMinionCollider = existingMinion.GetComponent<Collider2D>();
                    if (existingMinionCollider != null && minionCollider != null)
                    {
                        Physics2D.IgnoreCollision(minionCollider, existingMinionCollider, true);
                        Debug.Log("Minion-Minion collision ignored");
                    }
                }
            }
            
            // Minion'un attachable layer'larını al
            MinionEnemy minionScript = minion.GetComponent<MinionEnemy>();
            LayerMask attachableLayers = -1; // Default olarak tüm layer'lar
            if (minionScript != null)
            {
                attachableLayers = minionScript.attachableLayers;
            }
            
            // Yapışılabilen bir yüzey bul ve o yöne fırlat
            Vector2 throwDirection = FindBestThrowDirection(spawnPosition, attachableLayers);
            
            Rigidbody2D minionRb = minion.GetComponent<Rigidbody2D>();
            if (minionRb == null) 
                minionRb = minion.AddComponent<Rigidbody2D>();
            
            float throwForce = Random.Range(10f, 15f);
            minionRb.AddForce(throwDirection * throwForce, ForceMode2D.Impulse);
            
            // Minion'u 15 saniye sonra yok et (eğer player öldürmezse)
            Destroy(minion, 15f);
            
            Debug.Log($"Boss threw minion towards attachable surface in direction: {throwDirection}!");
        }
    }
    
    private Vector2 FindBestThrowDirection(Vector3 spawnPos, LayerMask attachableLayers)
    {
        float scanRadius = 15f; // Tarama mesafesi
        int rayCount = 24; // 24 farklı yöne ray at (360/24 = 15 derece aralıklar)
        
        Vector2 bestDirection = Vector2.right; // Varsayılan yön
        bool foundAttachableSurface = false;
        
        // Kullanılabilir yönleri topla
        List<Vector2> availableDirections = new List<Vector2>();
        
        for (int i = 0; i < rayCount; i++)
        {
            // 360 dereceyi ray sayısına böl
            float angle = (360f / rayCount) * i;
            Vector2 rayDirection = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            
            // Raycast at
            RaycastHit2D hit = Physics2D.Raycast(spawnPos, rayDirection, scanRadius, attachableLayers);
            
            if (hit.collider != null)
            {
                foundAttachableSurface = true;
                
                // Uygun mesafedeki tüm yönleri kaydet (çok yakın olmasın)
                if (hit.distance > 3f)
                {
                    availableDirections.Add(rayDirection);
                }
                
                // Debug için ray çiz
                Debug.DrawRay(spawnPos, rayDirection * hit.distance, Color.green, 2f);
            }
            else
            {
                // Hiçbir şeye çarpmayan ray'ler
                Debug.DrawRay(spawnPos, rayDirection * scanRadius, Color.red, 1f);
            }
        }
        
        if (availableDirections.Count > 0)
        {
            // Rastgele bir uygun yön seç
            bestDirection = availableDirections[Random.Range(0, availableDirections.Count)];
            Debug.Log($"Selected random direction from {availableDirections.Count} available directions");
        }
        else if (!foundAttachableSurface)
        {
            // Hiç yapışılabilen yüzey bulunamadıysa, geniş açıda rastgele yön
            float randomAngle = Random.Range(0f, 360f);
            bestDirection = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad));
            Debug.Log("No attachable surface found, throwing in wide random direction");
        }
        
        return bestDirection;
    }
    
    // ========== PHASE 3 ==========
    private void StartPhase3()
    {
        currentPhase = 3;
        currentComboCount = 0;
        Debug.Log("BOSS PHASE 3 STARTED!");
        // Phase 3 animasyonu buraya eklenebilir
    }
    
    private void Phase3Behavior()
    {
        // Enhanced behavior - daha hızlı saldırılar
        Phase2Behavior(); // Tüm önceki davranışları korur
    }
    
    // ========== PARRY SYSTEM ==========
    public override void OnPlayerParry()
    {
        if (currentPhase == 3)
        {
            currentComboCount++;
            Debug.Log($"Boss parried! Combo: {currentComboCount}/{comboRequiredForStun}");
            
            if (currentComboCount >= comboRequiredForStun)
            {
                StartCoroutine(Stun());
                currentComboCount = 0;
            }
        }
        else
        {
            // Phase 1-2'de her parry stun yapar
            StartCoroutine(Stun());
        }
    }
    
    private IEnumerator Stun()
    {
        isStunned = true;
        Debug.Log("Boss is stunned!");
        
        // Tüm saldırıları durdur
        StopAllCoroutines();
        isCharging = false;
        isSpinning = false;
        
        yield return new WaitForSeconds(1f);
        
        isStunned = false;
        Debug.Log("Boss recovered from stun!");
    }
    
    protected override void Die()
    {
        Debug.Log("BOSS DEFEATED!");
        
        // Player'a dash yeteneği ver
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.UnlockDash();
        }
        
        base.Die();
    }
    
    // Debug görselleştirme
    private void OnDrawGizmosSelected()
    {
        // Spin attack radius
        if (isCharging || isSpinning)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 4f);
        }
        
        // Melee range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 3f);
    }
}
