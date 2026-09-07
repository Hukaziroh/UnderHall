using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SpawnManager : MonoBehaviour
{
    [Header("Wave Settings")]
    public TierData tierData;
    public GameObject magicCircleVFX;
    public GameObject spawnPoofVFX;
    public float spawnDelay = 1.5f;

    [Header("Room References")]
    public Door[] exitDoors;

    [Header("Reward Settings")]
    public GameObject rewardMaxHealthPrefab;
    public GameObject rewardGiftPrefab;
    public GameObject rewardGoldPrefab;

    [Header("Reward Icon Database")]
    public Sprite healthIcon;
    public Sprite giftIcon;
    public Sprite goldIcon;

    [HideInInspector]
    public RewardType currentRoomReward;

    private List<Transform> spawnPoints = new List<Transform>();
    private List<GameObject> activeEnemies = new List<GameObject>();

    private int currentWave = 1;
    private bool isSpawning = false;
    private bool isRoomCleared = false;
    private bool hasRoomStarted = false;

    private void Awake()
    {
        Transform[] allChildren = transform.root.GetComponentsInChildren<Transform>();

        foreach (Transform child in allChildren)
        {
            if (child.CompareTag("EnemySpawnPoint"))
            {
                bool isDuplicate = false;
                foreach (Transform existingPoint in spawnPoints)
                {
                    if (Vector3.Distance(existingPoint.position, child.position) < 0.1f)
                    {
                        isDuplicate = true;
                        break;
                    }
                }

                if (!isDuplicate)
                {
                    spawnPoints.Add(child);
                }
                else
                {
                    Debug.LogWarning($"[SpawnManager] 위치가 겹치는 스폰 포인트 발견! 중복 소환을 막기 위해 하나를 삭제 취급합니다. (위치: {child.position})");
                }
            }
        }
    }

    public void StartRoom()
    {
        if (tierData == null)
        {
            Debug.LogWarning("TierData가 없습니다! 바로 문을 엽니다.");
            ClearRoom();
            return;
        }
        hasRoomStarted = true;
        StartCoroutine(SpawnWaveRoutine(currentWave));
    }

    private void Update()
    {
        if (!hasRoomStarted || isRoomCleared || isSpawning) return;

        int beforeCount = activeEnemies.Count;
        activeEnemies.RemoveAll(enemy => enemy == null || !enemy.GetComponent<Collider>().enabled);

        if (activeEnemies.Count == 0)
        {
            currentWave++;

            if (currentWave > 3)
            {
                ClearRoom();
            }
            else
            {
                StartCoroutine(SpawnWaveRoutine(currentWave));
            }
        }
    }

    private IEnumerator SpawnWaveRoutine(int wave)
    {
        isSpawning = true;
        WaveData currentWaveData = null;

        if (wave == 1) currentWaveData = tierData.wave1;
        else if (wave == 2) currentWaveData = tierData.wave2;
        else if (wave == 3) currentWaveData = tierData.wave3;

        if (currentWaveData == null || currentWaveData.spawnInfos.Count == 0)
        {
            isSpawning = false;
            yield break;
        }

        List<AssetReference> enemiesToSpawn = new List<AssetReference>();

        // 💡 중복되던 부분 깔끔하게 하나로 통일!
        foreach (var info in currentWaveData.spawnInfos)
        {
            AssetReference enemyRef = tierData.GetEnemyPrefab(info.enemyType);

            if (enemyRef != null)
            {
                for (int i = 0; i < info.count; i++)
                {
                    enemiesToSpawn.Add(enemyRef);
                }
            }
        }

        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        foreach (AssetReference enemyRef in enemiesToSpawn)
        {
            if (availablePoints.Count == 0) break;

            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform selectedPoint = availablePoints[randomIndex];

            availablePoints.RemoveAt(randomIndex);
            StartCoroutine(SpawnSingleEnemy(enemyRef, selectedPoint.position, selectedPoint.rotation));
        }

        yield return new WaitForSeconds(spawnDelay + 0.1f);
        isSpawning = false;
    }

    private IEnumerator SpawnSingleEnemy(AssetReference enemyRef, Vector3 spawnPos, Quaternion spawnRot)
    {
        GameObject vfx = null;
        if (magicCircleVFX != null)
        {
            vfx = Instantiate(magicCircleVFX, spawnPos, Quaternion.Euler(0, 0, 0));
        }

        yield return new WaitForSeconds(spawnDelay);

        if (vfx != null) Destroy(vfx);
        Quaternion reversedRot = spawnRot * Quaternion.Euler(0f, -180f, 0f);

        if (spawnPoofVFX != null)
        {
            GameObject poof = Instantiate(spawnPoofVFX, spawnPos, Quaternion.Euler(-90, 0, 0));
            Destroy(poof, 2f);
        }

        // 어드레서블 비동기 소환
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(enemyRef, spawnPos, reversedRot);
        yield return handle;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            GameObject spawnedEnemy = handle.Result;
            activeEnemies.Add(spawnedEnemy);
        }
        else
        {
            Debug.LogError($"[SpawnManager] 어드레서블 몬스터 소환 실패!");
        }
    }

    private void ClearRoom()
    {
        isRoomCleared = true;
        if (GameManager.Instance != null && GameManager.Instance.IsGameCleared)
        {
            Debug.Log("[SpawnManager] 보스 클리어 상태이므로 보상 스폰 로직을 취소합니다.");
            return;
        }
        Debug.Log("[디버그 7] 모든 웨이브 클리어! 보상을 스폰합니다.");

        if (VFXManager.Instance != null)
            VFXManager.Instance.PlayRoomClear(transform.position);

        StartCoroutine(ClearRoomRoutine());
    }

    private IEnumerator ClearRoomRoutine()
    {
        if (VFXManager.Instance != null)
            VFXManager.Instance.PlayRoomClear(transform.position);

        yield return new WaitForSeconds(1.2f);

        StartCoroutine(SpawnRewardRoutine());
    }

    private IEnumerator SpawnRewardRoutine()
    {
        GameObject spawnPoint = GameObject.FindGameObjectWithTag("RewardSpawnPoint");
        Vector3 spawnPos = spawnPoint != null ? spawnPoint.transform.position : transform.position;

        GameObject prefabToSpawn = null;
        string spawnSoundName = "";

        switch (currentRoomReward)
        {
            case RewardType.MaxHealth:
                prefabToSpawn = rewardMaxHealthPrefab;
                spawnSoundName = "Reward_Health_Spawn";
                break;
            case RewardType.Gift:
                prefabToSpawn = rewardGiftPrefab;
                spawnSoundName = "Reward_Gift_Spawn";
                break;
            case RewardType.Gold:
                prefabToSpawn = rewardGoldPrefab;
                spawnSoundName = "Reward_Gold_Spawn";
                break;
        }

        if (prefabToSpawn != null)
        {
            if (VFXManager.Instance != null)
                VFXManager.Instance.PlayRewardAppear(spawnPos);

            yield return new WaitForSeconds(1.2f);

            GameObject rewardItem = Instantiate(prefabToSpawn, spawnPos, Quaternion.Euler(45, -45, 0));

            if (AudioManager.Instance != null && !string.IsNullOrEmpty(spawnSoundName))
            {
                AudioManager.Instance.PlaySFX(spawnSoundName);
            }

            StartCoroutine(FloatReward(rewardItem));

            RewardInteractable rewardScript = rewardItem.GetComponent<RewardInteractable>();
            if (rewardScript != null)
            {
                rewardScript.Initialize(this, currentRoomReward);
            }
        }
        else
        {
            Debug.LogWarning("보상 프리팹이 등록되지 않았습니다! 보상 없이 강제로 문을 엽니다.");
            OnRewardCollected();
        }
    }

    private IEnumerator FloatReward(GameObject reward)
    {
        if (reward == null) yield break;

        Vector3 basePos = reward.transform.position;
        float floatHeight = 0.3f;
        float floatSpeed = 2f;

        while (reward != null)
        {
            float newY = basePos.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            reward.transform.position = new Vector3(basePos.x, newY, basePos.z);
            yield return null;
        }
    }

    public void OnRewardCollected()
    {
        Debug.Log("보상 획득 완료! 다음 방 보상을 배정하고 문을 엽니다.");

        AssignNextRoomRewards();

        if (exitDoors != null && exitDoors.Length > 0)
        {
            foreach (Door door in exitDoors)
            {
                if (door != null) door.UnlockDoor();
            }
        }
    }

    private void AssignNextRoomRewards()
    {
        List<RewardType> availableRewards = new List<RewardType>
        {
            RewardType.MaxHealth,
            RewardType.Gift,
            RewardType.Gold
        };

        for (int i = 0; i < availableRewards.Count; i++)
        {
            RewardType temp = availableRewards[i];
            int randomIndex = Random.Range(i, availableRewards.Count);
            availableRewards[i] = availableRewards[randomIndex];
            availableRewards[randomIndex] = temp;
        }

        int rewardIndex = 0;
        foreach (Door door in exitDoors)
        {
            if (door != null)
            {
                if (rewardIndex >= availableRewards.Count)
                {
                    Debug.LogWarning("문의 개수가 보상 종류(3개)보다 많습니다. 일부 문은 보상이 세팅되지 않습니다.");
                    break;
                }

                RewardType selectedReward = availableRewards[rewardIndex];
                rewardIndex++;

                Sprite selectedSprite = null;
                switch (selectedReward)
                {
                    case RewardType.MaxHealth: selectedSprite = healthIcon; break;
                    case RewardType.Gift: selectedSprite = giftIcon; break;
                    case RewardType.Gold: selectedSprite = goldIcon; break;
                }

                door.SetNextRoomReward(selectedReward, selectedSprite);
            }
        }
    }
}