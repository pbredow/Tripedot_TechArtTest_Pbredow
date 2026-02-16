using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using DG.Tweening;

public class BottomHudController : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform buttonsParent;     // "Buttons" (HorizontalLayoutGroup lives here)
    public RectTransform marker;            // "Marker"
    public List<BottomHudItem> items;       // in order

    [Header("Marker Follow")]
    public bool markerFollowDuringLayout = true;

    [Header("Marker Move (only used when not following)")]
    public float moveFastTime = 0.10f;
    public float moveSettleTime = 0.12f;
    public Ease moveFastEase = Ease.OutQuad;
    public Ease moveSettleEase = Ease.OutCubic;

    [Header("Button Width (RectTransform)")]
    public float unselectedWidth = 150f;
    public float selectedWidth = 240f;
    public float widthAnimTime = 0.18f;
    public Ease widthEase = Ease.OutCubic;

    [Header("Icon Anim")]
    public float selectedIconScale = 1.15f;
    public float unselectedIconScale = 1f;
    public float selectedIconY = 40f;
    public float unselectedIconY = 0f;
    public float iconAnimTime = 0.16f;
    public Ease iconEase = Ease.OutCubic;

    [Header("Label Fade")]
    public float labelFadeInTime = 0.10f;
    public float labelFadeOutTime = 0.08f;

    int _currentIndex = -1;
    public int CurrentIndex => _currentIndex;

    /// When true, the controller will not register its own click listeners
    /// or auto-select on Start. An external component (e.g. BottomBarView)
    /// is expected to drive Select / DeselectCurrent calls.
    [HideInInspector] public bool externalClickHandling;

    bool _layoutDirty;
    int _activeWidthTweens;

    UnityAction[] _clickActions;

    void Awake()
    {
        if (items == null)
        {
            Debug.LogError("BottomHudController: Items list is null.");
            enabled = false;
            return;
        }

        _clickActions = new UnityAction[items.Count];
        for (int i = 0; i < items.Count; i++)
        {
            int idx = i;
            _clickActions[i] = () => Select(idx, false);
        }
    }

    void OnEnable()
    {
        if (items == null || externalClickHandling) return;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null || items[i].button == null) continue;
            items[i].button.onClick.AddListener(_clickActions[i]);
        }
    }

    void OnDisable()
    {
        if (items == null || _clickActions == null || externalClickHandling) return;
        RemoveClickListeners();
    }

    /// Removes any click listeners the controller registered on the buttons.
    /// Called by BottomBarView when it takes over click handling.
    public void RemoveClickListeners()
    {
        if (items == null || _clickActions == null) return;

        for (int i = 0; i < items.Count && i < _clickActions.Length; i++)
        {
            if (items[i] == null || items[i].button == null) continue;
            items[i].button.onClick.RemoveListener(_clickActions[i]);
        }
    }

    void Start()
    {
        if (buttonsParent == null || marker == null)
        {
            Debug.LogError("BottomHudController: Assign Buttons Parent and Marker.");
            enabled = false;
            return;
        }

        if (items == null || items.Count == 0)
        {
            Debug.LogError("BottomHudController: Items list is empty.");
            enabled = false;
            return;
        }

        FitWidthsToContainer();

        if (!externalClickHandling)
            Select(0, true);
    }

    /// <summary>
    /// Scales button and marker widths so the bar fills its container correctly
    /// on any resolution / aspect ratio. Called once in Start().
    /// </summary>
    void FitWidthsToContainer()
    {
        Canvas.ForceUpdateCanvases();

        float containerWidth = buttonsParent.rect.width;
        if (containerWidth <= 0) return;

        var lg = buttonsParent.GetComponent<HorizontalLayoutGroup>();
        float padding = 0f;
        float spacing = 0f;
        if (lg != null)
        {
            padding = lg.padding.left + lg.padding.right;
            spacing = lg.spacing * Mathf.Max(0, items.Count - 1);
        }

        float available = containerWidth - padding - spacing;
        if (available <= 0) return;

        float refTotal = (items.Count - 1) * unselectedWidth + selectedWidth;
        if (refTotal <= 0) return;

        float scale = available / refTotal;

        unselectedWidth = Mathf.Round(unselectedWidth * scale);
        selectedWidth   = Mathf.Round(selectedWidth * scale);

        if (marker != null)
        {
            Vector2 ms = marker.sizeDelta;
            marker.sizeDelta = new Vector2(Mathf.Round(ms.x * scale), ms.y);
        }

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null || items[i].rect == null) continue;
            SetWidth(items[i].rect, unselectedWidth);

            // Clamp Z to 0 – stray Z offsets on ScreenSpaceCamera canvases
            // can push elements outside the camera's clip planes.
            Vector3 lp = items[i].rect.localPosition;
            if (lp.z != 0f)
                items[i].rect.localPosition = new Vector3(lp.x, lp.y, 0f);
        }

        _layoutDirty = true;
    }

    void LateUpdate()
    {
        // Rebuild layout at most once per frame (instead of every tween update).
        if (_layoutDirty)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsParent);
            _layoutDirty = false;
        }

        if (!markerFollowDuringLayout) return;
        if (_activeWidthTweens <= 0) return;
        if (_currentIndex < 0 || _currentIndex >= items.Count) return;

        var it = items[_currentIndex];
        if (it == null || it.rect == null) return;

        float x = GetItemCenterX(it.rect);
        marker.anchoredPosition = new Vector2(x, marker.anchoredPosition.y);
    }

    public void Select(int index, bool instant)
    {
        if (items == null) return;
        if (index < 0 || index >= items.Count) return;
        if (_currentIndex == index && !instant) return;

        int prev = _currentIndex;

        if (prev >= 0)
            SetItemState(prev, selected: false, instant: instant);

        SetItemState(index, selected: true, instant: instant);

        // Show marker in case it was hidden by DeselectCurrent.
        if (marker != null)
            marker.gameObject.SetActive(true);

        // Ensure geometry is correct for immediate marker placement.
        _layoutDirty = true;
        LayoutRebuilder.ForceRebuildLayoutImmediate(buttonsParent);
        _layoutDirty = false;

        if (instant || markerFollowDuringLayout)
        {
            float x = GetItemCenterX(items[index].rect);
            marker.anchoredPosition = new Vector2(x, marker.anchoredPosition.y);
        }
        else
        {
            MoveMarkerTo(index);
        }

        _currentIndex = index;
    }

    /// Deselects the current button so nothing is active.
    public void DeselectCurrent(bool instant)
    {
        if (_currentIndex < 0) return;

        SetItemState(_currentIndex, selected: false, instant: instant);
        _currentIndex = -1;

        _layoutDirty = true;

        if (marker != null)
            marker.gameObject.SetActive(false);
    }

    void SetItemState(int index, bool selected, bool instant)
    {
        var it = items[index];
        if (it == null || it.rect == null) return;

        float targetWidth = selected ? selectedWidth : unselectedWidth;

        // Width tween: kill only our width tween for this item (avoid killing unrelated tweens).
        string widthTweenId = GetWidthTweenId(index);
        DOTween.Kill(widthTweenId);

        if (instant)
        {
            SetWidth(it.rect, targetWidth);
        }
        else
        {
            float w = it.rect.rect.width;

            _activeWidthTweens++;

            DOTween.To(
                    () => w,
                    x =>
                    {
                        w = x;
                        SetWidth(it.rect, x);
                        _layoutDirty = true;
                    },
                    targetWidth,
                    widthAnimTime
                )
                .SetEase(widthEase)
                .SetId(widthTweenId)
                .OnComplete(() => _activeWidthTweens = Mathf.Max(0, _activeWidthTweens - 1))
                .OnKill(() => _activeWidthTweens = Mathf.Max(0, _activeWidthTweens - 1));
        }

        // Icon + label tweens: safe to kill by target (they should only be animated here).
        if (it.icon != null) it.icon.DOKill();
        if (it.label != null) it.label.DOKill();

        float iconScale = selected ? selectedIconScale : unselectedIconScale;
        float iconY = selected ? selectedIconY : unselectedIconY;
        float labelAlpha = selected ? 1f : 0f;

        if (instant)
        {
            if (it.icon != null)
            {
                it.icon.localScale = Vector3.one * iconScale;
                Vector2 p = it.icon.anchoredPosition;
                it.icon.anchoredPosition = new Vector2(p.x, iconY);
            }

            if (it.label != null)
            {
                var c = it.label.color;
                c.a = labelAlpha;
                it.label.color = c;
            }

            return;
        }

        if (it.icon != null)
        {
            it.icon.DOScale(iconScale, iconAnimTime).SetEase(iconEase);
            it.icon.DOAnchorPosY(iconY, iconAnimTime).SetEase(iconEase);
        }

        if (it.label != null)
        {
            float t = selected ? labelFadeInTime : labelFadeOutTime;
            it.label.DOFade(labelAlpha, t);
        }
    }

    void SetWidth(RectTransform rt, float w)
    {
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
    }

    void MoveMarkerTo(int index)
    {
        if (marker == null) return;
        if (index < 0 || index >= items.Count) return;
        if (items[index] == null || items[index].rect == null) return;

        float targetX = GetItemCenterX(items[index].rect);

        // Kill only our marker move sequence (avoid killing other marker tweens if you add any later).
        DOTween.Kill("MarkerMove");

        float currentX = marker.anchoredPosition.x;
        float midX = Mathf.Lerp(currentX, targetX, 0.65f);

        Sequence seq = DOTween.Sequence().SetId("MarkerMove");
        seq.Append(marker.DOAnchorPosX(midX, moveFastTime).SetEase(moveFastEase));
        seq.Append(marker.DOAnchorPosX(targetX, moveSettleTime).SetEase(moveSettleEase));
    }

    float GetItemCenterX(RectTransform itemRect)
    {
        // Convert the item's rect center into marker parent's local space.
        var markerParent = (RectTransform)marker.parent;
        Vector3 worldCenter = itemRect.TransformPoint(itemRect.rect.center);
        Vector3 local = markerParent.InverseTransformPoint(worldCenter);
        return local.x;
    }

    string GetWidthTweenId(int index) => $"BottomHud_Width_{index}";
}