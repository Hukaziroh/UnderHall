using DG.Tweening;
using UnityEngine;

/// <summary>
/// 좌우 양문 DOTween 컨트롤러
/// 
/// [Inspector 세팅 방법]
/// 1. 빈 오브젝트(DoorGroup) 생성 → 이 스크립트 부착
/// 2. leftDoor  : 왼쪽 문 오브젝트 연결 (피벗이 왼쪽 끝에 있어야 함)
/// 3. rightDoor : 오른쪽 문 오브젝트 연결 (피벗이 오른쪽 끝에 있어야 함)
/// 4. Door.cs의 UnlockDoor()가 호출될 때만 열림
///    → 플레이어가 닿아도 자동으로 열리지 않음
/// </summary>
public class DoubleDoorController : MonoBehaviour
{
    [Header("문 오브젝트")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("열림 각도 설정")]
    [Tooltip("문이 열릴 때 Y축 회전 각도 (보통 90~110 사이)")]
    public float openAngle = 100f;

    [Header("타이밍 설정")]
    [Tooltip("흔들리는 예비동작 시간")]
    public float shakeDuration = 0.25f;

    [Tooltip("활짝 열리는 시간")]
    public float openDuration = 0.45f;

    [Tooltip("닫히는 시간")]
    public float closeDuration = 0.4f;

    [Header("흔들림 강도")]
    [Tooltip("열리기 전 흔들림 세기")]
    public float shakeStrength = 8f;

    public bool isOpen = false;
    private bool isAnimating = false;

    // 초기 회전값 저장
    private Vector3 leftClosedRot;
    private Vector3 rightClosedRot;

    void Start()
    {
        if (leftDoor == null || rightDoor == null)
        {
            Debug.LogError("[DoubleDoorController] leftDoor 또는 rightDoor가 연결되지 않았습니다!");
            return;
        }

        leftClosedRot = leftDoor.localEulerAngles;
        rightClosedRot = rightDoor.localEulerAngles;
    }

    // ───────────────────────────────────────────
    //   OnTriggerEnter/Exit 완전 제거
    //    → 플레이어가 닿아도 절대 자동으로 열리지 않음
    //    → 오직 Door.cs의 UnlockDoor()에서만 OpenDoor() 호출
    // ───────────────────────────────────────────

    // ───────────────────────────────────────────
    // 문 열기 (Door.cs의 UnlockDoor()에서만 호출됨)
    // ───────────────────────────────────────────
    public void OpenDoor()
    {
        if (isOpen || isAnimating) return;
        isAnimating = true;

        Sequence leftSeq = DOTween.Sequence();
        Sequence rightSeq = DOTween.Sequence();

        // ── 왼쪽 문 ──────────────────────────
        leftSeq.Append(
            leftDoor.DOLocalRotate(
                leftClosedRot + new Vector3(0, shakeStrength, 0),
                shakeDuration
            ).SetEase(Ease.OutQuad)
        );
        leftSeq.Append(
            leftDoor.DOLocalRotate(
                leftClosedRot + new Vector3(0, -openAngle, 0),
                openDuration
            ).SetEase(Ease.OutBack)
        );
        leftSeq.Append(
            leftDoor.DOShakeRotation(0.3f, new Vector3(0, shakeStrength * 0.3f, 0), 8, 45f)
        );

        // ── 오른쪽 문 ────────────────────────
        rightSeq.Append(
            rightDoor.DOLocalRotate(
                rightClosedRot + new Vector3(0, -shakeStrength, 0),
                shakeDuration
            ).SetEase(Ease.OutQuad)
        );
        rightSeq.Append(
            rightDoor.DOLocalRotate(
                rightClosedRot + new Vector3(0, openAngle, 0),
                openDuration
            ).SetEase(Ease.OutBack)
        );
        rightSeq.Append(
            rightDoor.DOShakeRotation(0.3f, new Vector3(0, shakeStrength * 0.3f, 0), 8, 45f)
        );

        leftSeq.OnComplete(() =>
        {
            isOpen = true;
            isAnimating = false;
        });

        leftSeq.Play();
        rightSeq.Play();
    }

    // ───────────────────────────────────────────
    // 문 닫기
    // ───────────────────────────────────────────
    public void CloseDoor()
    {
        if (!isOpen || isAnimating) return;
        isAnimating = true;

        Sequence leftSeq = DOTween.Sequence();
        Sequence rightSeq = DOTween.Sequence();

        leftSeq.Append(
            leftDoor.DOLocalRotate(leftClosedRot, closeDuration)
                    .SetEase(Ease.InBack)
        );
        leftSeq.Append(
            leftDoor.DOShakeRotation(0.2f, new Vector3(0, shakeStrength * 0.2f, 0), 6, 45f)
        );

        rightSeq.Append(
            rightDoor.DOLocalRotate(rightClosedRot, closeDuration)
                     .SetEase(Ease.InBack)
        );
        rightSeq.Append(
            rightDoor.DOShakeRotation(0.2f, new Vector3(0, shakeStrength * 0.2f, 0), 6, 45f)
        );

        leftSeq.OnComplete(() =>
        {
            isOpen = false;
            isAnimating = false;
        });

        leftSeq.Play();
        rightSeq.Play();
    }
}