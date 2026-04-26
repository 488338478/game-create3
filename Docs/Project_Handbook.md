# Game-create3 项目手册（唯一维护入口）

最后更新：2026-04-26  
适用版本：Unity 2022.3.62f3c1

## 0. 原型 / 实验文档入口
- 第二章双世界联动大原型（`Chapter2Prototype.unity` 运行时自动生成版）：`Docs/Chapter2_Demo_Prototype.md`
- 剧情机（`StoryPlayer` 绘本页 / CG 页 / 章节转场播放器）：`Docs/StoryPlayer_Manual.md`

## 1. 项目定位
- 类型：2D 横版滚动叙事冒险（Narrative Side-scroller）
- 主题：绘本 + 童话
- 目标体验：探索场景 -> 触发对话/事件 -> 轻玩法 -> 状态变化 -> 开启新路径

## 2. 当前模块总览

| 模块 | 当前状态 | 主要入口 |
|---|---|---|
| 玩家移动（旧） | 已实现基础移动/跳跃/输入锁定 | `SideScrollerPlayerController` |
| 镜头跟随（旧） | 已实现平滑跟随与边界限制 | `SideScrollCameraFollow` |
| 对话数据结构 | 已实现节点、选项、条件、变量修改 | `DialogueAsset` |
| 对话运行器 | 已实现进入对话、节点跳转、选项选择、结束对话 | `DialogueController` |
| 对话 UI 桥接 | 已实现说话人/正文/选项按钮渲染 | `DialoguePanelUI` |
| 剧情机 | 已实现剧情页播放、转场、页内事件、音频、输入推进、结束回调 | `StoryPlayer` / `StoryPageRenderer` / `StoryFlowBridge` |
| 剧情变量系统 | 已实现 Bool/Int/String 变量读写与条件判断 | `NarrativeVariableStore` |
| 可交互系统（旧） | 已实现交互接口、可交互基类、范围检测 | `IInteractable` / `InteractableBase` / `InteractionDetector` |
| 机关系统 | 已实现拉杆、门禁（条件开门） | `LeverSwitch` / `PuzzleGate` |
| 任务系统 | 已实现目标定义与完成追踪 | `ObjectiveDefinition` / `ObjectiveTracker` |
| 存档系统 | 已实现 JSON 存档/读档（位置+变量+任务） | `SaveDataModels` / `JsonSaveUtility` / `SaveGameController` |
| 演出触发 | 已实现 Timeline 触发与玩家输入锁定 | `SimpleCutsceneTrigger` |
| SideScroll 基础模块（新） | 第一版骨架已建立 | `Assets/Scripts/SideScroll/*` |
| 双世界关卡模块（DualWorld） | 第一版对齐子关闭环可玩，颜色子关仅占位 | `Assets/Scripts/DualWorld/*` |

## 3. 迁移策略
- 采用“并存迁移”
- 旧 `Prototype / Player / Camera / Interaction` 不删除、不改入口
- 新横板基础统一放在 `Assets/Scripts/SideScroll`
- 新系统先在独立模板场景和测试场景里验证，再逐步回接正式玩法

## 4. SideScroll 目录职责
- `Assets/Scripts/SideScroll/Core`
  - 枚举、共享基础类型
- `Assets/Scripts/SideScroll/Data`
  - `SideScrollWorkspaceConfig`
  - `CharacterMoveConfig`
  - `CharacterJumpConfig`
  - `CameraConfig`
  - `ConditionRequirementData`
- `Assets/Scripts/SideScroll/Character`
  - 角色控制器、输入代理、移动/跳跃 Motor、地面检测
- `Assets/Scripts/SideScroll/Camera`
  - `SideScrollCameraController`
  - `CameraZone`
- `Assets/Scripts/SideScroll/Interaction`
  - 交互接口、交互基类、拾取物、推动物、观察点、出口
- `Assets/Scripts/SideScroll/Trigger`
  - 基础触发器、对话触发、条件触发、目标触发、镜头触发
- `Assets/Scripts/SideScroll/Workspace`
  - `SideScrollWorkspaceBase`
  - `SideScrollStoryWorkspace`
  - `SideScrollGameplayWorkspace`
  - 测试/模板场景 bootstrap 与 runtime builder

## 5. SideScroll 核心架构原则
- 交互物、触发器、镜头区不直接互相找对象
- 它们统一向当前 `Workspace` 上报事件或状态
- `Workspace` 负责记录：
  - 已触发事件
  - 已拾取道具
  - 已完成目标
- `Workspace` 负责流程编排：
  - 条件判断
  - 完成判定
  - 退出逻辑
  - 相机切换

### 5.1 当前工作区事件中心
`SideScrollWorkspaceBase` 当前已支持：
- `RaiseWorkspaceEvent(string eventId)`
- `HasWorkspaceEvent(string eventId)`
- `RegisterPickup(string pickupId)`
- `HasPickup(string pickupId)`
- `RegisterGoal(string goalId)`
- `HasGoal(string goalId)`
- `EvaluateRequirements(IReadOnlyList<ConditionRequirementData>)`

### 5.2 自动扫描与归属绑定
`Workspace.Initialize()` 会自动扫描自己子节点内的：
- `ISideScrollInteractable`
- `TriggerZoneBase`
- `CameraZone`

然后把当前工作区引用回填给这些对象：
- `SideScrollInteractableBase.BindWorkspace(...)`
- `TriggerZoneBase.BindWorkspace(...)`
- `CameraZone.BindWorkspace(...)`

结论：
- 新增交互物/触发器时，只要放进 `WorkspaceRoot` 子树，工作区会自动接管
- 不需要让这些对象彼此直接拖引用

## 6. 工作区类型

### 6.1 SideScrollWorkspaceBase
职责：
- 解析玩家与相机引用
- 应用工作区配置
- 扫描并注册子节点对象
- 维护工作区事件、拾取、目标状态
- 控制进入/退出/暂停/恢复

主要公开接口：
- `PlayerController`
- `CameraController`
- `IsEntered`
- `RaiseWorkspaceEvent(...)`
- `HasWorkspaceEvent(...)`

### 6.2 SideScrollStoryWorkspace
职责：
- 管理观察点、剧情事件、剧情输入锁定
- 接收 `ObservationPoint` / `DialogueTriggerZone` / `CameraZone` 事件

当前限制：
- 这轮不直接接 `DialogueController`
- `DialogueTriggerZone` 只发 `dialogue.*` 工作区事件

### 6.3 SideScrollGameplayWorkspace
职责：
- 管理拾取、推物、目标达成、完成判定
- 接收 `PickupObject` / `PushableObject` / `GoalTriggerZone` / `ExitPoint` 事件

主要公开接口：
- `IsCompleted`
- `EvaluateCompletion()`

## 7. 角色与输入
角色入口：
- `SideScrollCharacterControllerBase`

组成：
- `CharacterInputProxy`
- `CharacterGroundDetector`
- `CharacterMovementMotor`
- `CharacterJumpMotor`
- `SideScrollInteractionDetector`

输入方案：
- SideScroll 侧使用 Unity `Input System`
- 当前 `PlayerInputSource` 运行时创建 `InputAction`

默认输入：
- 移动：`A/D`、左右方向键、手柄左摇杆 X
- 跳跃：`Space`、`W`、上方向键、手柄 South
- 交互：`E`、`Enter`、手柄 West

## 8. 相机系统
入口：
- `SideScrollCameraController`
- `CameraZone`

当前规则：
- 默认配置来自 `CameraConfig`
- 进入 `CameraZone` 时应用区域配置
- 离开 `CameraZone` 时恢复默认配置
- 当前只支持单层覆盖，不做多层优先级

## 9. 交互物
当前已实现：
- `ObservationPoint`
- `ExitPoint`
- `PickupObject`
- `PushableObject`
- `SideScrollDebugInteractable`

行为约定：
- `ObservationPoint`
  - 发 `observation.{id}`
- `ExitPoint`
  - 根据工作区条件决定可否交互
  - 发 `exit.{id}`
- `PickupObject`
  - 注册 pickup 并发 `pickup.{id}`
- `PushableObject`
  - 只负责单轴推动
  - 推动相关状态上报工作区

## 10. 触发器
当前已实现：
- `WorkspaceEventTriggerZone`
- `DialogueTriggerZone`
- `CameraTriggerZone`
- `ConditionTriggerZone`
- `GoalTriggerZone`

行为约定：
- `DialogueTriggerZone`
  - 发 `dialogue.{id}`
- `CameraTriggerZone`
  - 切换相机覆盖
- `ConditionTriggerZone`
  - 条件满足时发 `condition.{id}.passed`
- `GoalTriggerZone`
  - 注册目标并发 `goal.{id}`

## 11. Layer 约定
- `Ground`
- `Interactable`
- `Trigger`
- `Player`

用途：
- `Ground`：角色落地检测与地形碰撞
- `Interactable`：交互扫描
- `Trigger`：触发区 / 镜头区
- `Player`：横板角色对象

## 12. Prefab 清单
目录：
- `Assets/Prefabs/SideScroll`

当前交付：
- `WorkspaceRoot.prefab`
- `SideScrollPlayer.prefab`
- `CameraRig.prefab`
- `Interactable_ObservationPoint.prefab`
- `Interactable_ExitPoint.prefab`
- `Interactable_PickupObject.prefab`
- `Interactable_PushableObject.prefab`
- `Trigger_WorkspaceEvent.prefab`
- `Trigger_Dialogue.prefab`
- `Trigger_Goal.prefab`
- `CameraZone.prefab`

说明：
- 这轮 prefab 以“最小可复用入口”为目标
- 复杂装配仍由模板场景和 runtime builder 兜底

## 13. 场景入口

### 13.1 测试场景
- `Assets/Scenes/SS_Test_Workspace.unity`
- 运行入口：`SideScrollTestWorkspaceBootstrap`

### 13.2 Story 模板场景
- `Assets/Scenes/Templates/SS_Story_Template.unity`
- 运行入口：`SideScrollStoryTemplateBootstrap`
- runtime builder：`SideScrollTemplateSceneAutoBuilder.BuildStoryTemplateIfNeeded()`

### 13.3 Gameplay 模板场景
- `Assets/Scenes/Templates/SS_Gameplay_Template.unity`
- 运行入口：`SideScrollGameplayTemplateBootstrap`
- runtime builder：`SideScrollTemplateSceneAutoBuilder.BuildGameplayTemplateIfNeeded()`

## 14. 模板场景内容

### 14.1 Story 模板
运行时会生成：
- `WorkspaceRoot`
- `PlayerSpawn`
- `SideScrollPlayer`
- `CameraRig`
- `Environment`
- `ObservationPoint`
- `DialogueTriggerZone`
- `CameraZone`
- `ExitPoint`
- `CameraBounds`

### 14.2 Gameplay 模板
运行时会生成：
- `WorkspaceRoot`
- `PlayerSpawn`
- `SideScrollPlayer`
- `CameraRig`
- `Environment`
- `PickupObject`
- `PushableObject`
- `GoalTriggerZone`
- `ExitPoint`
- `CameraZone`
- `CameraBounds`

## 15. 手动挂载指南

### 15.1 WorkspaceRoot
挂：
- `SideScrollWorkspaceBase` 或其子类

推荐子节点：
- `PlayerSpawn`
- `SideScrollPlayer`
- `Environment`
- `Interactables`
- `Triggers`
- `CameraZones`

字段绑定：
- `workspaceConfig`
- `spawnPoint`
- `playerController`
- `cameraController`

### 15.2 Player
推荐组件：
- `SpriteRenderer`
- `BoxCollider2D`
- `Rigidbody2D`
- `CharacterInputProxy`
- `CharacterGroundDetector`
- `CharacterMovementMotor`
- `CharacterJumpMotor`
- `SideScrollInteractionDetector`
- `SideScrollCharacterControllerBase`

### 15.3 CameraRig
场景主相机：
- `Camera`
- `AudioListener`
- `CinemachineBrain`

`CameraRig`：
- `SideScrollCameraController`

子物体 `CM_VCam`：
- `CinemachineVirtualCamera`
- `CinemachineConfiner2D`

`CameraBounds`：
- 使用 `BoxCollider2D`
- 勾选 `Is Trigger = true`
- 只提供给 `CinemachineConfiner2D` 做边界约束
- 不应参与玩家阻挡或地形碰撞

### 15.4 交互物
交互物放到 `Interactables` 下，Layer 设为 `Interactable`

最小要求：
- `Collider2D`
- 一个交互脚本

### 15.5 触发器 / 镜头区
触发器放到 `Triggers` / `CameraZones` 下，Layer 设为 `Trigger`

最小要求：
- `Collider2D`
- `Is Trigger = true`
- 一个触发脚本或 `CameraZone`

## 16. 验收清单
- `dotnet build Game-create3.sln` 通过
- `SS_Test_Workspace` 可运行
- `SS_Story_Template` 可运行
- `SS_Gameplay_Template` 可运行
- `ObservationPoint` / `DialogueTriggerZone` / `CameraZone` / `ExitPoint` 在 Story 模板中可用
- `PickupObject` / `PushableObject` / `GoalTriggerZone` / `ExitPoint` 在 Gameplay 模板中可用
- `Chapter2Prototype` 不受新系统影响

## 17. 已知边界
- Story 侧这轮不直接接 `DialogueController`
- `DialogueTriggerZone` 只发工作区事件
- 条件系统当前只判断工作区内部状态，不接 Narrative 变量系统
- prefab 当前是最小入口资源，不是最终关卡生产标准件

## 18. 双世界关卡模块（DualWorld）第一版

### 18.1 目录结构
- `Assets/Scripts/DualWorld/Flow`
  子关阶段枚举、抽象基类、对齐子关具体实现、颜色子关 stub、关卡内流程控制器。
- `Assets/Scripts/DualWorld/Reality`
  右屏（现实侧）排版任务：`RealityAlignmentTask`、`DraggableAlignmentBlock`、`RealitySubmitResult`。
- `Assets/Scripts/DualWorld/Dream`
  左屏（梦境侧）推物舒适区：`DreamPushTarget` + `DreamPushable` 标记组件，以及路径开启 `DreamPathOpener`。
- `Assets/Scripts/DualWorld/CrossWorld`
  跨世界事件总线：`CrossWorldEvent` / `CrossWorldEventBus` / `DreamToRealityEnhancer` / `RealityToDreamRepair`。
- `Assets/Scripts/DualWorld/Chat`
  聊天任务管线：`ChatTaskDefinition`（ScriptableObject）/ `ChatTaskController` / `ChatTaskPanelUI`。
- `Assets/Scripts/DualWorld/ColorTokens`
  为颜色子关预留的 `ColorToken` 结构。
- `Assets/Scripts/DualWorld/Workspace`
  `DualWorldWorkspace`（继承 `SideScrollWorkspaceBase`）、运行时引导 `DualWorldRuntimeBootstrap`、自动搭建器 `DualWorldTestSceneAutoBuilder`。

### 18.2 子关阶段枚举（SubLevelPhase）
固定 8 个阶段，构成对齐子关闭环：
1. `RealityTaskActive`：右屏拖拽可提交，左屏遮罩。
2. `RealityTaskBlocked`：第一次提交失败，聊天弹卡关消息。
3. `DreamTaskUnlocked`：左屏激活，玩家可推物。
4. `RealityTaskEnhanced`：左屏完成 → 右屏吸附增强。
5. `RealityTaskCompleted`：玩家二次提交成功。
6. `DreamWorldResolved`：左屏路径开启动画结束。
7. `DreamTraversalActive`：玩家可在横版地形通行。
8. `SubLevelCompleted`：到达出口，子关结束。

### 18.3 入口与测试场景
- 测试场景：`Assets/Scenes/DW_Test_Workspace.unity`
- 直接 Play 即可。`DualWorldRuntimeBootstrap` 在 `AfterSceneLoad` 自动调用 `DualWorldTestSceneAutoBuilder.BuildIfNeeded()`，反射注入私有 `[SerializeField]` 字段，运行时构建工作区、对齐任务 UI、左屏推物舒适区、聊天面板。
- 因当前测试场景未生成横板玩家，自动搭建器还提供两个调试按钮：
  - `DEBUG: 模拟梦境完成` 直接调用 `AlignmentSubLevelFlow.OnDreamComplete`。
  - `DEBUG: 模拟走到出口` 直接调用 `OnTraversalReachedExit`，便于离线验收闭环。

### 18.4 与 SideScroll 的关系
- `DualWorldWorkspace : SideScrollWorkspaceBase`，复用工作区生命周期与玩家/相机绑定。
- 输入接管走 `CharacterInputProxy.SetSource(DisabledInputSource)`，不直接调用旧 `SideScrollerPlayerController.SetInputLocked`。
- 颜色子关（`ColorSubLevelFlow`）仅留 stub，不参与第一版验收。

### 18.5 第一版不在范围
- 颜色子关玩法
- 存档接入子关进度
- 真实剧情入口（`StoryFlowBridge.EnterSideScroller("chapter_dual_world")`）
- 美术、音效、最终演出资产

## 19. 强制规则
### Rule DOC-001：任何变更必须同步更新文档
- 功能逻辑、项目配置、开发流程变化时，同次提交必须更新 `Docs/Project_Handbook.md`
- 提交说明中必须写明 `Updated Docs/Project_Handbook.md`
- 纯注释、纯格式化、无行为变化重命名可写 `No doc impact`
