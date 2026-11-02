using System;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class BattleManager : MonoBehaviour
{
    [SerializeField] private BattleUnit _playerUnit;
    [SerializeField] private BattleUnit _enemyUnit;

    [SerializeField] private HUDManager _hudManager;

    private void Awake()
    {
        Initialization();
    }

    private void Initialization()
    {
        
    }
}
