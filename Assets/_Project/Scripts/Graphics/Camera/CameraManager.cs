using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    [Header("Cinemachine 연결")]
    public CinemachineCamera virtualCamera;
    public CinemachineImpulseSource impulseSource;

    private float defaultFOV;

    [Header("피격 흔들림")]
    public float hitShakeX = 0.4f;
    public float hitShakeY = 0.08f;

    [Header("타격 흔들림")]
    public float attackShakeX = 0.15f;
    public float attackShakeY = 0.03f;

    [Header("─ 전체 화면 피격 플래시 (UI) ─")]
    [SerializeField] private Image screenFlashImage;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0f, 0f, 0.4f);
    [SerializeField] private float flashInDuration = 0.03f;
    [SerializeField] private float flashOutDuration = 0.25f;

    [Header("─ 글로벌 볼륨 왜곡 설정 ─")]
    [SerializeField] private float hitDistortionStrength = 0.8f;

    [Header("─ 보스 착지 임펄스 설정 ─")]
    [SerializeField] private float bossSlamShakeX = 0.5f;
    [SerializeField] private float bossSlamShakeY = 0.8f;

    [Header("─ 보스 착지  설정 ─")]
    [Tooltip("착지 순간 FOV가 좁아지는 양 (클수록 강렬)")]
    [SerializeField] private float slamFOVPunch = 8f;

    [Tooltip("FOV 좁아지는 시간 (짧을수록 임팩트)")]
    [SerializeField] private float slamFOVInDuration = 0.04f;

    [Tooltip("FOV 원래대로 돌아오는 시간")]
    [SerializeField] private float slamFOVOutDuration = 0.35f;

    [Tooltip("착지 순간 슬로우모션 배율 (0.1 = 10% 속도)")]
    [SerializeField] private float slamSlowMotionScale = 0.15f;

    [Tooltip("슬로우모션 지속 시간 (실제 시간 기준)")]
    [SerializeField] private float slamSlowMotionDuration = 0.08f;

    [Tooltip("착지 플래시 색상 (흰색 계열 추천)")]
    [SerializeField] private Color slamFlashColor = new Color(1f, 1f, 1f, 0.5f);

    [Tooltip("착지 플래시 지속 시간")]
    [SerializeField] private float slamFlashDuration = 0.12f;

    // 글로벌 볼륨
    private Volume globalVolume;
    private ChromaticAberration chromaticAberration;
    private Tweener distortTweener;

    // 슬로우모션 트윈 (중복 방지)
    private Tweener slowMotionTweener;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        FindAndResetCamera();
        FindAndResetVolume();
        ResetFlashImage();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAndResetCamera();
        FindAndResetVolume();
        ResetFlashImage();
    }

    private void FindAndResetCamera()
    {
        virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        if (virtualCamera != null)
        {
            impulseSource = virtualCamera.GetComponent<CinemachineImpulseSource>();
            defaultFOV = virtualCamera.Lens.FieldOfView;
        }
        else
        {
            impulseSource = null;
        }
    }

    private void FindAndResetVolume()
    {
        globalVolume = FindAnyObjectByType<Volume>();
        if (globalVolume != null && globalVolume.profile != null)
        {
            if (globalVolume.profile.TryGet(out chromaticAberration))
                chromaticAberration.intensity.value = 0f;
        }
        else
        {
            chromaticAberration = null;
        }
    }

    private void ResetFlashImage()
    {
        if (screenFlashImage != null)
        {
            screenFlashImage.DOKill();
            screenFlashImage.color = new Color(hitFlashColor.r, hitFlashColor.g, hitFlashColor.b, 0f);
        }
    }

    // ── 피격 시 카메라 흔들림 ───────────────────
    public void ShakeOnHit()
    {
        if (impulseSource != null)
        {
            float randomX = Random.value > 0.5f ? hitShakeX : -hitShakeX;
            impulseSource.GenerateImpulse(new Vector3(randomX, hitShakeY, 0f));
        }

        if (screenFlashImage != null)
        {
            screenFlashImage.DOKill();
            DOTween.Sequence()
                .Append(screenFlashImage.DOColor(hitFlashColor, flashInDuration).SetEase(Ease.OutQuad))
                .Append(screenFlashImage.DOColor(new Color(hitFlashColor.r, hitFlashColor.g, hitFlashColor.b, 0f), flashOutDuration).SetEase(Ease.InQuad))
                .SetUpdate(true);
        }

        if (chromaticAberration != null)
        {
            distortTweener?.Kill();
            chromaticAberration.intensity.value = hitDistortionStrength;
            distortTweener = DOTween.To(
                () => chromaticAberration.intensity.value,
                x => chromaticAberration.intensity.value = x,
                0f, flashOutDuration
            ).SetEase(Ease.InQuad)
             .SetUpdate(true);
        }
    }

    // ── 타격 카메라 흔들림 ───────────────────────
    public void ShakeOnAttack()
    {
        if (impulseSource == null) return;
        float randomX = Random.value > 0.5f ? attackShakeX : -attackShakeX;
        impulseSource.GenerateImpulse(new Vector3(randomX, attackShakeY, 0f));
    }

    public void ShakeOnAttackDirectional(Vector3 attackDir)
    {
        if (impulseSource == null) return;
        Vector3 horizontal = new Vector3(attackDir.x, 0f, attackDir.z).normalized;
        float dot = Vector3.Dot(horizontal, Camera.main.transform.right);
        impulseSource.GenerateImpulse(new Vector3(dot * attackShakeX, attackShakeY, 0f));
    }
    public void ZoomTo(float targetFOV, float duration)
    {
        // 보스 슬램 FOV 트윈 즉시 중단
        DOTween.Kill("BossSlamFOV");

        if (virtualCamera == null) return;

        DOTween.To(
            () => virtualCamera.Lens.FieldOfView,
            x => { var l = virtualCamera.Lens; l.FieldOfView = x; virtualCamera.Lens = l; },
            targetFOV,
            duration
        )
        .SetEase(Ease.OutQuad)
        .SetUpdate(true)   // 슬로우모션 중에도 동작
        .SetId("ResurrectFOV");
    }

    // 부활 시작 시 슬램 TimeScale 트윈 차단용
    public void CancelSlamSlowMotion()
    {
        slowMotionTweener?.Kill();
    }
    // ── 보스 착지 셰이크 ─────────────────
    public void ShakeOnBossSlam()
    {
        // 1. 임펄스 흔들림 (묵직한 쿵)
        if (impulseSource != null)
        {
            float randomX = Random.value > 0.5f ? bossSlamShakeX : -bossSlamShakeX;
            impulseSource.GenerateImpulse(new Vector3(randomX, -bossSlamShakeY, 0f));
        }

        // 2. FOV 펀치 (착지 순간 화면이 쑥 당겨졌다가 복구)
        if (virtualCamera != null)
        {
            DOTween.Kill("BossSlamFOV");

            float punchedFOV = defaultFOV - slamFOVPunch; // 좁아짐 (당겨지는 느낌)

            DOTween.To(
                () => virtualCamera.Lens.FieldOfView,
                x => { var l = virtualCamera.Lens; l.FieldOfView = x; virtualCamera.Lens = l; },
                punchedFOV,
                slamFOVInDuration
            )
            .SetEase(Ease.OutQuad)
            .SetId("BossSlamFOV")
            .OnComplete(() =>
            {
                DOTween.To(
                    () => virtualCamera.Lens.FieldOfView,
                    x => { var l = virtualCamera.Lens; l.FieldOfView = x; virtualCamera.Lens = l; },
                    defaultFOV,
                    slamFOVOutDuration
                )
                .SetEase(Ease.OutElastic) // ← 탄성있게 복구 
                .SetId("BossSlamFOV");
            });
        }

        // 3. 슬로우모션 (착지 순간 찰나의 정지)
        if (Time.timeScale > 0f)
        {
            slowMotionTweener?.Kill();
            Time.timeScale = slamSlowMotionScale;
            slowMotionTweener = DOTween.To(
                () => Time.timeScale,
                x => Time.timeScale = x,
                1f,
                slamSlowMotionDuration
            )
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                // ↓ 이거 추가 - 복구 시점에 퍼즈 중이면 다시 0으로
                PauseMenuManager pause = Object.FindAnyObjectByType<PauseMenuManager>();
                if (pause != null && pause.IsPaused)
                    Time.timeScale = 0f;
            });
        }

        // 4. 착지 플래시 (흰색 번쩍)
        if (screenFlashImage != null)
        {
            screenFlashImage.DOKill();
            Color transparentSlam = new Color(slamFlashColor.r, slamFlashColor.g, slamFlashColor.b, 0f);

            DOTween.Sequence()
                .Append(screenFlashImage.DOColor(slamFlashColor, 0.02f).SetEase(Ease.OutQuad))
                .Append(screenFlashImage.DOColor(transparentSlam, slamFlashDuration).SetEase(Ease.InQuad))
                .SetUpdate(true);
        }


        // 5. 색수차 왜곡 (착지 충격파 느낌)
        if (chromaticAberration != null)
        {
            distortTweener?.Kill();
            chromaticAberration.intensity.value = 1f; // 최대로 번지고
            distortTweener = DOTween.To(
                () => chromaticAberration.intensity.value,
                x => chromaticAberration.intensity.value = x,
                0f,
                0.4f
            ).SetEase(Ease.InQuad);
        }
    }
}