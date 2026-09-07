using UnityEngine;

public class Monster : MonoBehaviour
{
    [Header("Monster Stats")]
    public float currentHealth = 50f;
    public float touchDamage = 40f;

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;

        Debug.Log($"몬스터 피격! 들어온 데미지: {damage} / 남은 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            Debug.Log("몬스터 처치됨!");
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Player player = collision.gameObject.GetComponent<Player>(); 

            if (player != null)
            {
                Debug.Log($"테스트 몬스터가 플레이어에게 {touchDamage}의 데미지를 줍니다!");
                player.TakeDamage(touchDamage);
            }
        }
    }
}