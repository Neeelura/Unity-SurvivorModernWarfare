using UnityEngine;

/// <summary>
/// 玩家在地面上的状态，提供基础的输入获取和瞄准功能
/// </summary>
public class PlayerState_Grounded : PlayerState_Base
{
    protected float h, v;

    public PlayerState_Grounded(PlayerController player, StateMachine stateMachine) : base(player, stateMachine) { }

    public override void OnUpdate()
    {
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");

        player.DoAim();
    }
}
