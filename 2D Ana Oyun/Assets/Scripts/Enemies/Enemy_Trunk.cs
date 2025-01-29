using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Trunk : Enemy
{

    [Header("Trunk Details")]
    [SerializeField] private float attackCoolDown = 1.5f;
    private float lastTimeAttacked;
    [SerializeField] private Enemy_Bullet bulletPrefab;
    [SerializeField] private Transform gunPoint;
    [SerializeField] private float bulletSpeed = 7f;

    protected override void Update()
    {
        base.Update();

        if (isDead)
            return;

        bool canAttack = Time.time > lastTimeAttacked + attackCoolDown;

        if (isPlayerDetected && canAttack)
            Attack();

        HandleMovement();

        if (isGrounded)
            HandleTurnAround();
    }

    private void HandleTurnAround()
    {
        if (!isGroundInfrontDetected || isWallDetected)
        {
            Flip();
            idleTimer = idleDuration;
            rb.velocity = Vector2.zero;
        }
    }

    private void HandleMovement()
    {
        if (idleTimer > 0)
            return;

        rb.velocity = new Vector2(moveSpeed * facingDir, rb.velocity.y);
    }

    private void Attack()
    {
        idleTimer = idleDuration + attackCoolDown;
        _animator.SetTrigger("attack");
        lastTimeAttacked = Time.time;
    }

    private void CreateBullet()
    {
        Enemy_Bullet newBullet = Instantiate(bulletPrefab, gunPoint.position, Quaternion.identity);
        Vector2 bulletVelocity = new Vector2(bulletSpeed * facingDir, 0f);
        newBullet.SetVelocity(bulletVelocity);

        if (facingDir == 1)
            newBullet.FlipSprite();

        Destroy(newBullet, 10f);
    }
}
