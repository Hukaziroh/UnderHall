using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "Game/Player Data")]
public class PlayerData : ScriptableObject
{
    [Header("===== 기본 스탯 =====")]

    [Tooltip("최대 체력")]
    [Range(50, 500)]
    public float maxHealth = 100f;

    [Tooltip("기본 공격 데미지")]
    [Range(1, 100)]
    public float damage = 10f;

    [Tooltip("이동 속도")]
    [Range(1, 20)]
    public float moveSpeed = 7.5f;

    [Header("===== 공격 =====")]

    [Tooltip("기본 공격 쿨다운 (초)")]
    [Range(0.1f, 3f)]
    public float attackCooldown = 0.5f;

    [Tooltip("기본 공격 사거리")]
    [Range(0.5f, 10f)]
    public float attackRange = 1.5f;

    [Tooltip("특수 공격 데미지 배율")]
    [Range(0.1f, 5f)]
    public float specialAttackMultiplier = 3f;

    [Tooltip("특수 공격 타격 반경")]
    [Range(1f, 10f)]
    public float specialAttackRange = 3f;

    [Tooltip("특수 공격 쿨다운 (초)")]
    [Range(1f, 30f)]
    public float specialAttackCooldown = 5f;

    [Header("===== 대쉬 =====")]

    [Tooltip("대쉬 속도")]
    [Range(5f, 30f)]
    public float dashSpeed = 15f;

    [Tooltip("대쉬 지속 시간 (초)")]
    [Range(0.1f, 1f)]
    public float dashDuration = 0.2f;

    [Tooltip("대쉬 횟수")]
    [Range(1, 4)]
    public int maxDashCount = 2;

    [Tooltip("대쉬 후 다시 사용 가능까지 시간 (초)")]
    [Range(0.1f, 5f)]
    public float dashCooldown = 1f;

    [Tooltip("대쉬 중 무적 시간 (초)")]
    [Range(0.05f, 1f)]
    public float dashInvincibilityTime = 0.2f;

    [Header("===== 피격 =====")]

    [Tooltip("피격 후 무적 시간 (초)")]
    [Range(0.1f, 2f)]
    public float hitInvincibilityTime = 0.5f;

    [Header("===== 부활 (죽음 도전) =====")]
    [Tooltip("최대 부활 가능 횟수")]
    public int maxResurrectionCount = 0;

    [Tooltip("부활 시 회복될 체력 비율 (0.2 = 20%)")]
    [Range(0.1f, 1f)]
    public float resurrectionHealthPercent = 0f;

    [Tooltip("부활 직후 무적 시간")]
    public float resurrectionInvincibilityTime = 2f;

    [Header("===== 재화 및 메타 강화 =====")]
    [Tooltip("현재 보유 중인 골드")]
    public int currentGold = 5000; 

    [Tooltip("골드 획득량 배율 (기본 1.0 = 100%)")]
    public float goldGainMultiplier = 1.0f;

    [Header("강화 레벨 (0이 기본상태)")]
    public int levelHP = 0;
    public int levelATK = 0;
    public int levelDash = 0;
    public int levelGold = 0;
    public int levelRevive = 0;
    public int levelSpeed = 0;

    [Header("===== 인게임 획득 기프트 (현재 런) =====")]
    public System.Collections.Generic.List<GiftType> acquiredGifts = new System.Collections.Generic.List<GiftType>();

    // PlayerData.cs 파일의 맨 아래쪽에 이 함수를 덮어씌우세요.
    public void ResetRunData()
    {
        acquiredGifts.Clear();

        float[] bonusHP = { 0, 10, 20, 35, 50, 70 };
        float[] bonusATK = { 0, 2, 4, 7, 10, 15 };
        float[] bonusGold = { 0f, 0.1f, 0.2f, 0.3f, 0.5f, 1.0f };
        float[] setRevive = { 0f, 0.2f, 0.4f, 0.6f, 0.8f, 1.0f };
        float[] bonusSpeed = { 0f, 0.5f, 1.0f, 1.5f, 2.0f, 2.5f }; 

        maxHealth = 100f + bonusHP[levelHP];
        damage = 10f + bonusATK[levelATK];
        maxDashCount = 2 + levelDash;
        goldGainMultiplier = 1.0f + bonusGold[levelGold];
        maxResurrectionCount = (levelRevive > 0) ? 1 : 0;
        resurrectionHealthPercent = setRevive[levelRevive];

        moveSpeed = 7.5f + bonusSpeed[levelSpeed];

        Debug.Log("[PlayerData] 모든 인게임 데이터가 초기화되었습니다.");
    }

    public void SaveToDevice()
    {
        PlayerPrefs.SetInt("Meta_Gold", currentGold);
        PlayerPrefs.SetInt("Meta_LevelHP", levelHP);
        PlayerPrefs.SetInt("Meta_LevelATK", levelATK);
        PlayerPrefs.SetInt("Meta_LevelDash", levelDash);
        PlayerPrefs.SetInt("Meta_LevelGold", levelGold);
        PlayerPrefs.SetInt("Meta_LevelRevive", levelRevive);
        PlayerPrefs.SetInt("Meta_LevelSpeed", levelSpeed); 
        PlayerPrefs.Save();
        Debug.Log("[PlayerData] 기기에 데이터 영구 저장 완료!");
    }

    public void LoadFromDevice()
    {
        currentGold = PlayerPrefs.GetInt("Meta_Gold", 0);
        levelHP = PlayerPrefs.GetInt("Meta_LevelHP", 0);
        levelATK = PlayerPrefs.GetInt("Meta_LevelATK", 0);
        levelDash = PlayerPrefs.GetInt("Meta_LevelDash", 0);
        levelGold = PlayerPrefs.GetInt("Meta_LevelGold", 0);
        levelRevive = PlayerPrefs.GetInt("Meta_LevelRevive", 0);
        levelSpeed = PlayerPrefs.GetInt("Meta_LevelSpeed", 0); 
        Debug.Log("[PlayerData] 기기에서 데이터를 성공적으로 불러왔습니다!");
    }
}