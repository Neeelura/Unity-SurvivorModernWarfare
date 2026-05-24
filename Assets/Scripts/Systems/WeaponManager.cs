using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 武器槽位（运行时数据）
/// 每个槽位记录：装备的武器 SO、当前等级、剩余冷却时间
/// 等级范围 1~5
/// </summary>
[System.Serializable]
public class WeaponSlot
{
    public WeaponData weaponData;   // 武器配置 SO
    public int level = 1;           // 当前等级（1~5）
    public float cooldownTimer;     // 剩余冷却时间

    /// <summary>
    /// 是否还可以继续升级
    /// </summary>
    public bool CanUpgrade => level < 5;

    /// <summary>
    /// 获取当前等级的实际攻击间隔
    /// 实际 CD = 基础间隔 / 等级频率倍率
    /// </summary>
    public float GetAttackInterval()
    {
        WeaponLevelData data = weaponData.GetLevelData(level);
        // 防止除零：频率倍率至少为 0.1
        float freq = Mathf.Max(data.frequencyMultiplier, 0.1f);
        return weaponData.attackInterval / freq;
    }

    /// <summary>
    /// 获取当前等级的实际基础伤害
    /// 实际伤害 = 武器基础伤害 × 等级伤害倍率
    /// </summary>
    public int GetBaseDamage()
    {
        WeaponLevelData data = weaponData.GetLevelData(level);
        return Mathf.RoundToInt(weaponData.baseDamage * data.damageMultiplier);
    }
}

/// <summary>
/// 武器管理器
/// 管理玩家的远程武器槽位，自动索敌并释放。
/// 武器升级规则：
/// - 新武器：装备至空槽位（4个槽位）
/// - 重复武器：当前武器等级+1（最高5级）
/// - 槽位满4个后：只出现已装备武器的升级选项
/// </summary>
public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    // 最多同时装备 4 把远程武器
    public const int MAX_SLOTS = 4;

    // 已装备的武器槽位列表
    public List<WeaponSlot> slots = new List<WeaponSlot>();

    // 武器模型挂载点
    // 索引0~3分别对应4个武器槽位，模型会实例化为对应挂载点的子对象
    public Transform[] weaponModelParents = new Transform[MAX_SLOTS];

    // 已实例化的武器模型实例（索引与 slots 对应，用于升级/移除时销毁旧模型）
    private List<GameObject> weaponModelInstances = new List<GameObject>();

    // 玩家引用
    private PlayerController player;

    // 预分配数组，用于 OverlapSphereNonAlloc，避免每帧 new 产生 GC
    private Collider[] hitResults = new Collider[20];

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        player = GetComponent<PlayerController>();

        // 确保槽位数据已初始化
        InitializeSlots();
        // 初始化玩家属性
        PlayerStats.Instance.Init();
        // 初始化武器加成
        PlayerStats.Instance.Recalculate();
    }

    private void OnDestroy()
    {
        Instance = null;
    }

    /// <summary>
    /// 初始化槽位数据
    /// </summary>
    private void InitializeSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            // 如果槽位没有 weaponData，跳过
            if (slots[i].weaponData == null) continue;

            // 如果 level 为 0，设置为 1
            if (slots[i].level < 1) slots[i].level = 1;

            // 为预配置的武器实例化模型
            RefreshWeaponModel(i);
        }
    }

    private void Update()
    {
        // 遍历所有武器槽位，处理自动释放
        for (int i = 0; i < slots.Count; i++)
        {
            WeaponSlot slot = slots[i];
            if (slot == null || slot.weaponData == null) continue;

            // 倒计时冷却
            slot.cooldownTimer -= Time.deltaTime;

            // 冷却结束，执行攻击
            if (slot.cooldownTimer <= 0f)
            {
                // 如果成功找到敌人并攻击后 重置冷却计时器
                if (ExecuteWeapon(slot))
                    slot.cooldownTimer = slot.GetAttackInterval();
            }
        }
    }

    /// <summary>
    /// 执行武器攻击，自动寻找范围内最近敌人并释放
    /// 根据武器的 WeaponEffect 执行不同的判定逻辑
    /// </summary>
    /// <param name="slot">武器槽位</param>
    private bool ExecuteWeapon(WeaponSlot slot)
    {
        WeaponData weapon = slot.weaponData;

        // 自动索敌
        Transform nearestEnemy = FindNearestEnemy(weapon.attackRange);
        if (nearestEnemy == null) return false;  // 范围内没有敌人，不浪费攻击

        // 根据武器特殊效果执行不同的攻击逻辑
        switch (weapon.weaponEffect)
        {
            case WeaponEffect.Penetrate:
                // 穿透：沿射击方向检测多个目标
                ExecutePenetrate(slot, nearestEnemy);
                break;

            case WeaponEffect.Scatter:
                // 散射：扇形范围内检测多个敌人
                ExecuteScatter(slot);
                break;

            case WeaponEffect.AOE:
                // 范围爆炸：对命中点周围所有敌人造成伤害
                ExecuteAOE(slot, nearestEnemy.position);
                break;

            case WeaponEffect.DOT:
                // 持续灼烧：命中敌人后附加持续伤害
                ExecuteDOT(slot, nearestEnemy);
                break;

            default:
                // 无特殊效果：单体伤害
                ExecuteSingleTarget(slot, nearestEnemy);
                break;
        }

        return true;
    }

    /// <summary>
    /// 计算当前武器的最终伤害（含等级倍率、暴击、随机波动）
    /// </summary>
    private int CalculateFinalDamage(WeaponSlot slot, out bool isCritical)
    {
        isCritical = false;
        int baseDamage = slot.GetBaseDamage();
        
        return PlayerStats.Instance.CalculateDamage(baseDamage, out isCritical);
    }

    /// <summary>
    /// 单体攻击：只命中最近的一个目标
    /// </summary>
    private void ExecuteSingleTarget(WeaponSlot slot, Transform target)
    {
        int damage = CalculateFinalDamage(slot, out bool isCritical);
        IDamageable damageable = target.GetComponent<IDamageable>();
        damageable?.TakeDamage(damage, target.position);
        DamagePopup.Show(target.position, damage, isCritical);
        WeaponVFX.Instance?.FireBullet(slot.weaponData.bulletPrefab, player.transform.position, target.position);
    }

    /// <summary>
    /// 穿透攻击：沿射击方向命中多个目标
    /// 以最近敌人方向为基准，命中该方向扇形内的 effectParam 个敌人
    /// </summary>
    private void ExecutePenetrate(WeaponSlot slot, Transform firstTarget)
    {
        WeaponData weapon = slot.weaponData;

        // 计算射击方向
        Vector3 direction = (firstTarget.position - player.transform.position).normalized;

        // 沿射击方向做球形检测，找到路径上的所有敌人
        int hitCount = Physics.OverlapSphereNonAlloc(
            player.transform.position,
            weapon.attackRange,
            hitResults,
            player.enemyLayers
        );

        // 收集路径上被穿透的敌人
        List<(IDamageable damageable, Vector3 pos)> penetrated = new List<(IDamageable, Vector3)>();

        for (int i = 0; i < hitCount; i++)
        {
            Vector3 enemyPos = hitResults[i].transform.position;
            Vector3 toEnemy = enemyPos - player.transform.position;
            float dot = Vector3.Dot(toEnemy.normalized, direction);

            if (dot > 0.7f)
            {
                IDamageable damageable = hitResults[i].GetComponent<IDamageable>();
                if (damageable != null)
                    penetrated.Add((damageable, enemyPos));
            }

            if (penetrated.Count >= weapon.effectParam)
                break;
        }

        int damage = CalculateFinalDamage(slot, out bool isCritical);
        for (int i = 0; i < penetrated.Count; i++)
        {
            penetrated[i].damageable.TakeDamage(damage, penetrated[i].pos);
            DamagePopup.Show(penetrated[i].pos, damage, isCritical);
            WeaponVFX.Instance?.FireBullet(slot.weaponData.bulletPrefab, player.transform.position, penetrated[i].pos);
        }
    }

    /// <summary>
    /// 散射攻击：扇形范围内同时命中多个目标
    /// 在攻击范围内命中最近的 effectParam 个敌人
    /// </summary>
    private void ExecuteScatter(WeaponSlot slot)
    {
        WeaponData weapon = slot.weaponData;

        int hitCount = Physics.OverlapSphereNonAlloc(
            player.transform.position,
            weapon.attackRange,
            hitResults,
            player.enemyLayers
        );

        // 命中最近的 effectParam 个敌人（伤害判定）
        int damage = CalculateFinalDamage(slot, out bool isCritical);
        int hitCountSoFar = 0;
        List<Vector3> hitPositions = new List<Vector3>();

        for (int i = 0; i < hitCount && hitCountSoFar < weapon.effectParam; i++, hitCountSoFar++)
        {
            IDamageable damageable = hitResults[i].GetComponent<IDamageable>();
            if (damageable != null)
            {
                Vector3 pos = hitResults[i].transform.position;
                damageable.TakeDamage(damage, pos);
                DamagePopup.Show(pos, damage, isCritical);
                hitPositions.Add(pos);
            }
        }

        // 视觉：发射 effectParam 颗子弹
        for (int i = 0; i < weapon.effectParam; i++)
        {
            Vector3 target;
            if (i < hitPositions.Count)
            {
                target = hitPositions[i];
            }
            else
            {
                // 随机散射方向
                Vector3 randomDir = Quaternion.Euler(0, Random.Range(-30f, 30f), 0)
                    * player.transform.forward;
                target = player.transform.position + randomDir * weapon.attackRange;
            }
            WeaponVFX.Instance?.FireBullet(slot.weaponData.bulletPrefab, player.transform.position, target);
        }
    }

    /// <summary>
    /// 范围爆炸攻击：对命中点周围所有敌人造成伤害
    /// effectParam 为爆炸半径
    /// </summary>
    private void ExecuteAOE(WeaponSlot slot, Vector3 center)
    {
        WeaponData weapon = slot.weaponData;

        // 在爆炸中心周围检测所有敌人
        int hitCount = Physics.OverlapSphereNonAlloc(
            center,
            weapon.effectParam,     // 爆炸半径
            hitResults,
            player.enemyLayers
        );

        // 对范围内所有敌人造成伤害
        int damage = CalculateFinalDamage(slot, out bool isCritical);
        for (int i = 0; i < hitCount; i++)
        {
            IDamageable damageable = hitResults[i].GetComponent<IDamageable>();
            damageable?.TakeDamage(damage, hitResults[i].transform.position);
            DamagePopup.Show(hitResults[i].transform.position, damage, isCritical);
        }
        // 火箭弹飞行 + 爆炸特效
        WeaponVFX.Instance?.FireRocket(slot.weaponData.bulletPrefab, slot.weaponData.explosionPrefab, player.transform.position, center);
    }

    /// <summary>
    /// 持续灼烧攻击
    /// 命中敌人后附加 DOT 灼烧 Buff：每秒造成基础伤害，持续 effectParam 秒
    /// </summary>
    private void ExecuteDOT(WeaponSlot slot, Transform target)
    {
        // 即时伤害部分
        int damage = CalculateFinalDamage(slot, out bool isCritical);
        IDamageable damageable = target.GetComponent<IDamageable>();
        damageable?.TakeDamage(damage, target.position);
        DamagePopup.Show(target.position, damage, isCritical);

        // 火焰喷射特效
        WeaponVFX.Instance?.FireFlame(slot.weaponData.bulletPrefab, player.transform.position, target.position);

        // DOT 灼烧部分
        EnemyController enemy = target.GetComponent<EnemyController>();
        if (enemy != null)
        {
            int dotDamage = slot.GetBaseDamage();  // DOT 每秒伤害
            enemy.ApplyDOT(dotDamage, slot.weaponData.effectParam);
        }
    }

    /// <summary>
    /// 在指定范围内寻找最近的敌人
    /// 使用 OverlapSphereNonAlloc 避免 GC 分配
    /// </summary>
    /// <param name="range">搜索半径</param>
    /// <returns>最近敌人的 Transform，无敌人返回 null</returns>
    private Transform FindNearestEnemy(float range)
    {
        // 球形检测范围内所有敌人
        int hitCount = Physics.OverlapSphereNonAlloc(
            player.transform.position,  // 检测中心（玩家位置）
            range,                      // 检测半径
            hitResults,                 // 结果缓存数组
            player.enemyLayers          // 只检测敌人层
        );

        float minSqrDist = float.MaxValue;  // 最小距离平方
        Transform nearest = null;

        for (int i = 0; i < hitCount; i++)
        {
            // 计算玩家到敌人的距离平方
            float sqrDist = (hitResults[i].transform.position - player.transform.position).sqrMagnitude;

            if (sqrDist < minSqrDist)
            {
                minSqrDist = sqrDist;
                nearest = hitResults[i].transform;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 装备一把新武器到空槽位
    /// 如果武器已装备（同 weaponName），则自动转为升级操作
    /// </summary>
    /// <param name="weapon">要装备的武器 SO</param>
    /// <returns>true = 装备/升级成功，false = 槽位已满且未找到同款武器</returns>
    public bool AddWeapon(WeaponData weapon)
    {
        if (weapon == null) return false;

        // 1. 检查是否已有同款武器（同 weaponName），如有则升级
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null
                && slots[i].weaponData.weaponName == weapon.weaponName)
            {
                return UpgradeWeapon(i);
            }
        }

        // 2. 检查是否有空槽位
        if (slots.Count >= MAX_SLOTS)
        {
            Debug.LogWarning($"[WeaponManager] 槽位已满（{MAX_SLOTS}个），无法装备新武器 {weapon.weaponName}");
            return false;
        }

        // 3. 添加新武器到空槽位
        WeaponSlot newSlot = new WeaponSlot
        {
            weaponData = weapon,
            level = 1,
            cooldownTimer = 0f,  // 初始即可攻击
        };
        slots.Add(newSlot);
        int slotIndex = slots.Count - 1;

        // 在角色身上实例化武器模型
        RefreshWeaponModel(slotIndex);

        // 武器变更后重新计算属性
        PlayerStats.Instance.Recalculate();

        // 广播武器变更事件
        Debug.Log($"[WeaponManager] 装备新武器: {weapon.weaponName} Lv.1 → 槽位 {slotIndex}");
        return true;
    }

    /// <summary>
    /// 升级指定槽位的武器
    /// </summary>
    /// <param name="slotIndex">槽位索引（0~3）</param>
    /// <returns>true = 升级成功，false = 已达到最大等级</returns>
    public bool UpgradeWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Count) return false;

        WeaponSlot slot = slots[slotIndex];
        if (slot == null || slot.weaponData == null) return false;

        if (!slot.CanUpgrade)
        {
            Debug.LogWarning($"[WeaponManager] {slot.weaponData.weaponName} 已达到最大等级 Lv.5");
            return false;
        }

        slot.level++;
        // 升级后立即重置冷却（让玩家立刻感受到升级效果）
        slot.cooldownTimer = 0f;

        // 属性变更后重新计算
        PlayerStats.Instance.Recalculate();

        Debug.Log($"[WeaponManager] {slot.weaponData.weaponName} 升级到 Lv.{slot.level}");
        return true;
    }

    /// <summary>
    /// 检查是否还有空槽位
    /// </summary>
    public bool HasEmptySlot()
    {
        // 空槽位 = 当前武器数 < 最大槽位数
        return slots.Count < MAX_SLOTS;
    }

    /// <summary>
    /// 获取已装备武器列表（用于槽满时的升级选择）
    /// </summary>
    /// <returns>已装备的 WeaponSlot 列表</returns>
    public List<WeaponSlot> GetEquippedSlots()
    {
        List<WeaponSlot> result = new List<WeaponSlot>();
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null && slots[i].weaponData != null)
                result.Add(slots[i]);
        }
        return result;
    }

    /// <summary>
    /// 检查某把武器是否已装备
    /// </summary>
    public bool IsWeaponEquipped(WeaponData weapon)
    {
        if (weapon == null) return false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] != null
                && slots[i].weaponData != null
                && slots[i].weaponData.weaponName == weapon.weaponName)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 获取所有已装备武器的总护甲加成
    /// 遍历所有槽位，根据每个武器的当前等级累加护甲值
    /// </summary>
    public int GetTotalBonusArmor()
    {
        int total = 0;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null || slots[i].weaponData == null) continue;
            total += slots[i].weaponData.GetLevelData(slots[i].level).bonusArmor;
        }
        return total;
    }

    /// <summary>
    /// 获取所有已装备武器的总移速加成（百分比累加）
    /// </summary>
    public float GetTotalBonusSpeed()
    {
        float total = 0f;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null || slots[i].weaponData == null) continue;
            total += slots[i].weaponData.GetLevelData(slots[i].level).bonusSpeed;
        }
        return total;
    }

    /// <summary>
    /// 获取所有已装备武器的总伤害加成（百分比累加）
    /// </summary>
    public float GetTotalBonusDamage()
    {
        float total = 0f;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null || slots[i].weaponData == null) continue;
            total += slots[i].weaponData.GetLevelData(slots[i].level).bonusDamage;
        }
        return total;
    }

    /// <summary>
    /// 获取所有已装备武器的总攻击范围加成（百分比累加）
    /// </summary>
    public float GetTotalBonusAttackRange()
    {
        float total = 0f;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null || slots[i].weaponData == null) continue;
            total += slots[i].weaponData.GetLevelData(slots[i].level).bonusAttackRange;
        }
        return total;
    }

    /// <summary>
    /// 获取所有已装备武器的总拾取范围加成（百分比累加）
    /// </summary>
    public float GetTotalBonusPickupRange()
    {
        float total = 0f;
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null || slots[i].weaponData == null) continue;
            total += slots[i].weaponData.GetLevelData(slots[i].level).bonusPickupRange;
        }
        return total;
    }

    /// <summary>
    /// 刷新指定槽位的武器模型
    /// 销毁旧模型（如果存在），从 WeaponData 读取 weaponModelPrefab 并实例化到对应的挂载点下
    /// 在 AddWeapon 和 InitializeSlots 时调用，升级不需要更换模型
    /// </summary>
    /// <param name="slotIndex">槽位索引（0~3）</param>
    private void RefreshWeaponModel(int slotIndex)
    {
        // 确保模型实例列表与槽位数量一致
        while (weaponModelInstances.Count <= slotIndex)
            weaponModelInstances.Add(null);

        // 销毁旧模型（如果存在）
        if (weaponModelInstances[slotIndex] != null)
        {
            Destroy(weaponModelInstances[slotIndex]);
            weaponModelInstances[slotIndex] = null;
        }

        // 检查挂载点是否已配置
        if (slotIndex >= weaponModelParents.Length || weaponModelParents[slotIndex] == null)
        {
            Debug.LogWarning($"[WeaponManager] 槽位 {slotIndex} 的挂载点未配置，跳过模型实例化");
            return;
        }

        // 检查武器是否有模型预制体
        WeaponSlot slot = slots[slotIndex];
        if (slot == null || slot.weaponData == null || slot.weaponData.weaponModelPrefab == null)
            return;

        // 实例化模型并挂载到对应的空子对象下
        GameObject model = Instantiate(
            slot.weaponData.weaponModelPrefab,
            weaponModelParents[slotIndex]
        );
        model.transform.localPosition = Vector3.zero;
        weaponModelInstances[slotIndex] = model;
    }
}
