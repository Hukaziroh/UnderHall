using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 하데스 스타일 데미지 숫자 스포너
/// 같은 몹에 연속 타격 시 가로로 나란히 정렬됨
/// </summary>
public class DamageNumberSpawner : MonoBehaviour
{
    public static DamageNumberSpawner Instance;

    [Header("데미지 숫자 프리팹")]
    public GameObject damageNumberPrefab;

    [Header("스타일 설정")]
    [Tooltip("몹 머리 위 기본 높이")]
    public float baseHeight = 2.0f;

    [Tooltip("숫자 간격 (가로)")]
    public float spacing = 0.4f;

    [Tooltip("같은 몹에 연속 타격 인정 시간")]
    public float groupTime = 0.5f;

    // 몹별로 마지막 스폰 위치와 시간 추적
    private Dictionary<GameObject, EnemyDamageGroup> groups
        = new Dictionary<GameObject, EnemyDamageGroup>();

    private class EnemyDamageGroup
    {
        public float lastTime;
        public float currentX;    // 현재 가로 오프셋
        public int count;       // 연속 타격 횟수
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void Show(float damage, Vector3 worldPos, bool isCritical = false, GameObject enemy = null)
    {
        if (damageNumberPrefab == null)
        {
            Debug.LogWarning("[DamageNumberSpawner] 프리팹이 연결되지 않았습니다!");
            return;
        }

        Camera cam = Camera.main;
        if (cam == null) return;

        // 몹 머리 위 기준 위치
        Vector3 basePos = worldPos + Vector3.up * baseHeight;

        // ── 가로 오프셋 계산 ────────
        float xOffset = 0f;

        if (enemy != null)
        {
            // 기존 그룹 확인
            if (groups.TryGetValue(enemy, out var group))
            {
                // groupTime 안에 연속 타격이면 오른쪽으로 밀기
                if (Time.time - group.lastTime < groupTime)
                {
                    group.count++;
                    // 홀짝으로 좌우 번갈아 배치 
                    group.currentX = (group.count % 2 == 0)
                        ? spacing * (group.count / 2)
                        : -spacing * (group.count / 2);
                    xOffset = group.currentX;
                }
                else
                {
                    // 시간 초과 → 초기화
                    group.lastTime = Time.time;
                    group.currentX = 0f;
                    group.count = 0;
                    xOffset = 0f;
                }
                group.lastTime = Time.time;
            }
            else
            {
                // 새 그룹 생성
                groups[enemy] = new EnemyDamageGroup
                {
                    lastTime = Time.time,
                    currentX = 0f,
                    count = 0
                };
            }
        }

        // 카메라 기준 오른쪽 방향으로 오프셋 적용
        Vector3 spawnPos = basePos + cam.transform.right * xOffset;

        GameObject obj = Instantiate(damageNumberPrefab, spawnPos, Quaternion.identity);
        obj.transform.rotation = cam.transform.rotation;

        DamageNumber dn = obj.GetComponent<DamageNumber>();
        if (dn != null) dn.Play(damage, isCritical);
    }
}