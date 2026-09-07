using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("===== 기본 정보 =====")]

    [Tooltip("적 이름")]
    public string enemyName;
    
    [Tooltip("적 타입 (분류용)")]
    public EnemyType enemyType;

    [Header("===== 스탯 =====")]

    [Tooltip("최대 체력")]
    [Range(1, 3000)]
    public float maxHealth = 30f;

    [Tooltip("이동 속도")]
    [Range(0.5f, 10f)]
    public float moveSpeed = 3f;

    [Header("===== 공격 =====")]

    [Tooltip("공격 데미지")]
    [Range(1, 50)]
    public float damage = 5f;

    [Tooltip("공격 사거리")]
    [Range(0.5f, 20f)]
    public float attackRange = 1.5f;

    [Tooltip("공격 쿨다운 (초)")]
    [Range(0.5f, 5f)]
    public float attackCooldown = 1f;

    [Header("===== AI =====")]

    [Tooltip("플레이어 감지 거리")]
    [Range(1, 100)]
    public float detectionRange = 10f;

    [Header("Sound Settings")]
    public string attackSoundName = "Enemy_Attack"; 
    public string deathSoundName = "Enemy_Death";
}

