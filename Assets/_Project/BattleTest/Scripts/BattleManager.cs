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


    private List<Card> _cards = new List<Card>();
    public IReadOnlyList<Card> Cards => _cards;


    private BattleState _currentBattleState;

    public DeckManager DeckManager { get; private set; }

    private void Awake()
    {
        Initialization();
    }

    private void Initialization()
    {

        foreach (var test in testDeck)
        {
            Card card = Instantiate(_cardPrefab, _handContainer);
            card.SetCard(test.Suit, test.Rank, _cardSelection);
            card.SetSprite(_cardSpriteDatabase.GetSprite(card));
            _cards.Add(card);
        }

        DeckManager = new DeckManager(_cards);
        DeckManager.DrawCards(_handCountPerTurn);
        _handLayout.ArrangeCards(_cards);


        _skillIconSelection.Setup(_playerUnit, DeckManager, _cardSelection);
    }

    private void Start()
    {
        _currentBattleState = BattleState.Start;
        StartPlayerTurn();
    }


    private void StartPlayerTurn()
    {
        _skillIconSelection.RefreshIcons();
    }

}
