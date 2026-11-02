using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Core.Events;

namespace Tools
{
    /// <summary>
    /// Fader d'opacité (fondu). Se pose sur une Image plein écran.
    /// FadeIn = le fader apparaît (couvre l'écran), FadeOut = il disparaît (découvre l'écran).
    /// Animation via DOTween.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(Image))]
    public class Fader : MonoBehaviour, IEventListener<FadeStopEvent>, IEventListener<FadeInEvent>, IEventListener<FadeOutEvent>, IEventListener<FadeEvent>
    {
        public enum ForcedInitStates { None, Active, Inactive }

        [InspectorGroup("Identification", true, 122)]
        public int ID;

        [InspectorGroup("Opacity", true, 123)]
        public float InactiveAlpha = 0f;
        public float ActiveAlpha = 1f;
        public Color FadeColor = Color.black;
        public ForcedInitStates ForcedInitState = ForcedInitStates.Inactive;

        [InspectorGroup("Timing", true, 124)]
        public float DefaultDuration = 0.2f;
        public Ease DefaultEase = Ease.Linear;
        public bool IgnoreTimescale = true;
        public bool CanFadeToCurrentAlpha = true;

        [InspectorGroup("Interaction", true, 125)]
        public bool ShouldBlockRaycasts = false;

        protected CanvasGroup _canvasGroup;
        protected Image _image;
        protected float _initialAlpha;
        protected Tween _tween;

        // -------------------------
        // Initialisation
        // -------------------------

        protected virtual void Awake()
        {
            Initialization();
        }

        protected virtual void Initialization()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _image = GetComponent<Image>();

            _image.color = FadeColor;

            switch (ForcedInitState)
            {
                case ForcedInitStates.Inactive:
                    _canvasGroup.alpha = InactiveAlpha;
                    _image.enabled = false;
                    break;
                case ForcedInitStates.Active:
                    _canvasGroup.alpha = ActiveAlpha;
                    _image.enabled = true;
                    if (ShouldBlockRaycasts) _canvasGroup.blocksRaycasts = true;
                    break;
            }
        }

        protected virtual void OnDestroy()
        {
            _tween?.Kill();
        }

        // -------------------------
        // Debug
        // -------------------------

        public void ResetFader()
        {
            _tween?.Kill();
            _canvasGroup.alpha = InactiveAlpha;
            DisableFader();
        }

        public void DefaultFade()
        {
            FadeEvent.Trigger(DefaultDuration, ActiveAlpha, DefaultEase, ID);
        }

        // -------------------------
        // Core fade logic
        // -------------------------

        protected virtual void StartFading(float initialAlpha, float endAlpha, float duration, Ease ease, bool ignoreTimeScale)
        {
            if (!CanFadeToCurrentAlpha && Mathf.Approximately(_canvasGroup.alpha, endAlpha)) return;

            IgnoreTimescale = ignoreTimeScale;
            _tween?.Kill();

            _initialAlpha = initialAlpha;
            EnableFader();
            _canvasGroup.alpha = initialAlpha;

            if (duration <= 0f)
            {
                _canvasGroup.alpha = endAlpha;
                OnFadeFinished();
                return;
            }

            _tween = _canvasGroup.DOFade(endAlpha, duration)
                                 .SetEase(ease)
                                 .SetUpdate(IgnoreTimescale)
                                 .OnComplete(OnFadeFinished);
        }

        protected virtual void OnFadeFinished()
        {
            _tween = null;
            if (Mathf.Approximately(_canvasGroup.alpha, InactiveAlpha))
            {
                DisableFader();
            }
        }

        protected virtual void EnableFader()
        {
            _image.enabled = true;
            if (ShouldBlockRaycasts)
                _canvasGroup.blocksRaycasts = true;
        }

        protected virtual void DisableFader()
        {
            _image.enabled = false;
            if (ShouldBlockRaycasts)
                _canvasGroup.blocksRaycasts = false;
        }

        public virtual void FadeIn(float duration, Ease ease, bool ignoreTimeScale = true)
        {
            StartFading(InactiveAlpha, ActiveAlpha, duration, ease, ignoreTimeScale);
        }

        public virtual void FadeOut(float duration, Ease ease, bool ignoreTimeScale = true)
        {
            StartFading(ActiveAlpha, InactiveAlpha, duration, ease, ignoreTimeScale);
        }

        public virtual void FadeTo(float targetAlpha, float duration, Ease ease, bool ignoreTimeScale = true)
        {
            float target = targetAlpha < 0f ? ActiveAlpha : targetAlpha;
            StartFading(_canvasGroup.alpha, target, duration, ease, ignoreTimeScale);
        }

        // -------------------------
        // Event listeners
        // -------------------------

        public void OnEvent(FadeEvent e)
        {
            if (e.ID != ID) return;
            FadeTo(e.TargetAlpha, e.Duration, e.Curve, e.IgnoreTimeScale);
        }

        public void OnEvent(FadeInEvent e)
        {
            if (e.ID != ID) return;
            FadeIn(e.Duration, e.Curve, e.IgnoreTimeScale);
        }

        public void OnEvent(FadeOutEvent e)
        {
            if (e.ID != ID) return;
            FadeOut(e.Duration, e.Curve, e.IgnoreTimeScale);
        }

        public void OnEvent(FadeStopEvent e)
        {
            if (e.ID != ID) return;
            _tween?.Kill();
            _tween = null;
            if (e.Restore)
                _canvasGroup.alpha = _initialAlpha;
        }

        // -------------------------
        // Enable / Disable
        // -------------------------

        private void OnEnable()
        {
            this.EventStartListening<FadeEvent>();
            this.EventStartListening<FadeOutEvent>();
            this.EventStartListening<FadeStopEvent>();
            this.EventStartListening<FadeInEvent>();
        }

        private void OnDisable()
        {
            this.EventStopListening<FadeEvent>();
            this.EventStopListening<FadeOutEvent>();
            this.EventStopListening<FadeStopEvent>();
            this.EventStopListening<FadeInEvent>();
        }
    }
}