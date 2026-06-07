using UnityEngine;

/// <summary>
/// 玩家可以攻击的状态，提供攻击方法
/// </summary>
public class PlayerState_CanAttack : PlayerState_Grounded
{
    public PlayerState_CanAttack(PlayerController player, StateMachine stateMachine) : base(player, stateMachine) { }

    public override void OnUpdate()
    {
        base.OnUpdate();

        if (Input.GetMouseButtonDown(0))
        {
            stateMachine.ChangeState(player.attackState);
        }
    }
}
