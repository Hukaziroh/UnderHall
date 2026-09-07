using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private Player player;
    private Vector3 moveDirection;
    public float rotationSpeed = 15f;
    private Camera mainCam;

    private readonly int isMovingHash = Animator.StringToHash("isMoving");

    private void Awake()
    {
        player = GetComponent<Player>();
        mainCam = Camera.main;
    }

    private void Update()
    {
        Vector2 input = player.GetInputVector();
        Transform camTransform = mainCam.transform;
        Vector3 forward = camTransform.forward;
        Vector3 right = camTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        moveDirection = (forward * input.y + right * input.x).normalized;

        if (player.CurrentState == PlayerState.Idle || player.CurrentState == PlayerState.Move)
        {
            if (moveDirection.magnitude > 0.1f)
            {
                player.ChangeState(PlayerState.Move);
            }
            else
            {
                player.ChangeState(PlayerState.Idle);
            }
        }
    }

    private void FixedUpdate()
    {
        switch (player.CurrentState)
        {
            case PlayerState.Idle:
                StopMovement();
                break;
            case PlayerState.Move:
                ApplyMovement();
                break;
            case PlayerState.Attack:
                StopMovement();
                break;
            case PlayerState.Dead:
                StopMovement();
                break;
            case PlayerState.Dash:            
                break;
            case PlayerState.SpecialAttack:
                ApplyMovement();
                break;
        }
    }

    private void ApplyMovement()
    {
        Vector3 targetVelocity = moveDirection * player.playerData.moveSpeed;

        if (Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, out RaycastHit hit, 1.2f))
        {
            targetVelocity = Vector3.ProjectOnPlane(targetVelocity, hit.normal).normalized * player.playerData.moveSpeed;

            if (player.rb.linearVelocity.y > 0.1f)
            {
                // 이때는 짓누르지 말고 물리 엔진이 밀어올리는 힘을 그대로 존중합니다.
                targetVelocity.y = Mathf.Clamp(player.rb.linearVelocity.y, 0f, 3f);
            }
            else if (targetVelocity.y <= 0f)
            {
                // 평지나 내리막길일 때만 확실하게 바닥으로 당겨줍니다.
                targetVelocity.y -= 8f;
            }
        }
        else
        {
            // 공중에 완전히 떠 있을 때는 기존 중력(낙하 속도) 유지
            targetVelocity.y = player.rb.linearVelocity.y;
        }

        player.rb.linearVelocity = targetVelocity;

        if (moveDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
        }

        player.animator.SetBool(isMovingHash, true);
    }

    private void StopMovement()
    {
        player.rb.linearVelocity = new Vector3(0f, player.rb.linearVelocity.y, 0f);

        if (player.CurrentState == PlayerState.Idle)
        {
            player.animator.SetBool(isMovingHash, false);
        }
    }

    public void PlayFootstepSound()
    {
        int randomIndex = Random.Range(1, 11);

        string soundName = "Footstep_" + randomIndex;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX(soundName);
        }
    }
}