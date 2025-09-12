using UnityEngine;

public abstract class Skill : MonoBehaviour
{
	public float cooldown = 0.5f;

	private float _nextUseTime;

	protected bool IsReady()
	{
		return Time.time >= _nextUseTime;
	}

	protected void ArmCooldown()
	{
		_nextUseTime = Time.time + Mathf.Max(0f, cooldown);
	}

	public virtual bool TryUse()
	{
		if (!IsReady()) return false;
		bool used = Use();
		if (used) ArmCooldown();
		return used;
	}

	public virtual bool TryUseOn(Transform target)
	{
		if (!IsReady()) return false;
		bool used = UseOn(target);
		if (used) ArmCooldown();
		return used;
	}

	protected virtual bool Use()
	{
		return false;
	}

	protected virtual bool UseOn(Transform target)
	{
		return false;
	}
}


