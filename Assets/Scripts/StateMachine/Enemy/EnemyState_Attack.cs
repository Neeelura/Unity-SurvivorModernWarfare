using UnityEngine;

public class EnemyState_Attack : EnemyState_Base
{
    public override int Priority => 1;
    private float attackTimer;  // 攻击冷却计时器

    public EnemyState_Attack(EnemyController enemy, StateMachine stateMachine) : base(enemy, stateMachine) { }

    public override void OnEnter()
    {
        attackTimer = enemy.attackCooldown;

        // 事件订阅
        enemy.animController.OnAttackHitEvent += OnAttackHit;

        enemy.animController.DoAttack();
    }

    public override void OnUpdate()
    {
        base.OnUpdate();

        // 如果玩家不见了或者超出攻击范围，切换回 Chase 状态
        if (enemy.player == null || nowTargetSqrDist > enemy.attackRangeSqr)
        {
            stateMachine.ChangeState(enemy.chaseState);
        }

        attackTimer -= Time.deltaTime;
        if (attackTimer <= 0f)
        {
            stateMachine.ChangeState(enemy.attackState); // 重新进入攻击状态，触发下一次攻击
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        // 事件取消订阅
        enemy.animController.OnAttackHitEvent -= OnAttackHit;
    }

    public void OnAttackHit()
    {
        if (enemy.player != null)
        {
            enemy.DoAttack();
        }
    }
}
