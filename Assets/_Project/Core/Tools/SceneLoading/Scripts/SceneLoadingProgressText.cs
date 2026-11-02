using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace Core.Tools
{
    /// <summary>Affiche la progression dans un TextMeshPro, avec remap, décimales et suffixe.</summary>
    public class SceneLoadingProgressText : MonoBehaviour
    {
        [Tooltip("Laisse vide pour utiliser le TMP_Text de ce GameObject")]
        public TMP_Text Text;
        [Tooltip("Valeur affichée quand la progression est à 0")]
        public float RemapMin = 0f;
        [Tooltip("Valeur affichée quand la progression est à 1")]
        public float RemapMax = 100f;
        public int NumberOfDecimals = 0;
        [Tooltip("Texte ajouté après le nombre, ex : %")]
        public string Suffix = "%";

        void Awake()
        {
            if (Text == null) Text = GetComponent<TMP_Text>();
        }

        public void SetProgress(float value)
        {
            if (Text == null) return;
            float remapped = Mathf.Lerp(RemapMin, RemapMax, value);
            float display = (float)Math.Round(remapped, NumberOfDecimals);
            Text.text = display.ToString(CultureInfo.InvariantCulture) + Suffix;
        }
    }
}
