using System;
using UnityEngine;

public class EnemyAnimController : MonoBehaviour
{
    private Animator anim;

    // 动画相关事件
    public event Action OnAttackHitEvent;
    public event Action OnHitEndEvent;

    private readonly int hashIsMoving = Animator.StringToHash("IsMoving");
    private readonly int hashAttack = Animator.StringToHash("Attack");
    private readonly int hashHit = Animator.StringToHash("Hit");
    private readonly int hashDie = Animator.StringToHash("Die");

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void DoMove(bool isMoving)
    {
        anim.SetBool(hashIsMoving, isMoving);
    }

    public void DoAttack()
    {
        anim.SetTrigger(hashAttack);
    }

    public void DoHit()
    {
        anim.SetTrigger(hashHit);
    }

    public void DoDie()
    {
        anim.SetTrigger(hashDie);
    }

    public void OnAttackHit()
    {
        OnAttackHitEvent?.Invoke();
    }

    public void OnHitEnd()
    {
        OnHitEndEvent?.Invoke();
    }
}
