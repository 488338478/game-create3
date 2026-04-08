# Game-create3 项目手册（唯一维护入口）

最后更新：2026-03-23  
适用版本：Unity 2022.3.62f3c1

## 0. 原型 / 实验文档入口
- 第二章双世界联动大原型（`Chapter2Prototype.unity` 运行时自动生成版）：`Docs/Chapter2_Demo_Prototype.md`

## 1. 项目定位
- 类型：2D 横版滚动叙事冒险（Narrative Side-scroller）。
- 主题：绘本 + 童话。
- 目标体验：探索场景 -> 触发对话 -> 做选择 -> 解轻机关 -> 剧情状态变化 -> 开启新路径。

## 2. 当前已实现功能总览

| 功能模块 | 当前状态 | 对应脚本 |
|---|---|---|
| 玩家移动 | 已实现基础移动/跳跃/输入锁定 | `SideScrollerPlayerController` |
| 镜头跟随 | 已实现平滑跟随与边界限制 | `SideScrollCameraFollow` |
| 对话数据结构 | 已实现节点、选项、条件、变量修改 | `DialogueAsset` |
| 对话运行器 | 已实现进入对话、节点跳转、选项选择、结束对话 | `DialogueController` |
| 对话 UI 桥接 | 已实现说话人/正文/选项按钮渲染 | `DialoguePanelUI` |
| 剧情变量系统 | 已实现 Bool/Int/String 变量读写与条件判断 | `NarrativeVariableStore` |
| 可交互系统 | 已实现交互接口、可交互基类、范围检测 | `IInteractable`/`InteractableBase`/`InteractionDetector` |
| 机关系统 | 已实现拉杆、门禁（条件开门） | `LeverSwitch`/`PuzzleGate` |
| 任务系统 | 已实现目标定义与完成追踪 | `ObjectiveDefinition`/`ObjectiveTracker` |
| 存档系统 | 已实现 JSON 存档/读档（位置+变量+任务） | `SaveDataModels`/`JsonSaveUtility`/`SaveGameController` |
| 演出触发 | 已实现 Timeline 触发与玩家输入锁定 | `SimpleCutsceneTrigger` |

## 3. 功能依赖关系（先理解再搭）
1. `NarrativeVariableStore` 是核心状态中心。  
2. `DialogueController`、`PuzzleGate`、`ObjectiveTracker` 都依赖变量系统。  
3. `InteractionDetector` 负责“发现可交互对象并触发交互”。  
4. `SaveGameController` 负责把玩家位置、变量状态、任务状态持久化。  
5. `DialoguePanelUI` 只是展示层，核心逻辑不写在 UI 里。  

## 4. 脚本与参数详细说明（重点）

### 4.1 玩家移动
文件：`Assets/Scripts/Player/SideScrollerPlayerController.cs`

| 参数 | 类型 | 建议值 | 作用 |
|---|---|---|---|
| `moveSpeed` | `float` | 4~6 | 水平移动速度 |
| `jumpForce` | `float` | 8~12 | 起跳速度 |
| `groundCheckPoint` | `Transform` | 脚底空物体 | 地面检测点 |
| `groundCheckRadius` | `float` | 0.15~0.25 | 地面检测半径 |
| `groundMask` | `LayerMask` | Ground | 判定为地面的层 |

对应功能：
- 左右移动、跳跃。
- 支持 `SetInputLocked(true/false)`，用于对话/演出期间禁用玩家操作。

### 4.2 镜头跟随
文件：`Assets/Scripts/Camera/SideScrollCameraFollow.cs`

| 参数 | 类型 | 建议值 | 作用 |
|---|---|---|---|
| `target` | `Transform` | Player | 跟随目标 |
| `offset` | `Vector3` | `(0,1,-10)` | 镜头偏移 |
| `smoothTime` | `float` | 0.15~0.3 | 跟随平滑时间 |
| `useBounds` | `bool` | true | 是否启用边界约束 |
| `minX/maxX/minY/maxY` | `float` | 关卡边界 | 镜头活动范围 |

对应功能：
- 相机平滑跟随玩家。
- 防止镜头看到关卡外区域。

### 4.3 叙事变量系统
文件：`Assets/Scripts/Core/NarrativeTypes.cs`、`Assets/Scripts/Core/NarrativeVariableStore.cs`

`NarrativeVariableStore` 关键能力：
- 变量类型：`Bool / Int / String`。
- API：`GetBool/GetInt/GetString`、`SetBool/SetInt/SetString`。
- 条件判断：`EvaluateConditions(...)`。
- 快照：`CaptureSnapshot()` / `RestoreSnapshot(...)`。

变量命名建议：
- `story.*`：剧情状态，例如 `story.met_grandma`。
- `puzzle.*`：机关状态，例如 `puzzle.lever_a`。
- `quest.*`：任务状态，例如 `quest.chapter1_done`。

### 4.4 对话系统
文件：`Assets/Scripts/Narrative/DialogueAsset.cs`、`Assets/Scripts/Narrative/DialogueController.cs`

`DialogueAsset` 中每个节点包含：
- `nodeId`：节点唯一 ID。
- `speaker`：说话人。
- `body`：正文。
- `enterMutations`：进入节点时修改的变量。
- `choices`：选项列表。

选项包含：
- `text`：选项文字。
- `conditions`：显示/可选条件。
- `selectMutations`：选中后变量变化。
- `nextNodeId`：跳转节点。
- `endDialogue`：是否结束对话。

对应功能：
- 分支叙事。
- 对话直接改变机关和任务条件（通过变量）。

### 4.5 对话 UI
文件：`Assets/Scripts/UI/DialoguePanelUI.cs`

| 参数 | 类型 | 作用 |
|---|---|---|
| `dialogueController` | `DialogueController` | 对话数据来源 |
| `rootGroup` | `CanvasGroup` | 控制对话面板显隐 |
| `speakerLabel` | `TMP_Text` | 说话人文本 |
| `bodyLabel` | `TMP_Text` | 正文文本 |
| `choiceContainer` | `Transform` | 选项按钮父节点 |
| `choiceButtonPrefab` | `Button` | 选项按钮模板 |

对应功能：
- 根据对话状态实时刷新 UI。
- 动态创建选项按钮并回调 `SelectChoice(index)`。

### 4.6 交互系统
文件：`Assets/Scripts/Interaction/IInteractable.cs`、`Assets/Scripts/Interaction/InteractableBase.cs`、`Assets/Scripts/Interaction/InteractionDetector.cs`

`InteractionDetector` 关键参数：

| 参数 | 类型 | 建议值 | 作用 |
|---|---|---|---|
| `scanOrigin` | `Transform` | 玩家胸口/脚底附近 | 扫描中心点 |
| `scanRadius` | `float` | 0.8~1.5 | 交互扫描半径 |
| `interactableMask` | `LayerMask` | Interactable | 可交互层过滤 |
| `interactionKey` | `KeyCode` | `E` | 交互按键 |

对应功能：
- 玩家靠近可交互对象时可触发交互。
- 与 `InteractableBase` 配合可快速做门、按钮、对话触发器。

### 4.7 机关系统
文件：`Assets/Scripts/Puzzle/LeverSwitch.cs`、`Assets/Scripts/Puzzle/PuzzleGate.cs`

`LeverSwitch` 关键参数：

| 参数 | 类型 | 作用 |
|---|---|---|
| `variableStore` | `NarrativeVariableStore` | 写入变量 |
| `variableKey` | `string` | 例如 `puzzle.lever_a` |
| `mode` | `LeverMode` | Toggle / SetTrue / SetFalse |

`PuzzleGate` 关键参数：

| 参数 | 类型 | 作用 |
|---|---|---|
| `variableStore` | `NarrativeVariableStore` | 读取条件 |
| `unlockConditions` | `DialogueConditionData[]` | 开门条件集合 |
| `autoUnlockOnStart` | `bool` | 开场是否自动检查开门 |

对应功能：
- 拉杆改变量。
- 门根据变量条件开关，实现“叙事状态驱动机关”。

### 4.8 任务系统
文件：`Assets/Scripts/Quest/ObjectiveDefinition.cs`、`Assets/Scripts/Quest/ObjectiveTracker.cs`

`ObjectiveDefinition`：
- `objectiveId`：任务唯一 ID。
- `title`/`description`：任务文案。
- `completionConditions`：完成条件（变量条件集合）。

`ObjectiveTracker`：
- 根据变量变化自动刷新完成状态。
- 对外给出当前任务 `CurrentObjective`。

### 4.9 存档系统
文件：`Assets/Scripts/Save/SaveDataModels.cs`、`Assets/Scripts/Save/JsonSaveUtility.cs`、`Assets/Scripts/Save/SaveGameController.cs`

`SaveGameController` 关键参数：

| 参数 | 类型 | 建议 |
|---|---|---|
| `saveFileName` | `string` | `save_slot_01.json` |
| `playerTransform` | `Transform` | 玩家对象 |
| `variableStore` | `NarrativeVariableStore` | 剧情变量来源 |
| `objectiveTracker` | `ObjectiveTracker` | 任务状态来源 |

对应功能：
- 保存：场景名、玩家位置、变量快照、任务完成列表。
- 读取：恢复变量、任务与玩家位置。

### 4.10 演出触发
文件：`Assets/Scripts/Cutscene/SimpleCutsceneTrigger.cs`

| 参数 | 类型 | 作用 |
|---|---|---|
| `director` | `PlayableDirector` | 播放 Timeline |
| `playerController` | `SideScrollerPlayerController` | 演出期间锁定输入 |
| `oneShot` | `bool` | 是否只触发一次 |
| `targetTag` | `string` | 触发标签，通常是 `Player` |

对应功能：
- 进入触发区播放过场。
- 播放时禁用玩家输入，结束后恢复。

## 5. 场景装配清单（按这个顺序做）
1. 打开 `Assets/Scenes/SampleScene.unity`。  
2. 新建 `GameManager` 并挂脚本：  
- `NarrativeVariableStore`  
- `DialogueController`  
- `ObjectiveTracker`  
- `SaveGameController`  
3. 新建/配置 `Player`：  
- `Rigidbody2D`  
- `SideScrollerPlayerController`  
- `InteractionDetector`  
4. 新建 `Main Camera` 挂 `SideScrollCameraFollow` 并绑定 Player。  
5. 新建 Canvas 与对话面板，挂 `DialoguePanelUI` 并绑定 TMPro 组件。  
6. 场景中摆一个 `LeverSwitch` 和一个 `PuzzleGate`，用同一变量联动。  
7. 创建一个 `DialogueAsset`，在节点中改变量，验证剧情->机关联动。  
8. 调 `SaveGameController.Save()` / `Load()` 验证存档闭环。  

## 6. 第一章可玩切片建议（3~5 分钟）
1. 开场旁白与角色出场（1 个对话节点）。  
2. 玩家向右探索，遇到第一段分支对话（2 个选项）。  
3. 选项 A 直接给线索，选项 B 需先拉杆。  
4. 拉杆改 `puzzle.lever_a=true`，门开启。  
5. 穿过门进入结尾短演出（Timeline），完成当前目标。  

## 7. 调试与验收清单
- 对话：每个选项都有去向，不存在死节点。  
- 条件：`unlockConditions` 与变量类型一致。  
- 交互：`scanRadius` 不要过大，避免串交互。  
- 相机：边界值覆盖整个可玩区。  
- 存档：至少测试“存档后退出再进入读档”。  
- 任务：变量变化后任务状态可自动刷新。  

## 8. 强制规则
### Rule DOC-001：任何变更必须同步更新文档
- 触发条件：功能逻辑、项目配置、开发流程任一发生变化。  
- 强制动作：在同一次提交中更新 `Docs/Project_Handbook.md`。  
- 提交要求：提交或 PR 说明中必须写明 `Updated Docs/Project_Handbook.md`。  
- 例外情况：纯注释、纯格式化、无行为变化重命名可不更新，但需写 `No doc impact`。  
