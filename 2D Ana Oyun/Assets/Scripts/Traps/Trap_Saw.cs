using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap_Saw : MonoBehaviour
{
    private Animator anim;
    private SpriteRenderer _spriteRenderer;

    [SerializeField] float moveSpeed = 3f;
    [SerializeField] private float coolDown = 1f;

    [SerializeField] private Transform[] wayPoints;
    private Vector3[] wayPointPositions; 

    public int wayPointIndex = 1;
    public int moveDirection = 1;
    private bool canMove = true;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        UpdateWayPointsInfo();

        transform.position = wayPointPositions[0];
    }

    private void UpdateWayPointsInfo()
    {
        wayPointPositions = new Vector3[wayPoints.Length];

        for (int i = 0; i < wayPoints.Length; i++)
        {
            wayPointPositions[i] = wayPoints[i].position;
        }
    }

    private void Update()
    {
        anim.SetBool("active", canMove);

        if (canMove == false)
            return;


        transform.position = Vector2.MoveTowards(transform.position, wayPointPositions[wayPointIndex], moveSpeed * Time.deltaTime);

        if(Vector2.Distance(transform.position, wayPointPositions[wayPointIndex]) < 0.1f)
        {
            if (wayPointIndex == wayPointPositions.Length - 1 || wayPointIndex == 0)
            {
                moveDirection = moveDirection * -1;
                StartCoroutine(StopMovement(coolDown));
            }

            wayPointIndex = wayPointIndex + moveDirection;

            //wayPointIndex++;

            //if (wayPointIndex >= wayPoints.Length)
            //{
            //    wayPointIndex = 0;
            //    StartCoroutine(StopMovement(coolDown));
            //}
        }
    }

    IEnumerator StopMovement(float delay)
    {
        canMove = false;

        yield return new WaitForSeconds(delay);

        canMove= true;
        _spriteRenderer.flipX = !_spriteRenderer.flipX;
    }
}
