using System.Collections;
using UnityEngine;

public class EnemyBattleUnit : BattleUnit
{
    protected override IEnumerator EndTurn()
    {
        yield return null;
    }

    protected override IEnumerator StartTurn()
    {
        yield return null;
    }

}
