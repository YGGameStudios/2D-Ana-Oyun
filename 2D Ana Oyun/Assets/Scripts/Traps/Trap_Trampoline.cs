using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap_Trampoline : MonoBehaviour
{
    private Animator _animator;

    [SerializeField] private float pushPower;
    [SerializeField] private float duration = 0.5f;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            GameManager.instance.player.Push(transform.up * pushPower, duration);
            _animator.SetTrigger("activate");
        }
    }
}
