using UnityEngine;

[CreateAssetMenu(fileName = "ChargeAbility", menuName = "Enemy Abilities/Charge")]
public class ChargeAbility : EnemyAbility
{
    [Header("Charge Settings")]
    public float chargeSpeed = 15f;
    public float chargeDuration = 1f;
    
    protected override void ExecuteAbility(BaseEnemy caster, Transform target)
    {
        if (target == null) return;
        
        caster.StartCoroutine(ExecuteCharge(caster, target));
        Debug.Log($"{caster.enemyName} used Charge!");
    }
    
    private System.Collections.IEnumerator ExecuteCharge(BaseEnemy caster, Transform target)
    {
        Vector2 chargeDirection = ((Vector2)target.position - (Vector2)caster.transform.position).normalized;
        float chargeTimer = 0f;
        
        while (chargeTimer < chargeDuration)
        {
            caster.transform.position += (Vector3)chargeDirection * chargeSpeed * Time.deltaTime;
            chargeTimer += Time.deltaTime;
            yield return null;
        }
    }
}
