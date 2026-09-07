using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public enum GiftCategory
{
    BasicAttack,   
    SpecialAttack,  
    Utility        
}

[System.Serializable]
public class GiftInfo
{
    public GiftType type;
    public GiftCategory category; 
    public string giftName;
    [TextArea] public string description;
    public Sprite icon; 
}

public class GiftManager : MonoBehaviour
{
    public PlayerData playerData;

    [Header("UI 연결 (Canvas)")]
    public GameObject giftUIPanel;
    public Button[] giftButtons;
    public TextMeshProUGUI[] giftNameTexts;
    public TextMeshProUGUI[] giftDescTexts;

    public TextMeshProUGUI[] giftCategoryTexts; 
    public Image[] giftIconImages;

    [Header("카테고리별 아이콘")]
    public Sprite basicAttackIcon;
    public Sprite specialAttackIcon;
    public Sprite utilityIcon;

    [Header("전체 기프트 데이터베이스")]
    public List<GiftInfo> allGifts = new List<GiftInfo>();

    private void Awake()
    {
        if (allGifts.Count == 0)
        {
            allGifts.Add(new GiftInfo { type = GiftType.Execution });
            allGifts.Add(new GiftInfo { type = GiftType.Combo });
            allGifts.Add(new GiftInfo { type = GiftType.Critical });
            allGifts.Add(new GiftInfo { type = GiftType.Explosion });
            allGifts.Add(new GiftInfo { type = GiftType.RapidFire });
            allGifts.Add(new GiftInfo { type = GiftType.Endurance });
            allGifts.Add(new GiftInfo { type = GiftType.Awakening });
            allGifts.Add(new GiftInfo { type = GiftType.Vampirism });
            allGifts.Add(new GiftInfo { type = GiftType.Berserk });
        }

        foreach (var gift in allGifts)
        {
            switch (gift.type)
            {
                case GiftType.Execution:
                    gift.category = GiftCategory.BasicAttack;
                    gift.giftName = "처형";
                    gift.description = "체력 20% 이하 적에게 2배 피해";
                    break;
                case GiftType.Combo:
                    gift.category = GiftCategory.BasicAttack;
                    gift.giftName = "연격";
                    gift.description = "기본 공격 속도 +25%";
                    break;
                case GiftType.Critical:
                    gift.category = GiftCategory.BasicAttack;
                    gift.giftName = "치명타";
                    gift.description = "기본 공격 15% 확률로 2배 피해";
                    break;
                case GiftType.Explosion:
                    gift.category = GiftCategory.SpecialAttack;
                    gift.giftName = "확장";
                    gift.description = "특수 공격 범위 +50%";
                    break;
                case GiftType.RapidFire:
                    gift.category = GiftCategory.SpecialAttack;
                    gift.giftName = "간파";
                    gift.description = "특수 공격 10% 확률로 2배 피해";
                    break;
                case GiftType.Endurance:
                    gift.category = GiftCategory.SpecialAttack;
                    gift.giftName = "불굴";
                    gift.description = "특수 공격 피해 +30%";
                    break;
                case GiftType.Awakening:
                    gift.category = GiftCategory.Utility;
                    gift.giftName = "각성";
                    gift.description = "대쉬 후 1초간 다음 공격 2배 피해";
                    break;
                case GiftType.Vampirism:
                    gift.category = GiftCategory.Utility;
                    gift.giftName = "흡혈";
                    gift.description = "적 처치 시 HP 5 회복";
                    break;
                case GiftType.Berserk:
                    gift.category = GiftCategory.Utility;
                    gift.giftName = "광폭화";
                    gift.description = "HP 50% 이하일 때 피해 +40%";
                    break;
            }
        }
    }

    public void OpenGiftUI()
    {
        List<GiftInfo> availableGifts = allGifts.Where(g => !playerData.acquiredGifts.Contains(g.type)).ToList();

        if (availableGifts.Count == 0)
        {
            Debug.Log("더 이상 획득할 기프트가 없습니다!");
            return;
        }

        for (int i = 0; i < availableGifts.Count; i++)
        {
            GiftInfo temp = availableGifts[i];
            int randomIndex = Random.Range(i, availableGifts.Count);
            availableGifts[i] = availableGifts[randomIndex];
            availableGifts[randomIndex] = temp;
        }

        int optionsCount = Mathf.Min(3, availableGifts.Count);

        for (int i = 0; i < 3; i++)
        {
            if (i < optionsCount)
            {
                giftButtons[i].gameObject.SetActive(true);
                giftNameTexts[i].text = availableGifts[i].giftName;
                giftDescTexts[i].text = availableGifts[i].description;

                if (giftCategoryTexts != null && giftCategoryTexts.Length > i)
                {
                    giftCategoryTexts[i].text = GetCategoryString(availableGifts[i].category);
                }

                if (giftIconImages != null && giftIconImages.Length > i)
                {
                    Sprite categoryIcon = GetCategoryIcon(availableGifts[i].category);
                    if (categoryIcon != null)
                    {
                        giftIconImages[i].sprite = categoryIcon;
                        giftIconImages[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        giftIconImages[i].gameObject.SetActive(false); 
                    }
                }

                giftButtons[i].onClick.RemoveAllListeners();
                GiftType selectedType = availableGifts[i].type;
                giftButtons[i].onClick.AddListener(() => {
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Upgrade_Success");
                    SelectGift(selectedType);
                });
            }
            else
            {
                giftButtons[i].gameObject.SetActive(false);
            }
        }

        Time.timeScale = 0f;
        giftUIPanel.SetActive(true);
    }

    public void SelectGift(GiftType type)
    {
        playerData.acquiredGifts.Add(type);
        giftUIPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    private string GetCategoryString(GiftCategory category)
    {
        switch (category)
        {
            case GiftCategory.BasicAttack: return "기본 공격";
            case GiftCategory.SpecialAttack: return "특수 공격";
            case GiftCategory.Utility: return "유틸리티";
            default: return "알 수 없음";
        }
    }
    private Sprite GetCategoryIcon(GiftCategory category)
    {
        switch (category)
        {
            case GiftCategory.BasicAttack: return basicAttackIcon;
            case GiftCategory.SpecialAttack: return specialAttackIcon;
            case GiftCategory.Utility: return utilityIcon;
            default: return null;
        }
    }
}