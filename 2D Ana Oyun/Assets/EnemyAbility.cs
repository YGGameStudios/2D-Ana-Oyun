using System.Collections;
using UnityEngine;

[System.Serializable]
public abstract class EnemyAbility : ScriptableObject
{
    [Header("Ability Properties")]
    public string abilityName;
    public string description;
    public float cooldown = 1f;
    public float damage = 10f;
    public float range = 5f;
    
    protected float lastUsedTime;
    
    public bool CanUse()
    {
        return Time.time - lastUsedTime >= cooldown;
    }
    
    public virtual void Use(BaseEnemy caster, Transform target = null)
    {
        if (!CanUse()) return;
        
        lastUsedTime = Time.time;
        ExecuteAbility(caster, target);
    }
    
    protected abstract void ExecuteAbility(BaseEnemy caster, Transform target);
    
    public virtual EnemyAbility Clone()
    {
        return Instantiate(this);
    }
}
