using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossEnemy : BaseEnemy
{
    [Header("Animations")]
    public Animator animator;
    public string idleAnim = "BossIdle";
    public string hurtAnim = "BossHurt";
    public string deathAnim = "BossDeath";

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
    // Ek: Kırmızı/Mavi fireball ve interval aralığı
    [Header("Ranged Fireballs")]
    public GameObject redFireballPrefab;
    public GameObject blueFireballPrefab;
    public Vector2 phase1FireballIntervalRange = new Vector2(0.5f, 1.0f);
    private float nextFireballTime = 0f;
    public int fireballsBeforeDashMin = 4;
    public int fireballsBeforeDashMax = 7;
    private int targetFireballsBeforeDash = 5;

    [Header("Melee Parry Settings")] 
    public KeyCode upperParryKey = KeyCode.Q; // L1
    public KeyCode lowerParryKey = KeyCode.E; // R1
    [Range(0f,1f)] public float blockDamageRatio = 0.3f;
    public float parryStunDurationPhase1 = 1f;
    public float parryStunDurationPhase2 = 1f;
    public int comboRequiredForStun = 3; // Kaç başarılı parry gerekli
    
    [Header("Phase 2 - Melee + Minions")]
    public GameObject minionPrefab;
    public float minionSpawnCooldown = 8f;
    
    [Header("Phase 3 - Enhanced")]
    public float spinChargeDuration = 3f;
    public float spinDuration = 2f;
    public float spinRadius = 4f;
    public float spinDamage = 50f;
    public float spinPhase3ExtraSpeedTowardsPlayer = 1.5f; // Phase3 homing

    [Header("Phase Control")]
    public bool phase1Invulnerable = false; // İstenirse aktif edilebilir

    [Header("Phase Transition Animation Hooks")] 
    public string phase2TransitionAnim = "BossPhase2Transition";
    public string phase3TransitionAnim = "BossPhase3Transition";

    // Internal flags
    private bool performingMeleeSequence = false;
    private bool doingSpin = false;
    private bool isStunned = false;
    private bool isCharging = false;
    private bool isSpinning = false;
    private int currentPhase = 1;
    private int currentComboCount = 0;
    private int fireballsShot = 0;

    [Header("Melee Settings")]
    public float upperAttackRange = 2.5f;
    public float lowerAttackRange = 2.5f;
    public float upperAttackDamage = 20f;
    public float lowerAttackDamage = 20f;
    public float meleeReturnToIdleDelay = 0.15f;

    // Timers
    private float nextMinionSpawnTime = 0f;

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
        if (startPosition == Vector3.zero)
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
        // Ranged interval başlangıç ayarı
        targetFireballsBeforeDash = Random.Range(fireballsBeforeDashMin, fireballsBeforeDashMax + 1);
        nextFireballTime = Time.time + Random.Range(phase1FireballIntervalRange.x, phase1FireballIntervalRange.y);
    }
    
    private void Phase1Behavior()
    {
        // Random aralıklarla ateş topu at
        if (Time.time >= nextFireballTime)
        {
            ShootFireballPhase1();
            fireballsShot++;
            nextFireballTime = Time.time + Random.Range(phase1FireballIntervalRange.x, phase1FireballIntervalRange.y);
            
            // Belirli sayıda ateş topu attıktan sonra dash yap
            if (fireballsShot >= targetFireballsBeforeDash)
            {
                StartCoroutine(DashToPlayerAndMelee());
                fireballsShot = 0;
                targetFireballsBeforeDash = Random.Range(fireballsBeforeDashMin, fireballsBeforeDashMax + 1);
            }
        }
    }

    private void ShootFireballPhase1()
    {
        if (playerTarget == null) return;
        GameObject prefabToUse = null;
        if (redFireballPrefab != null && blueFireballPrefab != null)
            prefabToUse = (Random.value > 0.5f) ? redFireballPrefab : blueFireballPrefab;
        else
            prefabToUse = fireballPrefab; // geri dönüş
        if (prefabToUse == null) return;
        
        Vector2 playerDirection = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
        Vector2 randomDirection = playerDirection + new Vector2(Random.Range(-0.3f, 0.3f), Random.Range(-0.2f, 0.2f));
        
        GameObject fireball = Instantiate(prefabToUse, transform.position, Quaternion.identity);
        Rigidbody2D rb = fireball.GetComponent<Rigidbody2D>();
        if (rb == null) rb = fireball.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.velocity = randomDirection.normalized * 8f;
        
        ProjectileController projectile = fireball.GetComponent<ProjectileController>();
        if (projectile == null) projectile = fireball.AddComponent<ProjectileController>();
        projectile.damage = 25f;
        projectile.caster = this;
        Destroy(fireball, 5f);
    }

    private IEnumerator DashToPlayerAndMelee()
    {
        if (playerTarget == null) yield break;
        performingMeleeSequence = true;
        Debug.Log("Boss dashing to player for melee!");
        Vector2 dashDir = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
        float dashSpeed = 12f;
        float dashTime = 1.0f;
        float t=0f;
        while (t<dashTime)
        {
            transform.position += (Vector3)dashDir * dashSpeed * Time.deltaTime;
            t += Time.deltaTime;
            yield return null;
        }
        // Melee pattern: random sayıda saldırı + belki spin dahil
        yield return StartCoroutine(ExecuteMeleePattern());
        // Geri dön
    yield return StartCoroutine(ReturnToStartPosition());
        performingMeleeSequence = false;
    }

    private IEnumerator ExecuteMeleePattern()
    {
        int meleeCount = Random.Range(2,4); // üst/alt saldırı adedi
        bool attemptedSpin = false;
        for (int i=0;i<meleeCount;i++)
        {
            bool upper = Random.value>0.5f;
            if (upper)
                yield return StartCoroutine(PerformUpperAttack());
            else
                yield return StartCoroutine(PerformLowerAttack());
            if (isStunned) break;
        }
        // Spin yapma şansı
        if (!isStunned && Random.value > 0.5f)
        {
            attemptedSpin = true;
            yield return StartCoroutine(PerformSpinAttack());
        }
        if (attemptedSpin && isStunned) yield break;
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
        // Parry/Block penceresi: vuruş anında değerlendir
        if (playerTarget != null && Vector2.Distance(transform.position, playerTarget.position) <= range)
        {
            float finalDamage = damage;
            bool perfectParry = false;
            KeyCode key = isUpper ? upperParryKey : lowerParryKey;
            if (Input.GetKeyDown(key))
            {
                perfectParry = true;
                finalDamage = 0f;
            }
            else if (Input.GetKey(key))
            {
                finalDamage = damage * blockDamageRatio;
            }
            if (perfectParry)
            {
                // Faz 1-2: direkt sersemlet; Faz 3: combo say
                if (currentPhase < 3)
                {
                    yield return StartCoroutine(ParryStunRoutine());
                }
                else
                {
                    OnPlayerParry();
                }
            }
            PlayerController player = playerTarget.GetComponent<PlayerController>();
            if (player != null && finalDamage > 0f)
            {
                string hitType = isUpper ? "upper" : "lower";
                player.TakeDamage(finalDamage, hitType);
            }
        }
        yield return null;
    }

    private IEnumerator ParryStunRoutine()
    {
        if (isStunned) yield break;
        isStunned = true;
        float dur = currentPhase==1 ? parryStunDurationPhase1 : parryStunDurationPhase2;
        yield return new WaitForSeconds(dur);
        isStunned = false;
    }

    private IEnumerator PerformSpinAttack(string attackType = "spin", bool isFullSpinAttack = true)
    {
        if (!isFullSpinAttack)
        {
            yield break; // Basitleştirme: sadece full spin kullanıyoruz
        }
        Debug.Log("Spin charge start");
        isCharging = true;
        float charge = 0f;
        while (charge < spinChargeDuration)
        {
            charge += Time.deltaTime;
            // İstersen charge anim event
            yield return null;
        }
        if (isStunned){ isCharging=false; yield break; }
        Debug.Log("Spin unleashed");
        isSpinning = true;
        isCharging = false;
        float timer = 0f;
        while (timer < spinDuration)
        {
            transform.Rotate(0f,0f,720f*Time.deltaTime);
            if (currentPhase==3 && playerTarget!=null)
            {
                // Phase3: hafif homing
                Vector2 dir = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
                transform.position += (Vector3)dir * spinPhase3ExtraSpeedTowardsPlayer * Time.deltaTime;
            }
            if (playerTarget!=null && Vector2.Distance(transform.position, playerTarget.position) <= spinRadius)
            {
                var pc = playerTarget.GetComponent<PlayerController>();
                if (pc!=null)
                {
                    pc.TakeDamage(spinDamage, "unblockable");
                }
            }
            timer += Time.deltaTime;
            yield return null;
        }
        isSpinning = false;
    }

    private IEnumerator ReturnToStartPosition()
    {
        float speed = 6f;
        while (Vector2.Distance(transform.position, startPosition) > 0.05f)
        {
            Vector2 dir = ((Vector2)startPosition - (Vector2)transform.position).normalized;
            transform.position += (Vector3)dir * speed * Time.deltaTime;
            yield return null;
        }
    }

    private void Phase2Behavior()
    {
        // Minyon spawn periyodu
        if (Time.time >= nextMinionSpawnTime)
        {
            SpawnMinions();
            nextMinionSpawnTime = Time.time + Mathf.Max(2f, minionSpawnCooldown);
        }
        // Faz 1 davranışını da kısmen sürdür
        Phase1Behavior();
    }

    private void Phase3Behavior()
    {
        // Daha agresif: daha kısa aralıklarla fireball/dash
        phase1FireballIntervalRange = new Vector2(0.35f, 0.8f);
        fireballsBeforeDashMin = 3;
        fireballsBeforeDashMax = 5;
        Phase1Behavior();
    }

    private void PlayMeleeAnimation(string anim)
    {
        if (animator != null && !string.IsNullOrEmpty(anim))
        {
            animator.Play(anim);
        }
    }

    public override void OnPlayerParry()
    {
        // Faz 3'te combo gerekir
        if (currentPhase >= 3)
        {
            currentComboCount++;
            if (currentComboCount >= comboRequiredForStun)
            {
                currentComboCount = 0;
                StartCoroutine(ParryStunRoutine());
            }
        }
        else
        {
            StartCoroutine(ParryStunRoutine());
        }
    }

    private void StartPhase2()
    {
        currentPhase = 2;
        Debug.Log("BOSS PHASE 2 STARTED!");
        if (animator!=null && !string.IsNullOrEmpty(phase2TransitionAnim)) animator.Play(phase2TransitionAnim);
        canTakeDamage = true; // Artık hasar alınabilir
    }
    
    private void StartPhase3()
    {
        currentPhase = 3;
        currentComboCount = 0;
        Debug.Log("BOSS PHASE 3 STARTED!");
        if (animator!=null && !string.IsNullOrEmpty(phase3TransitionAnim)) animator.Play(phase3TransitionAnim);
    }

    private void SpawnMinions()
    {
        if (minionPrefab == null) return;
        
        int minionCount = Random.Range(2, 4);
        
        for (int i = 0; i < minionCount; i++)
        {
            Vector3 spawnOffset = new Vector3(Random.Range(-1f,1f), Random.Range(0.5f,1.5f),0);
            Vector3 spawnPosition = transform.position + spawnOffset;
            GameObject minion = Instantiate(minionPrefab, spawnPosition, Quaternion.identity);
            // Minyonu shooter yap
            var shooter = minion.GetComponent<BossMinionShooter>();
            if (shooter == null)
            {
                shooter = minion.AddComponent<BossMinionShooter>();
                shooter.fireballPrefab = (Random.value>0.5f) ? redFireballPrefab : blueFireballPrefab;
                shooter.minShootInterval = 0.5f;
                shooter.maxShootInterval = 1.0f;
                shooter.lifeTime = 10f;
                shooter.target = playerTarget;
            }
            // ...existing code...
        }
    }

    public override void TakeDamage(float dmg)
    {
        if (currentPhase==1 && phase1Invulnerable) return; // Faz1 dokunulmazsa
        base.TakeDamage(dmg);
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

// Local shooter component for Phase 2 minions
public class BossMinionShooter : MonoBehaviour
{
    public GameObject fireballPrefab;
    public float minShootInterval = 0.5f;
    public float maxShootInterval = 1.0f;
    public float bulletSpeed = 7f;
    public float lifeTime = 10f;
    public Transform target;

    private float nextShootTime = 0f;

    private void Start()
    {
        if (lifeTime > 0)
            Destroy(gameObject, lifeTime);
        ScheduleNext();
    }

    private void Update()
    {
        if (target == null || fireballPrefab == null) return;
        if (Time.time >= nextShootTime)
        {
            Shoot();
            ScheduleNext();
        }
    }

    private void ScheduleNext()
    {
        nextShootTime = Time.time + Random.Range(minShootInterval, maxShootInterval);
    }

    private void Shoot()
    {
        Vector2 dir = ((Vector2)target.position - (Vector2)transform.position).normalized;
        GameObject proj = Instantiate(fireballPrefab, transform.position, Quaternion.identity);
        var rb = proj.GetComponent<Rigidbody2D>();
        if (rb == null) rb = proj.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.velocity = dir * bulletSpeed;
        var pc = proj.GetComponent<ProjectileController>();
        if (pc == null) pc = proj.AddComponent<ProjectileController>();
        pc.damage = 10f;
        Destroy(proj, 5f);
    }
}
