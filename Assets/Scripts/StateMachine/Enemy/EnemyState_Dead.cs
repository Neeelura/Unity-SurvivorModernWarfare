using UnityEngine;

public class EnemyState_Dead : EnemyState_Base
{
    public override int Priority => 100;

    public EnemyState_Dead(EnemyController enemy, StateMachine stateMachine) : base(enemy, stateMachine) { }

    // 死亡后禁用 Collider + 播放死亡动画
    public override void OnEnter()
    {
        enemy.agent.isStopped = true;
        enemy.GetComponent<Collider>().enabled = false;
        enemy.animController.DoDie();
        enemy.despawnTimer = 2f;
    }

    public override void OnUpdate()
    {
        enemy.despawnTimer -= Time.deltaTime;
        if (enemy.despawnTimer <= 0f)
        {
            enemy.DoDie();
        }
    }
}
