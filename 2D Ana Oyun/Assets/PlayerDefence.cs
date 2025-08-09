using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDefence : MonoBehaviour
{
    // Start is called before the first frame update

    public Animator animator;
    public bool isDefenceHigh = false;
    void Start()
    {
        animator = GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on PlayerDefence.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        DefenceHighControl();
        DefenceLowControl();
    }

    void DefenceHighControl()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            animator.Play("PlayerHighDefence");
            isDefenceHigh = true;
        }
        else if (Input.GetKeyUp(KeyCode.Q))
        {
            isDefenceHigh = false;
            animator.Play("Idle/Move"); // Idle animasyonuna dön
        }

    }
    void DefenceLowControl()
    {
        if (Input.GetKey(KeyCode.E))
        {
            animator.Play("PlayerLowDefence");
            isDefenceHigh = false;
        }
        else if (Input.GetKeyUp(KeyCode.E))
        {
            isDefenceHigh = false;
            animator.Play("Idle/Move"); // Idle animasyonuna dön
        }
    }
}
