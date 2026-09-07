using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance;

    public GameObject enemyPrefab; 
    public Transform[] spawnPoint; 

    public System.Collections.Generic.List<EnemyBase> activeEnemies 
        = new System.Collections.Generic.List<EnemyBase>();
    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SpawnEnemy(3);
    }
    public void SpawnEnemy(int count)
    {
        for(int i = 0; i < count; i++)
        {
            int randomIndex = Random.Range(0, spawnPoint.Length);
            GameObject obj = Instantiate(enemyPrefab, spawnPoint[randomIndex].position, Quaternion.identity);

            activeEnemies.Add(obj.GetComponent<EnemyBase>());
        }
    }
    public void ReportDeath(EnemyBase enemy)
    {
        activeEnemies.Remove(enemy);
        if(activeEnemies.Count <= 0)
        {
            Debug.Log("몬스터 모두 처치");
        }
    }

    
    void Update()
    {
        
    }
}
