using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_SnailBody : MonoBehaviour
{
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private float zRotation;


    public void SetupBody(float yVelocity, float zRotation, int facingDir)
    {
        rb= GetComponent<Rigidbody2D>();
        sr= GetComponent<SpriteRenderer>();

        rb.velocity = new Vector2(rb.velocity.x, yVelocity);
        
        this.zRotation= zRotation;

        if (facingDir == 1)
            sr.flipX = true;
    }

    private void Update()
    {
        transform.Rotate(0f,0f, zRotation* Time.deltaTime);
    }
}
