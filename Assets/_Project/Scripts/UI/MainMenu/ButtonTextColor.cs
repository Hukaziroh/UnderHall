using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonTextColor : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI label;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;

    public GameObject hoverIcon;

    void OnEnable()
    {
        ResetState();
    }

    void OnDisable()
    {
        ResetState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        label.color = hoverColor;
        if (hoverIcon != null)
            hoverIcon.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetState();
    }

    void ResetState()
    {
        if (label != null)
            label.color = normalColor;
        if (hoverIcon != null)
            hoverIcon.SetActive(false);
    }
}