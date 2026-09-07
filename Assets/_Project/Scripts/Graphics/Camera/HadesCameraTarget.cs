using UnityEngine;
using UnityEngine.InputSystem; 

public class HadesCameraTarget : MonoBehaviour
{
    [Header("Target References")]
    [SerializeField] private Transform playerTransform; // 플레이어 본체의 Transform

    [Header("Camera Movement Settings")]
    [Range(0f, 0.5f)]
    [SerializeField] private float mouseInfluence = 0.08f; // 마우스가 카메라를 당기는 힘
    [SerializeField] private float maxMouseDistance = 3f;   // 카메라 타겟 최대 제한 거리
    [SerializeField] private float smoothTime = 0.25f;      // 부드러운 정도

    private Camera mainCamera;
    private Vector3 currentVelocity; // SmoothDamp 내부 연산용 변수

    private void Start()
    {
        // 1. 협업용 치트키: 부모(CameraSystem) 밑에서 독립시켜 월드 좌표 꼬임 차단
        transform.SetParent(null);

        // 첫 기동 시 초기화 시도
        TryFindReferences();
    }

    private void LateUpdate()
    {
        // 2.카메라나 플레이어가 유실(None/Missing)되었다면 실시간으로 다시 복구 시도
        if (mainCamera == null || playerTransform == null)
        {
            TryFindReferences();
        }

        // 만약 복구 시도를 했는데도 씬에 아직 플레이어나 카메라가 안 만들어졌다면 이번 프레임은 안전하게 패스
        if (playerTransform == null || mainCamera == null) return;

        // 1. 신형 인풋 시스템 방식으로 현재 마우스의 스크린 좌표 읽기
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();

        // 2. 마우스 스크린 좌표를 레이캐스트를 통해 3D 월드 바닥 좌표로 변환
        Ray ray = mainCamera.ScreenPointToRay(mouseScreenPos);
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, playerTransform.position.y, 0));

        if (groundPlane.Raycast(ray, out float enterDistance))
        {
            Vector3 mouseWorldPos = ray.GetPoint(enterDistance);

            // 3. 플레이어에서 마우스를 향하는 방향 벡터 계산 (Y축 평면화)
            Vector3 targetDirection = mouseWorldPos - playerTransform.position;
            targetDirection.y = 0;

            // 4. 설정한 영향력(Influence)을 적용하되, 최대 제한 거리를 넘지 않도록 세팅
            float targetDistance = targetDirection.magnitude * mouseInfluence;
            targetDistance = Mathf.Min(targetDistance, maxMouseDistance);

            // 5. 최종 목표 지점 계산
            Vector3 desiredPosition = playerTransform.position + targetDirection.normalized * targetDistance;

            // 6. SmoothDamp로 프레임 떨림 없이 부드럽게 좌표 추적
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPosition,
                ref currentVelocity,
                smoothTime
            );
        }
    }

    /// <summary>
    /// 깨진 카메라와 플레이어 레퍼런스를 실시간으로 다시 찾아주는 안전장치 함수
    /// </summary>
    private void TryFindReferences()
    {
        // 메인 카메라가 깨졌다면 현재 씬의 메인 카메라로 재배정
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera != null) Debug.Log($"[{gameObject.name}] 씬 전환에 맞춰 메인 카메라를 갱신했습니다.");
        }

        // 플레이어가 깨졌거나 없다면 다시 탐색
        if (playerTransform == null)
        {
            // 1순위: GameManager의 싱글톤 변수에서 낚아채기
            if (GameManager.Instance != null && GameManager.Instance.player != null)
            {
                playerTransform = GameManager.Instance.player.transform;
                Debug.Log($"[{gameObject.name}] GameManager를 통해 새 플레이어를 찾아 연결했습니다.");
            }
            // 2순위: 씬의 태그로 찾기
            else
            {
                GameObject playerObj = GameObject.FindWithTag("Player");
                if (playerObj != null)
                {
                    playerTransform = playerObj.transform;
                    Debug.Log($"[{gameObject.name}] 'Player' 태그를 통해 새 플레이어를 찾아 연결했습니다.");
                }
            }
        }
    }
}