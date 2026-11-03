using System.Collections.Generic;
using System.Linq;

namespace CardBattle
{
    /// <summary>
    /// Logique d'auto-sélection utilisée quand le joueur clique sur une icône
    /// de compétence : trouve un sous-ensemble de la main qui forme le combo
    /// demandé. Règle par défaut (facilement modifiable ici, centralisée) :
    /// privilégier les cartes de plus faible valeur, pour garder les cartes
    /// fortes disponibles pour d'autres combos dans le même tour.
    /// </summary>
    public static class CardComboMatcher
    {
        public static bool TryFindCards(PokerComboType combo, IReadOnlyList<Card> hand, out List<Card> result)
        {
            result = null;
            List<Card> playable = new List<Card>(hand);

            switch (combo)
            {
                case PokerComboType.HighCard:
                    if (hand.Count == 0) return false;
                    result = new List<Card> { hand.OrderBy(c => (int)c.Rank).First() };
                    return true;

                case PokerComboType.Pair:
                    return TryFindOfAKind(playable, 2, out result);

                case PokerComboType.ThreeOfAKind:
                    return TryFindOfAKind(playable, 3, out result);

                case PokerComboType.TwoPair:
                    return TryFindTwoPair(playable, out result);
                case PokerComboType.MiniFlush:
                    return TryFindSameSuit(playable, 3, out result);

                case PokerComboType.Straight:
                    return TryFindStraight(playable, requireSameSuit: false, out result);

                case PokerComboType.StraightFlush:
                    return TryFindStraight(playable, requireSameSuit: true, out result);

                case PokerComboType.FullHouse:
                    return TryFindFullHouse(playable, out result);
            }

            return false;
        }

        private static bool TryFindOfAKind(List<Card> hand, int count, out List<Card> result)
        {
            result = null;
            var group = hand
                .GroupBy(c => c.Rank)
                .Where(g => g.Count() >= count)
                .OrderBy(g => (int)g.Key) // valeur la plus basse d'abord
                .FirstOrDefault();

            if (group == null) return false;

            result = group.OrderBy(c => (int)c.Rank).Take(count).ToList();
            return true;
        }

        private static bool TryFindTwoPair(List<Card> hand, out List<Card> result)
        {
            result = null;
            var pairGroups = hand
                .GroupBy(c => c.Rank)
                .Where(g => g.Count() >= 2)
                .OrderBy(g => (int)g.Key)
                .Take(2)
                .ToList();

            if (pairGroups.Count < 2) return false;

            result = pairGroups.SelectMany(g => g.Take(2)).ToList();
            return true;
        }

        private static bool TryFindSameSuit(List<Card> hand, int count, out List<Card> result)
        {
            result = null;
            var group = hand
                .GroupBy(c => c.Suit)
                .Where(g => g.Count() >= count)
                .OrderBy(g => g.Min(c => (int)c.Rank))
                .FirstOrDefault();

            if (group == null) return false;

            result = group.OrderBy(c => (int)c.Rank).Take(count).ToList();
            return true;
        }

        private static bool TryFindStraight(List<Card> hand, bool requireSameSuit, out List<Card> result)
        {
            result = null;

            IEnumerable<IGrouping<Suit, Card>> candidateGroups = requireSameSuit
                ? hand.GroupBy(c => c.Suit)
                : new[] { new Grouping<Suit, Card>(default, hand) };

            foreach (var group in candidateGroups)
            {
                var bySingleRank = group
                    .GroupBy(c => c.Rank)
                    .ToDictionary(g => g.Key, g => g.First());

                var sortedRanks = bySingleRank.Keys.Select(r => (int)r).OrderBy(r => r).ToList();
                if (sortedRanks.Count < 5) continue;

                for (int startValue = sortedRanks.Min(); startValue <= sortedRanks.Max() - 4; startValue++)
                {
                    bool allPresent = true;
                    var window = new List<Card>();
                    for (int offset = 0; offset < 5; offset++)
                    {
                        var rank = (Rank)(startValue + offset);
                        if (!bySingleRank.TryGetValue(rank, out var card)) { allPresent = false; break; }
                        window.Add(card);
                    }

                    if (allPresent)
                    {
                        result = window;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryFindFullHouse(List<Card> hand, out List<Card> result)
        {
            result = null;

            var groups = hand.GroupBy(c => c.Rank).OrderBy(g => (int)g.Key).ToList();

            var threeGroup = groups.FirstOrDefault(g => g.Count() >= 3);
            if (threeGroup == null) return false;

            var pairGroup = groups.FirstOrDefault(g => g.Key != threeGroup.Key && g.Count() >= 2);
            if (pairGroup == null) return false;

            result = threeGroup.Take(3).Concat(pairGroup.Take(2)).ToList();
            return true;
        }

        /// <summary>Petit utilitaire pour traiter "toute la main" comme un groupe unique dans TryFindStraight.</summary>
        private class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
        {
            private readonly TKey key;
            private readonly IEnumerable<TElement> items;

            public Grouping(TKey key, IEnumerable<TElement> items)
            {
                this.key = key;
                this.items = items;
            }

            public TKey Key => key;
            public IEnumerator<TElement> GetEnumerator() => items.GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
