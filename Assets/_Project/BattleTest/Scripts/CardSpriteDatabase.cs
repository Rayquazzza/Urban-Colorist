using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardBattle
{
    /// <summary>
    /// Table de correspondance entre une Card (donnée pure) et le Sprite Kenney
    /// à afficher. Un seul asset, réutilisé par toutes les CardView - aucune
    /// logique de "quel sprite pour quelle carte" ne vit dans le code, tout est
    /// configuré ici, glissé dans l'inspecteur.
    ///
    /// Organisé par suit avec un tableau de 13 sprites (Deux à As, dans l'ordre)
    /// plutôt qu'une liste plate de 52 entrées - correspond à la façon dont
    /// Kenney exporte généralement ses feuilles de cartes (rangée par enseigne).
    /// </summary>
    [CreateAssetMenu(menuName = "CardBattle/CardSpriteDatabase", fileName = "CardSpriteDatabase")]
    public class CardSpriteDatabase : ScriptableObject
    {
        [Serializable]
        public class SuitSpriteSet
        {
            public Suit suit;

            [Tooltip("7 sprites dans l'ordre : 1, 2, 3, 4, 5, 6, 7")]
            public Sprite[] ranks = new Sprite[7];
        }

        public List<SuitSpriteSet> suits = new List<SuitSpriteSet>();


        public Sprite cardBackSprite; // pour une carte face cachée, si besoin plus tard

        public Sprite GetSprite(Card card)
        {
            var set = suits.Find(s => s.suit == card.Suit);
            if (set == null)
            {
                Debug.LogWarning($"CardSpriteDatabase: aucune entrée pour l'enseigne {card.Suit}");
                return null;
            }

            int index = (int)card.Rank - (int)Rank.Un; // Un=0, Deux=1, ..., Sept=6
            if (index < 0 || index >= set.ranks.Length)
            {
                Debug.LogWarning($"CardSpriteDatabase: rang {card.Rank} hors limites pour {card.Suit}");
                return null;
            }

            return set.ranks[index];
        }
    }
}
