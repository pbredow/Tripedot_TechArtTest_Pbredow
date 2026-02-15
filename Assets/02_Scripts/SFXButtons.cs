using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(Play);
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(Play);
    }

    void Play()
    {
        if (UISoundPlayer.Instance != null)
            UISoundPlayer.Instance.PlayClick();
    }
}