using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Destroy(gameObject); // Destroy bullet on collision
    }
}
