using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Rino : Enemy
{
    [Header("Rino Details")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float speedUpRate = 0.6f;
    private float defaultSpeed;
    [SerializeField] private Vector2 impactPower;

    protected override void Start()
    {
        base.Start();

        defaultSpeed = moveSpeed;
        canMove = false;
    }
    protected override void Update()
    {
        base.Update();

        HandleCharge();
    }

    private void HandleCharge()
    {
        if (canMove == false)
            return;

        HandleSpeedUp();

        rb.velocity = new Vector2(moveSpeed * facingDir, rb.velocity.y);

        if (isWallDetected)
            HitWall();

        if (!isGroundInfrontDetected)
        {
            TurnAround();
        }
    }

    private void HandleSpeedUp()
    {
        moveSpeed = moveSpeed + (Time.deltaTime * speedUpRate);
        if (moveSpeed >= maxSpeed)
            maxSpeed = moveSpeed;
    }

    private void TurnAround()
    {
        canMove = false;
        rb.velocity = Vector2.zero;
        Flip();
        ResetSpeed();
    }

    private void ResetSpeed() => moveSpeed = defaultSpeed;

    private void HitWall()
    {
        canMove = false;
        _animator.SetBool("wallHit", true);
        rb.velocity = new Vector2(impactPower.x * -facingDir, impactPower.y);
        ResetSpeed();
    }

    private void ChargeIsOver()
    {
        _animator.SetBool("wallHit", false);
        Invoke(nameof(Flip), 3f);
    }

    protected override void HandleCollision()
    {
        base.HandleCollision();

        if (isPlayerDetected && isGrounded)
            canMove = true;
    }

}
