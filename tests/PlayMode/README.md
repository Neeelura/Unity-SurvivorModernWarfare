# Play Mode 测试

在真实游戏场景中运行的集成测试。
适用于跨系统交互、物理检测、协程等场景。

需要 Assembly Definition: `tests/PlayMode/PlayModeTests.asmdef`

## 适用场景

- 跨系统交互（武器开火 → 敌人受击 → 伤害飘字）
- 波次生命周期（开始 → 生成 → 击杀 → 结束）
- 存档/读档回环验证
- 对象池生成/回收完整性
- WeaponManager 槽位管理压力测试

## 示例

```csharp
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class WaveSystemTests
{
    [UnityTest]
    public IEnumerator Test_WaveStart_SpawnsCorrectEnemyCount()
    {
        // 加载 GameScene，等待首波生成
        yield return new WaitForSeconds(5f);

        Assert.Greater(WaveManager.Instance.EnemiesToSpawn, 0);
    }

    [UnityTest]
    public IEnumerator Test_EnemyCount_Formula_MatchesWave10()
    {
        // 波次10：敌人数 = 20 + 10×15 + 100×2 = 370
        int count = WaveManager.Instance.GetEnemyCount(10);
        Assert.AreEqual(370, count);
    }
}
```