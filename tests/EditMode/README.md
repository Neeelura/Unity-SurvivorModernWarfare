# Edit Mode 测试

无需进入 Play 模式的纯逻辑单元测试。
适用于公式验证、状态机逻辑、数据解析等场景。

需要 Assembly Definition: `tests/EditMode/EditModeTests.asmdef`

## 适用场景

- 伤害公式验证（护甲减伤、暴击、随机波动）
- 经验曲线测试
- FSM 状态转换逻辑
- 数据序列化/反序列化
- PlayerStats.CalculateDamage() — 纯数学计算
- 敌人生成数量公式

## 示例

```csharp
using NUnit.Framework;
using UnityEngine;

public class CombatDamageTests
{
    [Test]
    public void Test_Armor100_ReducesDamageByHalf()
    {
        // 护甲100 → 减伤50%
        float reduction = 100f / (100f + 100f);
        Assert.AreEqual(0.5f, reduction, 0.01f);
    }

    [Test]
    public void Test_Armor300_ExcessHalved()
    {
        // 护甲300 → 实际200 + 100×0.5 = 250
        float rawArmor = 300f;
        float effectiveArmor = 200f + (rawArmor - 200f) * 0.5f;
        Assert.AreEqual(250f, effectiveArmor, 0.01f);
    }
}
```