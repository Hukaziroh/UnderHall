using UnityEngine;

/// <summary>
/// 타격 시 파티클 이펙트 스폰
/// 플레이어/몬스터 오브젝트에 붙여서 사용
/// 공격이 맞았을 때: SpawnHitVFX(hitPosition, hitNormal) 호출
/// </summary>
public class HadesHitVFX : MonoBehaviour
{
    [Header("파티클 프리팹 (Inspector에서 드래그)")]
    [SerializeField] GameObject hitSparkPrefab;   // 타격 불꽃
    [SerializeField] GameObject hitImpactPrefab;  // 타격 충격파

    /// <summary>타격 시 호출</summary>
    public void SpawnHitVFX(Vector3 position, Vector3 normal)
    {
        Spawn(hitSparkPrefab, position, normal);
        Spawn(hitImpactPrefab, position, normal);
    }

    void Spawn(GameObject prefab, Vector3 position, Vector3 normal)
    {
        if (prefab == null) return;
        var go = Instantiate(prefab, position, Quaternion.LookRotation(normal));
        var ps = go.GetComponent<ParticleSystem>();
        Destroy(go, ps != null ? ps.main.duration + ps.main.startLifetime.constantMax : 2f);
    }
}