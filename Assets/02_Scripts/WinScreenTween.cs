using DG.Tweening;
using UnityEngine;

/// <summary>
/// Staggered entrance animation for the Win Screen content.
/// Attach to the Win_Screen GameObject. On enable it animates all content children.
/// The first child (background / shader) is excluded from motion tweens.
/// </summary>
[DisallowMultipleComponent]
public class WinScreenTween : MonoBehaviour
{
    [Header("Content Root")]
    [Tooltip("The 'Content' child that holds all win-screen elements. Auto-found if left empty.")]
    public RectTransform contentRoot;

    [Header("Timing")]
    [Tooltip("Base duration for each element's entrance.")]
    public float elementDuration = 0.35f;
    [Tooltip("Delay between each element starting its entrance.")]
    public float staggerDelay = 0.10f;
    [Tooltip("Initial delay before the first element starts.")]
    public float initialDelay = 0.05f;
    public bool ignoreTimeScale = true;

    [Header("Title (LevelCompleted!)")]
    public float titleDropY = 80f;
    public float titleStartScale = 0.6f;

    [Header("Star")]
    public float starStartScale = 0f;

    [Header("Coin / Crown")]
    public float iconSlideX = 120f;

    [Header("Buttons")]
    public float buttonsRiseY = 100f;

    // Cached state per animated element
    private struct ElementState
    {
        public RectTransform rect;
        public CanvasGroup group;
        public Vector2 targetPos;
        public Vector3 targetScale;
    }

    private ElementState _title;
    private ElementState _star;
    private ElementState _coin;
    private ElementState _crown;
    private ElementState _buttons;
    private CanvasGroup _contentGroup;
    private Sequence _sequence;

    // ──────────────────────────────────────────────

    private void Reset()  => AutoAssignContent();
    private void Awake()  => AutoAssignContent();

    private void OnEnable()
    {
        if (!Application.isPlaying) return;

        if (contentRoot == null) AutoAssignContent();
        if (contentRoot == null) return;

        CacheElements();
        PlayEntrance();
    }

    private void OnDisable()
    {
        _sequence?.Kill();
        _sequence = null;
    }

    // ──────────────────────────────────────────────

    private void AutoAssignContent()
    {
        if (contentRoot != null) return;
        Transform t = transform.Find("Content");
        if (t != null) contentRoot = t as RectTransform;
    }

    private void CacheElements()
    {
        _title   = Resolve("LevelCompleted!");
        _star    = Resolve("Star");
        _coin    = Resolve("Coin-icn");
        _crown   = Resolve("Crown-icn");
        _buttons = Resolve("Buttons");

        _contentGroup = contentRoot.GetComponent<CanvasGroup>();
        if (_contentGroup == null)
            _contentGroup = contentRoot.gameObject.AddComponent<CanvasGroup>();
    }

    private ElementState Resolve(string childName)
    {
        Transform t = contentRoot.Find(childName);
        if (t == null) return default;

        RectTransform rt = t as RectTransform;
        CanvasGroup cg = rt.GetComponent<CanvasGroup>();
        if (cg == null) cg = rt.gameObject.AddComponent<CanvasGroup>();

        return new ElementState
        {
            rect        = rt,
            group       = cg,
            targetPos   = rt.anchoredPosition,
            targetScale = rt.localScale
        };
    }

    // ──────────────────────────────────────────────

    private void PlayEntrance()
    {
        _sequence?.Kill();

        // ── Prepare start states ──
        PrepareTitle();
        PrepareStar();
        PrepareCoin();
        PrepareCrown();
        PrepareButtons();

        // ── Build sequence ──
        _sequence = DOTween.Sequence().SetUpdate(ignoreTimeScale);

        float t = initialDelay;

        // 1) Title drops in and scales up
        if (_title.rect != null)
        {
            _sequence.Insert(t, _title.rect.DOAnchorPos(_title.targetPos, elementDuration).SetEase(Ease.OutCubic));
            _sequence.Insert(t, _title.rect.DOScale(_title.targetScale, elementDuration).SetEase(Ease.OutBack));
            _sequence.Insert(t, _title.group.DOFade(1f, elementDuration * 0.7f).SetEase(Ease.OutQuad));
            t += staggerDelay;
        }

        // 2) Star bounces in
        if (_star.rect != null)
        {
            _sequence.Insert(t, _star.rect.DOScale(_star.targetScale, elementDuration * 1.1f).SetEase(Ease.OutBack, 1.4f));
            _sequence.Insert(t, _star.group.DOFade(1f, elementDuration * 0.6f).SetEase(Ease.OutQuad));
            t += staggerDelay;
        }

        // 3) Coin slides in from left
        if (_coin.rect != null)
        {
            _sequence.Insert(t, _coin.rect.DOAnchorPos(_coin.targetPos, elementDuration).SetEase(Ease.OutCubic));
            _sequence.Insert(t, _coin.group.DOFade(1f, elementDuration * 0.7f).SetEase(Ease.OutQuad));
            t += staggerDelay;
        }

        // 4) Crown slides in from right
        if (_crown.rect != null)
        {
            _sequence.Insert(t, _crown.rect.DOAnchorPos(_crown.targetPos, elementDuration).SetEase(Ease.OutCubic));
            _sequence.Insert(t, _crown.group.DOFade(1f, elementDuration * 0.7f).SetEase(Ease.OutQuad));
            t += staggerDelay;
        }

        // 5) Buttons rise from below
        if (_buttons.rect != null)
        {
            _sequence.Insert(t, _buttons.rect.DOAnchorPos(_buttons.targetPos, elementDuration).SetEase(Ease.OutCubic));
            _sequence.Insert(t, _buttons.group.DOFade(1f, elementDuration * 0.7f).SetEase(Ease.OutQuad));
        }
    }

    // ── Start-state helpers ──

    private void PrepareTitle()
    {
        if (_title.rect == null) return;
        _title.rect.anchoredPosition = _title.targetPos + Vector2.up * titleDropY;
        _title.rect.localScale = _title.targetScale * titleStartScale;
        _title.group.alpha = 0f;
    }

    private void PrepareStar()
    {
        if (_star.rect == null) return;
        _star.rect.localScale = Vector3.one * starStartScale;
        _star.group.alpha = 0f;
    }

    private void PrepareCoin()
    {
        if (_coin.rect == null) return;
        _coin.rect.anchoredPosition = _coin.targetPos + Vector2.left * iconSlideX;
        _coin.group.alpha = 0f;
    }

    private void PrepareCrown()
    {
        if (_crown.rect == null) return;
        _crown.rect.anchoredPosition = _crown.targetPos + Vector2.right * iconSlideX;
        _crown.group.alpha = 0f;
    }

    private void PrepareButtons()
    {
        if (_buttons.rect == null) return;
        _buttons.rect.anchoredPosition = _buttons.targetPos + Vector2.down * buttonsRiseY;
        _buttons.group.alpha = 0f;
    }
}
