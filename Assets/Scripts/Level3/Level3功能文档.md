# Level3 功能与预制件文档

## 场景结构 (Level3.unity)

```
Level3Workspace          ← 根节点，挂 Level3Workspace + PhaseController + BossAttackSpawner + InvisibleWallController + FollowerCounter + ClimaxSequenceController + WorkspaceEventRouter + Level3EventBridge + Level3DualWorldTransition
├── DreamRoot            ← 世界空间游戏内容
│   ├── PlayerSpawn      ← 玩家出生点
│   ├── Ground           ← 地面碰撞
│   ├── Background       ← SpriteRenderer 背景（sortingOrder: -1）
│   ├── WallRight        ← 隐形墙精灵
│   ├── Boss             ← Boss 位置标记
│   ├── CameraRig        ← Cinemachine 虚拟相机（固定视角，无 Follow）
│   ├── SideScrollPlayer ← 玩家 prefab 实例
│   ├── Animal_1~4       ← 动物实例（初始 inactive）
│   └── PathBlock x4     ← 通路方块（初始 inactive）
├── RealityRoot          ← Canvas (Overlay, sortingOrder:10)
│   ├── Level3XiaohongshuUI / Level3HUD / Level3FailurePage / Level3VictoryOverlay
│   └── XiaohongshuPanel ← 小红书右侧面板
│       ├── msgBox / likeBox / peopleBox / fan  ← UI 装饰元素
│       ├── msg1~msg4     ← 四条暖心评论（初始 inactive）
│       └── redpoint      ← 红点提示（跟随最新 active 的 msg）
├── backgroundCanvas     ← Canvas (ScreenSpace-Camera, sortingOrder:-10)
│   └── Background       ← UI Image 背景图
├── SceneEssentials      ← prefab（MainCamera + EventSystem）
└── SceneRouterHooks     ← prefab（场景路由）
```

---

## 关卡流程（三阶段）

| 阶段 | 触发条件 | 事件 ID | 核心行为 |
|------|----------|---------|----------|
| Phase1 | 进入关卡 | `phase.1` | 弹幕下落 + 隐形墙压缩 |
| Phase2 | 15 秒后自动 | `phase.2` | 右侧小红书面板滑入 + 粉丝系统启动 + 弹反解锁 |
| Phase3 | 粉丝达到 8000 | `phase.3` | 弹幕停止 + 动物交互序列开始 |
| 通关 | 正确顺序交互 4 动物 | `level.complete` | 粉丝跳 10 万 + Victory 动画 |
| 失败 | 血量归零 | `level.fail` | Failure 页面 |

---

## 脚本功能一览

### 核心控制

| 脚本 | 职责 |
|------|------|
| `Level3Workspace` | 继承 SideScrollWorkspaceBase，启动 PhaseController，LateUpdate 中限制玩家在隐形墙范围内 |
| `Level3PhaseController` | 管理三阶段切换，Phase1 倒计时 15 秒后自动进 Phase2 |
| `Level3EventBridge` | 订阅 WorkspaceEventRaised，根据事件 ID 分发到各模块（替代 Inspector 手配 20+ 条绑定） |
| `Level3Events` | 所有事件 ID 常量定义 |

### 战斗系统

| 脚本 | 职责 |
|------|------|
| `BossAttackSpawner` | 对象池管理，根据 BossAttackPattern 生成弹幕波次 |
| `BossAttackPattern` | ScriptableObject，定义波次参数（间隔、速度、摆幅、数量上限） |
| `VerbalAttackProjectile` | 弹幕行为：下落 + 摆动 + 碰撞伤害 + 弹反后反弹 |
| `ParryController` | E 键弹反：检测半径内弹幕并 Deflect，触发 `parry.success` 事件 |
| `PlayerCombatState` | 玩家血量管理 + 受击无敌 + 闪烁反馈 |

### 环境

| 脚本 | 职责 |
|------|------|
| `InvisibleWallController` | Phase1 隐形墙逐渐压缩活动空间，受击加速压缩，Phase2 停止 |

### 双世界 / 粉丝系统

| 脚本 | 职责 |
|------|------|
| `Level3DualWorldTransition` | Phase2 触发时小红书面板从右侧滑入的过渡动画 |
| `FollowerCounter` | 粉丝数管理：每秒 +10、弹反 +800、受击 -200、达到阈值发事件 |
| `Level3XiaohongshuUI` | 小红书面板 UI：粉丝数滚动显示 + 评论 msg 依次激活 + redpoint 移动 |

### 高潮序列

| 脚本 | 职责 |
|------|------|
| `ClimaxSequenceController` | 阈值触发动物出现 + 监听 `interact.animal.N` + 验证交互顺序 + SetActive 通路方块 + 激活 ExitDoor |

### UI

| 脚本 | 职责 |
|------|------|
| `Level3HUD` | 阶段提示文字 + 受击红闪 + 血量图标 |
| `Level3FailurePage` | 失败页（重试 / 返回主菜单） |
| `Level3VictoryOverlay` | 通关动画（打字机效果 + 跳转下一场景） |

---

## 预制件

### Projectiles

| 文件 | 说明 |
|------|------|
| `Attack_Projectile.prefab` | 言语攻击弹幕（SpriteRenderer + BoxCollider2D(Trigger) + Rigidbody2D(Kinematic) + VerbalAttackProjectile）。sprites 数组存多种外观，生成时随机选一个 |

### Animals

| 文件 | 说明 |
|------|------|
| `Animal_1~4.prefab` | 四个颜色不同的小动物，带 InteractTrigger（interactId: `animal.1`~`animal.4`），玩家按 E 触发对应事件 |

### Blocks

| 文件 | 说明 |
|------|------|
| `PathBlock.prefab` | 通路方块（Layer: Ground, BoxCollider2D 非 trigger），正确交互后 SetActive(true) |
| `ExitDoor.prefab` | 出口门（Layer: Interactable, BoxCollider2D trigger），全部通路点亮后激活 |

### Data (ScriptableObject)

| 文件 | 说明 |
|------|------|
| `BossAttackPattern_Phase1.asset` | 三波次，间隔 1.2→0.9→0.7 秒，速度 3→3.5→4，最大 30/40/50 发 |
| `BossAttackPattern_Phase2.asset` | 两波次，间隔 0.6→0.4 秒，速度 5→5.5，摆幅更大，含 Targeted/Spread 模式 |

---

## 事件系统

所有模块通过 `SideScrollWorkspaceBase.RaiseWorkspaceEvent(string)` 通信，`Level3EventBridge` 统一监听分发。

关键事件：

```
phase.1 / phase.2 / phase.3          ← 阶段切换
player.hit / player.defeated          ← 玩家受击/死亡
parry.success                         ← 弹反成功
follower.changed                      ← 粉丝数变动
follower.2000 / 4000 / 6000 / 8000   ← 粉丝阈值
interact.animal.1~4                   ← 玩家与动物交互
animal.reveal.1~4                     ← 动物出现
sequence.correct / wrong / complete   ← 交互序列结果
level.complete / level.fail           ← 关卡结束
```

---

## Inspector 需手动配置项

1. **Level3EventBridge** — 大部分字段通过 `AutoResolve()` 自动查找，通常无需手动拖
2. **ClimaxSequenceController**
   - `animals[]` → 拖入 4 个 Animal GameObject
   - `pathBlocks[]` → 拖入 4 个 PathBlock GameObject
   - `exitDoor` → 拖入 ExitDoor
3. **Level3XiaohongshuUI**
   - `comment1~4` → 拖入 msg1~msg4
   - `redpoint` → 拖入 redpoint 的 RectTransform
   - `followerCountText` → 粉丝数文本
4. **BossAttackSpawner**
   - `projectilePrefab` → Attack_Projectile.prefab
   - `spawnZone` → 场景中标记生成区域的 BoxCollider2D
   - `phase1Pattern / phase2Pattern` → 对应 .asset 文件
5. **ParryController** — 需挂在玩家 GameObject 上（目前 Player prefab 未包含）
6. **PlayerCombatState** — 同上，需挂在玩家身上才能接收弹幕伤害

---

## 相机配置

- CameraRig 的 `defaultConfig` 已置空 → Play 时不套用任何 CameraConfig，相机保持编辑器中的位置/大小
- Cinemachine VirtualCamera 的 `m_Follow` 已置空 → 固定视角，不跟踪玩家
- orthographicSize: 6（场景 override）
