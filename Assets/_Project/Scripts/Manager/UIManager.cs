using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; //  [추가] 두트윈 네임스페이스

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Health UI")]
    public Image healthImage;
    public TextMeshProUGUI healthText;
    //창우_삭제 public GameObject interactPromptUI;  // 인스펙터에서 드래그

    //창우_[추가] HP 바 전체를 담고 있는 부모 RectTransform (인스펙터에서 드래그)
    [SerializeField] private RectTransform healthBarContainer;

    [Header("─ UI 피격 셰이크 설정 ─")]
    [SerializeField] private float shakeDuration = 0.2f;  // 흔들리는 시간
    [SerializeField] private float shakeStrength = 15f;   // 흔들리는 세기 (픽셀 단위)
    [SerializeField] private int shakeVibrato = 15;       // 진동 횟수
    [SerializeField] private float shakeRandomness = 90f; // 랜덤성


    //창우_삭제 public void ShowInteractPrompt()
    //창우_[추가] 피격 시 점멸할 (얕은) 붉은색 설정
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.4f, 0.4f, 1f);

    [Header("─ Interaction UI ─")]
    public GameObject interactPromptUI;

    [Header("─ Boss UI ─")]
    public GameObject bossHealthContainer; 
    public Slider bossHealthSlider;       

    private void Awake()
    {

        //창우_삭제 if (interactPromptUI != null)
        //          interactPromptUI.SetActive(true);

        if (Instance == null)
        {
            Instance = this;
            // 만약 씬 전환 시에도 UI를 유지하고 싶다면 DontDestroyOnLoad를 켜도 됩니다.
        }
        else
        {
            Destroy(gameObject);
        }
    }

    //변경_public void HideInteractPrompt()
    public void ShowInteractPrompt()
    {
        //창우_삭제if (interactPromptUI != null)
        //         interactPromptUI.SetActive(false);

        if (interactPromptUI != null) interactPromptUI.SetActive(true);
    }

    public void HideInteractPrompt()
    {
        //창우_삭제if (Instance == null) Instance = this;
        //else Destroy(gameObject);

        if (interactPromptUI != null) interactPromptUI.SetActive(false);
    }

    public void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthImage != null)
        {
            healthImage.fillAmount = currentHealth / maxHealth;
        }

        if (healthText != null)
        {
            healthText.text = $"{Mathf.CeilToInt(currentHealth)} / {Mathf.CeilToInt(maxHealth)}";
        }
    }

    /// <summary>
    /// 창우_ 플레이어 피격 시 호출할 UI 흔들기 함수
    /// </summary>
    public void PlayHealthBarShake()
    {
        if (healthBarContainer == null || healthImage == null) return;

        DOTween.Kill(healthBarContainer);
        DOTween.Kill(healthImage);

        // ↓ 흔들기 전 위치 저장 후 복구
        Vector2 originalPos = healthBarContainer.anchoredPosition;
        healthBarContainer.anchoredPosition = originalPos;

        healthBarContainer.DOShakeAnchorPos( // ← DOShakePosition 대신 이걸로
            duration: shakeDuration,
            strength: shakeStrength,
            vibrato: shakeVibrato,
            randomness: shakeRandomness,
            fadeOut: true
        ).OnComplete(() =>
            healthBarContainer.anchoredPosition = originalPos // ← 끝나면 원래 위치로
        );

        Sequence flashSequence = DOTween.Sequence(healthImage);
        flashSequence
            .Append(healthImage.DOColor(hitFlashColor, 0.05f).SetEase(Ease.OutFlash))
            .Append(healthImage.DOColor(Color.white, 0.15f).SetEase(Ease.InFlash));
    }

  

    // 보스 등장 시 체력바 켜기
    public void ShowBossUI(float maxHealth)
    {
        if (bossHealthContainer != null) bossHealthContainer.SetActive(true);
        if (bossHealthSlider != null)
        {
            bossHealthSlider.maxValue = maxHealth;
            bossHealthSlider.value = maxHealth;
        }
    }

    // 보스 피격 시 체력바 깎기 (두트윈으로 스무스하게)
    public void UpdateBossHealth(float currentHealth)
    {
        if (bossHealthSlider != null)
        {
            bossHealthSlider.DOKill();

            bossHealthSlider.DOValue(currentHealth, 0.2f).SetEase(Ease.OutCubic);
        }
    }

    // 보스 처치 시 체력바 끄기
    public void HideBossUI()
    {
        if (bossHealthContainer != null) bossHealthContainer.SetActive(false);
    }
}