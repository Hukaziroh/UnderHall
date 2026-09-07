using UnityEngine;

public class RewardInteractable : MonoBehaviour
{
    public RewardType rewardType;
    private SpawnManager currentRoomManager;
    private bool isCollected = false;

    public void Initialize(SpawnManager manager, RewardType type)
    {
        currentRoomManager = manager;
        rewardType = type;
    }

    public void Interact(Player player)
    {
        if (isCollected) return;
        isCollected = true;

        ApplyReward(player);

        if (currentRoomManager != null)
        {
            currentRoomManager.OnRewardCollected();
        }

        if (UIManager.Instance != null) UIManager.Instance.HideInteractPrompt();
        Destroy(gameObject);
    }

    private void ApplyReward(Player player)
    {
        if (rewardType == RewardType.MaxHealth)
        {
            player.playerData.maxHealth += 25f;
            player.Heal(25f);
        }
        else if (rewardType == RewardType.Gift)
        {
            GiftManager giftManager = FindAnyObjectByType<GiftManager>();
            if (giftManager != null) giftManager.OpenGiftUI();
        }
        else if (rewardType == RewardType.Gold)
        {
            int goldAmount = 100; 
            goldAmount = Mathf.RoundToInt(goldAmount * player.playerData.goldGainMultiplier);
            player.AddGold(goldAmount);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null) player.SetNearbyReward(this);

            if (UIManager.Instance != null) UIManager.Instance.ShowInteractPrompt();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Player player = other.GetComponent<Player>();
            if (player != null) player.ClearNearbyReward(this);

            if (UIManager.Instance != null) UIManager.Instance.HideInteractPrompt();
        }
    }
}