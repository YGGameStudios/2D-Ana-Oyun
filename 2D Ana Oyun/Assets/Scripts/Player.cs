using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;

    private float xInput;
    [Header("Hareketler")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpForce;
    [SerializeField] private float doubleJumpForce;

    [Header("Hareket Collision larý")]
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private LayerMask whatIsGround;
    private bool isGrounded;

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

        CollisionAyarlari();
        InputAyarlari();
        HareketAyarlari();
        FlipAyarlari();
        AnimasyonAyarlari();

    }

    private void InputAyarlari()
    {
        xInput = Input.GetAxisRaw("Horizontal");

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
    }

    private void AnimasyonAyarlari()
    {
        anim.SetFloat("xMove", rb.velocity.x);
        anim.SetFloat("yMove", rb.velocity.y);
        anim.SetBool("isGrounded", isGrounded);
    }

    private void HareketAyarlari()
    {
        rb.velocity = new Vector2(xInput * moveSpeed, rb.velocity.y);
    }

    private void FlipAyarlari()
    {
        if (rb.velocity.x < 0f && isFacingRight || rb.velocity.x > 0 && !isFacingRight)
        {
            transform.Rotate(0f, 180f, 0f);
            isFacingRight = !isFacingRight;
            facingDirection = facingDirection * -1;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y - groundCheckDistance));
    }
}
