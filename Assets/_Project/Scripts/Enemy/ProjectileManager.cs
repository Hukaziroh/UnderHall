using UnityEngine;

public class ProjectileManager : MonoBehaviour
{
    public static ProjectileManager Instance;
    
    [Header("투사체 프리팹")]
    public GameObject rangeEnemyProjectilePrefab; 

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void FireProjectile(Vector3 startPos, Vector3 direction, float speed, float damage)
    {
        GameObject obj = Instantiate(rangeEnemyProjectilePrefab, startPos, Quaternion.LookRotation(direction));
        Projectile projectile = obj.GetComponent<Projectile>();

        if (projectile != null)
        {
            projectile.Initialize(direction, speed, damage);
        }
    }
}