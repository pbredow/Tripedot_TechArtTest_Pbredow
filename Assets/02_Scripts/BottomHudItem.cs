using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BottomHudItem : MonoBehaviour
{
    public Button button;
    public RectTransform rect;   // BottomHud_Button rect
    public RectTransform icon;
    public TMP_Text label;

    void Awake()
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        if (button == null) button = GetComponent<Button>();
    }
}