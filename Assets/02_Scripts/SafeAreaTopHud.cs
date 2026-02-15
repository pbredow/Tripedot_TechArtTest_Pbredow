using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class SafeAreaTopHud : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("If empty, uses the parent canvas.")]
    public Canvas targetCanvas;

    [Tooltip("Top HUD rects to keep clear of notches/cutouts. If empty, this RectTransform is used.")]
    public List<RectTransform> topHudElements = new List<RectTransform>();

    [Header("Behavior")]
    [Tooltip("Apply in Edit Mode so Device Simulator previews update without entering Play Mode.")]
    public bool previewInEditMode = true;

    [Tooltip("Also apply left/right safe insets (for curved corners/islands).")]
    public bool applyHorizontalInsets = false;

    [Tooltip("Extra padding below the safe area top, in Canvas units.")]
    public float extraTopPadding = 0f;

    [Header("Debug (read-only at runtime)")]
    public float appliedTopInset;

    private readonly List<ElementState> _states = new List<ElementState>();
    private Rect _lastSafeArea;
    private Vector2Int _lastScreenSize;

    private struct ElementState
    {
        public RectTransform rect;
        public Vector2 baseAnchoredPosition;
        public Vector2 baseOffsetMin;
        public Vector2 baseOffsetMax;
    }

    private void OnEnable()
    {
        // Prevent cumulative offsets after script reloads / inspector refreshes.
        RestoreBaseOffsets();
        CacheTargets();
        ApplySafeArea(true);
    }

    private void OnDisable()
    {
        RestoreBaseOffsets();
    }

    private void OnValidate()
    {
        // Prevent cumulative offsets while editing values in Inspector.
        RestoreBaseOffsets();
        CacheTargets();
        ApplySafeArea(true);
    }

    private void Update()
    {
        if (!Application.isPlaying && !previewInEditMode)
            return;

        if (_lastSafeArea != Screen.safeArea || _lastScreenSize.x != Screen.width || _lastScreenSize.y != Screen.height)
            ApplySafeArea(true);
    }

    private void CacheTargets()
    {
        if (targetCanvas == null)
            targetCanvas = GetComponentInParent<Canvas>();

        _states.Clear();

        if (topHudElements == null || topHudElements.Count == 0)
        {
            RectTransform selfRect = transform as RectTransform;
            if (selfRect != null)
                _states.Add(new ElementState
                {
                    rect = selfRect,
                    baseAnchoredPosition = selfRect.anchoredPosition,
                    baseOffsetMin = selfRect.offsetMin,
                    baseOffsetMax = selfRect.offsetMax
                });
            return;
        }

        for (int i = 0; i < topHudElements.Count; i++)
        {
            RectTransform rect = topHudElements[i];
            if (rect == null)
                continue;

            _states.Add(new ElementState
            {
                rect = rect,
                baseAnchoredPosition = rect.anchoredPosition,
                baseOffsetMin = rect.offsetMin,
                baseOffsetMax = rect.offsetMax
            });
        }
    }

    private void ApplySafeArea(bool force)
    {
        if (_states.Count == 0)
            CacheTargets();

        if (targetCanvas == null || _states.Count == 0)
            return;

        Rect safeArea = Screen.safeArea;
        if (!force && safeArea == _lastSafeArea && _lastScreenSize.x == Screen.width && _lastScreenSize.y == Screen.height)
            return;

        float scaleFactor = Mathf.Max(0.0001f, targetCanvas.scaleFactor);
        float extraTopPaddingPixels = Mathf.Max(0f, extraTopPadding) * scaleFactor;
        float leftInset = Mathf.Max(0f, safeArea.xMin) / scaleFactor;
        float rightInset = Mathf.Max(0f, Screen.width - safeArea.xMax) / scaleFactor;
        float appliedMaxTopInset = 0f;
        Camera uiCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;

        for (int i = 0; i < _states.Count; i++)
        {
            ElementState state = _states[i];
            if (state.rect == null)
                continue;

            // Always reset to base, then compute the minimum required downward shift.
            state.rect.anchoredPosition = state.baseAnchoredPosition;

            Vector2 newOffsetMin = state.baseOffsetMin;
            Vector2 newOffsetMax = state.baseOffsetMax;

            if (applyHorizontalInsets)
            {
                newOffsetMin.x = state.baseOffsetMin.x + leftInset;
                newOffsetMax.x = state.baseOffsetMax.x - rightInset;
            }
            else
            {
                newOffsetMin.x = state.baseOffsetMin.x;
                newOffsetMax.x = state.baseOffsetMax.x;
            }

            state.rect.offsetMin = newOffsetMin;
            state.rect.offsetMax = newOffsetMax;

            // Find how much the rect actually overlaps the unsafe top region in screen space.
            Vector3[] corners = new Vector3[4];
            state.rect.GetWorldCorners(corners);
            float topYScreen = Mathf.Max(
                RectTransformUtility.WorldToScreenPoint(uiCamera, corners[1]).y,
                RectTransformUtility.WorldToScreenPoint(uiCamera, corners[2]).y
            );

            float allowedTopY = safeArea.yMax - extraTopPaddingPixels;
            float overlapPixels = Mathf.Max(0f, topYScreen - allowedTopY);
            float requiredShift = overlapPixels / scaleFactor;

            if (requiredShift > 0f)
            {
                state.rect.anchoredPosition = new Vector2(
                    state.baseAnchoredPosition.x,
                    state.baseAnchoredPosition.y - requiredShift
                );
            }

            appliedMaxTopInset = Mathf.Max(appliedMaxTopInset, requiredShift);
        }

        appliedTopInset = appliedMaxTopInset;

        _lastSafeArea = safeArea;
        _lastScreenSize = new Vector2Int(Screen.width, Screen.height);
    }

    private void RestoreBaseOffsets()
    {
        for (int i = 0; i < _states.Count; i++)
        {
            ElementState state = _states[i];
            if (state.rect == null)
                continue;

            state.rect.anchoredPosition = state.baseAnchoredPosition;
            state.rect.offsetMin = state.baseOffsetMin;
            state.rect.offsetMax = state.baseOffsetMax;
        }
    }
}
