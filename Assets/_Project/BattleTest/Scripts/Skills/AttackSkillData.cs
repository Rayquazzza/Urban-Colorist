using UnityEngine;

[CreateAssetMenu(fileName = "Attack Skill", menuName = "CardBattle/Attack Skill", order = 1)]
public class AttackSkillData : PokerSkillData
{
    public override Skill ToSkill()
    {
        return new AttackSkill();
    }

    
}
