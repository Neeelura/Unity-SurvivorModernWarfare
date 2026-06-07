using UnityEngine;

/// <summary>
/// 受伤接口，由所有可被攻击的对象（玩家、敌人）实现
/// </summary>
public interface IDamageable
{
    void TakeDamage(int damage, Vector3 hitPoint);
}
