using DG.Tweening;
using UnityEngine;
using Core.Events;

namespace Tools
{
    /// <summary>
    /// Fader directionnel : un panneau plein écran (RectTransform) qui GLISSE pour couvrir / découvrir l'écran.
    /// Écoute les mêmes événements que Fader (FadeInEvent, FadeOutEvent, FadeEvent, FadeStopEvent), filtrés par ID.
    ///
    /// - La position du panneau dans l'éditeur = sa position "couvert". Il doit faire la taille de l'écran
    ///   (anchors en stretch), car la distance parcourue vient de sa largeur / hauteur.
    /// - FadeIn  : le panneau arrive depuis le côté opposé à FadeInDirection et vient couvrir l'écran.
    /// - FadeOut : le panneau repart dans la direction FadeOutDirection, jusqu'à sortir de l'écran.
    /// - Exemple "rideau qui descend puis remonte" : FadeIn = TopToBottom, FadeOut = BottomToTop.
    ///
    /// Ne désactive jamais le GameObject lui-même : il masque le panneau avec alpha 0 + hors écran.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class DirectionalFader : MonoBehaviour, IEventListener<FadeStopEvent>, IEventListener<FadeInEvent>, IEventListener<FadeOutEvent>, IEventListener<FadeEvent>
    {
        /// <summary>Direction dans laquelle le panneau SE DÉPLACE.</summary>
        public enum Directions { TopToBottom, LeftToRight, RightToLeft, BottomToTop }
        public enum ForcedInitStates { None, Active, Inactive }

        [InspectorGroup("Identification", true, 122)]
        public int ID;

        [InspectorGroup("Directional Fader", true, 123)]
        public Directions FadeInDirection = Directions.TopToBottom;
        public Directions FadeOutDirection = Directions.BottomToTop;
        [Tooltip("Active = le panneau démarre couvrant l'écran (mode classique). Inactive = il démarre caché.")]
        public ForcedInitStates ForcedInitState = ForcedInitStates.Inactive;

        [InspectorGroup("Timing", true, 124)]
        public float DefaultDuration = 0.2f;
        public Ease DefaultEase = Ease.Linear;
        public bool IgnoreTimescale = true;

        [InspectorGroup("Interaction", true, 125)]
        public bool ShouldBlockRaycasts = true;

        protected RectTransform _rect;
        protected CanvasGroup _group;
        protected Vector2 _coveredPosition;
        protected bool _visible;
        protected Tween _tween;

        protected float Width => _rect.rect.width;
        protected float Height => _rect.rect.height;

        // -------------------------
        // Initialisation
        // -------------------------

        protected virtual void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _rect = GetComponent<RectTransform>();

            Canvas.ForceUpdateCanvases(); // pour que rect.width / height soient valides
            _coveredPosition = _rect.anchoredPosition;

            switch (ForcedInitState)
            {
                case ForcedInitStates.Active:
                    Show();
                    _rect.anchoredPosition = _coveredPosition;
                    break;
                case ForcedInitStates.Inactive:
                    Hide();
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
            _rect.anchoredPosition = _coveredPosition;
        }

        public void DefaultFade()
        {
            FadeIn(DefaultDuration, DefaultEase, IgnoreTimescale);
        }

        // -------------------------
        // Core fade logic
        // -------------------------

        public virtual void FadeIn(float duration, Ease ease, bool ignoreTimeScale = true)
        {
            IgnoreTimescale = ignoreTimeScale;
            _tween?.Kill();

            // si le panneau était caché, on le replace de l'autre côté de l'écran avant de le faire entrer
            if (!_visible)
            {
                _rect.anchoredPosition = BeforeEntryPosition();
            }

            Show();
            MoveTo(_coveredPosition, duration, ease, null);
        }

        public virtual void FadeOut(float duration, Ease ease, bool ignoreTimeScale = true)
        {
            if (!_visible) return; // rien à découvrir

            IgnoreTimescale = ignoreTimeScale;
            _tween?.Kill();
            MoveTo(ExitPosition(), duration, ease, Hide);
        }

        protected virtual void MoveTo(Vector2 target, float duration, Ease ease, TweenCallback onComplete)
        {
            if (duration <= 0f)
            {
                _rect.anchoredPosition = target;
                onComplete?.Invoke();
                return;
            }

            _tween = _rect.DOAnchorPos(target, duration)
                          .SetEase(ease)
                          .SetUpdate(IgnoreTimescale);

            if (onComplete != null)
            {
                _tween.OnComplete(onComplete);
            }
        }

        protected virtual void Show()
        {
            _visible = true;
            _group.alpha = 1f;
            if (ShouldBlockRaycasts) _group.blocksRaycasts = true;
        }

        protected virtual void Hide()
        {
            _visible = false;
            _tween = null;
            _group.alpha = 0f;
            if (ShouldBlockRaycasts) _group.blocksRaycasts = false;
            _rect.anchoredPosition = BeforeEntryPosition();
        }

        /// <summary>Position de départ avant de couvrir : à l'opposé de la direction d'entrée.</summary>
        protected virtual Vector2 BeforeEntryPosition()
        {
            switch (FadeInDirection)
            {
                case Directions.BottomToTop: return _coveredPosition + Vector2.down * Height;
                case Directions.LeftToRight: return _coveredPosition + Vector2.left * Width;
                case Directions.RightToLeft: return _coveredPosition + Vector2.right * Width;
                default: return _coveredPosition + Vector2.up * Height; // TopToBottom
            }
        }

        /// <summary>Position d'arrivée quand on découvre l'écran.</summary>
        protected virtual Vector2 ExitPosition()
        {
            switch (FadeOutDirection)
            {
                case Directions.BottomToTop: return _coveredPosition + Vector2.up * Height;
                case Directions.LeftToRight: return _coveredPosition + Vector2.right * Width;
                case Directions.RightToLeft: return _coveredPosition + Vector2.left * Width;
                default: return _coveredPosition + Vector2.down * Height; // TopToBottom
            }
        }

        // -------------------------
        // Event listeners
        // -------------------------

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

        public void OnEvent(FadeEvent e)
        {
            if (e.ID != ID) return;
            if (e.TargetAlpha < 0f || e.TargetAlpha >= 0.5f)
                FadeIn(e.Duration, e.Curve, e.IgnoreTimeScale);
            else
                FadeOut(e.Duration, e.Curve, e.IgnoreTimeScale);
        }

        public void OnEvent(FadeStopEvent e)
        {
            if (e.ID != ID) return;
            _tween?.Kill();
            _tween = null;
            if (e.Restore)
                _rect.anchoredPosition = _coveredPosition;
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