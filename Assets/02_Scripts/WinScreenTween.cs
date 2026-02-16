using DG.Tweening;
using UnityEngine;

/// <summary>
/// Dramatic staggered entrance animation for the Win Screen content.
/// Attach to the Win_Screen GameObject. On enable it animates all content children.
/// </summary>
[DisallowMultipleComponent]
public class WinScreenTween : MonoBehaviour
{
    [Header("Content Root")]
    [Tooltip("The 'Content' child that holds all win-screen elements. Auto-found if left empty.")]
    public RectTransform contentRoot;

    [Header("Timing")]
    public float elementDuration = 0.45f;
    public float staggerDelay = 0.12f;
    public float initialDelay = 0.08f;
    public bool ignoreTimeScale = true;

    [Header("Title (LevelCompleted!)")]
    public float titleDropY = 120f;
    public float titleStartScale = 0.3f;
    public float titleOvershoot = 1.6f;

    [Header("Star")]
    public float starStartScale = 0f;
    public float starSpinDegrees = 360f;
    public float starSpinDuration = 0.6f;
    public float starBounceOvershoot = 2f;

    [Header("Star Particles")]
    public int starParticleBurst = 40;

    [Header("Coin / Crown")]
    public float iconSlideX = 180f;
    public float iconOvershoot = 1.2f;

    [Header("Buttons")]
    public float buttonsRiseY = 140f;
    public float buttonsPunchScale = 0.06f;

    // ── Cached state ──

    private struct ElementState
    {
        public RectTransform rect;
        public CanvasGroup group;
        public Vector2 targetPos;
        public Vector3 targetScale;
    }

    private ElementState _title;
    private ElementState _star;
    private RectTransform _starImage;
    private ParticleSystem _starBurstParticles;
    private ElementState _coin;
    private ElementState _crown;
    private ElementState _buttons;
    private Sequence _sequence;
    private bool _cached;

    // ──────────────────────────────────────────────

    private void Reset()  => AutoAssignContent();
    private void Awake()  => AutoAssignContent();

    private void OnEnable()
    {
        if (!Application.isPlaying) return;

        if (contentRoot == null) AutoAssignContent();
        if (contentRoot == null) return;

        if (!_cached)
        {
            CacheElements();
            _cached = true;
        }
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

        // StarImage is the visual inside Star that spins.
        if (_star.rect != null)
        {
            Transform img = _star.rect.Find("StarImage");
            if (img != null) _starImage = img as RectTransform;

            Transform burst = _star.rect.Find("ParticlesMiniStarsBurst");
            if (burst != null) _starBurstParticles = burst.GetComponent<ParticleSystem>();
        }
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

        PrepareTitle();
        PrepareStar();
        PrepareCoin();
        PrepareCrown();
        PrepareButtons();

        _sequence = DOTween.Sequence().SetUpdate(ignoreTimeScale);

        float t = initialDelay;

        // ── 1) Title slams down with heavy overshoot ──
        if (_title.rect != null)
        {
            _sequence.Insert(t,
                _title.rect.DOAnchorPos(_title.targetPos, elementDuration)
                    .SetEase(Ease.OutBack, titleOvershoot));
            _sequence.Insert(t,
                _title.rect.DOScale(_title.targetScale, elementDuration)
                    .SetEase(Ease.OutBack, titleOvershoot));
            _sequence.Insert(t,
                _title.group.DOFade(1f, elementDuration * 0.5f)
                    .SetEase(Ease.OutQuad));
            // Punch to sell the impact.
            _sequence.Insert(t + elementDuration * 0.7f,
                _title.rect.DOPunchScale(Vector3.one * 0.08f, 0.2f, 2, 0.5f));
            t += staggerDelay * 1.5f;
        }

        // ── 2) Star: bounce in, StarImage spins 360, particle burst ──
        if (_star.rect != null)
        {
            // Scale bounces in big (whole Star group).
            _sequence.Insert(t,
                _star.rect.DOScale(_star.targetScale, starSpinDuration)
                    .SetEase(Ease.OutBack, starBounceOvershoot));

            // Fade in fast.
            _sequence.Insert(t,
                _star.group.DOFade(1f, starSpinDuration * 0.3f)
                    .SetEase(Ease.OutQuad));

            // Full 360 spin on StarImage only (not the text / particles).
            if (_starImage != null)
            {
                _sequence.Insert(t,
                    _starImage.DOLocalRotate(new Vector3(0, 0, -starSpinDegrees), starSpinDuration, RotateMode.FastBeyond360)
                        .SetEase(Ease.OutCubic));
            }

            // Punch scale at the end of the spin for a satisfying landing.
            _sequence.Insert(t + starSpinDuration * 0.85f,
                _star.rect.DOPunchScale(Vector3.one * 0.15f, 0.25f, 2, 0.4f));

            // Particle burst from ParticlesMiniStarsBurst timed with the landing.
            if (_starBurstParticles != null)
            {
                float burstTime = t + starSpinDuration * 0.9f;
                _sequence.InsertCallback(burstTime, () => _starBurstParticles.Emit(starParticleBurst));
            }

            t += staggerDelay * 2f;
        }

        // ── 3) Coin slides in from far left with bounce ──
        if (_coin.rect != null)
        {
            _sequence.Insert(t,
                _coin.rect.DOAnchorPos(_coin.targetPos, elementDuration)
                    .SetEase(Ease.OutBack, iconOvershoot));
            _sequence.Insert(t,
                _coin.group.DOFade(1f, elementDuration * 0.5f)
                    .SetEase(Ease.OutQuad));
            _sequence.Insert(t + elementDuration * 0.6f,
                _coin.rect.DOPunchScale(Vector3.one * 0.05f, 0.15f, 1, 0.4f));
            t += staggerDelay;
        }

        // ── 4) Crown slides in from far right with bounce ──
        if (_crown.rect != null)
        {
            _sequence.Insert(t,
                _crown.rect.DOAnchorPos(_crown.targetPos, elementDuration)
                    .SetEase(Ease.OutBack, iconOvershoot));
            _sequence.Insert(t,
                _crown.group.DOFade(1f, elementDuration * 0.5f)
                    .SetEase(Ease.OutQuad));
            _sequence.Insert(t + elementDuration * 0.6f,
                _crown.rect.DOPunchScale(Vector3.one * 0.05f, 0.15f, 1, 0.4f));
            t += staggerDelay;
        }

        // ── 5) Buttons rise with punch-scale landing ──
        if (_buttons.rect != null)
        {
            _sequence.Insert(t,
                _buttons.rect.DOAnchorPos(_buttons.targetPos, elementDuration)
                    .SetEase(Ease.OutBack, 1.1f));
            _sequence.Insert(t,
                _buttons.group.DOFade(1f, elementDuration * 0.5f)
                    .SetEase(Ease.OutQuad));
            _sequence.Insert(t + elementDuration * 0.7f,
                _buttons.rect.DOPunchScale(Vector3.one * buttonsPunchScale, 0.2f, 1, 0.4f));
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

        if (_starImage != null)
            _starImage.localRotation = Quaternion.identity;
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
