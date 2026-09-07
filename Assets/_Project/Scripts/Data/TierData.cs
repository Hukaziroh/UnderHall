using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets; // 💡 어드레서블 네임스페이스 추가

public enum EnemyType
{
    Anubis,
    Bruiser,
    Range,
    Dragon
}

[System.Serializable]
public class EnemyPrefabMapping
{
    public EnemyType enemyType;
    // 💡 GameObject에서 AssetReference로 변경되었습니다.
    public AssetReference enemyPrefab;
}

[System.Serializable]
public class SpawnInfo
{
    public EnemyType enemyType;
    public int count;
}

[System.Serializable]
public class WaveData
{
    public List<SpawnInfo> spawnInfos;
}

[CreateAssetMenu(fileName = "TierData", menuName = "Scriptable Objects/TierData")]
public class TierData : ScriptableObject
{
    [Header("===== 몬스터 프리팹 등록 (어드레서블) =====")]
    public List<EnemyPrefabMapping> enemyPool;

    [Header("===== 웨이브 설정 (드롭다운) =====")]
    public WaveData wave1;
    public WaveData wave2;
    public WaveData wave3;

    // 💡 반환형을 GameObject에서 AssetReference로 변경했습니다.
    public AssetReference GetEnemyPrefab(EnemyType type)
    {
        foreach (var mapping in enemyPool)
        {
            if (mapping.enemyType == type)
            {
                return mapping.enemyPrefab;
            }
        }

        Debug.LogWarning($"[TierData] {type} 에 해당하는 프리팹이 최상단에 등록되지 않았습니다!");
        return null;
    }
}