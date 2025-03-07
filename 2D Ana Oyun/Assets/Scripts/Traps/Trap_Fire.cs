using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap_Fire : MonoBehaviour
{
    [SerializeField] private float offDuration;
    [SerializeField] private Trap_FireButton fireButton;

    private Animator _animator;
    private CapsuleCollider2D fireCollider;
    private bool isActive;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        fireCollider= GetComponent<CapsuleCollider2D>();
    }

    private void Start()
    {
        if (fireButton == null)
            Debug.LogWarning("you don't have fire button on " + gameObject.name + "!");

        SetFire(true);
    }

    public void SwitchOffFire()
    {
        if (isActive == false)
            return;

        StartCoroutine(FireCoroutine());
    }

    private IEnumerator FireCoroutine()
    {
        SetFire(false);
        yield return new WaitForSeconds(offDuration);
        SetFire(true);
    }

    private void SetFire(bool active)
    {
        _animator.SetBool("active", active);
        fireCollider.enabled = active;
        isActive = active;
    }
}
