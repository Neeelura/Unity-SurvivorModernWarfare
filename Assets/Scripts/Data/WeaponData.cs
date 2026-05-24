using UnityEngine;

/// <summary>
/// 武器类型枚举
/// Melee 近战匕首
/// Ranged 远程武器
/// </summary>
public enum WeaponType
{
    Melee,
    Ranged,
}

/// <summary>
/// 武器特殊效果枚举
/// </summary>
public enum WeaponEffect
{
    None,        // 无特殊效果
    Penetrate,   // 穿透：子弹沿直线穿透多个目标（狙击枪）
    Scatter,     // 散射：扇形范围内同时发射多颗子弹（霰弹枪）
    AOE,         // 范围爆炸：命中后对周围造成AOE伤害（火箭筒）
    DOT,         // 持续灼烧：命中后附加持续伤害效果（火焰喷射器）
}

/// <summary>
/// 武器某一等级的数据
/// 每把武器有 1~5 级，每级独立配置伤害倍率、攻击频率、属性加成
/// 升级仅影响：伤害倍率、攻击频率、属性加成
/// </summary>
[System.Serializable]
public struct WeaponLevelData
{
    [Tooltip("伤害倍率")]
    public float damageMultiplier;

    [Tooltip("攻击频率倍率")]
    public float frequencyMultiplier;

    [Tooltip("护甲加成")]
    public int bonusArmor;

    [Tooltip("移动速度加成")]
    public float bonusSpeed;

    [Tooltip("伤害加成")]
    public float bonusDamage;

    [Tooltip("攻击范围加成")]
    public float bonusAttackRange;

    [Tooltip("拾取范围加成")]
    public float bonusPickupRange;
}

/// <summary>
/// 武器的静态配置
/// </summary>
[CreateAssetMenu(fileName = "NewWeapon", menuName = "Data/Weapon")]
public class WeaponData : ScriptableObject
{
    [Header("基础信息")]
    public string weaponName;         // 武器名称
    public WeaponType weaponType;     // 武器类型
    public WeaponEffect weaponEffect; // 特殊效果类型
    public int effectParam;           // 特效参数（根据 WeaponEffect 含义不同）
                                      // Penetrate: 穿透目标数量
                                      // Scatter: 散射子弹数量
                                      // AOE: 爆炸半径
                                      // DOT: 灼烧持续秒数

    [Header("基础数值")]
    public int baseDamage;            // 基础伤害值
    public float attackRange;         // 攻击范围
    public float attackInterval;      // 攻击间隔CD 基础值（远程武器自动释放间隔）

    [Header("视觉特效")]
    public Sprite icon;                     // 武器图标
    public GameObject bulletPrefab;         // 子弹预制体
    public GameObject explosionPrefab;      // 爆炸预制体（火箭筒击中后使用）
    public GameObject weaponModelPrefab;    // 武器模型预制体

    [Header("默认属性加成")]
    public int bonusArmor;            // 护甲加成
    public float bonusSpeed;          // 移动速度加成
    public float bonusDamage;         // 伤害加成
    public float bonusAttackRange;    // 攻击范围加成
    public float bonusPickupRange;    // 拾取范围加成

    [Header("等级数据 (1~5 级)")]
    [Tooltip("按等级配置伤害倍率/频率倍率/属性加成，长度应为 5。留空则使用上方默认值")]
    public WeaponLevelData[] levelData;


    /// <summary>
    /// 获取指定等级的数据（level 取值 1~5）
    /// 如果 levelData 已配置，返回对应的等级数据
    /// 如果 levelData 未配置，使用默认属性加成字段，伤害/频率倍率固定为 1.0
    /// </summary>
    /// <param name="level">武器等级（1~5），超出范围会钳制到 1~5</param>
    public WeaponLevelData GetLevelData(int level)
    {
        int index = Mathf.Clamp(level, 1, 5) - 1;

        if (levelData != null && index < levelData.Length)
        {
            return levelData[index];
        }

        return new WeaponLevelData
        {
            damageMultiplier = 1f,
            frequencyMultiplier = 1f,
            bonusArmor = bonusArmor,
            bonusSpeed = bonusSpeed,
            bonusDamage = bonusDamage,
            bonusAttackRange = bonusAttackRange,
            bonusPickupRange = bonusPickupRange,
        };
    }
}
