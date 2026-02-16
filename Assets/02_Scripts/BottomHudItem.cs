using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class BottomHudItem : MonoBehaviour
{
    [Header("Refs")]
    public Button button;
    public RectTransform rect;   // BottomHud_Button rect
    public RectTransform icon;
    public TMP_Text label;

    [Header("Icon Sprites")]
    [Tooltip("Sprite shown when the button is unlocked / normal.")]
    public Sprite unlockedSprite;
    [Tooltip("Sprite shown when the button is locked.")]
    public Sprite lockedSprite;

    [Header("State")]
    [Tooltip("Locked buttons are non-interactive. Unlocking plays a jump animation.")]
    public bool isLocked;

    [Header("Unlock Animation")]
    public float unlockJumpHeight = 30f;
    public float unlockJumpDuration = 0.5f;
    public float unlockPunchScale = 0.25f;
    public float unlockFlipAngle = 25f;
    public float unlockFlipDuration = 0.35f;

    [Header("Toggle On  (punch on button rect)")]
    public float toggleOnPunch = 0.08f;
    public float toggleOnDuration = 0.3f;

    [Header("Toggle Off  (squash-and-return on button rect)")]
    public float toggleOffSquash = 0.94f;
    public float toggleOffDuration = 0.15f;

    [Header("Icon Activation  (rotation wobble — never conflicts with controller scale/pos)")]
    public float iconSpinAngle = 12f;
    public float iconSpinDuration = 0.35f;

    [Header("Locked Shake  (horizontal shake on icon)")]
    public float shakeStrength = 8f;
    public float shakeDuration = 0.4f;
    public int shakeVibrato = 10;

    private Image _iconImage;
    private ParticleSystem _particleSystem;
    private bool _lastLockedState;

    void Awake()
    {
        if (rect == null) rect = GetComponent<RectTransform>();
        if (button == null) button = GetComponent<Button>();
        if (icon != null)
        {
            _iconImage = icon.GetComponent<Image>();
            _particleSystem = icon.GetComponentInChildren<ParticleSystem>(true);
        }

        _lastLockedState = isLocked;
        ApplyLockedState(true);
    }

    void Start()
    {
        // Re-apply after all Awakes and the initial canvas layout pass,
        // which can override sizes set during Awake on nested prefabs.
        ApplyLockedState(true);
    }

    void Update()
    {
        if (isLocked != _lastLockedState)
        {
            bool wasLocked = _lastLockedState;
            _lastLockedState = isLocked;
            ApplyLockedState(false);

            // Was locked, now unlocked → celebrate with a jump.
            if (wasLocked && !isLocked)
                PlayUnlockJump();
        }
    }

    // ─── Locked State ─────────────────────────────────

    /// Set locked from code with an animated transition.
    public void SetLocked(bool locked)
    {
        if (isLocked == locked) return;
        bool wasLocked = isLocked;
        isLocked = locked;
        _lastLockedState = locked;
        ApplyLockedState(false);

        if (wasLocked && !locked)
            PlayUnlockJump();
    }

    /// Enables or disables the button, swaps the icon sprite, resizes it,
    /// and toggles the child particle system.
    private void ApplyLockedState(bool instant)
    {
        if (button != null)
            button.interactable = !isLocked;

        if (_iconImage != null)
        {
            Sprite target = isLocked ? lockedSprite : unlockedSprite;
            if (target != null)
                _iconImage.sprite = target;
        }

        if (icon != null)
        {
            float size = isLocked ? 90f : 154f;
            icon.sizeDelta = new Vector2(size, size);
            icon.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
            icon.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size);
            LayoutRebuilder.ForceRebuildLayoutImmediate(icon);
        }
    }

    /// Juicy unlock celebration: flip, hop, overshoot scale, straighten, and particle burst.
    public void PlayUnlockJump()
    {
        if (icon == null) return;
        DOTween.Kill(UnlockJumpId);

        if (_particleSystem != null)
            _particleSystem.Emit(20);

        var seq = DOTween.Sequence().SetId(UnlockJumpId);

        // 1) Quick tilt to one side (the "flip wind-up").
        seq.Append(
            icon.DOLocalRotate(new Vector3(0, 0, unlockFlipAngle), unlockFlipDuration * 0.3f)
                .SetEase(Ease.OutQuad));

        // 2) Big hop upward + overshoot flip to the other side.
        seq.Append(
            icon.DOPunchAnchorPos(Vector2.up * unlockJumpHeight, unlockJumpDuration, 1, 0.2f)
                .SetEase(Ease.OutQuad));
        seq.Join(
            icon.DOLocalRotate(new Vector3(0, 0, -unlockFlipAngle * 0.6f), unlockFlipDuration * 0.4f)
                .SetEase(Ease.InOutQuad));

        // 3) Straighten back to zero rotation with a satisfying settle.
        seq.Append(
            icon.DOLocalRotate(Vector3.zero, unlockFlipDuration * 0.3f)
                .SetEase(Ease.OutBack));

        // 4) Punch-scale on the button rect — big and bouncy.
        if (rect != null)
            seq.Insert(unlockFlipDuration * 0.3f,
                rect.DOPunchScale(Vector3.one * unlockPunchScale, unlockJumpDuration, 2, 0.4f));
    }

    // ─── Public Animation API ─────────────────────────

    /// Punch-scale on the button rect + icon rotation wobble.
    public void PlayToggleOn()
    {
        DOTween.Kill(ToggleTweenId);

        if (rect != null)
        {
            rect.DOPunchScale(Vector3.one * toggleOnPunch, toggleOnDuration, 1, 0.4f)
                .SetId(ToggleTweenId);
        }

        PlayIconActivation();
    }

    /// Subtle squash-and-return on the button rect.
    public void PlayToggleOff()
    {
        DOTween.Kill(ToggleTweenId);
        if (rect == null) return;

        var seq = DOTween.Sequence().SetId(ToggleTweenId);
        seq.Append(rect.DOScale(toggleOffSquash, toggleOffDuration * 0.4f).SetEase(Ease.InQuad));
        seq.Append(rect.DOScale(1f, toggleOffDuration * 0.6f).SetEase(Ease.OutBack));
    }

    /// Z-rotation wobble on the icon.
    /// Safe to layer on top of the controller's scale / anchorPosY tweens.
    public void PlayIconActivation()
    {
        if (icon == null) return;
        DOTween.Kill(IconActivationId);

        icon.DOPunchRotation(new Vector3(0, 0, iconSpinAngle), iconSpinDuration, 1, 0.4f)
            .SetId(IconActivationId);
    }

    /// Horizontal shake for locked-state feedback.
    public void PlayLockedShake()
    {
        if (icon == null) return;
        DOTween.Kill(LockedShakeId);

        icon.DOShakeAnchorPos(shakeDuration, new Vector2(shakeStrength, 0), shakeVibrato, 90, false, true)
            .SetId(LockedShakeId);
    }

    // ─── Tween IDs (instance-unique) ─────────────────

    private string ToggleTweenId    => $"BHI_Toggle_{GetInstanceID()}";
    private string IconActivationId => $"BHI_IconAct_{GetInstanceID()}";
    private string LockedShakeId    => $"BHI_Shake_{GetInstanceID()}";
    private string UnlockJumpId     => $"BHI_Unlock_{GetInstanceID()}";
}
