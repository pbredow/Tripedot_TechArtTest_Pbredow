using System;
using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

/// <summary>
/// Adds toggle behaviour and appear/disappear animations to the bottom bar.
///   - Tapping an OFF button  → selects it, plays toggle-on anim, fires <see cref="ContentActivated"/>.
///   - Tapping the ON  button → deselects it, plays toggle-off anim, fires <see cref="Closed"/>.
///   - Locked buttons are non-interactable (handled by <see cref="BottomHudItem"/>).
///   - <see cref="Appear"/> / <see cref="Disappear"/> slide + fade the entire bar.
/// Attach to the same GameObject as <see cref="BottomHudController"/>
/// (or assign it manually in the Inspector).
/// </summary>
public class BottomBarView : MonoBehaviour
{
    [SerializeField] private BottomHudController controller;

    [Serializable] public class ContentActivatedEvent : UnityEvent<int> { }

    [Header("Events")]
    [Tooltip("Fired when a button toggles ON. Parameter is the button index.")]
    public ContentActivatedEvent ContentActivated = new ContentActivatedEvent();

    [Tooltip("Fired when the active button toggles OFF (nothing selected).")]
    public UnityEvent Closed = new UnityEvent();

    // ─── Bar Appear / Disappear ─────────────────────────

    [Header("Bar Appear / Disappear")]
    [Tooltip("The transform to animate. Defaults to this RectTransform.")]
    [SerializeField] private RectTransform barRoot;

    public float appearDuration = 0.35f;
    public float appearSlideY = 120f;
    public Ease appearEase = Ease.OutCubic;

    public float disappearDuration = 0.25f;
    public Ease disappearEase = Ease.InCubic;

    // ─── Private state ──────────────────────────────────

    private CanvasGroup _barCanvasGroup;
    private Vector2 _barRestPosition;
    private bool _barPositionCached;
    private Sequence _barSequence;
    private UnityAction[] _clickActions;

    // ─── Lifecycle ──────────────────────────────────────

    void Awake()
    {
        if (controller == null)
            controller = GetComponent<BottomHudController>();

        if (controller == null)
        {
            Debug.LogError("BottomBarView: No BottomHudController found. Disabling.");
            enabled = false;
            return;
        }

        controller.externalClickHandling = true;
        controller.RemoveClickListeners();

        if (barRoot == null)
            barRoot = transform as RectTransform;

        _barCanvasGroup = barRoot.GetComponent<CanvasGroup>();
        if (_barCanvasGroup == null)
            _barCanvasGroup = barRoot.gameObject.AddComponent<CanvasGroup>();

        _clickActions = new UnityAction[controller.items.Count];
        for (int i = 0; i < controller.items.Count; i++)
        {
            int idx = i;
            _clickActions[i] = () => OnButtonClicked(idx);
        }
    }

    void OnEnable()
    {
        if (controller == null || controller.items == null) return;

        for (int i = 0; i < controller.items.Count; i++)
        {
            if (controller.items[i] == null || controller.items[i].button == null) continue;
            controller.items[i].button.onClick.AddListener(_clickActions[i]);
        }
    }

    void OnDisable()
    {
        _barSequence?.Kill();

        if (controller == null || controller.items == null || _clickActions == null) return;

        for (int i = 0; i < controller.items.Count && i < _clickActions.Length; i++)
        {
            if (controller.items[i] == null || controller.items[i].button == null) continue;
            controller.items[i].button.onClick.RemoveListener(_clickActions[i]);
        }
    }

    // ─── Bar Appear / Disappear ─────────────────────────

    /// Slides the bar up from below and fades it in.
    public void Appear(Action onComplete = null)
    {
        CacheBarPosition();
        _barSequence?.Kill();

        barRoot.anchoredPosition = _barRestPosition + Vector2.down * appearSlideY;
        _barCanvasGroup.alpha = 0f;
        _barCanvasGroup.interactable = true;
        _barCanvasGroup.blocksRaycasts = true;
        barRoot.gameObject.SetActive(true);

        _barSequence = DOTween.Sequence();
        _barSequence.Join(
            barRoot.DOAnchorPos(_barRestPosition, appearDuration).SetEase(appearEase));
        _barSequence.Join(
            _barCanvasGroup.DOFade(1f, appearDuration * 0.7f).SetEase(Ease.OutQuad));

        if (onComplete != null)
            _barSequence.OnComplete(() => onComplete.Invoke());
    }

    /// Slides the bar down and fades it out, then deactivates the GameObject.
    public void Disappear(Action onComplete = null)
    {
        CacheBarPosition();
        _barSequence?.Kill();

        Vector2 target = _barRestPosition + Vector2.down * appearSlideY;

        _barCanvasGroup.interactable = false;
        _barCanvasGroup.blocksRaycasts = false;

        _barSequence = DOTween.Sequence();
        _barSequence.Join(
            barRoot.DOAnchorPos(target, disappearDuration).SetEase(disappearEase));
        _barSequence.Join(
            _barCanvasGroup.DOFade(0f, disappearDuration * 0.6f).SetEase(Ease.InQuad));
        _barSequence.OnComplete(() =>
        {
            barRoot.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    private void CacheBarPosition()
    {
        if (_barPositionCached || barRoot == null) return;
        _barRestPosition = barRoot.anchoredPosition;
        _barPositionCached = true;
    }

    // ─── Button Click Handling ──────────────────────────

    private void OnButtonClicked(int index)
    {
        if (index < 0 || index >= controller.items.Count) return;
        var item = controller.items[index];
        if (item == null || item.isLocked) return;

        if (controller.CurrentIndex == index)
        {
            item.PlayToggleOff();
            controller.DeselectCurrent(false);
            Closed?.Invoke();
        }
        else
        {
            controller.Select(index, false);
            item.PlayToggleOn();
            ContentActivated?.Invoke(index);
        }
    }
}
