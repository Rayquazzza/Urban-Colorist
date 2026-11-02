using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Core.Tools
{
    [Serializable]
    public class ProgressEvent : UnityEvent<float> { }

    /// <summary>Étapes de la séquence de chargement (utilisées par l'event statique et les UnityEvents).</summary>
    public enum LoadingStatus
    {
        LoadStarted,
        BeforeEntryFade,
        EntryFade,
        AfterEntryFade,
        UnloadOriginScene,
        LoadDestinationScene,
        LoadProgressComplete,
        InterpolatedLoadProgressComplete,
        BeforeSceneActivation,
        DestinationSceneActivation,
        AfterSceneActivation,
        ExitFade,
        UnloadSceneLoader,
        LoadTransitionComplete
    }

    public enum UnloadMethods { None, ActiveScene, AllScenes }

    /// <summary>
    /// Points où la séquence peut être mise en attente depuis l'extérieur (voir SceneLoadingHolds).
    /// </summary>
    public enum HoldPoint
    {
        AfterEntryCover,        // après que l'écran soit couvert, avant de décharger l'ancienne scène
        AfterUnloadOrigin,      // après le déchargement de l'ancienne scène, avant de charger la nouvelle
        BeforeSceneActivation,  // nouvelle scène chargée mais pas encore activée
        BeforeExitReveal        // nouvelle scène activée, écran encore couvert
    }

    [Serializable]
    public class SpeedInterval
    {
        [Range(0f, 1f)] public float From = 0f;
        [Range(0f, 1f)] public float To = 1f;
        public float Speed = 1f;
        public bool Contains(float t) => t >= From && t <= To;
    }

    /// <summary>Événement statique émis à chaque étape de la séquence (pour que d'autres scripts puissent réagir).</summary>
    public static class SceneLoadingEvents
    {
        public static event Action<string, LoadingStatus> StatusChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => StatusChanged = null;

        public static void RaiseStatus(string sceneName, LoadingStatus status) => StatusChanged?.Invoke(sceneName, status);
    }

    /// <summary>
    /// Permet à n'importe quel script de bloquer la séquence à un point précis.
    /// Exemple : SceneLoadingHolds.Set(HoldPoint.BeforeSceneActivation, true); ... puis false quand ta sauvegarde est finie.
    /// </summary>
    public static class SceneLoadingHolds
    {
        static readonly Dictionary<HoldPoint, bool> _holds = new Dictionary<HoldPoint, bool>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _holds.Clear();

        public static void Set(HoldPoint point, bool state) => _holds[point] = state;
        public static bool IsHeld(HoldPoint point) => _holds.TryGetValue(point, out bool v) && v;
        public static void ClearAll() => _holds.Clear();
    }

    /// <summary>Réglages du chargement additif (délais, durées, courbes DOTween...).</summary>
    [Serializable]
    public class SceneLoadingSettings
    {
        public string LoadingSceneName = "AdditiveLoadingScreen";
        public ThreadPriority ThreadPriority = ThreadPriority.High;
        public UnloadMethods UnloadMethod = UnloadMethods.AllScenes;
        [Tooltip("Vérifie que les scènes sont dans le Build Settings avant de lancer")]
        public bool SecureLoad = true;

        [Header("Progress")]
        public bool InterpolateProgress = true;
        public float ProgressBarSpeed = 5f;
        public List<SpeedInterval> SpeedIntervals = new List<SpeedInterval>();

        [Header("Delays (secondes, temps non scalé)")]
        public float BeforeEntryFadeDelay = 0f;
        public float AfterEntryFadeDelay = 0.1f;
        public float BeforeSceneActivationDelay = 0.25f;
        public float AfterSceneActivationDelay = 0f;

        [Header("Fades")]
        public float EntryFadeDuration = 0.25f;
        public Ease EntryFadeEase = Ease.InOutCubic;
        public float ExitFadeDuration = 0.2f;
        public Ease ExitFadeEase = Ease.InOutCubic;

        public bool DebugMode = false;
    }
}