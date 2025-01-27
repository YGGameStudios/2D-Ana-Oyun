using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;

    private float xInput;
    private float yInput;
    [Header("Hareketler")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float doubleJumpForce;

    [Header("Hareket Collision larý")]
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float wallCheckDistance;
    private bool isGrounded;
    private bool isWallDetected;

    private bool isFacingRight = true;
    private int facingDirection = 1;

    private bool canDoubleJump;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (isGrounded)
            canDoubleJump = true;

        InputAyarlari();
        WallSlideAyarlari();
        HareketAyarlari();
        FlipAyarlari();
        CollisionAyarlari();
        AnimasyonAyarlari();

    }

    private void WallSlideAyarlari()
    {
        float yModifier = yInput < 0 ? 1 : 0.05f;

        if (isWallDetected && rb.velocity.y < 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * yModifier);
        }
    }

    private void InputAyarlari()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded)
            {
                Jump();
            }
            else if (canDoubleJump)
            {
                DoubleJump();
                canDoubleJump = false;
            }
        }
    }

    private void Jump() => rb.velocity = new Vector2(rb.velocity.x, jumpForce);

    private void DoubleJump() => rb.velocity = new Vector2(rb.velocity.x, doubleJumpForce);

    private void CollisionAyarlari()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, whatIsGround);
        isWallDetected = Physics2D.Raycast(transform.position, Vector2.right * facingDirection, wallCheckDistance, whatIsGround);
    }

    private void AnimasyonAyarlari()
    {
        anim.SetFloat("xMove", rb.velocity.x);
        anim.SetFloat("yMove", rb.velocity.y);
        anim.SetBool("isGrounded", isGrounded);
        anim.SetBool("isWallDetected", isWallDetected);
    }

    private void HareketAyarlari()
    {
        if (isWallDetected)
            return;

        rb.velocity = new Vector2(xInput * moveSpeed, rb.velocity.y);
    }

    private void FlipAyarlari()
    {
        if (xInput < 0f && isFacingRight || xInput > 0 && !isFacingRight)
        {
            transform.Rotate(0f, 180f, 0f);
            isFacingRight = !isFacingRight;
            facingDirection = facingDirection * -1;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y - groundCheckDistance));
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x + (wallCheckDistance * facingDirection), transform.position.y));
    }
}
