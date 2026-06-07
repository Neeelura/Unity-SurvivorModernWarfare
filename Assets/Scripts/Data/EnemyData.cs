using UnityEngine;

/// <summary>
/// 敌人类型枚举
/// 3种敌人类型对应不同的属性
/// </summary>
public enum EnemyType
{
    Wanderer,   // 游荡者：低血量，慢速，低伤害
    Runner,     // 奔跑者：低血量，快速，中伤害
    Brute,      // 壮汉：高血量，慢速，高伤害
}

/// <summary>
/// 敌人数据配置
/// 定义一种敌人类型的全部静态配置
/// 运行时由 EnemySpawner 根据 EnemyData + 当前波次，调用 EnemyController.Initialize() 配置
/// </summary>
[CreateAssetMenu(fileName = "NewEnemy", menuName = "Data/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("基础信息")]
    public string enemyName;         // 敌人名称
    public EnemyType enemyType;      // 敌人类型
    public GameObject prefab;        // 敌人预制体

    [Header("基础属性")]
    public int baseMaxHp = 100;         // 基础最大生命值
    public float baseMoveSpeed = 4f;    // 基础移动速度
    public int baseDamage = 20;         // 基础攻击伤害
    public float attackRange = 2f;      // 攻击范围
    public float attackCooldown = 1.5f; // 攻击冷却
    public float chaseRange = 10f;      // 追逐范围
    public float patrolRadius = 5f;     // 巡逻半径

    [Header("波次属性成长")]
    [Tooltip("每波 HP 增长率")]
    public float hpGrowthRate = 0.15f;
    [Tooltip("每波伤害增长率")]
    public float damageGrowthRate = 0.10f;
    [Tooltip("每波移速增长率")]
    public float speedGrowthRate = 0.03f;

    [Header("波次出现规则")]
    [Tooltip("首次出现的波次，在此之前该敌人生成权重为0")]
    [Range(1, 25)]
    public int firstWave = 1;
    [Tooltip("生成权重，数值越大出现概率越高")]
    public float spawnWeight = 1f;

    /// <summary>
    /// 计算指定波次的实际生命值
    /// 公式：baseMaxHp × (1 + (wave - 1) × hpGrowthRate)
    /// </summary>
    public int GetScaledHp(int wave)
    {
        float scale = 1f + (wave - 1) * hpGrowthRate;
        return Mathf.RoundToInt(baseMaxHp * scale);
    }

    /// <summary>
    /// 计算指定波次的实际移动速度
    /// 公式：baseMoveSpeed × (1 + (wave - 1) × speedGrowthRate)
    /// </summary>
    public float GetScaledSpeed(int wave)
    {
        float scale = 1f + (wave - 1) * speedGrowthRate;
        return baseMoveSpeed * scale;
    }

    /// <summary>
    /// 计算指定波次的实际攻击伤害
    /// 公式：baseDamage × (1 + (wave - 1) × damageGrowthRate)
    /// </summary>
    public int GetScaledDamage(int wave)
    {
        float scale = 1f + (wave - 1) * damageGrowthRate;
        return Mathf.RoundToInt(baseDamage * scale);
    }

    /// <summary>
    /// 检查该敌人类型是否在当前波次可用
    /// </summary>
    public bool IsAvailableAtWave(int wave)
    {
        return wave >= firstWave;
    }
}
