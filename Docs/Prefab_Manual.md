# Prefab 使用手册

> 配套 `GameCreate3` Editor 工具一键生成的全部 prefab。
> 关卡搭建只需要看这一份。

---

## 0. 一图速览

```
Assets/Prefabs/
├── SceneEssentials.prefab                          ← 每个新场景先拖
├── SideScroll/
│   ├── SideScrollPlayer.prefab                     ← 玩家（横板基础）
│   ├── CameraRig.prefab                            ← Cinemachine 相机
│   ├── Shells/
│   │   ├── SideScrollStoryWorkspace.prefab         ← 剧情型工作区壳
│   │   └── SideScrollGameplayWorkspace.prefab      ← 解谜型工作区壳
│   └── Atoms/
│       ├── ObservationPoint.prefab                 ← 按 E 观察
│       ├── PickupObject.prefab                     ← 走过捡起
│       ├── PushableObject.prefab                   ← 玩家可推
│       ├── ExitPoint.prefab                        ← 关卡出口
│       ├── WorkspaceEventTriggerZone.prefab        ← 通用事件触发
│       ├── DialogueTriggerZone.prefab              ← 触发对话
│       ├── GoalTriggerZone.prefab                  ← 触发完成目标
│       ├── ConditionTriggerZone.prefab             ← 条件门禁
│       ├── CameraTriggerZone.prefab                ← 镜头切换
│       ├── GroundBlock.prefab                      ← 地面块
│       └── CameraBounds.prefab                     ← 镜头限制框
├── DualWorld/
│   ├── DualWorldWorkspace.prefab                   ← 完整双世界工作区
│   ├── RealityCanvas.prefab                        ← 左屏拖块 UI
│   ├── DreamWorld.prefab                           ← 右屏地形
│   ├── ChatTaskPanel.prefab                        ← 聊天面板
│   ├── Logic/
│   │   ├── AlignmentSubLevelFlow.prefab            ← 对齐子关流程机
│   │   ├── LevelInGameFlowController.prefab        ← 子关序列控制器
│   │   ├── DreamToRealityEnhancer.prefab           ← 梦→现 桥
│   │   ├── RealityToDreamRepair.prefab             ← 现→梦 桥
│   │   └── ChatTaskController.prefab               ← 聊天事件路由
│   └── Atoms/
│       ├── DraggableBlock.prefab                   ← 可拖 UI 块
│       ├── AlignmentTarget.prefab                  ← 对齐目标位
│       ├── SubmitButton.prefab                     ← 提交按钮
│       ├── PushableBlock.prefab                    ← 梦境推方块
│       └── DreamPushTarget.prefab                  ← 梦境舒适区
└── StoryPlayer/
    ├── StoryPlayerRig.prefab                       ← 全局剧情播放器（DontDestroyOnLoad）
    ├── Triggers/
    │   ├── StoryTriggerZone.prefab                 ← 玩家走入触发
    │   ├── StoryInteractable.prefab                ← 按 E 触发
    │   ├── StoryWorkspaceEventTrigger.prefab       ← 工作区事件触发
    │   └── StoryAutoPlay.prefab                    ← 场景加载完即播
    └── Atoms/
        ├── Background.prefab                       ← 全屏背景图
        ├── FadeOverlay.prefab                      ← 淡入淡出遮罩
        └── InputBlocker.prefab                     ← 透明点击屏蔽层
```

---

## 1. SceneEssentials.prefab

**每个使用 SideScroll / DualWorld / StoryPlayer 的场景都要先拖一份**。

### 包含
- `Main Camera`（Camera + AudioListener + CinemachineBrain，tag = MainCamera）
- `EventSystem`（EventSystem + StandaloneInputModule）

### 用途
- Cinemachine 虚拟相机要靠 `CinemachineBrain` 才能影响 Main Camera 视角
- UI 拖拽、按钮、Drag handler 全部依赖 EventSystem

### 注意
- 场景里**最多一份**。如果场景已经有 Main Camera，先删了再拖（不然会有两个相机抢渲染）
- 不要把它放进任何 prefab 内部 —— Camera 和 EventSystem 是场景全局单例

---

## 2. SideScroll 模块

### 2.1 SideScrollPlayer.prefab

**横板玩家本体**。鼠键直接控（A/D 走、Space 跳、E 交互）。

#### 包含的组件（全在根 GameObject 上）
- `SpriteRenderer`（橙色长方形可视）
- `BoxCollider2D` (0.8 × 1.4)
- `Rigidbody2D`（FreezeRotation、gravityScale=3、Interpolate）
- `SideScrollCharacterControllerBase`（聚合脚本，连接 input → motors）
- `CharacterInputProxy`（输入插槽 + 按键锁存）
- `CharacterGroundDetector`（向下采样地面）
- `CharacterMovementMotor`（移动 motor，引用 `CharacterMoveConfig` SO）
- `CharacterJumpMotor`（跳跃 motor，引用 `CharacterJumpConfig` SO）
- `SideScrollInteractionDetector`（向前扫描可交互物）

#### 子节点
- `GroundCheck`（空 Transform，localPosition (0, -0.75, 0)）—— 地面检测点

#### 引用要求
| 字段 | 来源 | 是否需手挂 |
|---|---|---|
| `groundCheckPoint` | 子节点 GroundCheck | prefab 内已配 ✓ |
| `groundMask` | LayerMask("Ground") | prefab 内已配 ✓ |
| `interactableMask` | LayerMask("Interactable") | prefab 内已配 ✓ |
| `MovementMotor.config` | `Defaults/DefaultCharacterMoveConfig.asset` | prefab 内已配 ✓ |
| `JumpMotor.config` | `Defaults/DefaultCharacterJumpConfig.asset` | prefab 内已配 ✓ |

#### 用法
拖入工作区根（StoryWorkspace / GameplayWorkspace / DualWorldWorkspace）的子节点下，**自动被工作区识别**（通过 `GetComponentInChildren<SideScrollCharacterControllerBase>`）。

---

### 2.2 CameraRig.prefab

**Cinemachine 虚拟相机 + 限制框**。

#### 子结构
```
CameraRig (SideScrollCameraController)
├── CM_VCam (CinemachineVirtualCamera + Confiner2D + FramingTransposer)
└── CameraBounds (BoxCollider2D, isTrigger)
```

#### 用法
拖入工作区根的子节点下。工作区会自动 `GetComponentInChildren<SideScrollCameraController>` 找到它，并把 `vcam.Follow` 指到玩家。

#### 调整范围
点 `CameraBounds` 节点，改 `BoxCollider2D.size`，相机就只能在这个矩形内移动。

---

### 2.3 Shells/SideScrollStoryWorkspace.prefab

**剧情型工作区壳**。空 GameObject 上挂 `SideScrollStoryWorkspace`（继承 `SideScrollWorkspaceBase`）。

#### 多出来的能力
`SetStoryInputLocked(bool)` —— 演出期间锁玩家输入。

#### 用法
作为关卡根。玩家、镜头组、地形、触发器都放在它下面。

---

### 2.4 Shells/SideScrollGameplayWorkspace.prefab

**解谜型工作区壳**。挂 `SideScrollGameplayWorkspace`。

#### 多出来的能力
- `EvaluateCompletion()` —— 检查通关条件（捡齐物品 / 触发齐目标 / 走到出口）
- 达成时广播 `workspace.completed` 事件

#### 用法
同 Story 工作区，区别在于关卡设计要配套用 GoalTriggerZone / PickupObject 凑够通关条件。

---

### 2.5 Atoms 单组件件

下面 11 个都是**单 GameObject + 单组件**的 prefab，**拖到工作区任意子节点**即可生效（工作区 ScanSceneObjects 会自动绑）。

| Prefab | 组件 | 必填字段 | 行为 |
|---|---|---|---|
| `ObservationPoint` | ObservationPoint | observationText | 玩家走近按 E → 显示文本 |
| `PickupObject` | PickupObject | pickupId | 玩家走入 collider → 记录到工作区 collectedPickupIds |
| `PushableObject` | PushableObject | — | 物理推动（带 Rigidbody2D） |
| `ExitPoint` | ExitPoint | — | 玩家走入 → 工作区抛 `workspace.exit` 事件 |
| `WorkspaceEventTriggerZone` | WorkspaceEventTriggerZone | eventId, targetLayers | 玩家走入 → 工作区抛指定 eventId |
| `DialogueTriggerZone` | DialogueTriggerZone | dialogueAsset | 玩家走入 → 启动对话（依赖旧 Narrative 模块） |
| `GoalTriggerZone` | GoalTriggerZone | goalId | 玩家走入 → 记录 completed goal |
| `ConditionTriggerZone` | ConditionTriggerZone | requirements (List) | 玩家走入 → 评估需求列表，全满足才触发 |
| `CameraTriggerZone` | CameraTriggerZone | cameraConfig | 玩家走入 → 切换镜头配置 |
| `GroundBlock` | （仅 SpriteRenderer + BoxCollider2D） | — | 地面，layer=Ground |
| `CameraBounds` | （仅 BoxCollider2D trigger） | — | Cinemachine Confiner 用，绑到 vcam.Confiner2D 字段 |

---

## 3. DualWorld 模块

### 3.1 DualWorldWorkspace.prefab

**完整双世界工作区**。继承 `SideScrollWorkspaceBase`。

#### 嵌套了 5 个子 prefab
```
DualWorldWorkspace
├── LevelInGameFlow / AlignmentSubLevel    ← Logic prefab：流程机
├── CrossWorldBridges / DreamToReality...   ← Logic prefab：桥
├── PersistentUI / ChatTaskController       ← Logic prefab：聊天控制器
├── [nested] RealityCanvas.prefab           ← 左屏 UI
├── [nested] DreamWorld.prefab              ← 右屏地形
├── [nested] PersistentUI/ChatTaskPanel.prefab
├── [nested] SideScrollPlayer.prefab        ← 玩家（共享）
└── [nested] CameraRig.prefab               ← 相机（共享）
```

#### 用法
拖一份 `DualWorldWorkspace.prefab` 进场景，再加 `SceneEssentials.prefab`，Play 即可。**这是当前唯一可玩的双世界关卡**。

---

### 3.2 RealityCanvas.prefab

**左屏拖块 UI 全套**。

#### 包含
- Canvas (ScreenSpaceOverlay) + GraphicRaycaster
- AlignmentTask（RealityAlignmentTask 组件 + CanvasGroup）
  - 3 个 Target_i（半透明对齐目标位）
  - 3 个 Block_i（DraggableAlignmentBlock）
  - SubmitButton

#### 内部引用
`RealityAlignmentTask` 的 `blocks`、`targetRects`、`submitButton`、`interactionGroup` 全部指向自己子节点 ✓

#### 复用
单独拖也能用，但 `SubmitAttempted` 事件需要手挂订阅者（默认场景里是 `AlignmentSubLevelFlow`）。

---

### 3.3 DreamWorld.prefab

**右屏梦境地形**。

#### 包含
- Ground（layer=Ground）
- PushableBlock（DreamPushable + Rigidbody2D）
- DreamPushTarget（绿色舒适区，OnTrigger dwell 触发 `Completed`）
- BlockedPath（路径阻挡墙）
- OpenPath（默认 inactive，PathOpener 切到 active）
- DreamPathOpener（管理 BlockedPath/OpenPath 互斥）
- ExitTrigger（WorkspaceEventTriggerZone, eventId="alignment_exit"）

#### 内部引用
`DreamPathOpener.blockedPath` / `openPath` 都指向自己子节点 ✓

---

### 3.4 ChatTaskPanel.prefab

**持久 UI 聊天面板**。

#### 包含
- Canvas (ScreenSpaceOverlay, sortOrder=10) + GraphicRaycaster
- ChatTaskPanel（Image 背景 + CanvasGroup + ChatTaskPanelUI）
  - Accent（左侧彩条）
  - Title（任务名）
  - Body（正文）

#### 用法
和 `ChatTaskController.prefab` 配套使用：Controller 通过 `GetComponentInChildren<ChatTaskPanelUI>` 自动找到 panel。

---

### 3.5 Logic 单例 prefab

下面 5 个都是**裸 GameObject + 单组件**。直接拖会"看起来是空的"，但**挂到 DualWorldWorkspace 子树下**会通过 `GetComponentInParent` 找到 workspace。

| Prefab | 组件 | 必须的位置 |
|---|---|---|
| `AlignmentSubLevelFlow` | AlignmentSubLevelFlow | 任意子节点；引用 RealityCanvas + DreamWorld + ChatTaskDefinition |
| `LevelInGameFlowController` | LevelInGameFlowController | 顶层（subLevels 列表填子关流程机） |
| `DreamToRealityEnhancer` | DreamToRealityEnhancer | DualWorldWorkspace 子树下任意层级 |
| `RealityToDreamRepair` | RealityToDreamRepair | 同上 |
| `ChatTaskController` | ChatTaskController | 同上（建议放在 ChatTaskPanel 同级或父级，方便 panel 自查） |

---

### 3.6 Atoms 单组件件

| Prefab | 用途 |
|---|---|
| `DraggableBlock` | RectTransform + Image + DraggableAlignmentBlock；放在 Canvas 下，target 字段拖一个 AlignmentTarget 进去 |
| `AlignmentTarget` | RectTransform + Image（半透明黄色目标位） |
| `SubmitButton` | RectTransform + Image + Button + Label 子节点 |
| `PushableBlock` | 物理盒 + DreamPushable 标记 |
| `DreamPushTarget` | 触发器盒 + DreamPushTarget（dwell 计时） |

---

## 4. StoryPlayer 模块

### 4.1 StoryPlayerRig.prefab

**全局剧情播放器**。

#### 行为
- 平时不应放进任何关卡场景
- `StoryPlayerService.Play(sequence)` 第一次被调用时，自动从 `Resources/Prefabs/StoryPlayerRig` 加载 + Instantiate + DontDestroyOnLoad
- 后续整个项目共用这一份

#### 兼容
内部还是用旧的 `StoryPlayerTestBootstrap` Awake 自搭整套 UI（Canvas、文字框、淡入遮罩等）。这是过渡方案，未来重构后会改成 prefab 内 baked UI。

#### 注意
**不要往关卡里手动拖**。让 Service 自动管理就好。如果场景里已经手动拖了一份，Service 会优先用场景里的（不会重复实例化）。

---

### 4.2 Triggers 触发器 prefab（生产用，**关卡里直接拖**）

#### StoryTriggerZone.prefab

| 字段 | 含义 |
|---|---|
| sequence | 要播的剧情资产（StorySequence） |
| oneShot | 是否只触发一次 |
| targetLayers | 哪些 layer 进入触发（默认全部） |

**用法**：拖到关卡某个位置 → 调整 BoxCollider2D 大小 → 把剧情 asset 拖到 sequence 字段。

#### StoryInteractable.prefab

按 E 触发的剧情交互物。继承 `SideScrollInteractableBase`，自动接入工作区交互系统。

**用法**：拖到关卡某个位置（确保在 SideScroll 工作区子树下，否则不被识别为 interactable）→ sequence 字段挂剧情。

#### StoryWorkspaceEventTrigger.prefab

监听工作区事件触发的剧情。

| 字段 | 含义 |
|---|---|
| eventId | 监听哪个工作区事件（如 `alignment_exit`、`puzzle.solved`） |
| sequence | 触发后播什么 |

**用法**：拖到工作区子树下 → eventId 填要监听的事件名 → sequence 填剧情。

#### StoryAutoPlay.prefab

场景加载完立即播放（开场剧情）。

| 字段 | 含义 |
|---|---|
| sequence | 要播的剧情 |
| delaySec | 延时秒数 |

**用法**：拖到场景任意位置 → sequence 字段挂剧情。Awake 时立刻 `StoryPlayerService.Play(sequence)`。

---

### 4.3 Atoms

| Prefab | 用途 |
|---|---|
| `Background.prefab` | 全屏 Image，留作剧情背景 |
| `FadeOverlay.prefab` | 全屏黑 + CanvasGroup（默认 alpha=0），转场用 |
| `InputBlocker.prefab` | 透明 Image + Button，演出期间吞掉点击 |

这些都是 UI 子件，**只在自定义剧情画布时用**，常规剧情触发不需要。

---

## 5. ScriptableObject 资产

| 资产路径 | 类型 | 修改影响 |
|---|---|---|
| `Settings/Defaults/DefaultCharacterMoveConfig.asset` | CharacterMoveConfig | 玩家走路速度、加减速度 |
| `Settings/Defaults/DefaultCharacterJumpConfig.asset` | CharacterJumpConfig | 跳跃高度、coyote、buffer |
| `Settings/Defaults/DefaultCameraConfig.asset` | CameraConfig | 相机偏移、damping |
| `Settings/DualWorld/AlignmentChatTask.asset` | ChatTaskDefinition | 对齐子关 5 段聊天文本 |
| 自建 `*.asset` | StorySequence | 各段剧情数据 |

修改这些**不需要重生成 prefab** —— prefab 引用的就是这些 asset 文件，立刻生效。

---

## 6. 实际关卡搭建场景

### 场景类型 A：纯横板探索关（没剧情、没双世界）

```
1. 新场景 → 拖 SceneEssentials.prefab
2. 拖 Shells/SideScrollStoryWorkspace.prefab（或 Gameplay）
3. 在工作区下：
   - 拖 SideScrollPlayer.prefab
   - 拖 CameraRig.prefab（调 CameraBounds 大小）
   - 拖 N 个 Atoms/GroundBlock.prefab 拼地形
   - 拖 Atoms/ObservationPoint.prefab 做剧情点
   - 拖 Atoms/ExitPoint.prefab 做出口
4. Play
```

### 场景类型 B：解谜关（带通关条件）

```
同 A，但工作区用 SideScrollGameplayWorkspace.prefab
另外：
- 拖几个 Atoms/PickupObject.prefab，每个填不同 pickupId
- 拖一个 Atoms/GoalTriggerZone.prefab，goalId 配置完成条件
- 出口可以挂 ConditionTriggerZone（要求 goalId 已完成）
```

### 场景类型 C：开场带剧情过场的关卡

```
1. 新场景 → SceneEssentials + 工作区 + 玩家 + 相机 + 地形
2. 关卡入口处：拖 StoryPlayer/Triggers/StoryAutoPlay.prefab
3. 自建一个 StorySequence asset（填好对话文本）
4. 拖到 StoryAutoPlay 的 sequence 字段
5. Play → 场景加载完 → 剧情自动播 → 播完玩家可控
```

### 场景类型 D：剧情触发于路途中

```
同 C，但用 StoryTriggerZone 替代 AutoPlay：
1. 把 StoryTriggerZone.prefab 拖到玩家会经过的位置
2. 调 BoxCollider2D 尺寸
3. sequence 字段挂剧情
4. 玩家走到这里 → 自动播
```

### 场景类型 E：双世界关

```
1. 新场景 → SceneEssentials.prefab
2. 拖 DualWorldWorkspace.prefab
3. Play
```

完毕。所有内容（玩家、相机、双屏 UI、聊天面板、流程机）都已嵌套在 DualWorldWorkspace 内。

### 场景类型 F：混合关（横板探索 + 中途有双世界子关）

需要场景切换或场景内多工作区切换。当前框架不直接支持这种场景内多工作区，建议做成两个独立场景。详见 `StoryFlowBridge.EnterSideScroller` 的设计意图（未实现）。

---

## 7. 常见操作模板

### 7.1 在某个位置插一段剧情

```
方案 A：拖 StoryAutoPlay.prefab，sequence 字段挂剧情 → 场景加载即播
方案 B：拖 StoryTriggerZone.prefab，调位置和大小 → 走到这里播
方案 C：拖 StoryInteractable.prefab → 按 E 播
方案 D：从代码任意位置 StoryPlayerService.Play(seq) → 立即播
方案 E：拖 StoryWorkspaceEventTrigger.prefab，eventId 填 "puzzle.solved" → 解谜后播
```

### 7.2 让玩家拾取物品

```
1. 拖 Atoms/PickupObject.prefab
2. Inspector 里改 pickupId（如 "key_red"）
3. 玩家走入 → 工作区记录到 collectedPickupIds
4. 后续门禁可用 ConditionTriggerZone 检查 pickupId 是否已收集
```

### 7.3 设置关卡出口

```
1. 拖 Atoms/ExitPoint.prefab
2. （可选）改 prompt 文本
3. 玩家走入 → 工作区 raise "workspace.exit" 事件
4. 监听处理：Workspace.WorkspaceEventRaised += (id) => { if(id=="workspace.exit") ... }
```

### 7.4 配置镜头跟随范围

```
1. 选中 CameraRig.prefab 实例下的 CameraBounds 子节点
2. 改 BoxCollider2D.size 调整可视范围
3. （可选）拖 Atoms/CameraTriggerZone.prefab 在路径上某点 → 切镜头到不同 config
```

### 7.5 剧情结束做某件事

```csharp
StoryPlayerService.Play(sequence, onComplete: () => {
    // 这里写剧情结束后的逻辑
    Debug.Log("剧情播完了");
});
```

---

## 8. 引用关系图（关键的）

```
StoryPlayerService (静态)
  ↓ EnsureRig
StoryPlayerRig.prefab (DontDestroyOnLoad，全局唯一)
  ↑
  Triggers 内的 StoryTriggerZone / Interactable / EventTrigger / AutoPlay
  ↑
  代码中任意 StoryPlayerService.Play(sequence)


SideScrollWorkspaceBase
  ├─ playerController ← 自动找 GetComponentInChildren<SideScrollCharacterControllerBase>
  ├─ cameraController ← 自动找 GetComponentInChildren<SideScrollCameraController>
  └─ ScanSceneObjects 扫描所有 ISideScrollInteractable / TriggerZoneBase / CameraZone
     → 自动 BindWorkspace（注入 workspace 引用）


DualWorldWorkspace
  ├─ flowController = LevelInGameFlow/LevelInGameFlowController
  ├─ chatTaskController = PersistentUI/ChatTaskController
  └─ Bridges + ChatController 自动 GetComponentInParent<DualWorldWorkspace>
     → 不需要 Inspector 拖引用
```

---

## 9. 故障排查

### 问题 1：UI 拖不动 / 按钮点不响应
- 检查场景里有没有 EventSystem（拖 SceneEssentials.prefab）
- 检查 Canvas 上有没有 GraphicRaycaster

### 问题 2：玩家不动 / 跳不起来
- 检查 ProjectSettings → Tags and Layers 有没有 `Ground` 这个 layer
- 检查 GroundBlock 的 layer 设置正确（应该是 Ground）
- 检查 SideScrollPlayer 的 GroundCheck 子节点位置（应该在玩家脚底）

### 问题 3：相机不跟随
- 检查 Main Camera 上有没有 CinemachineBrain（拖 SceneEssentials.prefab 应该带上）
- 检查 CameraRig 是否在工作区子树下
- 检查 CM_VCam.Follow 字段（应该自动指向玩家）

### 问题 4：剧情不播
- 检查 sequence 字段是否真的挂了 StorySequence asset
- 看 Console 有没有 `[StoryPlayerService] Failed to acquire StoryPlayer rig`
- 如果有，检查 `Assets/Resources/Prefabs/StoryPlayerRig.prefab` 是否存在（没有就跑菜单生成）

### 问题 5：双世界 Logic prefab 显示警告 "No DualWorldWorkspace found"
- 桥 / 控制器**必须**挂在 DualWorldWorkspace 根的子树下
- 别放成工作区根的兄弟节点

### 问题 6：触发器走过去没反应
- 检查 BoxCollider2D 是 trigger（isTrigger = true）
- 检查 targetLayers 是否包含玩家的 layer（默认是 Player）
- 检查触发器是否在工作区子树下（这样才会被 ScanSceneObjects 注册）

---

## 10. 自定义你自己的 prefab

如果自动生成的不够用：

1. **从已有 prefab 派生 Variant**：右键 prefab → Create → Prefab Variant，然后改细节
2. **新增触发器类型**：实现自己的 `: TriggerZoneBase` 或 `: SideScrollInteractableBase` 子类，套这套 BindWorkspace 机制就行
3. **新增子关流程机**：实现 `: BaseSubLevelFlow` 子类，按 8 阶段枚举走

不要直接修改 `Assets/Editor/*` 生成的 prefab —— 重跑菜单会覆盖你的改动。

---

最后更新：2026-05-08
