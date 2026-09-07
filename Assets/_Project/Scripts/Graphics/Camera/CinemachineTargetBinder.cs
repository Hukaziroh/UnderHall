using UnityEngine;
using Unity.Cinemachine;

public class CinemachineTargetBinder : MonoBehaviour
{
    private CinemachineCamera _vcam;

    void Start()
    {
        _vcam = GetComponent<CinemachineCamera>();

        // 1. 하데스 카메라 타겟 스크립트가 붙은 오브젝트를 씬에서 찾습니다.
        HadesCameraTarget hadesTarget = FindAnyObjectByType<HadesCameraTarget>();

        if (hadesTarget != null)
        {
            // 2. 찾았다면 플레이어가 아닌 하데스 타겟 오브젝트를 Follow하도록 설정합니다.
            _vcam.Follow = hadesTarget.transform;

            // LookAt(바라보기)은 쿼터뷰/탑다운 핵앤슬래시에서는 아예 비워두거나(null), 
            // 캐릭터 중심을 고정하고 싶다면 player를 넣어둡니다. (보통 쿼터뷰는 널로 비워두는 게 부드럽습니다)
            //GameObject player = GameObject.FindWithTag("Player");
            //if (player != null) _vcam.LookAt = player.transform;

            Debug.Log("[CinemachineTargetBinder] 하데스 카메라 타겟 바인딩 완료!");
        }
        else
        {
            // 만약 하데스 타겟 오브젝트가 없다면 기존처럼 플레이어를 직접 따라갑니다. (예외 처리)
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                _vcam.Follow = player.transform;
                _vcam.LookAt = player.transform;
                Debug.LogWarning("[CinemachineTargetBinder] HadesCameraTarget을 찾지 못해 플레이어 본체에 연결했습니다.");
            }
        }
    }
}