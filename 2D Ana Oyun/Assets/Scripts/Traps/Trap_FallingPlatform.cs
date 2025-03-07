using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap_FallingPlatform : MonoBehaviour
{
    private Animator _animator;
    private Rigidbody2D _rigidbody;
    private BoxCollider2D[] _boxColliders;

    [SerializeField] private float speed = 0.75f;
    [SerializeField] private float travelDistance;
    public Vector3[] wayPoints;
    private int wayPointIndex = 0;
    public bool canMove = false;

    [Header("Platform Fall Details")]
    [SerializeField] private float fallDelay = 0.5f;
    [SerializeField] private float impactSpeed = 3f;
    [SerializeField] private float impactDuration = 0.1f;
    private float impactTimer;
    private bool impactHappend = false;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _boxColliders = GetComponents<BoxCollider2D>(); 
    }
    private void Start()
    {
        SetupWayPoints();

        float randomDelay = Random.Range(0f, 1f);
        Invoke(nameof(ActivatePlatform), randomDelay);
    }

    private void ActivatePlatform() => canMove = true;

    private void SetupWayPoints()
    {
        wayPoints = new Vector3[2];

        float yOffset = travelDistance / 2;

        wayPoints[0] = transform.position + new Vector3(0f,yOffset, 0f);
        wayPoints[1] = transform.position + new Vector3(0f,-yOffset, 0f);
    }

    private void Update()  // ****************************************************
    {
        HandleMovement();
        HandleImpact();
    }

    private void HandleMovement()
    {
        if (canMove == false)
            return;

        transform.position = Vector2.MoveTowards(transform.position, wayPoints[wayPointIndex], speed * Time.deltaTime);

        if (Vector2.Distance(transform.position, wayPoints[wayPointIndex]) < 0.1f)
        {
            wayPointIndex++;

            if(wayPointIndex >= wayPoints.Length)
            {
                wayPointIndex = 0;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (impactHappend)
            return;

        if (collision.gameObject.tag == "Player")
        {
            Invoke(nameof(SwitchOffPlatform), fallDelay);
            impactTimer = impactDuration;
            impactHappend = true;
        }
    }

    private void SwitchOffPlatform()
    {
        _animator.SetTrigger("deactivate");
        
        canMove= false;

        _rigidbody.isKinematic = false;
        _rigidbody.gravityScale = 3.5f;
        _rigidbody.drag = 0.5f;

        foreach (BoxCollider2D collider in _boxColliders)
        {
            collider.enabled = false;
        }
    }

    private void HandleImpact()
    {
        if (impactTimer < 0)
            return;

        impactTimer -= Time.deltaTime;
        transform.position = Vector2.MoveTowards(transform.position, transform.position + (Vector3.down * 10), impactSpeed * Time.deltaTime);
    }
}
