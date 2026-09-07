using UnityEngine;

public class Door : MonoBehaviour
{
    private bool isLocked = true;
    public bool IsLocked => isLocked;

    [Header("Next Room Reward")]
    public RewardType nextRewardType;

    [Header("Visual Settings")]
    public SpriteRenderer rewardIconRenderer;

    //창우_양문 컨트롤 추가
    [Header("양문 컨트롤러")]
    public DoubleDoorController doubleDoor;

    [Header("Boss Door Settings")]
    public bool isBossDoor = false;   // 보스방으로 가는 문이면 체크

    public void SetNextRoomReward(RewardType type, Sprite icon)
    {
        if (isBossDoor) return; // 보스문이면 보상체크 무시

        nextRewardType = type;

        if (rewardIconRenderer != null)
        {
            rewardIconRenderer.sprite = icon;
        }
    }

   
    public void UnlockDoor()
    {
        isLocked = false;
        if (rewardIconRenderer != null) rewardIconRenderer.color = Color.white;
        Debug.Log($"문이 열렸습니다! (다음 방 보상: {nextRewardType})");


        //창우_양문 컨트롤러 열기 호출
        if (doubleDoor != null)
            doubleDoor.OpenDoor();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySFX("Door_Open");
        }
    }

    public void Interact()
    {
        GameManager.Instance.GoToNextRoom(nextRewardType);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Player player = other.GetComponent<Player>();
        if (player != null) player.SetNearbyDoor(this);

        if (!isLocked && UIManager.Instance != null)
            UIManager.Instance.ShowInteractPrompt();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        Player player = other.GetComponent<Player>();
        if (player != null) player.ClearNearbyDoor(this);

        if (UIManager.Instance != null)
            UIManager.Instance.HideInteractPrompt();
    }

    void OnDestroy()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.HideInteractPrompt();
    }
}