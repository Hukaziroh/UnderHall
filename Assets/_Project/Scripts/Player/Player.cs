using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
public enum PlayerState { Idle, Move, Attack, Dash, SpecialAttack, Dead, Resurrecting }

[RequireComponent(typeof(Rigidbody), typeof(PlayerMovement), typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerDash))]
public class Player : MonoBehaviour
{
    [Header("VFX")]
    public Transform vfxPoint;

    [Header("Camera Zoom Settings")]
    public CinemachineCamera virtualCamera;
    public float zoomInFOV = 30f;
    public float zoomDuration = 0.5f;
    private float originalFOV;
    public float CurrentHealth => currentHealth;

    public PlayerData playerData;
    public PlayerState CurrentState { get; private set; }

    public Rigidbody rb { get; private set; }
    public PlayerMovement movement { get; private set; }
    public PlayerAttack attack { get; private set; }
    public Animator animator { get; private set; }
    public PlayerDash dash { get; private set; }
    public bool IsInvincible { get; private set; } = false;

    private Coroutine invincibilityCoroutine;
    private int remainingResurrections;
    private float currentHealth;
    private Vector2 inputVector;
    private int earnedGoldDuringRun = 0;
    private Door nearbyDoor;
    private RewardInteractable nearbyReward; // 보상 상호작용 추가

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        movement = GetComponent<PlayerMovement>();
        attack = GetComponent<PlayerAttack>();
        dash = GetComponent<PlayerDash>();
        animator = GetComponent<Animator>();

        CurrentState = PlayerState.Idle;
    }

    private void Start()
    {
        if (virtualCamera == null) virtualCamera = FindAnyObjectByType<CinemachineCamera>(FindObjectsInactive.Exclude);

        if (playerData != null)
        {
            currentHealth = playerData.maxHealth;
            remainingResurrections = playerData.maxResurrectionCount;

            if (UIManager.Instance != null) UIManager.Instance.UpdateHealthUI(currentHealth, playerData.maxHealth);
        }

        if (virtualCamera != null) originalFOV = virtualCamera.Lens.FieldOfView;
    }

    public void ChangeState(PlayerState newState)
    {
        CurrentState = newState;
    }

    public void GrantInvincibility(float duration)
    {
        if (invincibilityCoroutine != null) StopCoroutine(invincibilityCoroutine);
        invincibilityCoroutine = StartCoroutine(InvincibilityRoutine(duration));
    }

    private IEnumerator InvincibilityRoutine(float duration)
    {
        IsInvincible = true;
        yield return new WaitForSeconds(duration);
        IsInvincible = false;
    }

    public void TakeDamage(float damage)
    {
        if (IsInvincible || CurrentState == PlayerState.Dead || CurrentState == PlayerState.Resurrecting) return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log($"플레이어 피격! 남은 체력: {currentHealth}");
        if (UIManager.Instance != null) UIManager.Instance.UpdateHealthUI(currentHealth, playerData.maxHealth);

        //창우_UI매니저 호출해서 HP 깎는 애니메이션과 HP 흔드는 애니메이션을 동시에 실행합니다.
        if (UIManager.Instance != null)
        {
            //창우_먼저 UI의 실제 HP 수치와 게이지를 깎고
            UIManager.Instance.UpdateHealthUI(currentHealth, playerData.maxHealth);

            //창우_그와 동시에 HP  흔들어 줍니다.
            UIManager.Instance.PlayHealthBarShake();
        }
        // 창우_카메라 매니저를 호출하여 화면 흔들기 + 붉은색 플래시 융합 터뜨리기
        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.ShakeOnHit();
        }

        VFXManager.Instance.PlayPlayerHit(vfxPoint.position, Vector3.up, gameObject);

        if (currentHealth > 0 && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Player_Hit");
        }

        if (currentHealth <= 0)
        {
            if (remainingResurrections > 0)
            {
                StartCoroutine(ResurrectRoutine());
            }
            else
            {
                Die();
            }
        }
        else
        {
            GrantInvincibility(playerData.hitInvincibilityTime);
        }
    }

    public void Heal(float amount)
    {
        if (CurrentState == PlayerState.Dead) return;
        currentHealth += amount;
        if (currentHealth > playerData.maxHealth) currentHealth = playerData.maxHealth;
        Debug.Log($"[회복] 현재 체력: {currentHealth}");

        if (UIManager.Instance != null) UIManager.Instance.UpdateHealthUI(currentHealth, playerData.maxHealth);
    }

    private IEnumerator ResurrectRoutine()
    {
        ChangeState(PlayerState.Resurrecting);
        attack.CancelAttack();
        inputVector = Vector2.zero;
        rb.linearVelocity = Vector3.zero;

        rb.isKinematic = true;

        animator.Play("Hit");

        //창우_부활 시작 시점에 VFX 매니저를 호출하여 부활 시작 이펙트를 재생합니다.
        VFXManager.Instance.PlayPlayerResurrectStart(vfxPoint.position);
        //창우_슬램 슬로우모션 트윈 차단 후 timeScale 설정
        if (CameraManager.Instance != null)
            CameraManager.Instance.CancelSlamSlowMotion();


        animator.updateMode = AnimatorUpdateMode.UnscaledTime;
        Time.timeScale = 0.1f;

        //창우_CameraManager로 줌인 (코루틴 제거)
        if (CameraManager.Instance != null)
            CameraManager.Instance.ZoomTo(zoomInFOV, zoomDuration);


        VFXManager.Instance.PlayPlayerResurrectStart(vfxPoint.position);
        StartCoroutine(CameraZoomRoutine(zoomInFOV, zoomDuration));

        float waitTime = 4f;
        float currentTimer = 0f;
        while (currentTimer < waitTime)
        {
            if (!PauseMenuManager.isPaused)
            {
                currentTimer += Time.unscaledDeltaTime;
            }
            yield return null;
        }

        remainingResurrections--;
        currentHealth = playerData.maxHealth * playerData.resurrectionHealthPercent;

        if (UIManager.Instance != null) UIManager.Instance.UpdateHealthUI(currentHealth, playerData.maxHealth);

        if (PauseMenuManager.isPaused)
        {
            PauseMenuManager.previousTimeScale = 1f;
        }
        else
        {
            Time.timeScale = 1f;
        }

        animator.updateMode = AnimatorUpdateMode.Normal;

        //창우_줌아웃도 CameraManager로
        if (CameraManager.Instance != null)
            CameraManager.Instance.ZoomTo(originalFOV, 0.2f);

        StartCoroutine(CameraZoomRoutine(originalFOV, 0.2f));

        VFXManager.Instance.PlayPlayerResurrectEnd(vfxPoint.position);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Player_Resurrect_End");
        }

        animator.SetBool("isMoving", false);
        animator.CrossFade("idle", 0.1f);

        rb.isKinematic = false;
        ChangeState(PlayerState.Idle);
        GrantInvincibility(playerData.resurrectionInvincibilityTime);
    }

    private IEnumerator CameraZoomRoutine(float targetFOV, float duration)
    {
        if (virtualCamera == null) yield break;

        float startFOV = virtualCamera.Lens.FieldOfView;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            var lens = virtualCamera.Lens;
            lens.FieldOfView = Mathf.Lerp(startFOV, targetFOV, elapsed / duration);
            virtualCamera.Lens = lens;
            yield return null;
        }

        var finalLens = virtualCamera.Lens;
        finalLens.FieldOfView = targetFOV;
        virtualCamera.Lens = finalLens;
    }

    private void Die()
    {
        if (CurrentState == PlayerState.Dead) return;

        CommitGoldToSO();

        VFXManager.Instance.PlayPlayerDeath(vfxPoint.position, gameObject);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Player_Death");
        }

        ChangeState(PlayerState.Dead);
        attack.CancelAttack();
        inputVector = Vector2.zero;
        rb.linearVelocity = Vector3.zero;

        if (TryGetComponent<PlayerInput>(out var input))
        {
            input.enabled = false;
        }

        animator.Play("death");

        StartCoroutine(DeathToMainMenuRoutine());
    }
    private IEnumerator DeathToMainMenuRoutine()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("Death_BGM");
        }

        yield return new WaitForSeconds(2f);

        if (GameManager.Instance != null)
        {
            yield return StartCoroutine(GameManager.Instance.Fade(1f));
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void OnDash(InputValue value)
    {
        if (CurrentState == PlayerState.Dead || CurrentState == PlayerState.Resurrecting) return;
        if (value.isPressed) dash.ExecuteDash();
    }

    public void OnMove(InputValue value)
    {
        if (CurrentState == PlayerState.Dead || CurrentState == PlayerState.Resurrecting) return;
        inputVector = value.Get<Vector2>();
    }

    public void OnAttack(InputValue value)
    {
        if (Time.timeScale == 0f) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (CurrentState == PlayerState.Dead || CurrentState == PlayerState.Resurrecting) return;
        if (value.isPressed && CurrentState != PlayerState.Dash) attack.ExecuteAttack();
    }

    public void OnSpecialAttack(InputValue value)
    {
        Debug.Log("우클릭 신호 들어옴! 누름 상태: " + value.isPressed);
        if (!value.isPressed)
        {
            attack.StopSpecialAttack();
            return;
        }
        if (Time.timeScale == 0f) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
        if (CurrentState == PlayerState.Dead || CurrentState == PlayerState.Resurrecting) return;

        if (CurrentState != PlayerState.Dash && CurrentState != PlayerState.SpecialAttack)
        {
            attack.StartSpecialAttack();
        }
    }

    public void OnInteract(InputValue value)
    {
        if (CurrentState == PlayerState.Dead || CurrentState == PlayerState.Resurrecting) return;
        if (!value.isPressed) return;

        if (CurrentState == PlayerState.Attack || CurrentState == PlayerState.SpecialAttack)
        {
            attack.CancelAttack();
            ChangeState(PlayerState.Idle);
        }

        if (nearbyReward != null)
        {
            nearbyReward.Interact(this);
            return;
        }

        if (nearbyDoor != null && !nearbyDoor.IsLocked)
        {
            nearbyDoor.Interact();
        }
    }

    public void AddGold(int amount)
    {
        earnedGoldDuringRun += amount;
        Debug.Log($"[임시 획득] 골드 +{amount} (이번 판 총합: {earnedGoldDuringRun})");

    }
    public void CommitGoldToSO()
    {
        if (earnedGoldDuringRun > 0)
        {
            playerData.currentGold += earnedGoldDuringRun;
            Debug.Log($"[정산 완료] {earnedGoldDuringRun} 골드가 영구 저장되었습니다. 총액: {playerData.currentGold}");
            earnedGoldDuringRun = 0;
            playerData.SaveToDevice();
        }
    }
    public void ForceStop()
    {
        inputVector = Vector2.zero;
        rb.linearVelocity = Vector3.zero; 
        animator.SetBool("isMoving", false); 
        ChangeState(PlayerState.Idle);
    }
    public void SetNearbyDoor(Door door) => nearbyDoor = door;
    public void ClearNearbyDoor(Door door) { if (nearbyDoor == door) nearbyDoor = null; }


    public void SetNearbyReward(RewardInteractable reward) => nearbyReward = reward;
    public void ClearNearbyReward(RewardInteractable reward) { if (nearbyReward == reward) nearbyReward = null; }

    public Vector2 GetInputVector() => inputVector;
}