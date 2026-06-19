# 🐜 蚂蚁大战 - 2D ECS实时策略游戏

一款基于Unity ECS（Entity-Component-System）架构的2D实时策略蚂蚁大战游戏。

## 🎮 游戏玩法

### 核心设定
- **两队蚂蚁**：红方 🆚 蓝方
- **两种蚂蚁**：
  - 🐜 **工蚁**：负责寻找食物并搬回巢穴
  - ⚔️ **兵蚁**：负责攻击对方蚂蚁
- **地图**：200x200的战场，随机分布12个食物点
- **巢穴**：双方各有一个巢穴，存储食物并孵化新蚂蚁

### 策略指令
玩家不能直接控制单个蚂蚁，只能通过发布**全局策略指令**来影响蚂蚁的行为：

#### 工蚁策略
- **区域采集**：指定一个区域，工蚁优先去该区域寻找食物
- 支持四个预设区域：左上、右上、左下、右下

#### 兵蚁策略
- **集中防守**：兵蚁在巢穴附近巡逻，检测范围缩小
- **主动进攻**：兵蚁前往指定区域（如敌方巢穴）寻找敌人

## 🏗️ 架构设计

### ECS架构
本游戏采用Unity Entities包实现纯ECS架构：

#### Components（组件）
| 组件 | 描述 |
|------|------|
| `TeamComponent` | 队伍标识（红/蓝） |
| `PositionComponent` | 2D位置 |
| `VelocityComponent` | 速度和移动方向 |
| `HealthComponent` | 生命值 |
| `AntTypeComponent` | 蚂蚁类型（工蚁/兵蚁） |
| `AntStateComponent` | 蚂蚁状态机 |
| `CarryComponent` | 搬运食物能力 |
| `CombatComponent` | 战斗属性 |
| `StrategyComponent` | 策略指令 |
| `NestComponent` | 巢穴属性 |
| `FoodComponent` | 食物点属性 |

#### Tag Components
- `WorkerAntTag` - 工蚁标记
- `SoldierAntTag` - 兵蚁标记
- `NestTag` - 巢穴标记
- `FoodTag` - 食物标记

#### Systems（系统）
| 系统 | 职责 |
|------|------|
| `MovementSystem` | 根据速度更新位置 |
| `WorkerAntSystem` | 工蚁AI：寻食→采集→回巢 |
| `SoldierAntSystem` | 兵蚁AI：索敌→追击→攻击 |
| `NestSystem` | 巢穴孵化蚂蚁 |
| `FoodSystem` | 食物点管理（空食物销毁） |
| `StrategySystem` | 策略指令管理 |

### 系统更新顺序
```
SimulationSystemGroup
└── AntLogicSystemGroup
    ├── MovementSystem
    ├── WorkerAntSystem
    ├── SoldierAntSystem
    ├── NestSystem
    └── FoodSystem
```

## 📊 实时战斗日志

游戏运行时所有事件都会输出到控制台：

- ⚔️ **战斗事件**：攻击伤害、击杀
- 🍞 **采集事件**：工蚁采集食物
- 🏠 **存储事件**：食物存入巢穴
- 🐜 **孵化事件**：新蚂蚁出生
- 📋 **策略事件**：策略指令发布

示例输出：
```
[14:30:25.123] 🐜 蚂蚁大战游戏开始！
[14:30:25.456] 🍞 地图上生成了 12 个食物点
[14:30:28.789] 🍞 采集: [红方]工蚁 采集了 10.0 食物并返回巢穴
[14:30:30.012] 🏠 存储: [红方]巢穴 收到 10.0 食物，总存储: 110.0
[14:30:35.456] ⚔️ 战斗: [蓝方]兵蚁 攻击 [红方]工蚁，造成 10.0 伤害
[14:30:38.789] 💀 击杀: [蓝方]兵蚁 击杀了 [红方]工蚁
```

## 🚀 快速开始

### 环境要求
- Unity 2022.3 LTS 或更高版本
- Entities 1.2.0+
- Burst 1.8.12+
- Collections 2.4.2+
- Mathematics 1.3.1+

### 安装步骤

1. **创建Unity项目**
   - 新建一个2D项目
   - 推荐使用Unity 2022.3 LTS

2. **安装包依赖**
   - 打开Package Manager
   - 添加以下包（通过"Add package by name"）：
     - `com.unity.entities`
     - `com.unity.burst`
     - `com.unity.collections`
     - `com.unity.mathematics`

3. **导入脚本**
   - 将 `Assets/Scripts/` 目录下的所有文件复制到你的Unity项目中

4. **设置场景**
   - 创建一个空场景
   - 创建一个空GameObject，命名为 "GameBootstrap"
   - 添加 `GameBootstrapAuthoring` 组件到该GameObject
   - 创建另一个空GameObject，命名为 "GameView"
   - 添加 `GameView` 组件到该GameObject（用于Gizmos显示）

5. **运行游戏**
   - 点击Play按钮
   - 在Game视图左侧使用GUI按钮发布策略指令
   - 打开Console窗口查看实时战斗日志

### 配置说明

在 `GameConfig.cs` 中可以调整游戏参数：

| 参数 | 默认值 | 描述 |
|------|--------|------|
| `MapWidth/Height` | 200 | 地图尺寸 |
| `WorkerSpeed` | 8 | 工蚁移动速度 |
| `SoldierSpeed` | 10 | 兵蚁移动速度 |
| `WorkerHealth` | 30 | 工蚁生命值 |
| `SoldierHealth` | 80 | 兵蚁生命值 |
| `SoldierDamage` | 10 | 兵蚁攻击力 |
| `WorkerCarryCapacity` | 10 | 工蚁负重 |
| `InitialWorkerCount` | 15 | 初始工蚁数量 |
| `InitialSoldierCount` | 5 | 初始兵蚁数量 |
| `FoodPointCount` | 12 | 食物点数量 |

## 📁 项目结构

```
Assets/Scripts/
├── Components/          # ECS组件
│   ├── AntStateComponent.cs
│   ├── AntTypeComponent.cs
│   ├── CarryComponent.cs
│   ├── CombatComponent.cs
│   ├── FoodComponent.cs
│   ├── FoodTag.cs
│   ├── HealthComponent.cs
│   ├── NestComponent.cs
│   ├── NestTag.cs
│   ├── PositionComponent.cs
│   ├── SoldierAntTag.cs
│   ├── StrategyComponent.cs
│   ├── TeamComponent.cs
│   ├── VelocityComponent.cs
│   └── WorkerAntTag.cs
├── Systems/             # ECS系统
│   ├── FoodSystem.cs
│   ├── MovementSystem.cs
│   ├── NestSystem.cs
│   ├── SoldierAntSystem.cs
│   ├── StrategySystem.cs
│   ├── SystemGroups.cs
│   └── WorkerAntSystem.cs
├── Authoring/           # MonoBehaviour转换
│   └── GameBootstrapAuthoring.cs
├── Utils/               # 工具类
│   ├── BattleLogger.cs
│   ├── GameConfig.cs
│   └── MathUtils.cs
└── Game/                # 游戏管理
    ├── GameBootstrap.cs
    └── GameView.cs
```

## 🎯 游戏机制详解

### 工蚁行为状态机
```
Idle → SeekingFood → ReturningFood → Idle
         ↓
      (食物不足时返回Idle)
```

1. **Idle**：寻找最近的食物
2. **SeekingFood**：移动到食物点，采集食物
3. **ReturningFood**：携带食物返回巢穴

### 兵蚁行为状态机
```
Idle/Defending → Attacking → Idle
      ↓            ↓
  (巡逻)     (目标死亡)
```

1. **Idle**：搜索范围内的敌人
2. **Defending**：防守模式，缩小检测范围，在巢穴附近巡逻
3. **Attacking**：追击并攻击敌人

### 策略影响机制

策略通过**修改目标权重**来影响蚂蚁决策，而非强制控制：

- **区域采集**：策略区域内的食物目标权重×3，工蚁更倾向于去那里
- **防守**：兵蚁检测范围×0.6，更集中在巢穴附近
- **进攻**：只攻击策略区域内的敌人

### 巢穴孵化机制

- 消耗食物孵化新蚂蚁
- 工蚁优先级高于兵蚁
- 兵蚁数量不超过工蚁的50%
- 工蚁消耗20食物，兵蚁消耗50食物

## 🔧 扩展建议

### 可添加的功能
- [ ] 更多蚂蚁类型（侦察蚁、蚁后等）
- [ ] 蚂蚁升级系统
- [ ] 建筑物/防御塔
- [ ] 地图障碍物
- [ ] 更多策略类型
- [ ] 胜负判定系统
- [ ] 音效和视觉特效
- [ ] 多人对战

### 性能优化
- [ ] 使用Burst编译加速系统
- [ ] 使用IJobEntity并行处理
- [ ] 空间分区优化碰撞检测
- [ ] 对象池复用蚂蚁实体

## 📝 代码特点

- ✅ 纯ECS架构，性能优异
- ✅ 状态机设计，行为清晰
- ✅ 策略模式，易于扩展
- ✅ 完整的战斗日志系统
- ✅ 详细的代码注释
- ✅ 遵循Unity最佳实践

## 📄 License

MIT License
