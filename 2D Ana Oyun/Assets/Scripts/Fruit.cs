using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum FruitType { Apple, Banana, Cherry, Kiwi, Melon, Orange, Pineapple, Strawberry}

public class Fruit : MonoBehaviour
{
    [SerializeField] FruitType fruitType;

    [SerializeField] GameObject pickupVfx;

    GameManager gameManager;

    Animator anim;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        gameManager = GameManager.instance;
        SetRandomLookIfNeeded();
    }

    private void SetRandomLookIfNeeded()
    {
        if (gameManager.FruitsHaveRandomLook() == false)
        {
            UpdateFruitVisuals();
            return;
        }

        int randomIndex = Random.Range(0, 8);
        anim.SetFloat("fruitindex", randomIndex);
    }

    private void UpdateFruitVisuals() => anim.SetFloat("fruitindex", (int)fruitType);

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            gameManager.AddFruit();
            Destroy(gameObject);

            GameObject newFx = Instantiate(pickupVfx, transform.position,Quaternion.identity);
            Destroy(newFx, .5f);
        }
    }
}
