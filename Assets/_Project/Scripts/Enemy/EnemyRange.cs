using UnityEngine;

public class EnemyRange : EnemyBase
{
    [Header("===== 원거리 (탄막) 개별 설정 =====")]
    [Tooltip("인스펙터에서 직접 조절하는 공격 쿨타임")]
    public float rangeAttackCooldown = 1.5f;

    [Tooltip("투사체 날아가는 속도")]
    public float projectileSpeed = 8f;

    [Tooltip("투사체가 발사될 위치")]
    public Transform firePoint;

    [Tooltip("한 번에 발사할 총알 개수")]
    [Range(1, 15)]
    public int projectileCount = 1;

    [Tooltip("총알이 퍼지는 각도")]
    [Range(0f, 90f)]
    public float spreadAngle = 15f;

    private float rangeTimer;

    private bool isAppeared = false;

    protected override void Update()
    {
        if (target == null || isDead) return;

        float dist = Vector3.Distance(transform.position, target.position);

        if (!isAppeared)
        {
            if (dist <= enemyData.detectionRange)
            {
                isAppeared = true;
                anim.SetTrigger("Appear");
            }
            return;
        }

        if (dist <= enemyData.attackRange)
        {
            Attack();
        }
    }

    protected override void Attack()
    {
        if (target != null)
        {
            Vector3 lookDir = (target.position - transform.position).normalized;
            lookDir.y = 0;

            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 15f);
            }
        }

        rangeTimer += Time.deltaTime;

        if (rangeTimer >= rangeAttackCooldown)
        {
            anim.SetTrigger("Attack");
            rangeTimer = 0;
        }
    }

    public void FireProjectileEvent()
    {
        if (target == null || firePoint == null) return;

        Vector3 targetPostion = target.position + Vector3.up * 1f;

        Vector3 centerDirection = (targetPostion - firePoint.position).normalized;

        float startAngle = -spreadAngle * (projectileCount - 1) / 2f;

        for (int i = 0; i < projectileCount; i++)
        {
            float currentAngle = startAngle + (spreadAngle * i);

            Vector3 finalDirection = Quaternion.Euler(0, currentAngle, 0) * centerDirection;

            ProjectileManager.Instance.FireProjectile(
                firePoint.position,
                finalDirection,
                projectileSpeed,
                enemyData.damage
            );
        }
    }
}