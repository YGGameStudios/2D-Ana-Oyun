using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap_FireButton : MonoBehaviour
{
    private Animator _animator;
    private Trap_Fire trapFire;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        trapFire = GetComponentInParent<Trap_Fire>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            _animator.SetTrigger("activate");
            trapFire.SwitchOffFire();
        }
    }

}
