using UnityEngine;
using TMPro;

public class MetaUpgradePanel : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;

    private void OnEnable()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnUpgradeChanged += RefreshGold;
            RefreshGold();
        }
    }

    private void OnDisable()
    {
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.OnUpgradeChanged -= RefreshGold;
    }

    private void RefreshGold()
    {
        goldText.text = $"{LobbyManager.Instance.playerData.currentGold:N0} G";
    }
}