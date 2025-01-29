using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    float xInput;
    float yInput;

    bool canBeControlled = false;

    [Header("Movement")]
    [SerializeField] float moveSpeed = 0f;
    [SerializeField] float jumpForce = 0f;
    [SerializeField] float doubleJumpForce = 0f;
    [SerializeField] float respawnFinishedDelay = 0f;
    float defaultGravityScale;

    bool canDoubleJump;

    [Header("Wall Ýnteractions")]
    [SerializeField] float wallJumpDuration = 0.6f;
    [SerializeField] Vector2 wallJumpForce;
    bool isWallJumping;

    [Header("KnockBack")]
    [SerializeField] float knockbackDuration = 1f;
    [SerializeField] Vector2 knockbackPower;
    bool isknocked;


    [Header("Collision info")]
    [SerializeField] float groundCheckDistance;
    [SerializeField] LayerMask whatIsGround;
    [SerializeField] float wallCheckDistance;
    [Space]
    [SerializeField] private Transform enemyCheck;
    [SerializeField] private float enemyCheckRadius;
    [SerializeField] private LayerMask whatIsEnemy;
    bool isGrounded;
    bool isAirBorne;
    bool isWallDetected;

    [Header("Buffer & Cayote Jump")]
    [SerializeField] float bufferJumpWindow = .25f;
    public float bufferJumpActivated = -1;
    [SerializeField] float cayoteJumpWindow = .5f;
    float cayoteJumpActivated = -1;

    Rigidbody2D rb;
    Animator animator;
    CapsuleCollider2D capsuleCollider;

    bool isFacingRight = true;
    int facingDir = 1;

    [SerializeField] private Vector2 kutu;

    [Header("Player Visulation")]
    [SerializeField] GameObject deathVfx;
    [SerializeField] AnimatorOverrideController[] animators;
    [SerializeField] private int skinId;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        capsuleCollider = GetComponent<CapsuleCollider2D>();
    }

    private void Start()
    {
        defaultGravityScale = rb.gravityScale;
        StartCoroutine(RespawnFinishedCorutine());

        UpdateSkin();
    }


    void Update()
    {
        UpdateAirBorneStatus();

        if (canBeControlled == false)
        {
            HandleCollision();
            HandleAnimations();
            return;
        }

        if (isknocked)
            return;

        HandleEnemyDetection();
        HandleInput();
        HandleWallSlide();
        HandleMovement();
        HandleFlip();
        HandleCollision();
        HandleAnimations();

    }

    private void HandleEnemyDetection()
    {
        if (rb.velocity.y >= 0)
            return;

        //Collider2D[] colliders = Physics2D.OverlapCircleAll(enemyCheck.position, enemyCheckRadius, whatIsEnemy);
        Collider2D[] colliders = Physics2D.OverlapBoxAll(enemyCheck.position, kutu, whatIsEnemy);

        foreach (var enemy in colliders)
        {
            Enemy newEnemy = enemy.GetComponent<Enemy>();
            if (newEnemy != null)
            {
                newEnemy.Die();
                Jump();
            }
        }
    }

    public void RespawnFinished(bool finished)
    {
        if (finished)
        {
            canBeControlled = true;
            rb.gravityScale = defaultGravityScale;
            capsuleCollider.enabled = true;
        }
        else
        {
            canBeControlled = false;
            rb.gravityScale = 0;
            capsuleCollider.enabled = false;
        }
    }

    private IEnumerator RespawnFinishedCorutine()
    {
        RespawnFinished(false);
        yield return new WaitForSeconds(respawnFinishedDelay);
        RespawnFinished(true);
    }

    public void KnockBack(float sourceDamageXPosition)
    {
        float knockbackDir = 1f;

        if (transform.position.x < sourceDamageXPosition)
            knockbackDir = -1f;

        if (isknocked)
            return;

        StartCoroutine(KnockbackRoutine());

        rb.velocity = new Vector2(knockbackPower.x * knockbackDir, knockbackPower.y);
    }

    private IEnumerator KnockbackRoutine()
    {
        isknocked = true;
        animator.SetBool("isKnocked", true);

        yield return new WaitForSeconds(knockbackDuration);

        isknocked = false;
        animator.SetBool("isKnocked", false);
    }

    private void HandleWallSlide()
    {
        bool canWallSlide = isWallDetected && rb.velocity.y < 0;
        float yModifer = yInput < 0 ? 1f : 0.05f;

        if (canWallSlide == false)
            return;

        rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * yModifer);
    }

    private void UpdateAirBorneStatus()
    {
        if (isAirBorne && isGrounded)
        {
            HandleLanding();
        }

        if (!isAirBorne && !isGrounded)
        {
            BecomeAirBorne();
        }
    }

    private void BecomeAirBorne()
    {
        isAirBorne = true;

        if (rb.velocity.y < 1)
        {
            ActivateCayoteJump();
        }
    }

    private void HandleLanding()
    {
        isAirBorne = false;
        canDoubleJump = true;

        AttemptBufferJump();
    }

    private void HandleInput()
    {
        xInput = Input.GetAxisRaw("Horizontal");
        yInput = Input.GetAxisRaw("Vertical");


        if (Input.GetKeyDown(KeyCode.Space))
        {
            JumpButton();
            RequestBufferJump();
        }
    }

    private void RequestBufferJump()
    {
        if (isAirBorne)
        {
            bufferJumpActivated = Time.time;
        }
    }

    private void AttemptBufferJump()
    {
        if (Time.time < bufferJumpActivated + bufferJumpWindow)
        {
            bufferJumpActivated = Time.time - 1;
            Jump();
        }
    }

    private void ActivateCayoteJump()
    {
        cayoteJumpActivated = Time.time;
    }

    private void CancelCayoteJump()
    {
        cayoteJumpActivated = Time.time - 1;
    }

    private void JumpButton()
    {
        bool cayoteJumpAvailable = Time.time < cayoteJumpActivated + cayoteJumpWindow;

        if (isGrounded || cayoteJumpAvailable)
        {
            Jump();
        }
        else if (isWallDetected && !isGrounded)
        {
            WallJump();
        }
        else if (canDoubleJump)
        {
            DoubleJump();
        }

        CancelCayoteJump();
    }

    private void Jump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
    }

    private void DoubleJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, doubleJumpForce);
        canDoubleJump = false;
        isWallJumping = false;
    }

    private void WallJump()
    {
        canDoubleJump = true;
        rb.velocity = new Vector2(wallJumpForce.x * -facingDir, wallJumpForce.y);
        Flip();

        StopAllCoroutines();
        StartCoroutine(WallJumpRoutine());
    }

    private IEnumerator WallJumpRoutine()
    {
        isWallJumping = true;

        yield return new WaitForSeconds(wallJumpDuration);

        isWallJumping = false;
    }

    private void HandleCollision()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, whatIsGround);
        isWallDetected = Physics2D.Raycast(transform.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
    }

    private void HandleAnimations()
    {
        animator.SetFloat("xVelocity", rb.velocity.x);
        animator.SetFloat("yVelocity", rb.velocity.y);
        animator.SetBool("isGrounded", isGrounded);
        animator.SetBool("isWallDetected", isWallDetected);
    }

    private void HandleMovement()
    {
        if (isWallDetected)
            return;

        if (isWallJumping)
            return;

        rb.velocity = new Vector2(xInput * moveSpeed, rb.velocity.y);
    }

    private void HandleFlip()
    {
        if (xInput < 0 && isFacingRight || xInput > 0 && !isFacingRight)
        {
            Flip();
        }
    }

    private void Flip()
    {
        transform.Rotate(0f, 180f, 0f);
        isFacingRight = !isFacingRight;
        facingDir = facingDir * -1;
    }

    public void Die()
    {
        GameObject newDeathVfx = Instantiate(deathVfx, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }

    public void Push(Vector2 direction, float duration = 0)
    {
        StartCoroutine(PushCoroutine(direction, duration));
    }

    private IEnumerator PushCoroutine(Vector2 direction, float duration)
    {
        canBeControlled = false;

        rb.velocity = Vector2.zero;
        rb.AddForce(direction, ForceMode2D.Impulse);

        yield return new WaitForSeconds(duration);

        canBeControlled = true;
    }

    public void UpdateSkin()
    {
        if (SkinManager.instance == null)
            return;

        animator.runtimeAnimatorController = animators[SkinManager.instance.GetSkinId()];
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y - groundCheckDistance));
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x + (wallCheckDistance * facingDir), transform.position.y));
        //Gizmos.DrawWireSphere(enemyCheck.position, enemyCheckRadius);

        Gizmos.DrawWireCube(enemyCheck.position, kutu);
    }
}
