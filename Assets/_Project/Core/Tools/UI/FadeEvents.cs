using DG.Tweening;
using Core.Events;

namespace Tools
{
    /// <summary>Arrête le fade en cours sur le fader d'ID donné (Restore : revient à l'état initial).</summary>
    public struct FadeStopEvent
    {
        public int ID;
        public bool Restore;

        public FadeStopEvent(int id = 0, bool restore = false)
        {
            ID = id;
            Restore = restore;
        }

        public static void Trigger(int id = 0, bool restore = false)
        {
            EventBus.Publish(new FadeStopEvent(id, restore));
        }
    }

    /// <summary>Fait apparaître le fader (il COUVRE l'écran).</summary>
    public struct FadeInEvent
    {
        public int ID;
        public float Duration;
        public Ease Curve;
        public bool IgnoreTimeScale;

        public FadeInEvent(float duration, Ease curve, int id = 0, bool ignoreTimeScale = true)
        {
            ID = id;
            Duration = duration;
            Curve = curve;
            IgnoreTimeScale = ignoreTimeScale;
        }

        public static void Trigger(float duration, Ease curve, int id = 0, bool ignoreTimeScale = true)
        {
            EventBus.Publish(new FadeInEvent(duration, curve, id, ignoreTimeScale));
        }
    }

    /// <summary>Fait disparaître le fader (il DÉCOUVRE l'écran).</summary>
    public struct FadeOutEvent
    {
        public int ID;
        public float Duration;
        public Ease Curve;
        public bool IgnoreTimeScale;

        public FadeOutEvent(float duration, Ease curve, int id = 0, bool ignoreTimeScale = true)
        {
            ID = id;
            Duration = duration;
            Curve = curve;
            IgnoreTimeScale = ignoreTimeScale;
        }

        public static void Trigger(float duration, Ease curve, int id = 0, bool ignoreTimeScale = true)
        {
            EventBus.Publish(new FadeOutEvent(duration, curve, id, ignoreTimeScale));
        }
    }

    /// <summary>
    /// Fade vers une opacité précise (Fader). Le DirectionalFader l'interprète ainsi :
    /// TargetAlpha &lt; 0 ou &gt;= 0.5 = FadeIn, sinon FadeOut.
    /// </summary>
    public struct FadeEvent
    {
        public int ID;
        public float Duration;
        public float TargetAlpha;
        public Ease Curve;
        public bool IgnoreTimeScale;

        public FadeEvent(float duration, float targetAlpha, Ease curve, int id = 0, bool ignoreTimeScale = true)
        {
            ID = id;
            Duration = duration;
            TargetAlpha = targetAlpha;
            Curve = curve;
            IgnoreTimeScale = ignoreTimeScale;
        }

        public static void Trigger(float duration, float targetAlpha, Ease curve, int id = 0, bool ignoreTimeScale = true)
        {
            EventBus.Publish(new FadeEvent(duration, targetAlpha, curve, id, ignoreTimeScale));
        }
    }
}