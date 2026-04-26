# Game-create3 项目手册（唯一维护入口）

最后更新：2026-04-26  
适用版本：Unity 2022.3.62f3c1

## 0. 原型 / 实验文档入口
- 第二章双世界联动大原型（`Chapter2Prototype.unity` 运行时自动生成版）：`Docs/Chapter2_Demo_Prototype.md`

## 1. 项目定位
- 类型：2D 横版滚动叙事冒险（Narrative Side-scroller）。
- 主题：绘本 + 童话。
- 目标体验：探索场景 -> 触发对话 -> 做选择 -> 解轻机关 -> 剧情状态变化 -> 开启新路径。

## 2. 当前系统总览

| 功能模块 | 当前状态 | 对应脚本 |
|---|---|---|
| 玩家移动（旧） | 已实现基础移动/跳跃/输入锁定 | `SideScrollerPlayerController` |
| 镜头跟随（旧） | 已实现平滑跟随与边界限制 | `SideScrollCameraFollow` |
| 对话数据结构 | 已实现节点、选项、条件、变量修改 | `DialogueAsset` |
| 对话运行器 | 已实现进入对话、节点跳转、选项选择、结束对话 | `DialogueController` |
| 对话 UI 桥接 | 已实现说话人/正文/选项按钮渲染 | `DialoguePanelUI` |
| 剧情变量系统 | 已实现 Bool/Int/String 变量读写与条件判断 | `NarrativeVariableStore` |
| 可交互系统（旧） | 已实现交互接口、可交互基类、范围检测 | `IInteractable` / `InteractableBase` / `InteractionDetector` |
| 机关系统 | 已实现拉杆、门禁（条件开门） | `LeverSwitch` / `PuzzleGate` |
| 任务系统 | 已实现目标定义与完成追踪 | `ObjectiveDefinition` / `ObjectiveTracker` |
| 存档系统 | 已实现 JSON 存档/读档（位置+变量+任务） | `SaveDataModels` / `JsonSaveUtility` / `SaveGameController` |
| 演出触发 | 已实现 Timeline 触发与玩家输入锁定 | `SimpleCutsceneTrigger` |
| SideScroll 基础模块（新） | 第一版骨架已建立 | `Assets/Scripts/SideScroll/*` |
| 双世界关卡模块（DualWorld） | 第一版对齐子关闭环可玩，颜色子关仅占位 | `Assets/Scripts/DualWorld/*` |

## 3. 旧系统依赖关系
1. `NarrativeVariableStore` 是核心状态中心。
2. `DialogueController`、`PuzzleGate`、`ObjectiveTracker` 依赖变量系统。
3. `InteractionDetector` 负责发现可交互对象并触发交互。
4. `SaveGameController` 负责把玩家位置、变量状态、任务状态持久化。
5. `DialoguePanelUI` 只是展示层，核心逻辑不写在 UI 里。

## 4. 旧原型链路说明
以下脚本继续保留，当前不参与新 `SideScroll` 第一版测试场景：
- `Assets/Scripts/Player/SideScrollerPlayerController.cs`
- `Assets/Scripts/Camera/SideScrollCameraFollow.cs`
- `Assets/Scripts/Interaction/IInteractable.cs`
- `Assets/Scripts/Interaction/InteractableBase.cs`
- `Assets/Scripts/Interaction/InteractionDetector.cs`
- `Assets/Scripts/Prototype/*`

迁移策略：
- 第一版采用“并存迁移”。
- 旧原型不删除、不重命名、不替换入口。
- 新横板基础能力统一放到 `Assets/Scripts/SideScroll` 下。

## 5. SideScroll 第一版目录职责
- `Assets/Scripts/SideScroll/Core`
  放 `WorkspaceMode`、工作区枚举与基础共享类型。
- `Assets/Scripts/SideScroll/Workspace`
  放 `SideScrollWorkspaceBase` 与测试场景运行时搭建入口。
- `Assets/Scripts/SideScroll/Character`
  放角色控制器、移动 Motor、跳跃 Motor、地面检测、输入代理。
- `Assets/Scripts/SideScroll/Camera`
  放 `SideScrollCameraController`，统一封装 Cinemachine。
- `Assets/Scripts/SideScroll/Interaction`
  放新交互接口、交互基类、交互检测器。
- `Assets/Scripts/SideScroll/Trigger`
  放基础触发器和工作区事件触发器。
- `Assets/Scripts/SideScroll/Data`
  放 `ScriptableObject` 配置类型。

## 6. SideScroll 第一版核心脚本

### 6.1 工作区
- `SideScrollWorkspaceBase`
  负责 `Initialize() / Enter() / Exit() / Pause() / Resume()`
  负责工作区配置绑定、玩家/相机绑定、交互物与触发器注册。

### 6.2 角色
- `SideScrollCharacterControllerBase`
  协调输入代理、移动、跳跃、交互能力开关。
- `CharacterMovementMotor`
  处理水平移动、加速、减速、朝向翻转。
- `CharacterJumpMotor`
  处理跳跃、coyote time、jump buffer、上下落重力差。
- `CharacterGroundDetector`
  负责地面检测。
- `CharacterInputProxy`
  统一读取输入源。
- `ICharacterInputSource`
  当前第一版实现：
  `PlayerInputSource`
  `DisabledInputSource`

### 6.3 相机
- `SideScrollCameraController`
  负责 `SetFollowTarget()`、`ApplyCameraConfig()`、`ResetToDefault()`
  基于 `CinemachineVirtualCamera + CinemachineConfiner2D`

### 6.4 交互与触发
- `ISideScrollInteractable`
- `SideScrollInteractableBase`
- `SideScrollInteractionDetector`
- `TriggerZoneBase`
- `WorkspaceEventTriggerZone`

### 6.5 配置
- `SideScrollWorkspaceConfig`
- `CharacterMoveConfig`
- `CharacterJumpConfig`
- `CameraConfig`

## 7. 输入方案
- SideScroll 第一版使用 Unity `Input System`
- 当前直接在 `PlayerInputSource` 中创建运行时 `InputAction`
- 默认输入：
  - 移动：`A/D`、左右方向键、手柄左摇杆 X
  - 跳跃：`Space`、`W`、上方向键、手柄 South
  - 交互：`E`、`Enter`、手柄 West

项目当前 `activeInputHandler` 为 `2`，可兼容旧输入链路与新输入链路并存。

## 8. Layer 约定
本次新增最小必要 Layer：
- `Ground`
- `Interactable`
- `Trigger`
- `Player`

用途：
- `Ground`：角色地面检测与碰撞地形
- `Interactable`：交互扫描
- `Trigger`：工作区触发区域
- `Player`：横板角色对象

## 9. SideScroll 测试场景入口
- 场景：`Assets/Scenes/SS_Test_Workspace.unity`
- 运行方式：打开场景后直接 Play
- 运行时入口：`SideScrollTestWorkspaceBootstrap`
- 场景搭建器：`SideScrollTestWorkspaceAutoBuilder`

当前测试场景会在运行时自动生成：
- `WorkspaceRoot`
- `PlayerSpawn`
- `SideScrollPlayer`
- `CameraRig`
- `Environment`
- `DebugInteractable`
- `WorkspaceEventTrigger`
- `CameraBounds`

说明：
- 测试场景是第一版唯一验收入口。
- `Chapter2Prototype` 不接入新工作区基础框架。

## 10. SideScroll 第一版验收清单
- 进入 `SS_Test_Workspace` 后，工作区能自动初始化玩家、相机、输入链路。
- 玩家可左右移动、跳跃、落地，地面检测稳定。
- 输入禁用后，角色不再响应移动与跳跃。
- Cinemachine 相机能稳定跟随玩家，并受 `CameraBounds` 约束。
- 玩家靠近交互物后，按交互键可触发 `SideScrollDebugInteractable`。
- 玩家进入触发区后，可触发 `WorkspaceEventTriggerZone` 日志事件。
- 旧场景 `Chapter2Prototype` 不因新系统加入而失效。

## 11. 开发约束
- 新剧情、玩法扩展不要回写到旧 `Prototype` 链路。
- 横板基础扩展优先通过 `SideScrollWorkspaceBase` 子类完成。
- 如需新增 Story / Gameplay 工作区子类，沿用当前 `SideScroll` 目录结构。

## 12. 双世界关卡模块（DualWorld）第一版

### 12.1 目录结构
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

### 12.2 子关阶段枚举（SubLevelPhase）
固定 8 个阶段，构成对齐子关闭环：
1. `RealityTaskActive`：右屏拖拽可提交，左屏遮罩。
2. `RealityTaskBlocked`：第一次提交失败，聊天弹卡关消息。
3. `DreamTaskUnlocked`：左屏激活，玩家可推物。
4. `RealityTaskEnhanced`：左屏完成 → 右屏吸附增强。
5. `RealityTaskCompleted`：玩家二次提交成功。
6. `DreamWorldResolved`：左屏路径开启动画结束。
7. `DreamTraversalActive`：玩家可在横版地形通行。
8. `SubLevelCompleted`：到达出口，子关结束。

### 12.3 入口与测试场景
- 测试场景：`Assets/Scenes/DW_Test_Workspace.unity`
- 直接 Play 即可。`DualWorldRuntimeBootstrap` 在 `AfterSceneLoad` 自动调用 `DualWorldTestSceneAutoBuilder.BuildIfNeeded()`，反射注入私有 `[SerializeField]` 字段，运行时构建工作区、对齐任务 UI、左屏推物舒适区、聊天面板。
- 因当前测试场景未生成横板玩家，自动搭建器还提供两个调试按钮：
  - `DEBUG: 模拟梦境完成` 直接调用 `AlignmentSubLevelFlow.OnDreamComplete`。
  - `DEBUG: 模拟走到出口` 直接调用 `OnTraversalReachedExit`，便于离线验收闭环。

### 12.4 与 SideScroll 的关系
- `DualWorldWorkspace : SideScrollWorkspaceBase`，复用工作区生命周期与玩家/相机绑定。
- 输入接管走 `CharacterInputProxy.SetSource(DisabledInputSource)`，不直接调用旧 `SideScrollerPlayerController.SetInputLocked`。
- 颜色子关（`ColorSubLevelFlow`）仅留 stub，不参与第一版验收。

### 12.5 第一版不在范围
- 颜色子关玩法
- 存档接入子关进度
- 真实剧情入口（`StoryFlowBridge.EnterSideScroller("chapter_dual_world")`）
- 美术、音效、最终演出资产

## 13. 强制规则
### Rule DOC-001：任何变更必须同步更新文档
- 触发条件：功能逻辑、项目配置、开发流程任一发生变化。
- 强制动作：在同一次提交中更新 `Docs/Project_Handbook.md`。
- 提交要求：提交或 PR 说明中必须写明 `Updated Docs/Project_Handbook.md`。
- 例外情况：纯注释、纯格式化、无行为变化重命名可不更新，但需写 `No doc impact`。
