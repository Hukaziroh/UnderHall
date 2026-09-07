using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// VFX 통합 매니저 (오브젝트 풀링 및 MaterialPropertyBlock 최적화 적용)
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("─ 검/무기 이펙트 ─")]
    [SerializeField] GameObject weaponSwingPrefab;
    [SerializeField] GameObject weaponSkillPrefab;
    [SerializeField] GameObject weaponSkillExplosionPrefab;

    [Header("─ 타격 이펙트 ─")]
    [SerializeField] GameObject attackHitSparkPrefab;
    [SerializeField] GameObject attackImpactPrefab;

    [Header("─ 대시 이펙트 ─")]
    [SerializeField] GameObject dashStartPrefab;
    [SerializeField] GameObject dashTrailPrefab;
    [SerializeField] GameObject dashEndPrefab;

    [Header("─ 플레이어 피격/사망 ─")]
    [SerializeField] GameObject playerHitPrefab;
    [SerializeField] GameObject playerDeathPrefab;

    [Header("─ 플레이어 부활 ─")]
    [SerializeField] GameObject playerResurrectStartPrefab;
    [SerializeField] GameObject playerResurrectEndPrefab;

    [Header("─ 몬스터 공격 이펙트 ─")]
    [SerializeField] GameObject monsterAttackPrefab;

    [Header("─ 몬스터 피격/사망 ─")]
    [SerializeField] GameObject monsterHitPrefab;
    [SerializeField] GameObject monsterDeathPrefab;
    [SerializeField] GameObject monsterDeathSmokePrefab;

    [Header("─ 보스 지상 이펙트 ─")]
    [SerializeField] GameObject bossAttackPrefab;       // Attack / Attack02
    [SerializeField] GameObject bossBreathPrefab;       // BreatheFire
    [SerializeField] GameObject bossRushPrefab;         //  [추가] 보스 드래곤 돌진 이펙트

    [Header("─ 보스 공중 이펙트 ─")]
    [SerializeField] GameObject bossFlyAttackPrefab;    // FlyAttack
    [SerializeField] GameObject bossFlyBreathPrefab;    // FlyBreatheFire
    [SerializeField] GameObject bossDivePrefab;         // FlyDive
    [SerializeField] GameObject bossSlamPrefab; // 땅 착지 이펙트

    [Header("─ 풀링 설정 ─")]
    [SerializeField] int poolSizePerPrefab = 5;

    [Header("─ 피격 플래시 설정 ─")]
    [SerializeField] Color playerHitColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] Color monsterHitColor = new Color(1f, 0.3f, 0.1f, 1f);
    [SerializeField] float hitFlashDuration = 0.12f;

    [Header("─ 방 클리어 / 보상 이펙트 ─")]
    [SerializeField] GameObject roomClearVFXPrefab;
    [SerializeField] GameObject rewardAppearVFXPrefab;

    Dictionary<GameObject, Queue<GameObject>> pool = new Dictionary<GameObject, Queue<GameObject>>();
    Transform poolRoot;

    // 최적화를 위한 프로퍼티 블록 전역 변수화 (매번 new 생성 금지!)
    private MaterialPropertyBlock hitMpb;

    static readonly int HitBlendID = Shader.PropertyToID("_HitEffectBlend");
    static readonly int HitColorID = Shader.PropertyToID("_HitColor");

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        poolRoot = new GameObject("VFX_Pool").transform;
        poolRoot.SetParent(transform);

        // 프로퍼티 블록 초기화
        hitMpb = new MaterialPropertyBlock();

        // 풀링 예열
        PrewarmPool(monsterAttackPrefab);
        PrewarmPool(weaponSwingPrefab);
        PrewarmPool(weaponSkillPrefab);
        PrewarmPool(weaponSkillExplosionPrefab);
        PrewarmPool(attackHitSparkPrefab);
        PrewarmPool(attackImpactPrefab);
        PrewarmPool(dashStartPrefab);
        PrewarmPool(dashTrailPrefab);
        PrewarmPool(dashEndPrefab);
        PrewarmPool(playerHitPrefab);
        PrewarmPool(playerDeathPrefab);
        PrewarmPool(playerResurrectStartPrefab);
        PrewarmPool(playerResurrectEndPrefab);
        PrewarmPool(monsterHitPrefab);
        PrewarmPool(monsterDeathPrefab);
        PrewarmPool(monsterDeathSmokePrefab);
        PrewarmPool(bossAttackPrefab);
        PrewarmPool(bossBreathPrefab);
        PrewarmPool(bossRushPrefab); // 보스 돌진 풀링 예열 추가
        PrewarmPool(bossFlyAttackPrefab);
        PrewarmPool(bossFlyBreathPrefab);
        PrewarmPool(bossDivePrefab);
        PrewarmPool(bossSlamPrefab);
        PrewarmPool(roomClearVFXPrefab);
        PrewarmPool(rewardAppearVFXPrefab);
    }

    void PrewarmPool(GameObject prefab)
    {
        if (prefab == null) return;
        pool[prefab] = new Queue<GameObject>();
        for (int i = 0; i < poolSizePerPrefab; i++)
            pool[prefab].Enqueue(CreatePoolObject(prefab));
    }

    GameObject CreatePoolObject(GameObject prefab)
    {
        var go = Instantiate(prefab, poolRoot);
        go.SetActive(false);
        return go;
    }

    // ─────────────────────────────────────────
    // 풀 관리 기본 메커니즘
    // ─────────────────────────────────────────

    GameObject GetFromPool(GameObject prefab, Vector3 position, Vector3 direction, bool useUnscaledTime = false, bool autoReturn = true)
    {
        if (prefab == null) return null;
        if (!pool.ContainsKey(prefab))
            pool[prefab] = new Queue<GameObject>();

        GameObject go = pool[prefab].Count > 0
            ? pool[prefab].Dequeue()
            : CreatePoolObject(prefab);

        go.transform.position = position;
        go.transform.rotation = direction != Vector3.zero
            ? Quaternion.LookRotation(direction) * prefab.transform.rotation
            : prefab.transform.rotation;
        go.SetActive(true);

        var ps = go.GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();

        if (autoReturn)
        {
            float duration = ps != null
                ? ps.main.duration + ps.main.startLifetime.constantMax
                : 2f;

            StartCoroutine(useUnscaledTime
                ? ReturnToPoolUnscaled(go, prefab, duration)
                : ReturnToPool(go, prefab, duration));
        }

        return go;
    }

    IEnumerator ReturnToPool(GameObject go, GameObject prefab, float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnObject(go, prefab);
    }

    IEnumerator ReturnToPoolUnscaled(GameObject go, GameObject prefab, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        ReturnObject(go, prefab);
    }

    void ReturnObject(GameObject go, GameObject prefab)
    {
        if (go == null) return;
        go.SetActive(false);
        go.transform.SetParent(poolRoot);
        if (pool.ContainsKey(prefab))
            pool[prefab].Enqueue(go);
    }

    // ─────────────────────────────────────────
    // 인게임 컨텍스트 호출 함수들
    // ─────────────────────────────────────────

    public void PlayRoomClear(Vector3 position)
        => GetFromPool(roomClearVFXPrefab, position, Vector3.up);

    public GameObject PlayRewardAppear(Vector3 position)
        => GetFromPool(rewardAppearVFXPrefab, position, Vector3.up);

    public void PlayWeaponSwing(Vector3 position, Vector3 direction)
        => GetFromPool(weaponSwingPrefab, position, direction);

    private GameObject activeWeaponSkillInstance;
    private GameObject activeWeaponSkillPrefab;

    public void PlayWeaponSkillLoop(Transform targetTransform)
    {
        StopWeaponSkillLoop();
        activeWeaponSkillPrefab = weaponSkillPrefab;
        activeWeaponSkillInstance = GetFromPool(weaponSkillPrefab, targetTransform.position, targetTransform.forward, false, false);
        if (activeWeaponSkillInstance != null)
        {
            activeWeaponSkillInstance.transform.SetParent(targetTransform);
            activeWeaponSkillInstance.transform.localPosition = Vector3.zero;
            activeWeaponSkillInstance.transform.localRotation = weaponSkillPrefab.transform.localRotation;
        }
    }

    public void StopWeaponSkillLoop()
    {
        if (activeWeaponSkillInstance != null && activeWeaponSkillPrefab != null)
        {
            ReturnObject(activeWeaponSkillInstance, activeWeaponSkillPrefab);
            activeWeaponSkillInstance = null;
            activeWeaponSkillPrefab = null;
        }
    }

    public void PlayWeaponSkillExplosion(Transform targetTransform)
    {
        StopWeaponSkillLoop();
        activeWeaponSkillPrefab = weaponSkillExplosionPrefab;
        activeWeaponSkillInstance = GetFromPool(weaponSkillExplosionPrefab, targetTransform.position, targetTransform.forward, false, false);
        if (activeWeaponSkillInstance != null)
        {
            activeWeaponSkillInstance.transform.SetParent(targetTransform);
            activeWeaponSkillInstance.transform.localPosition = Vector3.zero;
            activeWeaponSkillInstance.transform.localRotation = weaponSkillExplosionPrefab.transform.localRotation;
        }
    }

    public void PlayAttackHit(Vector3 position, Vector3 normal)
    {
        GetFromPool(attackHitSparkPrefab, position, normal);
        GetFromPool(attackImpactPrefab, position, normal);
    }

    public void PlayDash(Vector3 position, Vector3 direction, float dashDuration)
    {
        GetFromPool(dashStartPrefab, position, direction);
        StartCoroutine(DashTrailRoutine(position, direction, dashDuration));
    }

    IEnumerator DashTrailRoutine(Vector3 startPos, Vector3 direction, float dashDuration)
    {
        float elapsed = 0f;
        float interval = 0.05f;
        while (elapsed < dashDuration)
        {
            GetFromPool(dashTrailPrefab, startPos, direction);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
        }
        GetFromPool(dashEndPrefab, startPos, direction);
    }

    public void PlayPlayerHit(Vector3 position, Vector3 normal, GameObject playerObj)
    {
        GetFromPool(playerHitPrefab, position, normal);
        StartCoroutine(FlashRoutine(playerObj, playerHitColor));
    }

    public void PlayPlayerDeath(Vector3 position, GameObject playerObj)
    {
        GetFromPool(playerDeathPrefab, position, Vector3.up);
    }

    public void PlayPlayerResurrectStart(Vector3 position)
        => GetFromPool(playerResurrectStartPrefab, position, Vector3.up, useUnscaledTime: true);

    public void PlayPlayerResurrectEnd(Vector3 position)
        => GetFromPool(playerResurrectEndPrefab, position, Vector3.up);

    public void PlayMonsterHit(Vector3 position, Vector3 normal, GameObject monsterObj)
    {
        GetFromPool(monsterHitPrefab, position, normal);
        StartCoroutine(FlashRoutine(monsterObj, monsterHitColor));
    }

    public void PlayMonsterDeath(Vector3 position, GameObject monsterObj)
    {
        GetFromPool(monsterDeathPrefab, position, Vector3.up);
        GetFromPool(monsterDeathSmokePrefab, position, Vector3.up);
    }

    public void PlayMonsterAttack(Vector3 position, Vector3 direction)
    {
        GetFromPool(monsterAttackPrefab, position, direction);
    }

    // ─────────────────────────────────────────
    // 보스 드래곤 전용 액션 연출 호출부
    // ─────────────────────────────────────────

    public void PlayBossAttack(Vector3 position, Vector3 direction)
        => GetFromPool(bossAttackPrefab, position, direction);

    public void PlayBossBreath(Transform head, Vector3 direction)
    {
        GameObject go = GetFromPool(bossBreathPrefab, head.position, direction);
        if (go != null)
        {
            go.transform.SetParent(head);
            go.transform.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// [신규 추가] 보스 드래곤 지상 돌진 이펙트 재생
    /// 호출 방식: VFXManager.Instance.PlayBossRush(vfxPoint.position, transform.forward);
    /// </summary>
    public void PlayBossRush(Transform targetPoint)
    {
        // 1. 풀에서 이펙트를 vfxPoint의 현재 위치와 방향에 맞춰 꺼냅니다.
        GameObject go = GetFromPool(bossRushPrefab, targetPoint.position, targetPoint.forward);

        if (go != null)
        {
            // 2. 이펙트를 vfxPoint의 자식으로 등록해서 보스가 움직일 때 완벽히 고정되어 전진하게 만듭니다.
            go.transform.SetParent(targetPoint);

            // 3. vfxPoint 기준 정중앙 정렬 및 회전값 초기화
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = bossRushPrefab.transform.localRotation;
        }
    }
    public void PlayBossSlam(Vector3 position)
    => GetFromPool(bossSlamPrefab, position, Vector3.up);

    public void PlayBossFlyAttack(Transform targetPoint)
    {
        GameObject go = GetFromPool(bossFlyAttackPrefab, targetPoint.position, targetPoint.forward);
        if (go != null)
        {
            go.transform.SetParent(targetPoint);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = bossFlyAttackPrefab.transform.localRotation;
        }
    }

    public void PlayBossFlyBreath(Vector3 position, Vector3 direction)
        => GetFromPool(bossFlyBreathPrefab, position, direction);

    public void PlayBossDive(Transform targetPoint)
    {
        GameObject go = GetFromPool(bossDivePrefab, targetPoint.position, targetPoint.forward);
        if (go != null)
        {
            go.transform.SetParent(targetPoint);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = bossDivePrefab.transform.localRotation;
        }
    }

    // ─────────────────────────────────────────
    // 고성능 셰이더 프로퍼티 블록 연출 루틴 (Material 복사 없음)
    // ─────────────────────────────────────────

    IEnumerator FlashRoutine(GameObject target, Color color)
    {
        if (target == null) yield break;

        var renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) yield break;

        // 1. 블록에 값 세팅 후 GPU에 주입 (메테리얼 복사 원천 차단)
        hitMpb.Clear();
        hitMpb.SetColor(HitColorID, color);
        hitMpb.SetFloat(HitBlendID, 1f);

        foreach (var r in renderers)
        {
            if (r != null) r.SetPropertyBlock(hitMpb);
        }

        // 피격 대기 시간
        yield return new WaitForSeconds(hitFlashDuration);

        // 2. 복구 전 오브젝트 파괴 예외 처리
        if (target == null) yield break;

        hitMpb.Clear();
        hitMpb.SetFloat(HitBlendID, 0f);

        foreach (var r in renderers)
        {
            if (r != null) r.SetPropertyBlock(hitMpb);
        }
    }
}