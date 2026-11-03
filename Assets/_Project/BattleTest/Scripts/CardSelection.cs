using System;
using System.Collections.Generic;
using UnityEngine;

public class CardSelection : MonoBehaviour
{
    private readonly List<Card> _selectedCards = new List<Card>();
    public IReadOnlyList<Card> SelectedCards { get { return _selectedCards; } }

    public bool AutoSelectForCombo(PokerComboType requiredCombo)
    {
        return false;
    }

    public void ToggleCard(Card card)
    {
        if( card == null) return;

        if (_selectedCards.Contains(card))
        {
            card.SetSelected(false);
            _selectedCards.Remove(card);
        }           
        else
        {
            _selectedCards.Add(card);
            card.SetSelected(true);
        }
            

    }
}
