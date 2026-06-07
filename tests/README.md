# 测试基础设施 — 幸存者：现代战争

**引擎**: Unity 2022.3 LTS
**测试框架**: Unity Test Framework（内置）
**CI**: `.github/workflows/tests.yml`
**建立日期**: 2026-06-07

## 目录结构

```
tests/
  unit/           # 独立单元测试（公式、状态机、纯逻辑）
  integration/    # 跨系统集成测试、存档回环测试
  smoke/          # 冒烟测试关键路径清单
  evidence/       # 截图记录和手动测试签核
```

## 运行测试

1. 打开 Unity Editor
2. Window → General → Test Runner
3. EditMode 选项卡 — 纯逻辑测试（无需进入 Play 模式）
4. PlayMode 选项卡 — 集成/场景测试
5. 点击 "Run All"

命令行（CI）：
```bash
unity-editor -runTests -testPlatform EditMode -projectPath . -testResults results.xml
```

## 测试命名规范

- **文件**: `[系统]_[功能]_test.cs`
- **函数**: `Test_[场景]_[预期结果]`
- **示例**: `CombatDamageTest.cs` → `Test_Armor100_ReducesDamageByHalf()`

## Story 类型 → 测试证据

| Story 类型 | 必需证据 | 存放位置 |
|---|---|---|
| 逻辑（公式/AI/状态机） | 自动化单元测试 — 必须通过 | `tests/unit/[系统]/` |
| 集成（多系统交互） | 集成测试或录屏测试文档 | `tests/integration/[系统]/` |
| 视觉/手感（动画/特效） | 截图 + 负责人签核 | `tests/evidence/` |
| UI（菜单/HUD/界面） | 手动走查文档或交互测试 | `tests/evidence/` |
| 配置/数据（数值调整） | 冒烟测试通过 | `production/qa/smoke-*.md` |

## CI

每次 Push 到 `main` 分支以及每个 Pull Request 都会自动运行测试。
测试失败则禁止合并。

## 启用 Unity Test Framework

Unity Test Framework 从 Unity 2019 起内置，无需额外安装。