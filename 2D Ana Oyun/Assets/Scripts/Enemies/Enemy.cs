using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    protected Animator _animator;
    protected Rigidbody2D rb;
    protected Collider2D[] _colliders2d;
    private SpriteRenderer _spriteRenderer => GetComponent<SpriteRenderer>();

    [SerializeField] protected float moveSpeed = 2f;

    protected int facingDir = -1;

    [Header("Basic Collision")]
    [SerializeField] protected float groundCheckDistance = 1.1f;
    [SerializeField] protected float wallCheckDistance = 0.7f;
    [SerializeField] protected LayerMask whatIsGround;
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected LayerMask whatIsPlayer;
    [SerializeField] protected float playerDetectionDistance;
    protected bool isPlayerDetected;
    protected bool isWallDetected;
    protected bool isGrounded;
    protected bool isGroundInfrontDetected;
    protected bool canMove = true;

    protected bool facingRight = false;

    [SerializeField] protected float idleDuration = 1.5f;
    protected float idleTimer;

    [Header("Death Details")]
    [SerializeField] protected float deathImpactSpeed = 5;
    [SerializeField] protected float deathRotationSpeed = 150;
    protected int deathRotationDirection = 1;
    protected bool isDead;

    protected Transform player;

    //***************************************************************************************************************
    protected virtual void Awake()
    {
        _animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        _colliders2d = GetComponentsInChildren<Collider2D>();
    }

    protected virtual void Start()
    {
        InvokeRepeating(nameof(UpdatePlayerRef), 0f, 1f);

        if (_spriteRenderer.flipX == true && !facingRight)
        {
            _spriteRenderer.flipX = false;
            Flip();
        }
    }

    private void UpdatePlayerRef()
    {
        if (player == null)
            player = GameManager.instance.player.transform;
    }

    protected virtual void Update()
    {
        HandleAnimator();
        HandleCollision();

        idleTimer -= Time.deltaTime;

        if (isDead)
            HandleDeathRotation();
    }

    protected virtual void HandleFlip(float xValue)
    {
        if (xValue < transform.position.x && facingRight || xValue > transform.position.x && !facingRight)
            Flip();
    }

    protected virtual void Flip()
    {
        facingDir = facingDir * -1;
        transform.Rotate(0f, 180f, 0f);
        facingRight = !facingRight;
    }

    protected virtual void HandleCollision()
    {
        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, whatIsGround);
        isGroundInfrontDetected = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
        isWallDetected = Physics2D.Raycast(transform.position, Vector2.right * facingDir, wallCheckDistance, whatIsGround);
        isPlayerDetected = Physics2D.Raycast(transform.position, Vector2.right * facingDir, playerDetectionDistance, whatIsPlayer);
    }

    public virtual void Die()
    {
        foreach (var collider in _colliders2d)
        {
            collider.enabled = false;
        }

        _animator.SetTrigger("hit");
        rb.velocity = new Vector2(rb.velocity.x, deathImpactSpeed);

        isDead = true;

        if (Random.Range(0, 100) < 50)
            deathRotationDirection = deathRotationDirection * -1;

        Destroy(gameObject, 10f);
    }

    private void HandleDeathRotation()
    {
        transform.Rotate(0f, 0f, (deathRotationSpeed * deathRotationDirection) * Time.deltaTime);
    }

    protected virtual void HandleAnimator()
    {
        _animator.SetFloat("xVelocity", rb.velocity.x);
    }

    [ContextMenu("Change Facing Direction")]

    public void FlipDefaultFacingDirection()
    {
        _spriteRenderer.flipX = !_spriteRenderer.flipX;
    }

    protected virtual void OnDrawGizmos()
    {
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x, transform.position.y - groundCheckDistance));
        Gizmos.DrawLine(groundCheck.position, new Vector2(groundCheck.position.x, groundCheck.position.y - groundCheckDistance));
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x + (wallCheckDistance * facingDir), transform.position.y));
        Gizmos.DrawLine(transform.position, new Vector2(transform.position.x + (playerDetectionDistance * facingDir), transform.position.y));
    }
}
