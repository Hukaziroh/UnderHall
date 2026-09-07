using UnityEngine;

public class Projectile : MonoBehaviour
{
    private float speed;
    private float damage;
    private Vector3 direction;

    public void Initialize(Vector3 dir, float moveSpeed, float atkDamage)
    {
        direction = dir.normalized;
        speed = moveSpeed;
        damage = atkDamage;

        Destroy(gameObject, 5f);
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }

            Destroy(gameObject); 
        }
    }
}