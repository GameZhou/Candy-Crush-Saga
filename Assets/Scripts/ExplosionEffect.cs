using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionEffect : MonoBehaviour
{
    void End()
    {
        GameManager.instance.AnimaPlayEnd();
        Destroy(gameObject);
    }
}
