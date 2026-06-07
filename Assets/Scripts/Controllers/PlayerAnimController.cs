using System;
using UnityEngine;

/// <summary>
/// 玩家动画控制器，负责根据玩家状态切换动画，并通过事件通知
/// </summary>
public class PlayerAnimController : MonoBehaviour
{
    private Animator anim;

    // 动画相关事件
    public event Action OnAttackHitEvent;
    public event Action OnAttackEndEvent;

    private readonly int hashHSpeed = Animator.StringToHash("HSpeed");
    private readonly int hashVSpeed = Animator.StringToHash("VSpeed");
    private readonly int hashAttack_0 = Animator.StringToHash("Attack_0");
    private readonly int hashAttack_1 = Animator.StringToHash("Attack_1");
    private readonly int hashAttack_2 = Animator.StringToHash("Attack_2");

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void DoMove(float h, float v)
    {
        anim.SetFloat(hashHSpeed, h);
        anim.SetFloat(hashVSpeed, v);
    }

    public void DoAttack(int comboIndex)
    {
        switch (comboIndex)
        {
            case 0:
                anim.SetTrigger(hashAttack_0);
                break;
            case 1:
                anim.SetTrigger(hashAttack_1);
                break;
            case 2:
                anim.SetTrigger(hashAttack_2);
                break;
            default:
                break;
        }
    }

    public void OnAttackHit()
    {
        OnAttackHitEvent?.Invoke();
    }

    public void OnAttackEnd()
    {
        OnAttackEndEvent?.Invoke();
    }
}
