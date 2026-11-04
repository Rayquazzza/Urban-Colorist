using System.Collections;
using UnityEngine;

public abstract class BattleUnit : MonoBehaviour
{
    protected Animator _animator;

    protected virtual void Awake()
    {
        _animator = gameObject.GetComponentAroundOrAdd<Animator>();
    }

    protected abstract IEnumerator StartTurn();

    protected abstract IEnumerator EndTurn();
}
