using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player & UI")]
    public GameObject player;
    public CanvasGroup fadeCanvasGroup;
    public float fadeDuration = 1f;
    public float blackScreenDuration = 0.5f;

    [Header("Stage Settings")]
    public StageData stageData;

    [Header("Current State")]
    public GameObject currentMapInstance;
    private int currentRoomIndex = 0;
    private bool isTransitioning = false;
    public bool IsGameCleared { get; private set; } = false;

    private RewardType upcomingReward;
    public PlayerData playerData;

    private void Awake()
    {
        if (playerData != null) playerData.LoadFromDevice();
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        upcomingReward = (RewardType)Random.Range(0, 3);

        if (player != null)
        {
            Player p = player.GetComponent<Player>();
            if (p != null && p.playerData != null)
            {
                p.playerData.ResetRunData();
            }
        }

        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha = 1f;
            fadeCanvasGroup.blocksRaycasts = true; 
        }
    }

    private void Start()
    {
        playerData.ResetRunData();

        Debug.Log("[GameManager] 게임 시작됨! 첫 번째 방을 설정합니다.");
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM("Stage_BGM");
        }

        if (fadeCanvasGroup != null)
        {
            StartCoroutine(Fade(0f));
        }
        if (currentMapInstance == null)
        {
            SpawnManager foundManager = FindAnyObjectByType<SpawnManager>();
            if (foundManager != null)
            {
                currentMapInstance = foundManager.transform.root.gameObject;
            }
            else
            {
                Debug.LogError("[GameManager] 씬에 SpawnManager가 없습니다!");
            }
        }
        // ======================================================================
        // 창우_첫 번째 맵을 찾았으니 시작하자마자 카메라에 바운드를 강제 주입합니다!
        // ======================================================================
        if (currentMapInstance != null)
        {
            CameraConfinerAutoBinder autoBinder = FindAnyObjectByType<CameraConfinerAutoBinder>();
            if (autoBinder != null)
            {
                // 씬 전체에서 첫 번째 맵의 CameraBounds 오브젝트를 탐색
                GameObject boundsObj = GameObject.FindWithTag("CameraBounds");
                if (boundsObj != null)
                {
                    Collider targetCol = boundsObj.GetComponent<Collider>();
                    if (targetCol != null)
                    {
                        autoBinder.AssignBounds(targetCol); // 첫 방 카메라 영역 강제 셋팅!
                    }
                }
                else
                {
                    Debug.LogWarning("[GameManager] 첫 맵에서 'CameraBounds' 태그 오브젝트를 찾지 못했습니다.");
                }
            }
        }
        // ======================================================================
        if (currentMapInstance != null)
        {
            SpawnManager spawnManager = currentMapInstance.GetComponentInChildren<SpawnManager>();
            if (spawnManager != null)
            {
                spawnManager.currentRoomReward = upcomingReward;
                spawnManager.StartRoom();
            }
        }
    }

    // Door에서 넘겨준 보상 타입을 받아옵니다.
    public void GoToNextRoom(RewardType selectedReward)
    {
        if (isTransitioning) return;

        upcomingReward = selectedReward;
        currentRoomIndex++;

        if (stageData != null && currentRoomIndex < stageData.roomSequence.Count)
        {
            GameObject nextMapToLoad = stageData.roomSequence[currentRoomIndex];
            StartCoroutine(MapTransitionRoutine(nextMapToLoad));
        }
        else
        {
            Debug.Log("모든 스테이지 클리어! 클리어 연출을 시작합니다.");
            ClearGame();
        }
    }
    public void ClearGame()
    {
        if (!isTransitioning)
        {
            IsGameCleared = true;
            StartCoroutine(GameClearRoutine());
        }
    }

    private IEnumerator GameClearRoutine()
    {
        isTransitioning = true; 

        Player p = player.GetComponent<Player>();
        if (p != null)
        {
            p.attack.CancelAttack(); 
            p.ForceStop();

            if (p.TryGetComponent<UnityEngine.InputSystem.PlayerInput>(out var input))
            {
                input.enabled = false; 
            }
            float baseClearGold = 200f;
            float currentMultiplier = p.playerData.goldGainMultiplier;
            int finalBonusGold = Mathf.RoundToInt(baseClearGold * currentMultiplier);
            p.CommitGoldToSO();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Clear_BGM");
        }

        yield return new WaitForSeconds(4f);

        if (fadeCanvasGroup != null)
        {
            yield return StartCoroutine(Fade(1f));
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
    private IEnumerator MapTransitionRoutine(GameObject nextMapPrefab)
    {
        isTransitioning = true;

        Player p = player.GetComponent<Player>();
        if (p != null) p.attack.CancelAttack();

        yield return StartCoroutine(Fade(1f));

        if (stageData != null && currentRoomIndex == stageData.roomSequence.Count - 1)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayBGM("Boss_BGM"); 
                Debug.Log("보스방 진입! 보스 브금을 재생합니다.");
            }
        }

        if (currentMapInstance != null)
        {
            Destroy(currentMapInstance);
            yield return null;
        }

        currentMapInstance = Instantiate(nextMapPrefab, Vector3.zero, Quaternion.identity);


        //창우_CameraConfinerAutoBinder 찾아서 새 맵의 카메라 바운드 콜라이더 주입하기
        CameraConfinerAutoBinder autoBinder = FindAnyObjectByType<CameraConfinerAutoBinder>();
        if (autoBinder != null)
        {
            // 생성된 맵 인스턴스 안에서 "CameraBounds" 태그를 가진 오브젝트를 찾습니다.
            GameObject boundsObj = GameObject.FindWithTag("CameraBounds");
            if (boundsObj != null)
            {
                Collider targetCol = boundsObj.GetComponent<Collider>();
                if (targetCol != null)
                {
                    // 수동으로 카메라에게 새 콜라이더 주입!
                    autoBinder.AssignBounds(targetCol);
                }
            }
            else
            {
                Debug.LogWarning("[GameManager] 새 맵에서 'CameraBounds' 태그를 가진 오브젝트를 찾지 못했습니다.");
            }
        }

        Transform spawnPoint = currentMapInstance.transform.Find("SpawnPoint");
        if (spawnPoint != null)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            Vector3 safePos = spawnPoint.position + new Vector3(0f, 1.0f, 0f);

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                player.transform.position = safePos;
                rb.position = safePos;
                rb.rotation = spawnPoint.rotation;
                Physics.SyncTransforms();
            }
            else
            {
                player.transform.position = safePos;
                player.transform.rotation = spawnPoint.rotation;
                Physics.SyncTransforms();
            }
        }

        yield return new WaitForSeconds(blackScreenDuration);
        yield return StartCoroutine(Fade(0f));

        SpawnManager spawnManager = currentMapInstance.GetComponentInChildren<SpawnManager>();
        if (spawnManager != null)
        {
            spawnManager.currentRoomReward = upcomingReward;
            spawnManager.StartRoom();
        }

        isTransitioning = false;
    }

    public IEnumerator Fade(float targetAlpha)
    {
        if (fadeCanvasGroup == null) yield break;
        float startAlpha = fadeCanvasGroup.alpha;
        float time = 0f;

        fadeCanvasGroup.blocksRaycasts = true;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = targetAlpha;

        if (targetAlpha == 0f)
        {
            fadeCanvasGroup.blocksRaycasts = false;
        }
    }
}