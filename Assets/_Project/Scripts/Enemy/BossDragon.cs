using System.Collections;
using UnityEngine;

public class BossDragon : EnemyBase
{
    [Header("Boss Attack Settings")]
    public float attackCooldown = 3f;

    [Header("Phase 2 Settings")]
    public float flyHeight = 5f;
    public float dashSpeed = 35f;
    private bool isPhase2 = false;
    private bool isFlying = false;
    private bool isAttacking = false;
    private bool isCooldown = false;

    [Header("Aerial Breath Height Setting")]
    public float aerialHoverHeightAdd = 10f;
    public float landingYOffset = 0.5f;
    public GameObject flyingBreathVFX;

    // [VFX/feat]창우_ Boss 전용 공격 이펙트 포인트 (머리)
    [Header("Boss Bones")]
    [SerializeField] private Transform headBone;

    [Header("Bite Attack Settings (Red Gizmo)")]
    public float biteHitRadius = 1f;
    public float biteHitOffset = 5f;
    public float biteHitHeight = 1.5f;

    [Header("Breath Settings (Blue Gizmo)")]
    public float breathDuration = 4.5f;
    public float breathHitRadius = 1.5f;
    public float breathHitOffset = 8f;
    public float breathHitHeight = -0.5f;

    // [추가] 물어뜯기 횟 카운트
    private int biteCount = 0;
    // 2페이즈 랜덤 억까 방지용 순서 제어 인덱스
    private int phase2AttackIndex = 0;

    protected override void Start()
    {
        base.Start();
        StartCoroutine(BossThinkRoutine());
    }

    // [추가] 부모의 매 프레임 행동(이동, 회전 등)을 통제
    protected override void Update()
    {
        if (isDead) return;

        if (isAttacking)
        {
            if (agent.enabled) agent.velocity = Vector3.zero;
            return;
        }
        // 공격 중이거나 비행 중일 때는 부모의 로직(회전 등)을 완전 무시 (얼음 상태)
        if (isAttacking || isFlying)
        {
            if (agent.enabled)
            {
                agent.updateRotation = false;
                agent.velocity = Vector3.zero;
            }

            anim.SetFloat("MoveSpeed", 0f);

            return;
        }

        if (agent.enabled) agent.updateRotation = true;
        // 공격 중이 아닐 때만 부모의 기본 로직을 실행하여 플레이어를 추적
        base.Update();
    }

    protected override void Attack() { }

    //public override void TakeDamage(float damage)
    //{
    //    if (isDead || isFlying) return;

    //    currentHealth -= damage;

    //    if (currentHealth <= 0)
    //    {
    //        Die();
    //    }
    //    else
    //    {
    //        anim.SetTrigger("Hurt");
    //        if (vfxPoint != null)
    //            VFXManager.Instance.PlayMonsterHit(vfxPoint.position, Vector3.up, gameObject);
    //    }
    //}

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        StopAllCoroutines();
        StartCoroutine(DieRoutine());
    }

    IEnumerator DieRoutine()
    {
        agent.isStopped = true;
        agent.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        anim.ResetTrigger("Attack");
        anim.ResetTrigger("BreatheFire");
        anim.ResetTrigger("FIy Breath Fire");
        anim.SetBool("flyBreatheFire", false);

        anim.SetTrigger("Death");
        anim.SetFloat("MoveSpeed", 0);

        yield return new WaitForSeconds(3f);

        if (vfxPoint != null)
            VFXManager.Instance.PlayMonsterDeath(vfxPoint.position, gameObject);
        else
            Destroy(gameObject);
    }

    private void SetGhostMode(bool isGhost)
    {
        Collider[] allCols = GetComponentsInChildren<Collider>();
        foreach (Collider c in allCols)
        {
            c.isTrigger = isGhost;
        }

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = !isGhost;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = isGhost;
    }

    IEnumerator BossThinkRoutine()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(0.2f);

            if (isDead || isAttacking || isFlying || isCooldown || target == null || !agent.isOnNavMesh) continue;

            if (!isPhase2 && currentHealth <= enemyData.maxHealth * 0.5f)
                isPhase2 = true;

            float distance = Vector3.Distance(transform.position, target.position);
            if (distance <= attackRange)
            {
                StartCoroutine(ExecuteRandomAttack());
            }
            else
            {
                if (isFlying) continue;
                agent.isStopped = false;
                agent.SetDestination(target.position);
            }
        }
    }

    IEnumerator ExecuteRandomAttack()
    {
        if (isDead || isAttacking) yield break;

        isAttacking = true;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        // 공격 시작 시 플레이어 방향을 한 번만 딱 바라봄 (이후에는 안 따라감)
        Vector3 lookPos = new Vector3(target.position.x, transform.position.y, target.position.z);
        transform.LookAt(lookPos);

        agent.nextPosition = transform.position;

        if (!isFlying && !isDead)
        {
            if (biteCount < 3)
            {
                // 물어뜯기 공격 (0~2회차)
                anim.SetTrigger("Attack");
                VFXManager.Instance.PlayBossAttack(transform.position, transform.forward);
                yield return StartCoroutine(BossSingleHit(0.5f));
                biteCount++;
            }
            else
            {
                // 3번 물어뜯은 후 (3회차)
                biteCount = 0;

                if (isPhase2)
                {
                    if (phase2AttackIndex == 0)
                    {
                        yield return StartCoroutine(AerialBreathPattern());
                    }
                    else if (phase2AttackIndex == 1)
                    {
                        yield return StartCoroutine(DashAndSlamPattern());
                    }
                    else
                    {
                        // 브레스 발사
                        anim.SetTrigger("BreatheFire");
                        yield return StartCoroutine(BossBreathHit(0.5f, breathDuration));
                    }

                    phase2AttackIndex++;
                    if (phase2AttackIndex > 2) phase2AttackIndex = 0;
                }
                else
                {
                    // 1페이즈일 때는 무조건 지상 브레스
                    anim.SetTrigger("BreatheFire");
                    yield return StartCoroutine(BossBreathHit(0.5f, breathDuration));
                }
            }
        }

        if (isDead) yield break;
        yield return new WaitForSeconds(1.0f);

        isAttacking = false;
        agent.isStopped = false;

        isCooldown = true;
        yield return new WaitForSeconds(attackCooldown);
        isCooldown = false;
    }

    // 애니메이션 이벤트 함수: 입 벌리는 프레임에 맞춰 호출
    public void OnBreathEffectStart()
    {
        if (isDead || headBone == null) return;

        // [수정 완료] 몸통 방향(transform.forward) 대신 지정된 뼈대 방향(headBone.forward)으로 발사
        VFXManager.Instance.PlayBossBreath(headBone, headBone.forward);
    }

    IEnumerator DashAndSlamPattern()
    {
        if (isDead) yield break;

        isFlying = true;
        isAttacking = true;

        SetGhostMode(true);
        bool originalRootMotion = anim.applyRootMotion;
        anim.applyRootMotion = false;

        // 원본 바닥 Y축 높이 저장을 위해 스킬 시작 위치 백업
        Vector3 skillStartPos = transform.position;
        if (agent.enabled) agent.enabled = false;

        yield return StartCoroutine(MoveTowardPlayerWithDamage(0.8f));
        if (isDead) yield break;

        yield return new WaitForSeconds(0.4f);
        if (isDead) yield break;

        yield return StartCoroutine(MoveTowardPlayerWithDamage(0.8f));
        if (isDead) yield break;

        // 비행 시작: flyIdle 전환 후 상승
        anim.SetTrigger("flyIdle");
        yield return new WaitForSeconds(0.2f);

        float t = 0;
        Vector3 flyStartPos = transform.position;
        Vector3 flyTargetPos = flyStartPos;
        flyTargetPos.y = skillStartPos.y + flyHeight + 3f;

        while (t < 0.5f)
        {
            if (isDead) yield break;
            transform.position = Vector3.Lerp(flyStartPos, flyTargetPos, t / 0.5f);
            t += Time.deltaTime;
            yield return null;
        }
        transform.position = flyTargetPos;

        yield return new WaitForSeconds(0.1f);

        t = 0;
        Vector3 diveStartPos = transform.position;

        // 착지 시 플레이어 쪽으로 자연스럽게 유도
        Vector3 landDir = transform.position - target.position;
        landDir.y = 0;
        if (landDir == Vector3.zero) landDir = transform.forward;

        // 플레이어 바로 앞(2.5 거리)에 착지 지점 계산
        Vector3 diveTargetPos = target.position + landDir.normalized * 2.5f;
        diveTargetPos.y = skillStartPos.y + landingYOffset;

        // 버벅거림 방지: 내비메시 위 안전 구역으로 보정
        UnityEngine.AI.NavMeshHit navHit;
        if (UnityEngine.AI.NavMesh.SamplePosition(diveTargetPos, out navHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            diveTargetPos.x = navHit.position.x;
            diveTargetPos.z = navHit.position.z;
        }

        while (t < 0.3f)
        {
            if (isDead) yield break;
            transform.position = Vector3.Lerp(diveStartPos, diveTargetPos, t / 0.3f);
            t += Time.deltaTime;
            yield return null;
        }
        transform.position = diveTargetPos;

        VFXManager.Instance.PlayBossAttack(transform.position, transform.forward);
        yield return StartCoroutine(BossSingleHit(0.1f));

        yield return new WaitForSeconds(0.5f);

        if (!agent.enabled)
        {
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out navHit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(navHit.position);
            }
            agent.baseOffset = 0f;
            agent.enabled = true;
        }

        SetGhostMode(false);
        anim.applyRootMotion = originalRootMotion;

        if (!isDead)
        {
            anim.CrossFade("Ground.Ground Locomotion", 0.2f);
            anim.SetFloat("MoveSpeed", 0f);
        }

        isFlying = false;
    }

    IEnumerator AerialBreathPattern()
    {
        if (isDead || target == null) yield break;

        isFlying = true;
        isAttacking = true;

        SetGhostMode(true);
        bool originalRootMotion = anim.applyRootMotion;
        anim.applyRootMotion = false;

        // 원본 위치 백업
        Vector3 skillStartPos = transform.position;
        if (agent.enabled) agent.enabled = false;

        anim.SetTrigger("flyIdle");
        yield return new WaitForSeconds(0.2f);

        // 1. 공중으로 상승할 때 플레이어와 적당한 거리(6.0)를 두면서 올라감
        Vector3 dirToPlayer = transform.position - target.position;
        dirToPlayer.y = 0;
        if (dirToPlayer == Vector3.zero) dirToPlayer = transform.forward;

        Vector3 startPos = transform.position;
        Vector3 targetHoverPos = target.position + dirToPlayer.normalized * 6f + Vector3.up * (flyHeight + aerialHoverHeightAdd);

        float t = 0;
        while (t < 1.5f)
        {
            if (isDead) yield break;
            float smoothT = Mathf.SmoothStep(0, 1, t / 1.5f);
            transform.position = Vector3.Lerp(startPos, targetHoverPos, smoothT);

            // 상승 도중 타겟 바라보기
            Vector3 lookDir = target.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * 5f);

            t += Time.deltaTime;
            yield return null;
        }

        // 공중 브레스 발사 모션 트리거
        anim.SetTrigger("FIy Breath Fire");
        anim.SetBool("flyBreatheFire", true);

        if (flyingBreathVFX != null) flyingBreathVFX.SetActive(true);

        float breathTime = 3.0f;
        float timer = 0f;
        float damageTick = 0f;

        while (timer < breathTime)
        {
            if (isDead) yield break;

            Vector3 curDirToPlayer = transform.position - target.position;
            curDirToPlayer.y = 0;
            if (curDirToPlayer == Vector3.zero) curDirToPlayer = transform.forward;

            // 목적지 높이 계산
            Vector3 desiredHoverPos = target.position + curDirToPlayer.normalized * 6f + Vector3.up * (flyHeight + aerialHoverHeightAdd);

            // 💡 [수정 핵심] 최대 이동 거리를 플레이어 인식거리의 딱 절반인 15f로 봉인!
            Vector3 horizontalOffset = desiredHoverPos - skillStartPos;
            horizontalOffset.y = 0;
            horizontalOffset = Vector3.ClampMagnitude(horizontalOffset, 15f); // 🔒 30에서 15로 반토막 조절

            Vector3 constrainedHoverPos = skillStartPos + horizontalOffset;
            constrainedHoverPos.y = desiredHoverPos.y;

            // 조절된 컴팩트한 안전 영역 안에서만 Lerp 추적
            transform.position = Vector3.Lerp(transform.position, constrainedHoverPos, Time.deltaTime * 1.5f);

            Vector3 lookDir = target.position - transform.position;
            lookDir.y = 0;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);

            damageTick += Time.deltaTime;
            if (damageTick >= 0.5f)
            {
                damageTick = 0f;
                Collider[] hits = Physics.OverlapSphere(transform.position + (Vector3.down * (flyHeight + aerialHoverHeightAdd)), 4f);
                foreach (var hitCol in hits)
                {
                    if (hitCol.CompareTag("Player"))
                    {
                        hitCol.GetComponent<Player>()?.TakeDamage(enemyData.damage * 0.5f);
                    }
                }
            }

            timer += Time.deltaTime;
            yield return null;
        }

        if (flyingBreathVFX != null) flyingBreathVFX.SetActive(false);
        anim.SetBool("flyBreatheFire", false);

        // ==========================================
        // 3. 복귀 비행: 거리가 훨씬 가까워졌으므로 더 안정적으로 비행 모션이 연출됨
        // ==========================================
        Vector3 returnStartPos = transform.position;
        Vector3 returnHoverPos = skillStartPos;
        returnHoverPos.y = returnStartPos.y;

        float returnDist = Vector3.Distance(returnStartPos, returnHoverPos);
        float returnDuration = returnDist / 12f;
        if (returnDuration < 1.0f) returnDuration = 1.0f;

        t = 0;
        while (t < returnDuration)
        {
            if (isDead) yield break;

            float smoothT = Mathf.SmoothStep(0, 1, t / returnDuration);
            transform.position = Vector3.Lerp(returnStartPos, returnHoverPos, smoothT);

            Vector3 moveDir = returnHoverPos - transform.position;
            moveDir.y = 0;
            if (moveDir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDir), Time.deltaTime * 5f);

            t += Time.deltaTime;
            yield return null;
        }
        transform.position = returnHoverPos;

        // ==========================================
        // 4. 수직 착지
        // ==========================================
        Vector3 fallStartPos = transform.position;
        Vector3 groundPos = skillStartPos;
        groundPos.y = skillStartPos.y + landingYOffset;

        UnityEngine.AI.NavMeshHit navHit;
        if (UnityEngine.AI.NavMesh.SamplePosition(groundPos, out navHit, 5f, UnityEngine.AI.NavMesh.AllAreas))
        {
            groundPos.x = navHit.position.x;
            groundPos.z = navHit.position.z;
        }

        t = 0;
        while (t < 0.8f)
        {
            if (isDead) yield break;
            float smoothT = Mathf.SmoothStep(0, 1, t / 0.8f);
            transform.position = Vector3.Lerp(fallStartPos, groundPos, smoothT);
            t += Time.deltaTime;
            yield return null;
        }
        transform.position = groundPos;

        yield return new WaitForSeconds(0.5f);

        if (!agent.enabled)
        {
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out navHit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(navHit.position);
            }
            agent.baseOffset = 0f;
            agent.enabled = true;
        }

        SetGhostMode(false);
        anim.applyRootMotion = originalRootMotion;

        anim.CrossFade("Ground.Ground Locomotion", 0.2f);
        anim.SetFloat("MoveSpeed", 0f);

        isFlying = false;
    }

    IEnumerator BossSingleHit(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isDead) yield break;

        Vector3 hitPos = transform.position + (transform.forward * biteHitOffset) + (Vector3.up * biteHitHeight);
        Collider[] hits = Physics.OverlapSphere(hitPos, biteHitRadius);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Player"))
            {
                hit.GetComponent<Player>()?.TakeDamage(enemyData.damage);
                break;
            }
        }
    }

    IEnumerator BossBreathHit(float delay, float duration)
    {
        yield return new WaitForSeconds(delay);
        float timer = 0f;
        while (timer < duration)
        {
            if (isDead) yield break;
            if (headBone != null)
            {
                Matrix4x4 matrix = Matrix4x4.TRS(headBone.position, transform.rotation, Vector3.one);

                float halfLength = breathHitOffset / 2f;
                float halfThickness = breathHitRadius / 2f;

                Vector3 boxCenterLocal = new Vector3(0f, breathHitHeight, halfLength);
                Vector3 boxCenterWorld = matrix.MultiplyPoint3x4(boxCenterLocal);

                Vector3 halfExtents = new Vector3(halfThickness, halfThickness, halfLength);
                Quaternion boxRotation = transform.rotation;

                Collider[] hits = Physics.OverlapBox(boxCenterWorld, halfExtents, boxRotation);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Player"))
                    {
                        hit.GetComponent<Player>()?.TakeDamage(enemyData.damage * 0.5f);
                    }
                }
            }
            yield return new WaitForSeconds(0.5f);
            timer += 0.5f;
        }
    }

    IEnumerator MoveTowardPlayerWithDamage(float duration)
    {
        float t = 0;
        bool hasHitThisDash = false;
        Vector3 dashDir = (target.position - transform.position).normalized;
        dashDir.y = 0;
        if (dashDir != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(dashDir);

        while (t < duration)
        {
            if (isDead) yield break;
            transform.position += transform.forward * dashSpeed * Time.deltaTime;

            if (!hasHitThisDash)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position + Vector3.up, 3f);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Player"))
                    {
                        hit.GetComponent<Player>()?.TakeDamage(enemyData.damage);
                        hasHitThisDash = true;
                        break;
                    }
                }
            }
            t += Time.deltaTime;
            yield return null;
        }

        if (agent.enabled)
        {
            UnityEngine.AI.NavMeshHit agentHit;
            if (UnityEngine.AI.NavMesh.SamplePosition(transform.position, out agentHit, 10f, UnityEngine.AI.NavMesh.AllAreas))
            {
                agent.Warp(agentHit.position);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.4f);
        Gizmos.DrawWireSphere(transform.position + (transform.forward * biteHitOffset) + (Vector3.up * biteHitHeight), biteHitRadius);

        if (headBone != null)
        {
            Gizmos.color = new Color(0, 0, 1, 0.4f);
            Matrix4x4 oldGizmoMatrix = Gizmos.matrix;
            Matrix4x4 matrix = Matrix4x4.TRS(headBone.position, transform.rotation, Vector3.one);

            float halfLength = breathHitOffset / 2f;
            Vector3 localCenter = new Vector3(0f, breathHitHeight, halfLength);
            Gizmos.matrix = matrix * Matrix4x4.Translate(localCenter);

            Gizmos.DrawWireCube(Vector3.zero, new Vector3(breathHitRadius, breathHitRadius, breathHitOffset));

            Gizmos.matrix = oldGizmoMatrix;
        }
    }
}