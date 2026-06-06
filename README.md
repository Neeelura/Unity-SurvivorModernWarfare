# 幸存者：现代战争 (Survivor: Modern Warfare)

**类吸血鬼幸存者 × 俯视角生存 Roguelite × 现代军事题材**

---

## 一、项目简介

本作是一款基于 Unity 引擎开发的 2.5D 俯视角生存 Roguelite 游戏 Demo。玩家操控角色在僵尸浪潮中生存，通过击杀敌人获取经验升级、收集并强化武器，最终存活至第 25 波或战死沙场。

核心玩法循环：**击杀僵尸 → 获取经验 → 升级抽选武器 → 变强 → 击杀更多僵尸**。单局长约 15–30 分钟，提供"割草爽感 + 构筑策略 + 成长反馈"的复合体验。

---

## 二、技术栈

| 层级 | 技术方案 |
|------|---------|
| 游戏引擎 | Unity 2022.3 LTS |
| 编程语言 | C# |
| 架构模式 | FSM 有限状态机 / 事件驱动 / 单例模式 / 对象池 |
| 数据管线 | ScriptableObject 配置武器与敌人数值，策划可脱离代码调参 |
| AI 寻路 | Unity NavMesh + NavMeshAgent |
| UI 系统 | UGUI + 动态面板管理 |
| 动画 | Animator + 动画事件回调驱动攻击判定 |
| 版本管理 | Git |

### 核心架构设计

```
Framework/  (框架层)     EventCenter / StateMachine / PoolManager
Systems/    (游戏系统)    WaveManager / WeaponManager / PlayerStats / WeaponVFX
Controllers/(控制器)      PlayerController(FSM) / EnemyController(FSM)
Data/       (数据配置)    WeaponData(SO) / EnemyData(SO)
UI/         (界面)        BasePanel → 子面板 + DamagePopup 飘字
StateMachine/(状态机)     PlayerStateMachine + EnemyStateMachine
Pickups/    (掉落物)      PickupBase → ExpOrb / Medkit
```

### 技术亮点

**1. FSM 有限状态机**

- 通用 `StateMachine` 支持 `OnEnter/OnUpdate/OnExit` 生命周期和优先级打断
- 玩家 3 状态（Idle → Move → Attack 三段连击），敌人 5 状态（Patrol → Chase → Attack → Hit → Dead）
- 玩家与敌人共用同一套 FSM 框架

**2. 事件驱动架构**

- 支持 0~2 个泛型参数的静态 `EventCenter`，各模块通过事件解耦通信
- 例如 `EnemyDied` 事件同时被 `PlayerLevel`（加经验）、`WaveManager`（计数）、`PlayerHUDPanel`（更新UI）三方监听，彼此无需相互引用

**3. 数据驱动的武器系统**
- 基于 ScriptableObject 的 `WeaponData` 配置 6 把武器 × 5 级升级树
- `WeaponLevelData` 结构体封装每级独立的伤害倍率、攻击频率、属性加成
- `WeaponManager` 管理 4 个被动武器槽位，各自独立 CD 自动索敌释放
- 近战走 FSM 手动三段攻击，远程武器走 CD 自动释放

**4. 四种武器特殊效果**
- **穿透**（狙击枪）：OverlapSphereNonAlloc + 点积筛选，沿射击方向穿透多个目标
- **散射**（霰弹枪）：扇形范围内命中指定数量敌人，额外子弹飞向随机方向（纯视觉）
- **AOE**（火箭筒）：命中点周围球形范围伤害，附带火箭弹飞行 + 爆炸特效
- **DOT**（火焰喷射器）：即时伤害 + 敌人身上挂灼烧 Buff，每秒跳伤害不触发受击硬直

**5. 对象池 **

- `PoolManager` 单例管理敌人、掉落物、飘字的实例复用
- 所有攻击判定使用 `Physics.OverlapSphereNonAlloc` 预分配数组，避免运行时 GC 抖动

**6. 完整的战斗数值体系**
- 护甲非线性衰减：`实际伤害 = 原始伤害 × 100/(100+护甲)`，超 200 收益减半
- 暴击系统：基础 5% 暴击率 + 150% 暴击倍率 + 随机波动 0.9~1.1
- 经验曲线：`100 + level×50 + level²×10`
- 敌人生成公式：`20 + wave×15 + wave²×2`

**7. 波次与敌人生成系统**
- 25 波递增难度，3 种敌人类型（游荡者/奔跑者/壮汉）加权随机生成
- 敌人属性随波次线性成长（HP/伤害/移速）
- NavMesh 动态采样生成位置，对象池管理实例

**8. 动态 UI 面板管理**
- `UIManager` 纯 C# 单例 + `Resources.Load` 动态实例化面板 Prefab
- `BasePanel` 抽象基类统一 `Init() → Show()/Hide()` 生命周期
- `DamagePopup` 伤害飘字系统：对象池 + Coroutine 上浮淡出动画，暴击放大变红
- `WeaponVFX` 武器特效系统：对象池 + Coroutine 直线飞行，火箭弹爆炸，火焰柱持续

---

## 三、已实现功能模块

| Phase | 模块 | 状态 |
|-------|------|------|
| 1 | 框架搭建 | ✅ |
| 2 | 角色属性系统 | ✅ |
| 3 | 经验与升级系统 | ✅ |
| 4 | 武器系统 | ✅ |
| 5 | 波次与敌人 | ✅ |
| 6 | UI 系统 | ✅ |
| 7 | 存档系统 | ✅ |

---

## 四、个人感想

**Situation**  
作为一名软件工程专业本科生，我独立发起并主导了这个从零搭建的游戏项目，目标是完整落地一款类吸血鬼幸存者玩法的 Roguelite 游戏 Demo。

**Task**  
设计可扩展的游戏框架，实现战斗、成长、波次、UI 等功能模块，构建数据驱动的武器与敌人配置管线，确保后期大量敌人生成时不出现 GC 抖动。

**Action**  
自研了 FSM 状态机与泛型事件中心作为框架底座，基于 ScriptableObject 建立数据管线让策划与开发解耦，用对象池 + NonAlloc API 优化运行时性能，在纯 C# 单例与 MonoBehaviour 之间合理取舍保证代码简洁性，并通过事件驱动解耦各模块间的依赖关系。

**Result**  
完成了一个架构清晰、模块低耦合的游戏 Demo。25 波敌人后期每波 1600+ 实例无 GC 压力，通过这个项目深入理解了游戏框架设计、性能优化、数据管线与设计模式的工程应用。

---

## 五、后续开发方向

| 方向 | 说明 |
|------|------|
| **XLua 热更新** | Lua 端实现部分业务逻辑（如伤害公式、武器升级），C# 暴露接口供 Lua 调用 |
| **对象池预热 + 异步加载** | PoolManager PreloadAsync 在波次间隙异步预充敌人实例，平滑加载曲线 |
| **更多敌人 / Boss 战** | 每 5 波增加 Boss 战、更多敌人种类与攻击模式 |
