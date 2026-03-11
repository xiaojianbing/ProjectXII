# Feature Spec: Character 统一角色系统

## Feature 简述
一套**统一的角色数据与行为管理系统**，由两大核心模块构成：
- **CharacterStats（数值层）**：为所有"可交互实体"提供 HP、能量、Buff 等属性管理。
- **CharacterContext（行为层）**：为所有"有行为的实体"提供状态标志、动画桥接和组件引用枢纽。

覆盖：Player、NPC、Enemy、Boss、可破坏道具（箱子、门、可击碎墙壁等）。

## 玩家体验目标
- 玩家攻击任何"可交互物体"（敌人、木箱、岩壁）时，获得**一致的打击反馈**——掉血、碎裂、闪烁。
- 不同对象被击中后有**符合直觉的差异化反应**：敌人硬直后退，木箱直接碎裂，Boss 不动如山但血条减少。
- 能量槽在战斗中**自然积攒**，玩家永远能看到自己距离大招还差多少。
- 大招霸体期间被攻击：正常扣血，但**不被打断**。

## 竞品参考

| 游戏           | 参考机制                                           | 对我们的启示                                  |
| -------------- | -------------------------------------------------- | --------------------------------------------- |
| 《空洞骑士》   | 敌人/可破坏物/玩家共用 HP 系统；灵魂值通过攻击积攒 | 统一的 Damageable 接口，攻击即积攒资源        |
| 《鬼泣5》      | 敌人有独立的硬直阈值、浮空重量；Style Rank 积攒    | 属性不仅是 HP，还包含受击参数（重量、硬直值） |
| 《失落的皇冠》 | 能量通过击杀/处决/弹反积攒，速率不同               | 能量获取与战斗行为挂钩                        |
| 《仁王》       | 气力槽（Ki）影响攻防节奏                           | Buff/Debuff 对属性的动态修改                  |

## 优先级
**P0 基础设施** — 横跨 P0-A 至 P0-D 的底层数据+行为层，在 P0-A 阶段预埋。

## 支持的实体类型

| 实体       | Stats (HP) | Stats (能量) | Stats (Buff) | Context (状态机) | Context (动画) | 示例             |
| ---------- | :--------: | :----------: | :----------: | :--------------: | :------------: | ---------------- |
| Player     |     ✅      |      ✅       |      ✅       |    ✅ 完整HFSM    |       ✅        | 主角             |
| Enemy      |     ✅      |      ❌       |      ✅       |    ✅ AI状态机    |       ✅        | 杂兵、精英怪     |
| Boss       |     ✅      |      ❌       |      ✅       |   ✅ 多阶段FSM    |       ✅        | Boss 战          |
| NPC        |     ✅      |      ❌       |      ❌       |    ✅ 简化FSM     |       ✅        | 友方 NPC         |
| 可破坏道具 |     ✅      |      ❌       |      ❌       |        ❌         |       ❌        | 木箱、碎墙、灯笼 |

---

## 核心概念

### 三层架构总览

```
┌─────────────────────────────────────┐
│         StateMachine (HFSM)         │  ← 决定"该做什么"（各状态节点）
│   Idle / Run / Jump / Attack / ...  │
├─────────────────────────────────────┤
│        CharacterContext             │  ← "我现在是什么状态，能做什么"
│   状态标志 + 组件引用 + 动画桥接     │
├─────────────────────────────────────┤
│        CharacterStats               │  ← "我有多少数值资源"
│   HP / Energy / Buffs / Modifiers   │
└─────────────────────────────────────┘
```

- **StateMachine** 读 Context 判断能否切换，读 Stats 判断资源是否足够
- **Context** 是中间枢纽，持有对 Stats、Input、Physics 的引用
- **Stats** 纯数据层，不关心行为逻辑

---

### A. CharacterStats 模块（数值层）

#### A1. IDamageable 接口
所有可受击对象的统一入口。攻击系统只需调用 `IDamageable.TakeDamage(HitData)`，不关心对面是 Player 还是木箱。

#### A2. 属性值 (StatValue)
每个属性由 `BaseStat`（基础值）+ `Modifier`（修改器列表）构成，避免硬编码。Buff/装备/技能通过增减 Modifier 动态影响最终值。

#### A3. 能量槽 (Energy Gauge)
- 仅 Player 拥有
- 积攒来源：击杀(少) / 处决(中) / 弹反(多)（已确认，参考《失落的皇冠》）
- 消耗用途：P0-D 大招释放

#### A4. Buff 系统
- 临时性属性修改器，带持续时间
- 可叠加或互斥（按 BuffType 配置）
- 示例：攻击力+20% (5秒)、中毒持续掉血、移速降低

---

### B. CharacterContext 模块（行为层）

#### B1. 状态标志 (State Flags)
运行时标志，由当前状态机节点设置，供其他系统查询：

| 标志            | 类型 | 用途                    | 设置者                   |
| --------------- | ---- | ----------------------- | ------------------------ |
| IsGrounded      | bool | 地面判定                | 物理检测                 |
| IsInvincible    | bool | 无敌帧（Dash / 受击后） | Dash状态、HitStun状态    |
| HasSuperArmor   | bool | 霸体（大招期间）        | UltimateState            |
| CanAct          | bool | 能否接受输入            | HitStun/Death 设为 false |
| FacingDirection | int  | 朝向 (1/-1)             | 移动状态                 |

#### B2. 组件引用枢纽
Context 持有所有核心组件引用，状态机节点通过 Context 访问，无需各自 GetComponent：
- `Rigidbody2D` / `BoxCollider2D`
- `CharacterStats`
- `PlayerInputHandler`（仅 Player）
- `Animator`
- `StateMachine`

#### B3. 动画桥接
状态切换时由 Context 统一通知 Animator，避免各状态直接操作动画：
- `Context.PlayAnimation(string animName)`
- `Context.SetAnimParam(string param, value)`

#### B4. 受击决策协调
`TakeDamage` 流程中，Context 负责协调 Stats 和状态机：
1. `Stats.ApplyDamage(hitData)` → 扣 HP
2. `Context.HasSuperArmor ?` → 是则不切换状态
3. 否则 → `StateMachine.ChangeState(HitStunState)`

---

## 验收标准

| 场景           | 预期表现                                                            |
| -------------- | ------------------------------------------------------------------- |
| 攻击敌人       | 敌人 HP 减少，攻击者获得能量                                        |
| 攻击木箱       | 木箱 HP 归零后播放碎裂，无能量获取                                  |
| Buff 生效      | 属性实时变化，Buff 到期自动移除且属性复原                           |
| HP 归零        | 触发 OnDeath 事件，由各实体自行处理（敌人消失、玩家重生、道具碎裂） |
| 大招期间被攻击 | Stats 正常扣血，但 Context 检测到 SuperArmor，**不进入硬直状态**    |
| 状态冲突       | 高优先级动作（受击）可打断低优先级（移动），反之不可                |

## 跨阶段预埋计划

| 阶段 | CharacterStats                            | CharacterContext                              |
| ---- | ----------------------------------------- | --------------------------------------------- |
| P0-A | 基础结构 + HP + `IDamageable`（陷阱扣血） | 基础结构 + 状态标志 + 组件引用 + 动画桥接     |
| P0-B | HitData → TakeDamage + 能量槽 + Buff 框架 | 受击决策协调（SuperArmor 判断）+ HitStun 状态 |
| P0-C | 处决 → 能量积攒 + 连招 Buff               | 动画取消窗口标志 + 连招状态切换协调           |
| P0-D | 能量消耗 → 大招释放                       | SuperArmor 激活 + Ultimate 状态管理           |
