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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        xInput = Input.GetAxisRaw("Horizontal");

        HareketAyarlari();
        AnimasyonAyarlari();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        }
    }

    private void AnimasyonAyarlari()
    {
        anim.SetFloat("xMove", rb.velocity.x);
    }

    private void HareketAyarlari()
    {
        rb.velocity = new Vector2(xInput * moveSpeed, rb.velocity.y);
    }
}
