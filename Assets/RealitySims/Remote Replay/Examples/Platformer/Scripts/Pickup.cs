using UnityEngine;

public class Pickup : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            // Destroy the coin object
            Destroy(gameObject);
        }
    }
}