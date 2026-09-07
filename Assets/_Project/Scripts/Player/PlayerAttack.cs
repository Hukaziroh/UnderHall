using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Reflection; // EnemyBase 강제 접근(처형)을 위해 필요함

public class PlayerAttack : MonoBehaviour
{
    private Player player;

    [Header("Combo Settings")]
    private int currentCombo = 0;
    private bool isNextAttackBuffered = false;
    private readonly int maxCombo = 3;
    private Coroutine attackCoroutine;
    private bool isAttackOnCooldown = false;

    [Header("Special (Spin) Settings")]
    private Coroutine specialAttackCoroutine;
    private bool isSpecialAttackOnCooldown = false;
    private bool isSpinning = false;

    [Header("VFX")]
    public Transform weaponVFXPoint; // 창우_VFXPoint_Weapon 드래그
    public Transform skillVFXPoint;  // 창우_VFXPoint_Body 드래그

    private Collider[] hitColliders = new Collider[100];

    // 각성(대시 후 다음 공격 2배) 버프 상태
    public bool isAwakened = false;

    private void Awake() => player = GetComponent<Player>();

    // 마우스 좌클릭: 기본 공격
    public void ExecuteAttack()
    {
        if (isAttackOnCooldown || isSpinning || player.CurrentState == PlayerState.SpecialAttack || player.CurrentState == PlayerState.Dash || player.CurrentState == PlayerState.Dead) return;

        if (player.CurrentState != PlayerState.Attack)
            attackCoroutine = StartCoroutine(ComboAttackRoutine());
        else if (currentCombo < maxCombo)
            isNextAttackBuffered = true;
    }
    // 마우스 우클릭 누름: 스킬 시작
    public void StartSpecialAttack()
    {
        if (isSpecialAttackOnCooldown || isSpinning || player.CurrentState == PlayerState.Dash || player.CurrentState == PlayerState.Dead) return;

        if (player.CurrentState != PlayerState.Attack && player.CurrentState != PlayerState.SpecialAttack)
        {
            if (specialAttackCoroutine != null) StopCoroutine(specialAttackCoroutine);

            if (player.playerData.acquiredGifts.Contains(GiftType.Explosion))
                VFXManager.Instance.PlayWeaponSkillExplosion(skillVFXPoint);
            else
                VFXManager.Instance.PlayWeaponSkillLoop(skillVFXPoint);

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySFXLoop("Player_Spin");

            specialAttackCoroutine = StartCoroutine(SpinRoutine());
        }
    }

    // 마우스 우클릭 뗌: 스킬 중지
    public void StopSpecialAttack()
    {
        if (!isSpinning) return;

        if (specialAttackCoroutine != null)
        {
            StopCoroutine(specialAttackCoroutine);
            specialAttackCoroutine = null;
        }

        isSpinning = false;

        VFXManager.Instance.StopWeaponSkillLoop();

        // 만약 VFXManager 안에 폭발 이펙트 전용 정지 함수가 있다면 아래 주석(//)을 반드시 풀어주세요!!
        // VFXManager.Instance.StopWeaponSkillExplosion(); 

        if (AudioManager.Instance != null)
            AudioManager.Instance.StopSFXLoop("Player_Spin");

        if (player.CurrentState == PlayerState.SpecialAttack)
        {
            player.animator.CrossFade("idle", 0.15f);
            player.ChangeState(PlayerState.Idle);
        }

        StartCoroutine(SpecialCooldownRoutine());
    }

    // 대시 등으로 인한 강제 취소
    public void CancelAttack()
    {
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        if (specialAttackCoroutine != null) StopCoroutine(specialAttackCoroutine);

        bool wasSpinning = isSpinning;

        isSpinning = false;
        //창우_마지막 잔상 즉시 제거
        VFXManager.Instance.StopWeaponSkillLoop();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopSFXLoop("Player_Spin");
        }

        currentCombo = 0;
        isNextAttackBuffered = false;
        isAttackOnCooldown = false;
        if (player.animator != null)
        {
            player.animator.ResetTrigger("Attack");
            player.animator.ResetTrigger("SpecialAttack");

            player.animator.SetBool("isAttacking", false);
            player.animator.SetBool("isSpecial", false);
            player.animator.speed = 1f;
        }

        if (player.CurrentState == PlayerState.Attack || player.CurrentState == PlayerState.SpecialAttack)
        {
            player.animator.CrossFade("idle", 0.1f);
            player.ChangeState(PlayerState.Idle);
        }

        if (wasSpinning)
        {
            StartCoroutine(SpecialCooldownRoutine());
        }
    }

    // 기본 공격 콤보 루틴
    private IEnumerator ComboAttackRoutine()
    {
        player.ChangeState(PlayerState.Attack);
        player.animator.SetBool("isMoving", false);
        currentCombo = 1;

        // [연격] 기본 공격 속도 +25%
        if (player.playerData.acquiredGifts.Contains(GiftType.Combo))
            player.animator.speed = 1.25f;

        while (currentCombo <= maxCombo)
        {
            isNextAttackBuffered = false;
            LookAtMouse();

            player.animator.CrossFade("attack" + currentCombo, 0.02f);
            yield return new WaitForSeconds(0.05f / player.animator.speed);

            // 기본 공격 이펙트
            VFXManager.Instance.PlayWeaponSwing(weaponVFXPoint.position, weaponVFXPoint.forward);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySFX("Attack_" + currentCombo);
            }

            ExecuteHitDetection(transform.position + transform.forward * (player.playerData.attackRange * 0.5f),
                                player.playerData.attackRange * 0.5f, 1f, false);

            AnimatorStateInfo stateInfo = player.animator.GetCurrentAnimatorStateInfo(0);
            yield return new WaitForSeconds((stateInfo.length * 0.5f) / player.animator.speed);

            if (isNextAttackBuffered) currentCombo++;
            else break;
        }

        player.animator.speed = 1f; // 공속 복구
        currentCombo = 0;
        player.animator.CrossFade("idle", 0.15f);
        player.ChangeState(PlayerState.Idle);
        StartCoroutine(AttackCooldownRoutine());
    }

    // 특수 공격 (가렌 E 스타일) 루틴
    private IEnumerator SpinRoutine()
    {
        isSpinning = true;
        player.ChangeState(PlayerState.SpecialAttack);
        player.animator.CrossFade("specialAttack", 0.1f);


        float tickRate = 0.25f;
        float tickTimer = tickRate;

        while (isSpinning)
        {
            if (player.CurrentState == PlayerState.Dead || player.CurrentState == PlayerState.Resurrecting) break;

            tickTimer += Time.deltaTime;

            if (tickTimer >= tickRate)
            {
                float currentRadius = player.playerData.specialAttackRange;
                if (player.playerData.acquiredGifts.Contains(GiftType.Explosion)) currentRadius *= 1.8f;

                ExecuteHitDetection(transform.position, currentRadius, player.playerData.specialAttackMultiplier, true);
                tickTimer = 0f;
            }
            yield return null;
        }

        StopSpecialAttack();
    }

    // 통합 데미지 판정 시스템
    private void ExecuteHitDetection(Vector3 center, float radius, float damageMultiplier, bool isSpecial)
    {
        Vector3 pointBottom = center;
        pointBottom.y += 0.2f; // 무릎/발목 높이

        Vector3 pointTop = center;
        pointTop.y += 1.8f; // 머리 꼭대기 높이
        int hitCount = Physics.OverlapSphereNonAlloc(center, radius, hitColliders);

        // [각성] 버프 사용 여부 확인
        bool useAwakening = false;
        if (isAwakened)
        {
            useAwakening = true;
            isAwakened = false; // 한 번 쓰면 바로 버프 소모
        }

        for (int i = 0; i < hitCount; i++)
        {
            Collider col = hitColliders[i];
            if (col.gameObject == player.gameObject) continue;

            var target = col.GetComponentInParent<EnemyBase>();

            // 몬스터가 존재하고 아직 콜라이더가 켜져있다면 (살아있다면)
            if (target != null && col.enabled)
            {
                float finalDamage = player.playerData.damage * damageMultiplier;
                bool isCritical = false; // 1_창우_여기 추가


                // [패시브 계열]
                // [광폭] 내 체력이 50% 이하면 데미지 +40%
                if (player.playerData.acquiredGifts.Contains(GiftType.Berserk) &&
                   (player.CurrentHealth <= player.playerData.maxHealth * 0.5f))
                    finalDamage *= 1.4f;

                // [각성] 대시 직후라면 데미지 2배
                if (useAwakening) finalDamage *= 2f;

                // [일반 공격 계열]
                if (!isSpecial)
                {
                    // [처형] 리플렉션으로 EnemyBase의 private 체력 읽어오기
                    if (player.playerData.acquiredGifts.Contains(GiftType.Execution))
                    {
                        // 방금 EnemyBase에 뚫어둔 통로로 체력 값을 즉시 가져옵니다. (속도 매우 빠름)
                        float enemyCurrentHP = target.CurrentHealth;
                        float enemyMaxHP = target.MaxHealth;

                        // 체력이 20% 이하면 데미지 2배!
                        if (enemyCurrentHP <= enemyMaxHP * 0.2f) finalDamage *= 2f;
                    }

                    // [치명타] 15% 확률로 2배
                    if (player.playerData.acquiredGifts.Contains(GiftType.Critical) && Random.value <= 0.15f)
                    {
                        finalDamage *= 2f;

                        isCritical = true; //2_창우_Show 호출 삭제하고 이걸로 교체

                        Debug.Log("크리티컬 터짐!");


                    }
                }
                // [특수 공격 계열]
                else
                {
                    // [지속력] 특수공격 데미지 30% 증가
                    if (player.playerData.acquiredGifts.Contains(GiftType.Endurance))
                        finalDamage *= 1.3f;

                    // [추가] [속사] 10% 확률로 특수공격 데미지 2배
                    if (player.playerData.acquiredGifts.Contains(GiftType.RapidFire) && Random.value <= 0.10f)
                    {
                        finalDamage *= 2f;
                        isCritical = true; // 데미지 텍스트가 크리티컬로 뜨게 만듭니다!
                        Debug.Log("[속사] 특수 공격 치명타 터짐!");
                    }
                }

                // 타격 직전의 콜라이더 상태 저장
                bool wasAlive = col.enabled;

                string attackType = isSpecial ? "특수공격" : "기본공격";
                Debug.Log($"[데미지 판정] {attackType} 명중! 최종 데미지: {finalDamage}");

                // 데미지 적용
                target.TakeDamage(finalDamage, isCritical); // 3_창우_isCritical 추가


                //창우_카메라 흔들림 추가
                CameraManager.Instance.ShakeOnAttackDirectional(transform.forward);

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySFX("Hit_Monster");
                }

                // [흡혈] 때린 직후에 콜라이더가 꺼졌다? = 적이 죽었다!
                if (wasAlive && !col.enabled && player.playerData.acquiredGifts.Contains(GiftType.Vampirism))
                {
                    player.Heal(5f);
                }

                // 데미지 들어갈 때 타격 이펙트
                Vector3 hitNormal = (col.transform.position - transform.position).normalized;
                VFXManager.Instance.PlayAttackHit(col.transform.position, hitNormal);
            }
        }
    }

    private void LookAtMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (new Plane(Vector3.up, transform.position).Raycast(ray, out float enter))
        {
            Vector3 lookDir = (ray.GetPoint(enter) - transform.position).normalized;
            lookDir.y = 0;
            if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);
        }
    }

    private IEnumerator AttackCooldownRoutine()
    {
        isAttackOnCooldown = true;
        yield return new WaitForSeconds(player.playerData.attackCooldown);
        isAttackOnCooldown = false;
    }

    private IEnumerator SpecialCooldownRoutine()
    {
        isSpecialAttackOnCooldown = true;
        float finalCooldown = player.playerData.specialAttackCooldown;

        yield return new WaitForSeconds(finalCooldown);
        isSpecialAttackOnCooldown = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (player == null || player.playerData == null) return;
        Gizmos.color = Color.red;
        Vector3 gizmoPos = transform.position + transform.forward * (player.playerData.attackRange * 0.5f);
        gizmoPos.y += 1f;
        Gizmos.DrawWireSphere(gizmoPos, player.playerData.attackRange * 0.5f);
    }
}