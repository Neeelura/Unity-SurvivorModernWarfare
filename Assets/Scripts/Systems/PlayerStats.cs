using UnityEngine;

/// <summary>
/// 角色属性管理器
/// 统一管理玩家的所有属性：基础攻击力、护甲、移速、伤害加成、攻击范围、拾取范围。
/// 属性来源：
///   - 基础值：角色自带（如移速 100%）
///   - 等级加成：每升1级基础攻击力+10%
///   - 武器加成：装备武器提供的属性（如护甲+5、移速+5%）
///
/// 护甲减伤公式：实际伤害 = 原始伤害 × (100 / (100 + 护甲))
/// 移动速度上限200%
/// 伤害加成无上限
/// </summary>
public class PlayerStats
{
    private static PlayerStats instance = new PlayerStats();
    public static PlayerStats Instance => instance;

    public int hp;                      // 当前生命值 
    private float baseSpeed;            // 基础移动速度
    private float basePickupRange;      // 基础拾取范围
    private float baseAttackRange;      // 基础攻击范围

    private float critRate;             // 暴击率
    private float critMultiplier;       // 暴击伤害倍率

    private int level = 1;              // 当前等级

    // 计算后的最终属性
    // 每次装备/卸下武器或升级时，调用 Recalculate() 重新计算

    // 基础攻击加成：每升1级 +10%
    // 公式：(level - 1) × 0.1
    public float AttackBonus => (level - 1) * 0.1f;

    // 最终护甲值
    public int TotalArmor { get; private set; }

    // 最终移动速度
    public float TotalSpeed { get; private set; }

    // 最终伤害加成
    public float TotalDamageBonus { get; private set; }

    // 最终攻击范围
    public float TotalAttackRange { get; private set; }

    // 最终拾取范围
    public float TotalPickupRange { get; private set; }

    private PlayerStats()
    {
        EventCenter.AddListener<int>("PlayerLevelUp", OnLevelUp);
    }

    public void Init(PlayerController player)
    {
        // 初始化基础属性
        hp = player.maxHp;
        baseSpeed = player.baseSpeed;
        basePickupRange = player.basePickupRange;
        baseAttackRange = player.baseAttackRange;
        critRate = player.critRate;
        critMultiplier = player.critMultiplier;
    }

    /// <summary>
    /// 升级回调
    /// </summary>
    private void OnLevelUp(int newLevel)
    {
        level = newLevel;
        Recalculate();
    }

    /// <summary>
    /// 重新计算所有最终属性
    /// 在以下时机调用：武器装备/卸下、武器升级、角色升级
    /// 计算顺序：基础值 + 武器加成 → 应用上限/衰减 → 更新到 PlayerController
    /// </summary>
    public void Recalculate()
    {
        WeaponManager weaponManager = WeaponManager.Instance;
        if (weaponManager == null) return;

        // 从 WeaponManager 获取所有已装备远程武器的属性加成
        float bonusArmor = weaponManager.GetTotalBonusArmor();
        float bonusSpeedPercent = weaponManager.GetTotalBonusSpeed();
        float bonusDamagePercent = weaponManager.GetTotalBonusDamage();
        float bonusAttackRangePercent = weaponManager.GetTotalBonusAttackRange();
        float bonusPickupRangePercent = weaponManager.GetTotalBonusPickupRange();

        // 计算最终护甲
        // 超过200的部分只算50%
        float rawArmor = bonusArmor;
        if (rawArmor > 200f)
        {
            TotalArmor = Mathf.RoundToInt(200f + (rawArmor - 200f) * 0.5f);
        }
        else
        {
            TotalArmor = Mathf.RoundToInt(rawArmor);
        }

        // 计算最终移动速度（上限200%）
        // baseSpeed × (1 + 百分比加成)
        TotalSpeed = baseSpeed * (1f + bonusSpeedPercent);
        // 限制最大速度为基础速度的200%
        TotalSpeed = Mathf.Min(TotalSpeed, baseSpeed * 2f);

        // 计算最终伤害加成
        TotalDamageBonus = bonusDamagePercent;

        // 计算最终攻击范围
        TotalAttackRange = baseAttackRange * (1f + bonusAttackRangePercent);

        // 计算最终拾取范围
        TotalPickupRange = basePickupRange * (1f + bonusPickupRangePercent);

    }

    /// <summary>
    /// 计算最终伤害值
    /// 公式：武器基础伤害 × (1 + 基础攻击加成) × (1 + 伤害加成) × 暴击倍率 × 随机波动(0.9~1.1)
    /// </summary>
    /// <param name="baseDamage">武器基础伤害</param>
    /// <param name="isCritical">是否暴击</param>
    /// <returns>最终伤害值</returns>
    public int CalculateDamage(int baseDamage, out bool isCritical)
    {
        // 判定暴击：随机数 < 暴击率 则暴击
        isCritical = Random.value < critRate;
        float critMult = isCritical ? critMultiplier : 1f;

        // 随机波动 0.9~1.1
        float randomVariance = Random.Range(0.9f, 1.1f);

        // 完整伤害公式
        float finalDamage = baseDamage
            * (1f + AttackBonus)        // 等级基础攻击加成
            * (1f + TotalDamageBonus)   // 武器伤害加成
            * critMult                  // 暴击倍率
            * randomVariance;           // 随机波动

        return Mathf.RoundToInt(finalDamage);
    }
}
