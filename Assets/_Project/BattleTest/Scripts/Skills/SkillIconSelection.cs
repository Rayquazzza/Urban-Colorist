using CardBattle;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillIconSelection : MonoBehaviour
{

    [SerializeField] private SkillIconButton _skillIconButtonPrefab;

    [SerializeField] private RectTransform _iconContainer;
    [SerializeField] private float _spacing = 100f;

    private PlayerBattleUnit _unit;
    private DeckManager _deck;
    private CardSelection _cardSelection;
    private readonly Dictionary<PokerSkillData, SkillIconButton> _viewsBySkill = new Dictionary<PokerSkillData, SkillIconButton>();
    private readonly List<PokerSkillData> _orderedSkills = new List<PokerSkillData>();

    public void Setup(PlayerBattleUnit unit, DeckManager deck, CardSelection cardSelection)
    {
        _unit = unit;   
        _deck = deck;
        _cardSelection = cardSelection;
    }    


    public void RefreshIcons()
    {
        
        if (_unit == null) return;

        List<PokerSkillData> formableSkills = _unit.SkillDatas.Where(skill => CardComboMatcher.TryFindCards(skill.requiredCombo, _deck.Hand, out _)).ToList();
        if (formableSkills.Count == 0) Debug.Log("There is any skill formable");
        List<PokerSkillData> toRemove = _orderedSkills.Where(s => !formableSkills.Contains(s)).ToList();

        foreach (var skill in toRemove)
        {
            if(_viewsBySkill.TryGetValue(skill, out var icon) && icon != null)
                    icon.AnimateOut(() => Destroy(icon.gameObject));
            _viewsBySkill.Remove(skill);
            _orderedSkills.Remove(skill);
        }

        List<SkillIconButton> newlyAdded = new List<SkillIconButton>();
        foreach (var skill in formableSkills)
        {
            if (_viewsBySkill.ContainsKey(skill)) continue;

            Debug.Log("Instantiate prefab");
            SkillIconButton icon = Instantiate(_skillIconButtonPrefab, _iconContainer);
            icon.Setup(skill, _cardSelection);
            _viewsBySkill[skill] = icon;
            _orderedSkills.Add(skill);
            newlyAdded.Add(icon);
        }

        Debug.Log("Arranging Icons");
        ArrangeIcons(newlyAdded);
        RefreshPreviewDimming();

    }

    /// <summary>
    /// Grise les skills qui ne seraient plus formables si la sélection
    /// actuelle était jouée (main restante = main - cartes sélectionnées).
    /// </summary>
    private void RefreshPreviewDimming()
    {
        IReadOnlyList<Card> selected = _cardSelection.SelectedCards;
        List<Card> remainingHand = _deck.Hand.Where(c => !selected.Contains(c)).ToList();

        PokerComboType currentCombo = selected.Count > 0 ? new PokerHandEvaluator().Evaluate(selected.ToList()) : PokerComboType.Invalid;

        foreach (var skill in _orderedSkills)
        {
            if (skill.requiredCombo == currentCombo && currentCombo != PokerComboType.Invalid)
            {
                _viewsBySkill[skill].SetPreviewDimmed(false); // c'est LE skill qu'on est en train de former, jamais grisé
                continue;
            }

            bool stillFormable = CardComboMatcher.TryFindCards(skill.requiredCombo, remainingHand, out _);
            _viewsBySkill[skill].SetPreviewDimmed(!stillFormable);
        }
    }

    private void ArrangeIcons(List<SkillIconButton> newlyAdded)
    {
        int count = _orderedSkills.Count;
        if(count == 0) return;

        float middle = (count - 1) / 2f;

        for (int i = 0; i < count; i++)
        {
            float x = (i - middle) * _spacing;
            var icon = _viewsBySkill[_orderedSkills[i]];

            if (newlyAdded.Contains(icon))
                icon.SetInitialPosition(x);

            icon.AnimateToPosition(x);
        }
    }
}
