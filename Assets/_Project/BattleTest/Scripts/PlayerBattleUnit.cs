
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBattleUnit : BattleUnit
{
    [SerializeField] private List<PokerSkillData> _skillDatas = new List<PokerSkillData>();

    public List<PokerSkillData> SkillDatas { get { return _skillDatas; } }
    
    protected override IEnumerator EndTurn()
    {
        yield return null;
    }


    protected override IEnumerator StartTurn()
    {
        yield return null;
    }
}
