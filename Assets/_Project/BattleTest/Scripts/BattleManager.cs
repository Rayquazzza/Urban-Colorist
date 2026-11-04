using CardBattle;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[Serializable]
public class TestCard
{
    public Suit Suit;
    public Rank Rank;
}

public enum BattleState { Start, PlayerTurn, EnemyTurn, Resolution, Victory, GameOver }

public class BattleManager : MonoBehaviour
{

    [Header("Test")]
    [SerializeField] private List<TestCard> testDeck;

    [Space(5)]
    [Header("Battlers")]
    [SerializeField] private PlayerBattleUnit _playerUnit;
    [SerializeField] private BattleUnit _enemyUnit;

    [Space(5)]
    [Header("HUD")]
    [SerializeField] private HUDManager _hudManager;

    [Space(5)]
    [Header("Data")]
    [SerializeField] private CardSpriteDatabase _cardSpriteDatabase;


    [Space(5)]
    [Header("Prefabs")]
    [SerializeField] private Card _cardPrefab;

    [Space(5)]
    [Header("Hand")]
    [SerializeField] private CardHandLayout _handLayout;
    [SerializeField] private RectTransform _handContainer;
    [SerializeField] private int _handCountPerTurn = 6;

    [Space(5)]
    [Header("Selection")]
    [SerializeField] private CardSelection _cardSelection;
    [SerializeField] private SkillIconSelection _skillIconSelection;



    private BattleState _currentBattleState;

    public DeckManager DeckManager { get; private set; }

    private void Awake()
    {
        Initialization();
    }

    private void Initialization()
    {    
        var cards = new List<Card>();

        foreach (var test in testDeck)
        {
            Card card = Instantiate(_cardPrefab, _handContainer);
            card.SetCard(test.Suit, test.Rank, _cardSelection);
            card.SetSprite(_cardSpriteDatabase.GetSprite(card));
            cards.Add(card);
        }

        DeckManager = new DeckManager(cards);

        
    }

    private void Start()
    {
        DeckManager.DrawCards(_handCountPerTurn);
        _hudManager.InitCards(DeckManager.Hand.ToList());
        _handLayout.ArrangeCards(DeckManager.Hand.ToList());

        _skillIconSelection.Setup(_playerUnit, DeckManager, _cardSelection);

        _currentBattleState = BattleState.Start;

        StartPlayerTurn();
    }


    private void StartPlayerTurn()
    {
        _skillIconSelection.RefreshIcons();
        _hudManager.RefreshPlayerTurnUI(DeckManager, _cardSelection);
    }

}
