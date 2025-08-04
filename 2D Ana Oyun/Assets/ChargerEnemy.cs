using UnityEngine;

public class ChargerEnemy : BaseEnemy
{
    [Header("Charger Enemy Custom Settings")]
    public bool useFastMovement = true;
    public bool useChargeAttack = true;
    public float chargeSpeedMultiplier = 3f;
    
    private bool isCharging = false;
    
    protected override void Update()
    {
        // Özel hareket kontrolü
        if (useFastMovement && !isCharging)
        {
            moveSpeed = 5f; // Hızlı hareket
        }
        
        base.Update(); // Ana hareket sistemi
    }
    
    protected override void Attack()
    {
        if (useChargeAttack && !isCharging)
        {
            StartChargeAttack();
        }
        else
        {
            base.Attack(); // Normal saldırı
        }
    }
    
    private void StartChargeAttack()
    {
        isCharging = true;
        SetCanMove(false); // Hareket durdur
        
        Debug.Log($"{enemyName} starts charging!");
        
        // 1 saniye sonra charge yap
        Invoke(nameof(ExecuteCharge), 1f);
    }
    
    private void ExecuteCharge()
    {
        if (target != null)
        {
            Vector2 chargeDirection = ((Vector2)target.position - (Vector2)transform.position).normalized;
            transform.position += (Vector3)chargeDirection * moveSpeed * chargeSpeedMultiplier;
            
            Debug.Log($"{enemyName} charges forward!");
        }
        
        // Charge bitince normal hareket
        Invoke(nameof(EndCharge), 0.5f);
    }
    
    private void EndCharge()
    {
        isCharging = false;
        SetCanMove(true); // Hareket tekrar aktif
        Debug.Log($"{enemyName} charge ended!");
    }
    
    protected override void Start()
    {
        base.Start();
        
        // Charger Enemy özelleştirmeleri
        enemyName = "Charger Enemy";
        maxHealth = 120f;
        currentHealth = maxHealth;
    }
    
    // Özel kontrol metodları
    public void ToggleFastMovement()
    {
        useFastMovement = !useFastMovement;
        Debug.Log($"Fast movement: {useFastMovement}");
    }
    
    public void ToggleChargeAttack()
    {
        useChargeAttack = !useChargeAttack;
        Debug.Log($"Charge attack: {useChargeAttack}");
    }
}
