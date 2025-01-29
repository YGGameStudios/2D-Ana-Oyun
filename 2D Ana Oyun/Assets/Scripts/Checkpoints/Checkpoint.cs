using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    Animator anim;
    bool active = false;

    [SerializeField] private bool canBeReactivated;
    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Start()
    {
        canBeReactivated = GameManager.instance.canReactivate;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (active && canBeReactivated == false)
            return;

        Player player = collision.gameObject.GetComponent<Player>();

        if (player != null)
            ActivateCheckpoint();
    }

    private void ActivateCheckpoint()
    {
        active = true;
        anim.SetTrigger("activate");
        GameManager.instance.UpdateRespawnPosition(transform);
    }
}
