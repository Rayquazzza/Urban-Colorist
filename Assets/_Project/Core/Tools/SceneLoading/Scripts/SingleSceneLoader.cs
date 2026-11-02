using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Tools;

namespace Core.Tools
{
    /// <summary>
    /// Version classique (mode Single) : la scène de chargement REMPLACE la scène d'origine,
    /// puis la destination remplace la scène de chargement.
    /// À placer dans la scène de chargement avec un Fader (ou DirectionalFader) dont "Forced Init State" est sur Active
    /// (on arrive sur un écran couvert, puis on le découvre).
    ///
    /// Astuce : pour cacher la coupure A > chargement, couvre l'écran dans A avant d'appeler LoadScene.
    ///
    /// Séquence :
    /// reveal initial > charge en Single (activation bloquée) > barre de progression
    /// > animation "terminé" > delay > [hold] > cover > activation de la nouvelle scène
    /// </summary>
    public class SingleSceneLoader : MonoBehaviour
    {
        const float AsyncProgressLimit = 0.9f;

        [Header("Binding")]
        [Tooltip("Doit correspondre à l'ID du Fader ou du DirectionalFader")]
        public int FaderID = 500;

        [Header("UI")]
        public CanvasGroup LoadingProgressBar;
        public CanvasGroup LoadingAnimation;
        public CanvasGroup LoadingCompleteAnimation;
        [Tooltip("Durée du fondu entre barre/animation et animation 'terminé'")]
        public float UIFadeDuration = 0.1f;

        [Header("Time")]
        public float StartFadeDuration = 0.2f;
        public Ease StartFadeEase = Ease.OutCubic;
        public float ExitFadeDuration = 0.2f;
        public Ease ExitFadeEase = Ease.OutCubic;
        [Tooltip("Délai avant de quitter la scène une fois le chargement terminé")]
        public float LoadCompleteDelay = 0.5f;
        public float ProgressBarSpeed = 2f;
        public bool InterpolateProgress = true;

        [Header("Events")]
        public ProgressEvent OnRealtimeProgress;
        public ProgressEvent OnInterpolatedProgress;
        public UnityEvent OnLoadStarted;
        public UnityEvent OnLoadProgressComplete;
        public UnityEvent OnInterpolatedLoadProgressComplete;
        public UnityEvent OnLoadComplete;

        static string _sceneToLoad = "";

        float _loadProgress;
        float _interpolatedProgress;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _sceneToLoad = "";

        /// <summary>Appelle ça depuis n'importe quelle scène.</summary>
        public static void LoadScene(string sceneToLoad, string loadingSceneName = "LoadingScreen")
        {
            _sceneToLoad = sceneToLoad;
            Application.backgroundLoadingPriority = ThreadPriority.High;
            SceneManager.LoadScene(loadingSceneName); // mode Single : coupure sèche vers la scène de chargement
        }

        void Awake() => Time.timeScale = 1f;

        void Start()
        {
            SetGroup(LoadingProgressBar, 1f);
            SetGroup(LoadingAnimation, 1f);
            SetGroup(LoadingCompleteAnimation, 0f);

            if (!string.IsNullOrEmpty(_sceneToLoad)) StartCoroutine(LoadAsynchronously());
        }

        void Update()
        {
            OnRealtimeProgress?.Invoke(_loadProgress);

            _interpolatedProgress = InterpolateProgress
                ? Mathf.MoveTowards(_interpolatedProgress, _loadProgress, Time.unscaledDeltaTime * ProgressBarSpeed)
                : _loadProgress;
            OnInterpolatedProgress?.Invoke(_interpolatedProgress);
        }

        IEnumerator LoadAsynchronously()
        {
            Notify(LoadingStatus.LoadStarted, OnLoadStarted);

            // on découvre l'écran (le panneau part)
            yield return null;
            FadeOutEvent.Trigger(StartFadeDuration, StartFadeEase, FaderID, true);
            yield return new WaitForSecondsRealtime(StartFadeDuration);

            // chargement sans activation
            AsyncOperation op = SceneManager.LoadSceneAsync(_sceneToLoad, LoadSceneMode.Single);
            op.allowSceneActivation = false;

            while (op.progress < AsyncProgressLimit)
            {
                _loadProgress = Mathf.Clamp01(op.progress / AsyncProgressLimit);
                yield return null;
            }

            _loadProgress = 1f;
            Notify(LoadingStatus.LoadProgressComplete, OnLoadProgressComplete);

            while (_interpolatedProgress < 1f) yield return null;
            Notify(LoadingStatus.InterpolatedLoadProgressComplete, OnInterpolatedLoadProgressComplete);

            // barre/animation -> animation "terminé"
            ShowCompleteAnimation();
            yield return new WaitForSecondsRealtime(LoadCompleteDelay);

            while (SceneLoadingHolds.IsHeld(HoldPoint.BeforeSceneActivation)) yield return null;

            // on recouvre l'écran puis on active la nouvelle scène
            Notify(LoadingStatus.ExitFade, null);
            FadeInEvent.Trigger(ExitFadeDuration, ExitFadeEase, FaderID, true);
            yield return new WaitForSecondsRealtime(ExitFadeDuration);

            op.allowSceneActivation = true;
            Notify(LoadingStatus.LoadTransitionComplete, OnLoadComplete);
        }

        void ShowCompleteAnimation()
        {
            FadeGroup(LoadingProgressBar, 0f);
            FadeGroup(LoadingAnimation, 0f);
            if (LoadingCompleteAnimation != null) LoadingCompleteAnimation.gameObject.SetActive(true);
            FadeGroup(LoadingCompleteAnimation, 1f);
        }

        void FadeGroup(CanvasGroup group, float alpha)
        {
            if (group == null) return;
            group.DOFade(alpha, UIFadeDuration).SetUpdate(true);
        }

        static void SetGroup(CanvasGroup group, float alpha)
        {
            if (group != null) group.alpha = alpha;
        }

        void Notify(LoadingStatus status, UnityEvent unityEvent)
        {
            SceneLoadingEvents.RaiseStatus(_sceneToLoad, status);
            unityEvent?.Invoke();
        }
    }
}