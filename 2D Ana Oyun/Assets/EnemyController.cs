using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Bu sınıf artık BaseEnemy sistemine geçtiğimiz için kullanımdan kaldırıldı
// Yerine BaseEnemy ve alt sınıflarını (FireEnemy, ChargerEnemy, HealerEnemy) kullanın

[System.Obsolete("Use BaseEnemy and its subclasses instead")]
public class EnemyController : MonoBehaviour
{
    void Start()
    {
        Debug.LogWarning("EnemyController is obsolete. Use BaseEnemy system instead.");
    }
}
