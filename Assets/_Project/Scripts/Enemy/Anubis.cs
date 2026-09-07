using System.Collections;
using UnityEngine;

public class Anubis : EnemyBase
{
    [Header("Anubis Special Skill")]
    public float spinAttackCooldown = 8f;
    private float spinTimer = 0f;
    public float spinHitRadius = 3.5f;
    public float spinDamage = 30f;
    public float spinDuration = 3.0f;

    protected override void Update()
    {
        timer += Time.deltaTime;
        spinTimer += Time.deltaTime;

        if (isDead || target == null) return;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        bool isAttackingNow = stateInfo.IsName("Attack");
        bool isHurtNow = stateInfo.IsName("Hurt");
        bool isSpinningNow = stateInfo.IsName("SpinAttack");

        if (isAttackingNow || isHurtNow || isSpinningNow)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetFloat("MoveSpeed", 0f);
            return;
        }

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist <= attackRange)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            Vector3 lookPos = new Vector3(target.position.x, transform.position.y, target.position.z);
            transform.LookAt(lookPos);
            anim.SetFloat("MoveSpeed", 0f);

            if (anim.GetCurrentAnimatorStateInfo(0).IsName("Attack") == false &&
                anim.GetCurrentAnimatorStateInfo(0).IsName("SpinAttack") == false)
            {
                Attack();
            }
        }
        else
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);

            Vector3 lookDir = target.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 15f);
            }

            float speed = agent.velocity.magnitude;
            anim.SetFloat("MoveSpeed", speed, 0.05f, Time.deltaTime);
        }
    }

    protected override void Attack()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (spinTimer >= spinAttackCooldown && timer >= enemyData.attackCooldown)
        {
            StartCoroutine(SpinAttackRoutine());
            timer = 0f;
            spinTimer = 0f;
        }
        else if (timer >= enemyData.attackCooldown)
        {
            base.Attack();
        }
    }

    IEnumerator SpinAttackRoutine()
    {
        isSuperArmor = true;
        anim.SetTrigger("SpinAttack");

        yield return new WaitForSeconds(spinDuration);

        isSuperArmor = false;
    }

    public void OnSpinHitEvent()
    {
        if (isDead) return;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, spinHitRadius);
        foreach (Collider hit in hitColliders)
        {
            if (hit.CompareTag("Player"))
            {
                hit.GetComponent<Player>()?.TakeDamage(spinDamage);
                break; // 1타당 1번만 데미지
            }
        }
    }

    IEnumerator HitSuperArmorRoutine()
    {
        isSuperArmor = true;
        yield return new WaitForSeconds(3.0f);
        isSuperArmor = false;
    }

    public override void TakeDamage(float damage, bool isCritical = false)
    {
        base.TakeDamage(damage, isCritical);

        if (isDead)
        {
            StopAllCoroutines();
            return;
        }

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        bool isAttackingNow = stateInfo.IsName("Attack");
        bool isSpinningNow = stateInfo.IsName("SpinAttack");

        if (isSuperArmor || isAttackingNow || isSpinningNow)
        {
            anim.ResetTrigger("Hurt");
        }
        else if (currentHealth > 0)
        {
            StopAllCoroutines();
            StartCoroutine(HitSuperArmorRoutine());
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spinHitRadius);
    }
}