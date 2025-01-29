using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Plant : Enemy
{
    [Header("Plant Details")]
    [SerializeField] private float attackCoolDown = 1.5f;
    private float lastTimeAttacked;
    [SerializeField] private Enemy_Bullet bulletPrefab;
    [SerializeField] private Transform gunPoint;
    [SerializeField] private float bulletSpeed = 7f;

    protected override void Update()
    {
        base.Update();

        bool canAttack = Time.time > lastTimeAttacked + attackCoolDown;

        if (isPlayerDetected && canAttack)
            Attack();
    }

    private void Attack()
    {
        _animator.SetTrigger("attack");
        lastTimeAttacked = Time.time;
    }

    private void CreateBullet()
    {
        Enemy_Bullet newBullet = Instantiate(bulletPrefab, gunPoint.position, Quaternion.identity);
        Vector2 bulletVelocity = new Vector2(bulletSpeed * facingDir, 0f);
        newBullet.SetVelocity(bulletVelocity);

        Destroy(newBullet, 10f);
    }

    protected override void HandleAnimator()
    {
        // ana scriptteki handle animasyonlarý çalýþmasýn diye boþ kalacak 
    }
}
