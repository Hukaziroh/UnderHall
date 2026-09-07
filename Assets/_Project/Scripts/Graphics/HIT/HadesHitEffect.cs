using System.Collections;
using UnityEngine;

/// <summary>
/// 피격 이펙트 통합 관리
/// 플레이어/몬스터 오브젝트에 붙여서 사용
/// 피격 시: OnHit() 호출
/// 사망 시: OnDeath() 호출
/// </summary>
public class HadesHitEffect : MonoBehaviour
{
    [Header("피격 플래시")]
    [SerializeField] Color hitColor = new Color(1f, 0.15f, 0.15f, 1f);
    [SerializeField] float hitDuration = 0.12f;

    [Header("사망 Dissolve")]
    [SerializeField] float dissolveDuration = 1.2f;

    Material[] materials;

    static readonly int HitBlendID = Shader.PropertyToID("_HitEffectBlend");
    static readonly int HitColorID = Shader.PropertyToID("_HitColor");
    static readonly int FadeAmountID = Shader.PropertyToID("_FadeAmount");

    void Awake()
    {
        var renderers = GetComponentsInChildren<Renderer>();
        var matList = new System.Collections.Generic.List<Material>();
        foreach (var r in renderers)
            foreach (var m in r.materials)
                matList.Add(m);
        materials = matList.ToArray();

        foreach (var mat in materials)
        {
            if (mat.HasProperty(HitColorID)) mat.SetColor(HitColorID, hitColor);
            if (mat.HasProperty(HitBlendID)) mat.SetFloat(HitBlendID, 0f);
            if (mat.HasProperty(FadeAmountID)) mat.SetFloat(FadeAmountID, 0f);
        }
    }

    /// <summary>피격 시 호출</summary>
    public void OnHit()
    {
        StopCoroutine("FlashRoutine");
        StartCoroutine("FlashRoutine");
    }

    /// <summary>사망 시 호출</summary>
    public void OnDeath()
    {
        StopAllCoroutines();
        StartCoroutine(DissolveRoutine());
    }

    IEnumerator FlashRoutine()
    {
        SetFloat(HitBlendID, 1f);
        yield return new WaitForSeconds(hitDuration);
        SetFloat(HitBlendID, 0f);
    }

    IEnumerator DissolveRoutine()
    {
        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            SetFloat(FadeAmountID, elapsed / dissolveDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        SetFloat(FadeAmountID, 1f);
        yield return new WaitForSeconds(0.1f);
        Destroy(gameObject);
    }

    void SetFloat(int id, float value)
    {
        foreach (var mat in materials)
            if (mat.HasProperty(id))
                mat.SetFloat(id, value);
    }
}