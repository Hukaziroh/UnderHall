using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeCardUI : MonoBehaviour
{
    public enum UpgradeType { HP, ATK, Dash, Gold, Revive, Speed }

    [Header("이 카드가 담당할 강화")]
    [SerializeField] private UpgradeType upgradeType;

    [Header("UI 참조")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;

    [Header("아이콘")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite iconSprite;

    private void OnEnable()
    {
        Debug.Log($"[OnEnable] {gameObject.name} - LobbyManager InstanceID: {LobbyManager.Instance?.GetInstanceID()}");
        buyButton.onClick.AddListener(OnBuyClicked);

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnUpgradeChanged += Refresh;
            Refresh();
        }
    }

    private void OnDisable()
    {
        buyButton.onClick.RemoveListener(OnBuyClicked);
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.OnUpgradeChanged -= Refresh;
    }

    private void OnBuyClicked()
    {
        var lm = LobbyManager.Instance;
        switch (upgradeType)
        {
            case UpgradeType.HP: lm.BuyUpgradeHP(); break;
            case UpgradeType.ATK: lm.BuyUpgradeATK(); break;
            case UpgradeType.Dash: lm.BuyUpgradeDASH(); break;
            case UpgradeType.Gold: lm.BuyUpgradeGOLD(); break;
            case UpgradeType.Revive: lm.BuyUpgradeREVIVE(); break;
            case UpgradeType.Speed: lm.BuyUpgradeSPEED(); break;
        }
    }

    private void Refresh()
    {
        Debug.Log($"[Card Refresh] type: {upgradeType}");
        var lm = LobbyManager.Instance;
        var pd = lm.playerData;

        int level = GetCurrentLevel(pd);
        int maxLevel = (upgradeType == UpgradeType.Dash) ? 1 : 5;
        int cost = GetCost(level, maxLevel, lm);

        nameText.text = GetName();
        descriptionText.text = GetDescription();
        effectText.text = GetEffectPreview(level, maxLevel, lm);
        levelText.text = $"Lv. {level}/{maxLevel}";
        if (iconSprite != null) iconImage.sprite = iconSprite;
        if (cost < 0)
        {
            costText.text = "MAX";
            buyButtonText.text = "완료";
            buyButton.interactable = false;
        }
        else
        {
            costText.text = $"{cost} G";
            buyButtonText.text = "구매";
            buyButton.interactable = (pd.currentGold >= cost);
        }
    }

    private int GetCurrentLevel(PlayerData pd)
    {
        switch (upgradeType)
        {
            case UpgradeType.HP: return pd.levelHP;
            case UpgradeType.ATK: return pd.levelATK;
            case UpgradeType.Dash: return pd.levelDash;
            case UpgradeType.Gold: return pd.levelGold;
            case UpgradeType.Revive: return pd.levelRevive;
            case UpgradeType.Speed: return pd.levelSpeed;
        }
        return 0;
    }

    private int GetCost(int level, int maxLevel, LobbyManager lm)
    {
        if (level >= maxLevel) return -1;
        return (upgradeType == UpgradeType.Dash) ? lm.GetCostDash() : lm.GetCost5Level(level);
    }

    private string GetName()
    {
        switch (upgradeType)
        {
            case UpgradeType.HP: return "강인한 신체";
            case UpgradeType.ATK: return "날카로운 검";
            case UpgradeType.Dash: return "삼중 도약";
            case UpgradeType.Gold: return "황금 손길";
            case UpgradeType.Revive: return "불사의 가호";
            case UpgradeType.Speed: return "신속한 발";
        }
        return "";
    }

    private string GetDescription()
    {
        switch (upgradeType)
        {
            case UpgradeType.HP: return "최대 체력 증가량";
            case UpgradeType.ATK: return "공격력 증가량";
            case UpgradeType.Dash: return "연속 대쉬 +1";
            case UpgradeType.Gold: return "골드 획득량 증가";
            case UpgradeType.Revive: return "사망 시 1회 부활";
            case UpgradeType.Speed: return "이동 속도 증가량";
        }
        return "";
    }

    private string GetEffectPreview(int level, int maxLevel, LobbyManager lm)
    {
        switch (upgradeType)
        {
            case UpgradeType.HP:
                if (level >= maxLevel) return $"+{lm.GetBonusHP(level)} (MAX)";
                return $"+{lm.GetBonusHP(level)} → +{lm.GetBonusHP(level + 1)}";
            case UpgradeType.ATK:
                if (level >= maxLevel) return $"+{lm.GetBonusATK(level)} (MAX)";
                return $"+{lm.GetBonusATK(level)} → +{lm.GetBonusATK(level + 1)}";
            case UpgradeType.Dash:
                if (level >= maxLevel) return "+1 대쉬 (MAX)";
                return "+1 대쉬 획득";
            case UpgradeType.Gold:
                if (level >= maxLevel) return $"+{lm.GetBonusGold(level) * 100:0}% (MAX)";
                return $"+{lm.GetBonusGold(level) * 100:0}% → +{lm.GetBonusGold(level + 1) * 100:0}%";
            case UpgradeType.Revive:
                if (level >= maxLevel) return $"{lm.GetSetRevive(level) * 100:0}% (MAX)";
                return $"{lm.GetSetRevive(level) * 100:0}% → {lm.GetSetRevive(level + 1) * 100:0}%";
            case UpgradeType.Speed:
                if (level >= maxLevel) return $"+{lm.GetBonusSpeed(level)} (MAX)";
                return $"+{lm.GetBonusSpeed(level)} → +{lm.GetBonusSpeed(level + 1)}";
        }
        return "";
    }
}