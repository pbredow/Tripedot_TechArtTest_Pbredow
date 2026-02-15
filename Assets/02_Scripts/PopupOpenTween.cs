using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public class PopupOpenTween : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Only this transform animates. Keep blur/background outside this target.")]
    public RectTransform popupRoot;

    [Header("Open Tween")]
    public float duration = 0.28f;
    public float startYOffset = 40f;
    public float startScale = 0.94f;
    public bool ignoreTimeScale = true;

    private CanvasGroup _canvasGroup;
    private Sequence _sequence;
    private Vector2 _targetAnchoredPosition;
    private Vector3 _targetScale;

    private void Reset()
    {
        AutoAssignRefs();
    }

    private void Awake()
    {
        AutoAssignRefs();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
            return;

        PlayOpen();
    }

    private void OnDisable()
    {
        _sequence?.Kill();
        _sequence = null;
    }

    private void AutoAssignRefs()
    {
        if (popupRoot == null)
        {
            Transform popup = transform.Find("Popup");
            popupRoot = popup as RectTransform;
        }

        if (popupRoot == null)
            popupRoot = transform as RectTransform;
    }

    private void PlayOpen()
    {
        if (popupRoot == null)
            return;

        _sequence?.Kill();
        _sequence = null;

        _targetAnchoredPosition = popupRoot.anchoredPosition;
        _targetScale = popupRoot.localScale;

        _canvasGroup = popupRoot.GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = popupRoot.gameObject.AddComponent<CanvasGroup>();

        popupRoot.anchoredPosition = _targetAnchoredPosition + Vector2.down * startYOffset;
        popupRoot.localScale = _targetScale * startScale;
        _canvasGroup.alpha = 0f;

        _sequence = DOTween.Sequence();
        _sequence.SetUpdate(ignoreTimeScale);
        _sequence.Join(popupRoot.DOAnchorPos(_targetAnchoredPosition, duration).SetEase(Ease.OutCubic));
        _sequence.Join(popupRoot.DOScale(_targetScale, duration).SetEase(Ease.OutBack));
        _sequence.Join(_canvasGroup.DOFade(1f, duration * 0.85f).SetEase(Ease.OutQuad));
    }
}
