using UnityEngine;
using UnityEngine.UI;

namespace Core.Tools
{
    /// <summary>Branche SetProgress sur l'event "Interpolated Progress" du loader. L'Image doit être de type Filled.</summary>
    public class SceneLoadingProgressBar : MonoBehaviour
    {
        [Tooltip("Laisse vide pour utiliser l'Image de ce GameObject")]
        public Image Fill;

        void Awake()
        {
            if (Fill == null) Fill = GetComponent<Image>();
        }

        public void SetProgress(float value)
        {
            if (Fill != null) Fill.fillAmount = value;
        }
    }
}
