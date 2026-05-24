/// <summary>
/// 受击状态
/// </summary>
public class EnemyState_Hit : EnemyState_Base
{
    public override int Priority => 10;

    public EnemyState_Hit(EnemyController enemy, StateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void OnEnter()
    {
        enemy.animController.OnHitEndEvent += OnHitEnd; // 订阅受击动画结束事件

        enemy.animController.DoMove(false);
        enemy.agent.isStopped = true; // 停止移动
        enemy.animController.DoHit(); // 播放受击动画
    }

    public override void OnExit()
    {
        enemy.agent.isStopped = false; // 恢复移动

        enemy.animController.OnHitEndEvent -= OnHitEnd; // 取消订阅受击动画结束事件
    }

    public void OnHitEnd()
    {
        stateMachine.ChangeState(enemy.chaseState);
    }
}
