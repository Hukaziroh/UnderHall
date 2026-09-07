using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    private TextMeshPro tmp;
    private Camera mainCam;

    [Header("이동 설정")]
    public float floatHeight = 0.5f;      // 살짝만 위로 
    public float floatDuration = 0.6f;
    public float fadeDelay = 0.4f;
    public float fadeDuration = 0.3f;

    [Header("색상 설정")]
    public Color normalColor = Color.white;
    public Color criticalColor = new Color(1f, 0.8f, 0f); // 금색

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
        mainCam = Camera.main;
    }

    // 매 프레임 카메라 바라보기
    void LateUpdate()
    {
        if (mainCam != null)
            transform.rotation = mainCam.transform.rotation;
    }

    public void Play(float damage, bool isCritical = false)
    {
        tmp.color = isCritical ? criticalColor : normalColor;
        tmp.text = isCritical ? $"<b>{(int)damage}!</b>" : $"{(int)damage}";
        tmp.alpha = 1f;
        transform.localScale = isCritical ? Vector3.one * 1.5f : Vector3.one;

        // 카메라 기준 위로 살짝 이동
        Vector3 targetPos = transform.position + mainCam.transform.up * floatHeight;

        Sequence seq = DOTween.Sequence();

        // 1) 살짝 위로 이동
        seq.Append(
            transform.DOMove(targetPos, floatDuration)
                     .SetEase(Ease.OutQuad)
        );

        // 2) 대기 후 페이드
        seq.AppendInterval(fadeDelay);
        seq.Append(
            DOTween.To(() => tmp.alpha, x => tmp.alpha = x, 0f, fadeDuration)
                   .SetEase(Ease.InQuad)
        );

        seq.OnComplete(() => Destroy(gameObject));
        seq.Play();
    }
}