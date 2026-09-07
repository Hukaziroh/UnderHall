using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }
    public PlayerData playerData;
    public System.Action OnUpgradeChanged;

    [Header("강화 비용 (골드)")]
    private readonly int[] cost5Levels = { 50, 100, 200, 400, 800 };
    private readonly int costDash = 300;

    [Header("강화 수치 (인덱스 0은 기본 상태)")]
    private readonly float[] bonusHP = { 0, 10, 20, 35, 50, 70 };
    private readonly float[] bonusATK = { 0, 2, 4, 7, 10, 15 };
    private readonly float[] bonusGold = { 0f, 0.1f, 0.2f, 0.3f, 0.5f, 1.0f };
    private readonly float[] setRevive = { 0f, 0.2f, 0.4f, 0.6f, 0.8f, 1.0f };
    private readonly float[] bonusSpeed = { 0f, 0.5f, 1.0f, 1.5f, 2.0f, 2.5f };

    [Header("기본 스탯 (기준점)")]
    private readonly float baseMaxHealth = 100f;
    private readonly float baseDamage = 10f;
    private readonly int baseMaxDashCount = 2;
    private readonly float baseGoldMultiplier = 1.0f;
    private readonly float baseMoveSpeed = 5f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (playerData != null)
        {
            playerData.LoadFromDevice();
        }
        ApplyAllUpgrades();
    }

    // 1. 강인한 신체 (HP)
    public void BuyUpgradeHP()
    {
        if (playerData.levelHP < 5 && playerData.currentGold >= cost5Levels[playerData.levelHP])
        {
            playerData.currentGold -= cost5Levels[playerData.levelHP];
            playerData.levelHP++;
            ApplyAllUpgrades();
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Upgrade_Success");
            Debug.Log($"[강화 성공] 체력 증가! (Lv.{playerData.levelHP}) 남은 골드: {playerData.currentGold}");
            Debug.Log($"Max HP: {playerData.maxHealth} (Lv.{playerData.levelHP})");
        }
        else Debug.LogWarning("골드가 부족하거나 이미 만렙입니다!");
    }

    // 2. 날카로운 검 (ATK)
    public void BuyUpgradeATK()
    {
        if (playerData.levelATK < 5 && playerData.currentGold >= cost5Levels[playerData.levelATK])
        {
            playerData.currentGold -= cost5Levels[playerData.levelATK];
            playerData.levelATK++;
            ApplyAllUpgrades();
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Upgrade_Success");
            Debug.Log($"[강화 성공] 공격력 증가! (Lv.{playerData.levelATK}) 남은 골드: {playerData.currentGold}");
            Debug.Log($"ATK: {playerData.damage} (Lv.{playerData.levelATK})");
        }
        else Debug.LogWarning("골드가 부족하거나 이미 만렙입니다!");
    }

    // 3. 이중 도약 (DASH)
    public void BuyUpgradeDASH()
    {
        if (playerData.levelDash < 1 && playerData.currentGold >= costDash)
        {
            playerData.currentGold -= costDash;
            playerData.levelDash++;
            ApplyAllUpgrades();
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Upgrade_Success");
            Debug.Log($"[강화 성공] 이중 도약 획득! (Lv.{playerData.levelDash}) 남은 골드: {playerData.currentGold}");
            Debug.Log($"Dash Count: {playerData.maxDashCount} (Lv.{playerData.levelDash})");
        }
        else Debug.LogWarning("골드가 부족하거나 이미 만렙입니다!");
    }

    // 4. 황금 손길 (GOLD)
    public void BuyUpgradeGOLD()
    {
        if (playerData.levelGold < 5 && playerData.currentGold >= cost5Levels[playerData.levelGold])
        {
            playerData.currentGold -= cost5Levels[playerData.levelGold];
            playerData.levelGold++;
            ApplyAllUpgrades();
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Upgrade_Success");
            Debug.Log($"[강화 성공] 골드 획득량 증가! (Lv.{playerData.levelGold}) 남은 골드: {playerData.currentGold}");
            Debug.Log($"Gold Multiplier: x{playerData.goldGainMultiplier} (Lv.{playerData.levelGold})");
        }
        else Debug.LogWarning("골드가 부족하거나 이미 만렙입니다!");
    }

    // 5. 불사의 가호 (REVIVE)
    public void BuyUpgradeREVIVE()
    {
        if (playerData.levelRevive < 5 && playerData.currentGold >= cost5Levels[playerData.levelRevive])
        {
            playerData.currentGold -= cost5Levels[playerData.levelRevive];
            playerData.levelRevive++;
            
            ApplyAllUpgrades();
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Upgrade_Success");
            Debug.Log($"[강화 성공] 부활 체력 증가! (Lv.{playerData.levelRevive}) 남은 골드: {playerData.currentGold}");
            Debug.Log($"Revive HP: {playerData.resurrectionHealthPercent * 100}% (Lv.{playerData.levelRevive})");
        }
        else Debug.LogWarning("골드가 부족하거나 이미 만렙입니다!");
    }
    // 6. 이속증가
    public void BuyUpgradeSPEED()
    {
        if (playerData.levelSpeed < 5 && playerData.currentGold >= cost5Levels[playerData.levelSpeed])
        {
            playerData.currentGold -= cost5Levels[playerData.levelSpeed];
            playerData.levelSpeed++;

            ApplyAllUpgrades();
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX("Upgrade_Success");
            Debug.Log($"[강화 성공] 이동 속도 증가! (Lv.{playerData.levelSpeed}) 남은 골드: {playerData.currentGold}");
            Debug.Log($"Move Speed: {playerData.moveSpeed} (Lv.{playerData.levelSpeed})");
        }
        else Debug.LogWarning("골드가 부족하거나 이미 만렙입니다!");
    }

    // 전체 스탯 적용 (강화를 누를 때마다 최종 스탯 계산)
    private void ApplyAllUpgrades()
    {
        playerData.ResetRunData();

        playerData.maxHealth = baseMaxHealth + bonusHP[playerData.levelHP];
        playerData.damage = baseDamage + bonusATK[playerData.levelATK];
        playerData.maxDashCount = baseMaxDashCount + playerData.levelDash;
        playerData.goldGainMultiplier = baseGoldMultiplier + bonusGold[playerData.levelGold];
        playerData.maxResurrectionCount = (playerData.levelRevive > 0) ? 1 : 0;
        playerData.resurrectionHealthPercent = setRevive[playerData.levelRevive];

        playerData.moveSpeed = baseMoveSpeed + bonusSpeed[playerData.levelSpeed]; 

        Debug.Log("==== 현재 플레이어 스탯 현황 ====");
        Debug.Log($"Max HP: {playerData.maxHealth} (Lv.{playerData.levelHP})");
        Debug.Log($"ATK: {playerData.damage} (Lv.{playerData.levelATK})");
        Debug.Log($"Dash Count: {playerData.maxDashCount} (Lv.{playerData.levelDash})");
        Debug.Log($"Gold Multiplier: x{playerData.goldGainMultiplier} (Lv.{playerData.levelGold})");
        Debug.Log($"Revive HP: {playerData.resurrectionHealthPercent * 100}% (Lv.{playerData.levelRevive})");
        Debug.Log($"Move Speed: {playerData.moveSpeed} (Lv.{playerData.levelSpeed})");

        playerData.SaveToDevice();
        OnUpgradeChanged?.Invoke();
    }

    // === UI에서 값 읽기 위한 Getter ===
    public int GetCost5Level(int currentLevel)
    {
        if (currentLevel < 0 || currentLevel >= cost5Levels.Length) return 0;
        return cost5Levels[currentLevel];
    }

    public int GetCostDash() => costDash;

    public float GetBonusHP(int level) => bonusHP[level];
    public float GetBonusATK(int level) => bonusATK[level];
    public float GetBonusGold(int level) => bonusGold[level];
    public float GetSetRevive(int level) => setRevive[level];
    public float GetBonusSpeed(int level) => bonusSpeed[level];
}