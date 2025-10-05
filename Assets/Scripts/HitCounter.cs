using System;
using UnityEngine;

public class HitCounter : MonoBehaviour
{
    private int _hits;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<QuadraticDrag>())
        {
            if (PlayerPrefs.HasKey("Hits"))
            {
                _hits = PlayerPrefs.GetInt("Hits") + 1;
                PlayerPrefs.SetInt("Hits", _hits);
                Debug.Log($"Hit! Total: {PlayerPrefs.GetInt("Hits")}");
            }
            else
            {
                PlayerPrefs.SetInt("Hits", 1);
                Debug.Log($"Hit! Total: {PlayerPrefs.GetInt("Hits")}");
            }
        }
    }
}


