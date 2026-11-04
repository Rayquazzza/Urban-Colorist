using System.Collections.Generic;
using UnityEngine;

public class DeckManager
{
    private readonly List<Card> _drawPile = new();
    private readonly List<Card> _discardPile = new();
    private readonly List<Card> _hand = new();

    public IReadOnlyList<Card> Hand => _hand;

    public DeckManager(IEnumerable<Card> cards)
    {
        _drawPile.AddRange(cards);
        _discardPile.Clear();
        _hand.Clear();
        Shuffle();
    }

    public List<Card> DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (_drawPile.Count == 0)
                RecycleDiscardIntoDrawPile();

            if (_drawPile.Count == 0)
                break;

            var drawn = _drawPile[0];
            _drawPile.RemoveAt(0);
            _hand.Add(drawn);
        }

        return _hand;
    }

    public void Discard(Card card) => _discardPile.Add(card);

    private void Shuffle()
    {
        for (int i = _drawPile.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (_drawPile[i], _drawPile[j]) = (_drawPile[j], _drawPile[i]);
        }
    }

    public void DiscardPlayedCards(List<Card> playedCards)
    {
        foreach(var card in playedCards)
        {
            if (_hand.Remove(card))
                _discardPile.Add(card);
        }
    }

    private void RecycleDiscardIntoDrawPile()
    {
        _drawPile.AddRange(_discardPile);
        _discardPile.Clear();
        Shuffle();
    }
}