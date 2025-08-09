using System.Collections;
using UnityEngine;

public class MinionShooter : MonoBehaviour
{
    [Header("Shooting")] 
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
