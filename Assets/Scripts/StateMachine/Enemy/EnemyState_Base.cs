using UnityEngine;

/// <summary>
/// 敌人状态基类，提供一些公共功能和属性
/// </summary>
public class EnemyState_Base : IState
{
    protected EnemyController enemy;
    protected StateMachine stateMachine;

    protected float nowTargetSqrDist;

    public virtual int Priority => 0;

    public EnemyState_Base(EnemyController enemy, StateMachine stateMachine)
    {
        this.enemy = enemy;
        this.stateMachine = stateMachine;
    }

    public virtual void OnEnter() { }
    public virtual void OnUpdate()
    {
        // 计算玩家与敌人之间的距离平方
        nowTargetSqrDist = (enemy.transform.position - enemy.player.transform.position).sqrMagnitude;
    }
    public virtual void OnExit() { }
}
