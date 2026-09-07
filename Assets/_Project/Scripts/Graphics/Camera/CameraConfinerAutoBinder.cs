using UnityEngine;
using Unity.Cinemachine;

public class CameraConfinerAutoBinder : MonoBehaviour
{
    private CinemachineConfiner3D confiner;

    private void Awake()
    {
        confiner = GetComponent<CinemachineConfiner3D>();
    }

    // ★ 이 이름(AssignBounds)과 매개변수 타입(Collider)이 아래 GameManager와 완벽히 일치해야 합니다!
    public void AssignBounds(Collider targetCollider)
    {
        if (confiner == null)
            confiner = GetComponent<CinemachineConfiner3D>();

        if (confiner != null && targetCollider != null)
        {
            confiner.BoundingVolume = targetCollider;
            confiner.enabled = true;
            Debug.Log($"[CameraConfiner] {targetCollider.gameObject.name}의 콜라이더가 카메라 경계로 직접 주입되었습니다!");
        }
        else
        {
            if (confiner != null) confiner.enabled = false;
            Debug.LogWarning("[CameraConfiner] 주입하려는 콜라이더가 없거나 Confiner 컴포넌트를 찾을 수 없습니다.");
        }
    }
}