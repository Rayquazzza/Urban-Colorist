using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Tools;

namespace Core.Tools
{
    /// <summary>
    /// Version additive : la scène de chargement est chargée PAR-DESSUS la scène d'origine.
    /// À placer dans la scène de chargement (avec un Fader ou DirectionalFader du même ID).
    ///
    /// Séquence :
    /// delay avant fade d'entrée > entry fade (couvre) > delay après > [hold] > décharge origine > [hold]
    /// > charge destination > delay avant activation > [hold] > activation > delay après
    /// > [hold] > exit fade (découvre) > décharge la scène de chargement
    /// </summary>
    public class AdditiveSceneLoader : MonoBehaviour
    {
        const float AsyncProgressLimit = 0.9f;

        [Header("Settings")]
        [Tooltip("Doit correspondre à l'ID du Fader ou du DirectionalFader")]
        public int FaderID = 500;

        [Header("Progress Events")]
        public ProgressEvent OnRealtimeProgress;
        public ProgressEvent OnInterpolatedProgress;

        [Header("State Events")]
        public UnityEvent OnLoadStarted;
        public UnityEvent OnBeforeEntryFade;
        public UnityEvent OnEntryFade;
        public UnityEvent OnAfterEntryFade;
        public UnityEvent OnUnloadOriginScene;
        public UnityEvent OnLoadDestinationScene;
        public UnityEvent OnLoadProgressComplete;
        public UnityEvent OnInterpolatedLoadProgressComplete;
        public UnityEvent OnBeforeSceneActivation;
        public UnityEvent OnDestinationSceneActivation;
        public UnityEvent OnAfterSceneActivation;
        public UnityEvent OnExitFade;
        public UnityEvent OnUnloadSceneLoader;

        // État partagé entre la scène d'origine et la scène de chargement (static = survit aux changements de scène)
        static string _sceneToLoad = "";
        static SceneLoadingSettings _settings;
        static Scene[] _initialScenes;
        static bool _loadingInProgress;

        float _loadProgress;
        float _interpolatedProgress;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _sceneToLoad = "";
            _settings = null;
            _initialScenes = null;
            _loadingInProgress = false;
        }

        // ---------------------------------------------------------------- API

        /// <summary>Appelle ça depuis n'importe quelle scène.</summary>
        public static void LoadScene(string sceneToLoad, SceneLoadingSettings settings = null)
        {
            settings = settings ?? new SceneLoadingSettings();

            if (_loadingInProgress)
            {
                Debug.LogError("AdditiveSceneLoader : un chargement est déjà en cours.");
                return;
            }

            if (settings.SecureLoad)
            {
                if (!Application.CanStreamedLevelBeLoaded(sceneToLoad))
                {
                    Debug.LogError($"AdditiveSceneLoader : '{sceneToLoad}' n'est pas dans le Build Settings.");
                    return;
                }
                if (!Application.CanStreamedLevelBeLoaded(settings.LoadingSceneName))
                {
                    Debug.LogError($"AdditiveSceneLoader : '{settings.LoadingSceneName}' n'est pas dans le Build Settings.");
                    return;
                }
            }

            _loadingInProgress = true;
            _sceneToLoad = sceneToLoad;
            _settings = settings;
            _initialScenes = GetScenesToUnload(settings.UnloadMethod); // AVANT de charger la scène de chargement

            Application.backgroundLoadingPriority = settings.ThreadPriority;
            SceneManager.LoadScene(settings.LoadingSceneName, LoadSceneMode.Additive);
        }

        static Scene[] GetScenesToUnload(UnloadMethods method)
        {
            switch (method)
            {
                case UnloadMethods.None:
                    return new Scene[0];
                case UnloadMethods.ActiveScene:
                    return new[] { SceneManager.GetActiveScene() };
                default:
                    var list = new List<Scene>();
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        Scene s = SceneManager.GetSceneAt(i);
                        if (s.isLoaded) list.Add(s);
                    }
                    return list.ToArray();
            }
        }

        // ---------------------------------------------------------------- Unity

        void Awake()
        {
            Time.timeScale = 1f; // on peut arriver ici depuis un menu pause
            if (string.IsNullOrEmpty(_sceneToLoad) || _settings == null) return; // lancée directement dans l'éditeur
            StartCoroutine(LoadSequence());
        }

        void Update()
        {
            if (_settings == null) return;

            OnRealtimeProgress?.Invoke(_loadProgress);

            if (_settings.InterpolateProgress)
            {
                _interpolatedProgress = Mathf.MoveTowards(_interpolatedProgress, _loadProgress,
                    Time.unscaledDeltaTime * ComputeSpeed(_interpolatedProgress));
            }
            else
            {
                _interpolatedProgress = _loadProgress;
            }
            OnInterpolatedProgress?.Invoke(_interpolatedProgress);
        }

        void OnDestroy()
        {
            _loadingInProgress = false;
            _sceneToLoad = "";
            _settings = null;
        }

        float ComputeSpeed(float t)
        {
            if (_settings.SpeedIntervals != null)
            {
                foreach (SpeedInterval interval in _settings.SpeedIntervals)
                {
                    if (interval.Contains(t)) return interval.Speed;
                }
            }
            return _settings.ProgressBarSpeed;
        }

        // ---------------------------------------------------------------- Séquence

        IEnumerator LoadSequence()
        {
            SceneLoadingSettings s = _settings;
            _loadProgress = 0f;
            _interpolatedProgress = 0f;

            Notify(LoadingStatus.LoadStarted, OnLoadStarted);

            // 1. delay avant fade d'entrée
            if (s.BeforeEntryFadeDelay > 0f)
            {
                Notify(LoadingStatus.BeforeEntryFade, OnBeforeEntryFade);
                yield return new WaitForSecondsRealtime(s.BeforeEntryFadeDelay);
            }

            // 2. entry fade : le panneau couvre l'écran (l'ancienne scène est encore là dessous)
            if (s.EntryFadeDuration > 0f)
            {
                Notify(LoadingStatus.EntryFade, OnEntryFade);
                yield return null; // laisse le fader s'abonner / s'initialiser
                FadeInEvent.Trigger(s.EntryFadeDuration, s.EntryFadeEase, FaderID, true);
                yield return new WaitForSecondsRealtime(s.EntryFadeDuration);
            }

            yield return WaitWhileHeld(HoldPoint.AfterEntryCover);

            if (s.AfterEntryFadeDelay > 0f)
            {
                Notify(LoadingStatus.AfterEntryFade, OnAfterEntryFade);
                yield return new WaitForSecondsRealtime(s.AfterEntryFadeDelay);
            }

            // 3. on décharge l'ancienne scène (invisible car couverte)
            yield return UnloadOriginScenes();
            yield return WaitWhileHeld(HoldPoint.AfterUnloadOrigin);

            // 4. on charge la destination sans l'activer
            AsyncOperation loadOp = null;
            yield return LoadDestination(op => loadOp = op);

            // 5. delay + hold avant activation
            if (s.BeforeSceneActivationDelay > 0f)
            {
                Notify(LoadingStatus.BeforeSceneActivation, OnBeforeSceneActivation);
                yield return new WaitForSecondsRealtime(s.BeforeSceneActivationDelay);
            }
            yield return WaitWhileHeld(HoldPoint.BeforeSceneActivation);

            // 6. activation
            yield return null;
            loadOp.allowSceneActivation = true;
            while (!loadOp.isDone) yield return null;

            Scene destination = SceneManager.GetSceneByName(System.IO.Path.GetFileNameWithoutExtension(_sceneToLoad));
            if (destination.IsValid()) SceneManager.SetActiveScene(destination);
            Notify(LoadingStatus.DestinationSceneActivation, OnDestinationSceneActivation);

            if (s.AfterSceneActivationDelay > 0f)
            {
                Notify(LoadingStatus.AfterSceneActivation, OnAfterSceneActivation);
                yield return new WaitForSecondsRealtime(s.AfterSceneActivationDelay);
            }

            yield return WaitWhileHeld(HoldPoint.BeforeExitReveal);

            // 7. exit fade : le panneau repart, on découvre la nouvelle scène
            if (s.ExitFadeDuration > 0f)
            {
                Notify(LoadingStatus.ExitFade, OnExitFade);
                FadeOutEvent.Trigger(s.ExitFadeDuration, s.ExitFadeEase, FaderID, true);
                yield return new WaitForSecondsRealtime(s.ExitFadeDuration);
            }

            // 8. on décharge la scène de chargement (ce qui détruit ce composant)
            Notify(LoadingStatus.UnloadSceneLoader, OnUnloadSceneLoader);
            yield return null;
            AsyncOperation unloadLoader = SceneManager.UnloadSceneAsync(s.LoadingSceneName);
            while (unloadLoader != null && !unloadLoader.isDone) yield return null;
        }

        IEnumerator UnloadOriginScenes()
        {
            Notify(LoadingStatus.UnloadOriginScene, OnUnloadOriginScene);

            foreach (Scene scene in _initialScenes)
            {
                if (!scene.IsValid() || !scene.isLoaded)
                {
                    Debug.LogWarning("AdditiveSceneLoader : scène invalide ignorée : " + scene.name);
                    continue;
                }

                AsyncOperation op = SceneManager.UnloadSceneAsync(scene);
                while (op != null && !op.isDone) yield return null;
            }
        }

        IEnumerator LoadDestination(System.Action<AsyncOperation> onOperation)
        {
            Notify(LoadingStatus.LoadDestinationScene, OnLoadDestinationScene);

            AsyncOperation op = SceneManager.LoadSceneAsync(_sceneToLoad, LoadSceneMode.Additive);
            op.allowSceneActivation = false;
            onOperation(op);

            // la progression Unity s'arrête à 0.9 tant que l'activation est bloquée : on la normalise sur 0..1
            while (op.progress < AsyncProgressLimit)
            {
                _loadProgress = Mathf.Clamp01(op.progress / AsyncProgressLimit);
                yield return null;
            }

            _loadProgress = 1f;
            Notify(LoadingStatus.LoadProgressComplete, OnLoadProgressComplete);

            // on attend que la barre soit visuellement pleine
            if (_settings.InterpolateProgress)
            {
                while (_interpolatedProgress < 1f) yield return null;
            }
            Notify(LoadingStatus.InterpolatedLoadProgressComplete, OnInterpolatedLoadProgressComplete);
        }

        IEnumerator WaitWhileHeld(HoldPoint point)
        {
            while (SceneLoadingHolds.IsHeld(point)) yield return null;
        }

        void Notify(LoadingStatus status, UnityEvent unityEvent)
        {
            if (_settings != null && _settings.DebugMode)
            {
                Debug.Log($"[AdditiveSceneLoader] [{Time.frameCount}] {status}");
            }
            SceneLoadingEvents.RaiseStatus(_sceneToLoad, status);
            unityEvent?.Invoke();
        }
    }
}