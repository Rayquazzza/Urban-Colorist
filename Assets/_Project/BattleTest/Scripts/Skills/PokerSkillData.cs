using UnityEngine;

public abstract class PokerSkillData : ScriptableObject
{
    [Header("Condition de déclenchement")]
    public PokerComboType requiredCombo;

    [Header("Identité")]
    public string skillId;
    public string skillName;
    [TextArea] public string description;
    public Sprite icon;

    public abstract Skill ToSkill();
}