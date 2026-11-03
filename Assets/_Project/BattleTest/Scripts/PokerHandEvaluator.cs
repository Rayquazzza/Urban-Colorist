using System.Collections.Generic;
using System.Linq;

namespace CardBattle
{
    public interface IHandEvaluator
    {
        PokerComboType Evaluate(List<Card> selection);
    }

    /// <summary>
    /// Contrairement au poker classique (meilleure main sur 5 cartes fixes),
    /// Zoeti détecte la forme EXACTE de la sélection du joueur - chaque combo
    /// a un nombre de cartes précis attendu (Pair=2, MiniFlush=3, Straight=5...).
    /// Une sélection qui ne correspond à aucune forme reconnue est Invalid.
    /// </summary>
    public class PokerHandEvaluator : IHandEvaluator
    {
        public PokerComboType Evaluate(List<Card> selection)
        {
            if (selection == null || selection.Count == 0)
                return PokerComboType.Invalid;

            if (selection.Count == 1)
                return PokerComboType.HighCard;

            List<int> rankGroupSizes = selection
                .GroupBy(c => c.Rank)
                .Select(g => g.Count())
                .OrderByDescending(count => count)
                .ToList();

            bool allSameSuit = selection.All(c => c.Suit == selection[0].Suit);
            bool isSequential = IsSequential(selection);

            switch (selection.Count)
            {
                case 2:
                    if (rankGroupSizes[0] == 2) return PokerComboType.Pair;
                    break;

                case 3:
                    if (rankGroupSizes[0] == 3) return PokerComboType.ThreeOfAKind; // priorité au brelan
                    if (allSameSuit) return PokerComboType.MiniFlush;
                    break;

                case 4:
                    if (rankGroupSizes.Count == 2 && rankGroupSizes[0] == 2 && rankGroupSizes[1] == 2)
                        return PokerComboType.TwoPair;
                    break;

                case 5:
                    if (isSequential && allSameSuit) return PokerComboType.StraightFlush;
                    if (rankGroupSizes[0] == 3 && rankGroupSizes.Count > 1 && rankGroupSizes[1] == 2)
                        return PokerComboType.FullHouse;
                    if (isSequential) return PokerComboType.Straight;
                    break;
            }

            return PokerComboType.Invalid;
        }

        private bool IsSequential(List<Card> selection)
        {
            var orderedRanks = selection
                .Select(c => (int)c.Rank)
                .Distinct()
                .OrderBy(r => r)
                .ToList();

            if (orderedRanks.Count != selection.Count) return false; // doublons de rang = pas de suite

            for (int i = 1; i < orderedRanks.Count; i++)
            {
                if (orderedRanks[i] != orderedRanks[i - 1] + 1)
                    return false;
            }

            return true;
        }
    }
}
