using System.Collections;
using UnityEngine;

public class TargetedShootSkill : Skill
{
	public GameObject projectilePrefab;
	public Transform firePoint;
	public float projectileSpeed = 8f;
	public float projectileLifetime = 4f;

	// Queue shot so physics changes happen in FixedUpdate
	private bool _shotQueued;
	private Vector3 _queuedSpawnPos;
	private Vector2 _queuedDir;
	private GameObject _queuedPrefab;
	private float _queuedLifetime;

	private void Awake()
	{
		if (firePoint == null)
		{
			firePoint = transform;
		}
	}

	protected override bool UseOn(Transform target)
	{
		if (projectilePrefab == null || firePoint == null || target == null)
		{
			return false;
		}

		Vector3 spawnPos = firePoint.position;
		Vector2 dir = ((Vector2)(target.position - spawnPos)).normalized;

		// Queue for FixedUpdate
		_queuedSpawnPos = spawnPos;
		_queuedDir = dir;
		_queuedPrefab = projectilePrefab;
		_queuedLifetime = projectileLifetime;
		_shotQueued = true;
		return true;
	}

	private void FixedUpdate()
	{
		if (_shotQueued)
		{
			_shotQueued = false;
			FireNow(_queuedPrefab, _queuedSpawnPos, _queuedDir, _queuedLifetime);
		}
	}

	private void FireNow(GameObject prefab, Vector3 spawnPos, Vector2 dir, float lifetime)
	{
		if (prefab == null) return;
		GameObject projectile = Object.Instantiate(prefab, spawnPos, Quaternion.identity);
		Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
		if (rb != null)
		{
			rb.velocity = dir * projectileSpeed;
		}
		else
		{
			StartCoroutine(MoveProjectileTransform(projectile.transform, dir, lifetime));
		}
		if (lifetime > 0f)
		{
			Object.Destroy(projectile, lifetime);
		}
	}

	private IEnumerator MoveProjectileTransform(Transform proj, Vector2 dir, float lifetime)
	{
		float elapsed = 0f;
		while (proj != null && elapsed < lifetime)
		{
			proj.Translate((Vector3)(dir * projectileSpeed * Time.fixedDeltaTime), Space.World);
			elapsed += Time.fixedDeltaTime;
			yield return new WaitForFixedUpdate();
		}
	}
}


