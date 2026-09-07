using UnityEngine;
using System.Collections;

public class PlayerDash : MonoBehaviour
{
    [Header("VFX")]
    public Transform dashVFXPoint; // VFXPoint_Dash 드래그

    private Player player;
    private readonly int doDashHash = Animator.StringToHash("doDash");

    private int currentDashCount;
    private Coroutine cooldownCoroutine;
    private Coroutine awakeningCoroutine;

    private void Awake()
    {
        player = GetComponent<Player>();
    }

    private void Start()
    {

        currentDashCount = player.playerData.maxDashCount;
    }

    public void ExecuteDash()
    {
        if (currentDashCount <= 0 || player.CurrentState == PlayerState.Dash) return;

        StartCoroutine(DashRoutine());
    }

    private IEnumerator DashRoutine()
    {
        currentDashCount--;

        if (cooldownCoroutine != null) StopCoroutine(cooldownCoroutine);
        cooldownCoroutine = StartCoroutine(DashCooldownRoutine());

        if (player.CurrentState == PlayerState.Attack || player.CurrentState == PlayerState.SpecialAttack)
        {
            player.attack.CancelAttack();
        }

        player.GrantInvincibility(player.playerData.dashInvincibilityTime);
        player.ChangeState(PlayerState.Dash);
        player.animator.SetTrigger(doDashHash);
        player.animator.SetBool("isMoving", false);

        Vector2 input = player.GetInputVector();
        Vector3 dashDirection;

        if (input.sqrMagnitude > 0)
        {
            Transform camTransform = Camera.main.transform;
            Vector3 forward = camTransform.forward;
            Vector3 right = camTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            dashDirection = (forward * input.y + right * input.x).normalized;
            transform.rotation = Quaternion.LookRotation(dashDirection);
        }
        else
        {
            dashDirection = transform.forward;
        }

        // 대시 VFX 재생
        VFXManager.Instance.PlayDash(dashVFXPoint.position, dashDirection, player.playerData.dashDuration);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Player_Dash");
        }

        float startTime = Time.time;
        while (Time.time < startTime + player.playerData.dashDuration)
        {
            Vector3 finalDashDirection = dashDirection;

            // 1. 플레이어 살짝 위에서 바닥으로 레이저를 쏴서 땅의 각도(Normal)를 알아냅니다.
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 1f))
            {
                // 2. 바닥 경사면에 맞춰서 대시 방향을 비스듬하게 꺾어줍니다.
                finalDashDirection = Vector3.ProjectOnPlane(dashDirection, hit.normal).normalized;
            }

            // 3. 꺾인 방향으로 이동 속도를 적용합니다.
            Vector3 targetVelocity = finalDashDirection * player.playerData.dashSpeed;

            // 4. (물리 폭발 원인 제거) 평지일 때는 기존에 받던 중력(y값)을 그대로 존중해 줍니다.
            if (Mathf.Abs(hit.normal.y - 1f) < 0.01f)
            {
                targetVelocity.y = player.rb.linearVelocity.y;
            }

            player.rb.linearVelocity = targetVelocity;
            yield return null;
        }

        // 대시 종료 후 안전하게 상태 복구
        player.rb.linearVelocity = Vector3.zero;
        player.animator.CrossFade("idle", 0.1f);
        player.ChangeState(PlayerState.Idle);

        if (player.playerData.acquiredGifts.Contains(GiftType.Awakening))
        {
            if (awakeningCoroutine != null) StopCoroutine(awakeningCoroutine);
            awakeningCoroutine = StartCoroutine(AwakeningTimerRoutine());
        }
    }

    private IEnumerator DashCooldownRoutine()
    {
        yield return new WaitForSeconds(player.playerData.dashCooldown);
        currentDashCount = player.playerData.maxDashCount;
    }


    private IEnumerator AwakeningTimerRoutine()
    {
        player.attack.isAwakened = true; // 공격 스크립트에 버프 ON
        yield return new WaitForSeconds(1f);
        player.attack.isAwakened = false; // 1초 뒤 버프 OFF
    }
}