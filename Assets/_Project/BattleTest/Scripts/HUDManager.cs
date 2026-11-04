using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CardBattle;

public class HUDManager : MonoBehaviour
{
    [Header("Health Bars")]
    [SerializeField] private RadialHealthBar _playerHealthBar;
    [SerializeField] private EnemyHealthBar _enemyHealthBar;

    [Header("Hand UI Settings")]
    [SerializeField] private CardHandLayout _handLayout;
    [SerializeField] private RectTransform _handContainer;
    [SerializeField] private Card _cardPrefab;
    [SerializeField] private CardSpriteDatabase _cardSpriteDatabase;


    private List<Card> _cardsInHand = new List<Card>();
   
    public void ReconcileHandDisplay(DeckManager deckManager, CardSelection cardSelection)
    {
        if (deckManager == null) return;

        var currentHand = deckManager.Hand;


        List<Card> toRemove = _cardsInHand.Where(card => !currentHand.Contains(card)).ToList();

        foreach (Card card in toRemove)
        {
            _cardsInHand.Remove(card);
            Destroy(card.gameObject);
        }


        foreach (var cardInstance in currentHand)
        {
            // On vérifie si cette instance exacte de carte n'est pas déjà affichée
            if (!_cardsInHand.Contains(cardInstance))
            {
                if (_cardPrefab != null)
                {
                    Card cardView = Instantiate(_cardPrefab, _handContainer);
                    cardView.SetCard(cardInstance.Suit, cardInstance.Rank, cardSelection);
                    cardView.SetSprite(_cardSpriteDatabase.GetSprite(cardView));
                    _cardsInHand.Add(cardView); // Stocke la vue instanciée
                }
            }
        }

    }


    public void InitCards(List<Card> cards) => _cardsInHand = cards;

    public void RefreshPlayerTurnUI(DeckManager deckManager, CardSelection cardSelection)
    {
        ReconcileHandDisplay(deckManager, cardSelection);

    }
}