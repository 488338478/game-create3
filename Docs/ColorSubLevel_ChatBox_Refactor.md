# ColorSubLevelFlow ChatBox 重构方案 v2

最后更新：2026-06-17

---

## 0. 核心洞察：两层抽象

AlignmentSubLevelFlow 和 ColorSubLevelFlow 的 **ChatBox 管道是 100% 同构的**。从 `SubmitRequested` 订阅、延迟评估、player 语料追加、到 `OnPhaseEntered` 里开关 submit 按钮——两份代码一字不差。差异只在各自任务对象的具体操作。

**策略：ChatBox 管道提到 BaseSubLevelFlow，两个子类只保留各自任务类型特有逻辑。**

```
BaseSubLevelFlow (现有)
└── ChatBox 管道 (新增) ← 同构代码只写一次
      chatBox, HandleSubmitRequested, SubmitRealityTask
      SetSubmitInteractable, SubscribeToChatBox

AlignmentSubLevelFlow             ColorSubLevelFlow
  只保留 alignment 特有操作         只保留 color 特有操作
  + DreamPushTarget               + DreamColorCollectController
  + assist/snap 逻辑               + 调色板/流星收集
```

---

## 1. 两条链路的对比

### 1.1 正式链路 —— 对齐子关（AlignmentSubLevelFlow）

```
┌──────────────────────────────────────────────────────┐
│                  ChatBox (scene root)                 │
│  ChatBoxUI + ChatTaskController                      │
│  SubmitRequested → Flow.HandleSubmitRequested()      │
└──────────────────────────────────────────────────────┘
         │                    ▲
         │ SubmitRequested     │ SetSubmitInteractable / Publish / Raise
         ▼                    │
┌──────────────────────────────────────────────────────┐
│  AlignmentSubLevelFlow : BaseSubLevelFlow             │
│                                                      │
│  OnInitialized: chatBox.SubmitRequested += Handler   │
│  HandleSubmitRequested:                              │
│    1. ChatTaskController.AppendPlayerSubmit()        │
│    2. 延迟 → realityTask.Submit()                    │
│  OnRealitySubmit: fail → Raise(Failed) / success →   │
│    Raise(Completed)                                  │
│  OnPhaseEntered: Active→Publish+SetSubmit(true)      │
│                  Completed→SetSubmit(false)           │
└──────────────────────────────────────────────────────┘
```

### 1.2 当前断裂链路 —— 颜色子关（ColorSubLevelFlow）

```
┌──────────────────────────────────────────────────────┐
│            ColorPuzzleController                      │
│  ❌ 自己持有 playerSubmitLines[]                      │
│  ❌ 自己的 submitButton 引用                          │
│  ❌ FindObjectOfType<ChatTaskController>              │
│  ❌ ShowBossLine() → BossFeedbackPanel               │
│  ❌ SceneRouter.GoScene("Level2Cutscene")            │
│  ❌ using GameCreate3.DualWorld                       │
│  ❌ using GameCreate3.Core.SceneRouting               │
└──────────────────────────────────────────────────────┘
         │ OnSubmitAttempted
         ▼
┌──────────────────────────────────────────────────────┐
│  ColorSubLevelFlow : BaseSubLevelFlow                 │
│  ❌ 不订阅 ChatBoxUI.SubmitRequested                  │
│  ❌ 不调 Raise(Failed/Completed)                     │
│  ❌ taskDefinition = null (prefab 没挂)               │
└──────────────────────────────────────────────────────┘
```

---

## 2. 目标架构

```
┌──────────────────────────────────────────────────────┐
│                  ChatBox (scene root)                 │
│  ChatBoxUI + ChatTaskController                      │
└──────────────────────────────────────────────────────┘
         │                    ▲
         │ SubmitRequested     │ Publish / Raise / SetSubmitInteractable
         ▼                    │
┌──────────────────────────────────────────────────────┐
│            BaseSubLevelFlow (ChatBox 管道)             │
│                                                      │
│  protected void SubscribeToChatBox()                 │
│  protected void UnsubscribeFromChatBox()             │
│  private   void HandleSubmitRequested()              │
│  private   void SubmitRealityTask()                  │
│  protected void PublishChatTask(def)                 │
│  protected void RaiseChatEvent(Failed/Completed)     │
│  protected void SetSubmitInteractable(bool)          │
│  protected void AppendPlayerSubmit()                 │
│                                                      │
│  子类只需在 OnPhaseEntered 里调这些 protected 方法    │
└──────────────────────────────────────────────────────┘
         ▲                              ▲
         │                              │
┌────────┴──────────────┐  ┌────────────┴──────────────────┐
│ AlignmentSubLevelFlow  │  │ ColorSubLevelFlow              │
│                        │  │                                │
│ 特有:                  │  │ 特有:                          │
│  RealityAlignmentTask  │  │  ColorPuzzleController         │
│  DreamPushTarget       │  │  DreamColorCollectController   │
│  assist/snap           │  │  调色板 / 流星收集             │
│  DreamPathOpener       │  │  颜色匹配验证                  │
│  blocked 路径替换       │  │  dreamStartsUnlocked           │
└───────────────────────┘  └───────────────────────────────┘
```

---

## 3. 分步实施方案

### Step 1: BaseSubLevelFlow 提取 ChatBox 管道

**文件**: `Assets/Scripts/DualWorld/Flow/BaseSubLevelFlow.cs`

在现有抽象基类基础上新增 ChatBox 管道字段和方法：

```csharp
// === 新增字段 ===
[Header("Chat")]
[SerializeField] private ChatTaskDefinition taskDefinition;

[Header("Submit Tuning")]
[SerializeField] private float submitToNpcReplyDelaySec = 0.35f;

[Header("Scene Transition")]
[SerializeField] private float successTransitionDelaySec = 1.1f;
[SerializeField] private string successSceneName = "";

private ChatBoxUI chatBox;
private Coroutine pendingSubmitRoutine;

// === 新增 protected 方法 ===

/// <summary>子类在 OnInitialized 末尾调用</summary>
protected void SubscribeToChatBox()
{
    chatBox = Workspace?.ChatTaskController?.ChatBox
              ?? FindObjectOfType<ChatBoxUI>(true);
    if (chatBox != null) chatBox.SubmitRequested += HandleSubmitRequested;
}

/// <summary>子类在 OnDestroy 中调用</summary>
protected void UnsubscribeFromChatBox()
{
    if (chatBox != null) chatBox.SubmitRequested -= HandleSubmitRequested;
}

protected void PublishChatTask()
{
    if (taskDefinition != null)
        Workspace?.ChatTaskController?.Publish(taskDefinition);
}

protected void RaiseChatEvent(ChatTaskController.Event evt)
{
    Workspace?.ChatTaskController?.Raise(evt);
}

protected void AppendPlayerSubmit()
{
    Workspace?.ChatTaskController?.AppendPlayerSubmit();
}

protected void SetSubmitInteractable(bool v)
{
    if (chatBox != null) chatBox.SetSubmitInteractable(v);
}

protected void GoSuccessScene()
{
    if (!string.IsNullOrWhiteSpace(successSceneName))
        StartCoroutine(DelayThenGoScene());
}

// === 私有实现 ===

private void HandleSubmitRequested()
{
    if (!CanSubmit()) return;
    AppendPlayerSubmit();
    if (submitToNpcReplyDelaySec > 0f && isActiveAndEnabled)
    {
        if (pendingSubmitRoutine != null) StopCoroutine(pendingSubmitRoutine);
        pendingSubmitRoutine = StartCoroutine(
            DelayThen(submitToNpcReplyDelaySec, SubmitRealityTask));
    }
    else SubmitRealityTask();
}

private void SubmitRealityTask()
{
    pendingSubmitRoutine = null;
    if (!CanSubmit()) return;
    DoSubmitRealityTask();  // 由子类实现
}

private IEnumerator DelayThenGoScene()
{
    if (successTransitionDelaySec > 0f)
        yield return new WaitForSeconds(successTransitionDelaySec);
    SceneRouter.GoScene(successSceneName);
}

protected IEnumerator DelayThen(float s, Action action)
{
    yield return new WaitForSeconds(s);
    action?.Invoke();
}

// === 子类必须实现的抽象 ===

/// <summary>当前是否可以提交（realityTask != null && interactable）</summary>
protected abstract bool CanSubmit();

/// <summary>执行实际的 reality 任务提交</summary>
protected abstract void DoSubmitRealityTask();
```

### Step 2: AlignmentSubLevelFlow 瘦身

**文件**: `Assets/Scripts/DualWorld/Flow/AlignmentSubLevelFlow.cs`

删除的部分（全部提到基类）：
- `chatBox` 字段
- `chatBox.SubmitRequested +=/-=` 订阅
- `HandleSubmitRequested()` 整个方法
- `SubmitRealityTask()` 整个方法
- `chatBox.SetSubmitInteractable()` 调用 → 改为 `SetSubmitInteractable()`
- `Workspace?.ChatTaskController?.Publish(...)` → 改为 `PublishChatTask()`
- `Workspace?.ChatTaskController?.Raise(...)` → 改为 `RaiseChatEvent()`
- `Workspace?.ChatTaskController?.AppendPlayerSubmit()` → 改为 `AppendPlayerSubmit()`

新增：
```csharp
protected override bool CanSubmit()
    => realityTask != null && realityTask.IsInteractable;

protected override void DoSubmitRealityTask()
    => realityTask?.Submit();
```

原有 `OnInitialized` 末尾加 `SubscribeToChatBox();`，`OnDestroy` 中加 `UnsubscribeFromChatBox();`。

### Step 3: ColorSubLevelFlow 瘦身 + 补全

**文件**: `Assets/Scripts/DualWorld/Flow/ColorSubLevelFlow.cs`

#### 3.1 删除（ColorPuzzleController 特有的冗余）

`HandleRealityTaskSubmit` 中不再需要手动构造 `RealitySubmitResult`——ColorPuzzleController 的 `TrySubmit()` 已经产出 `ColorSubmitResult`，直接在 `OnSubmitAttempted` 回调里转。

#### 3.2 补全 ChatBox 管道

```csharp
protected override void OnInitialized()
{
    ResolveRuntimeReferences();
    ResolveHintRouter();

    // 任务事件
    if (realityTask != null) realityTask.OnSubmitAttempted += HandleRealityTaskSubmit;
    if (dreamCollector != null) dreamCollector.Completed += HandleDreamCollectorCompleted;
    if (dreamCollector != null) dreamCollector.ItemCollected += HandleDreamCollectorItemCollected;
    if (Workspace != null) Workspace.WorkspaceEventRaised += HandleWorkspaceEvent;

    // ChatBox 管道（从基类继承）
    SubscribeToChatBox();
}
```

```csharp
protected override bool CanSubmit()
    => realityTask != null && realityTask.IsInteractable;

protected override void DoSubmitRealityTask()
    => realityTask?.Submit();
```

#### 3.3 补全 OnPhaseEntered 中的 ChatBox 调用

```csharp
case SubLevelPhase.RealityTaskActive:
    // ... 现有重置逻辑 ...
    PublishChatTask();          // ← 代替原来的 if (taskDefinition) ...
    SetSubmitInteractable(true); // ← 新增
    break;

case SubLevelPhase.RealityTaskBlocked:
    SetSubmitInteractable(false); // ← 新增
    // ... 现有逻辑 ...
    break;

case SubLevelPhase.RealityTaskCompleted:
    SetSubmitInteractable(false); // ← 新增
    // ... 现有逻辑 ...
    break;

case SubLevelPhase.SubLevelCompleted:
    Workspace?.EventBus.Raise(...);
    GoSuccessScene();            // ← 新增，从 ColorPuzzleController 移过来
    break;
```

#### 3.4 恢复 Raise 调用（commit 803e52f 删除的）

```csharp
private void HandleRealityTaskSubmit(ColorSubmitResult result)
{
    if (!result.success)
    {
        realityFailCount++;
        RaiseChatEvent(ChatTaskController.Event.Failed);  // ← 恢复
    }
    else
    {
        RaiseChatEvent(ChatTaskController.Event.Completed); // ← 恢复
    }

    var submitResult = new RealitySubmitResult(/* ... */);
    OnRealitySubmit(submitResult);
}
```

### Step 4: 清理 ColorPuzzleController

**文件**: `Assets/Scripts/DualWorld/Color/ColorPuzzleController.cs`

#### 删除的字段

```
playerSubmitLines[]        → ChatTaskDefinition 管理
submitButton               → submit 统一走 ChatBox
feedbackPanel (BossFeedbackPanel) → NPC 反馈走 ChatBox log
submitToBossReplyDelaySec  → BaseSubLevelFlow
successTransitionDelaySec  → BaseSubLevelFlow
successSceneName           → BaseSubLevelFlow
initialBossLine            → ChatTaskDefinition.initialMessage
successSummaryFormat       → ChatTaskDefinition
rejectLines[]              → ChatTaskDefinition.failureMessages
approveLines[]             → ChatTaskDefinition.successMessages
playerSubmitIndex          → ChatTaskController 管理
chatTaskController         → 不应跨层引用
pendingSubmitRoutine       → BaseSubLevelFlow
pendingSuccessTransitionRoutine → BaseSubLevelFlow
successTransitionQueued    → BaseSubLevelFlow
```

#### 删除的方法

```
ShowBossLine()
EnsureChatTaskController()
AppendPlayerSubmitLine()
ResolvePlayerSubmitLine()
DelayThenSubmit()
DelayThenGoSuccessScene()
SubmitAfterPlayerLine()
QueueSuccessTransition()
```

#### 删除的 using

```
using GameCreate3.DualWorld;
using GameCreate3.Core.SceneRouting;
```

#### 暴露 Submit() 公开方法

```csharp
/// <summary>供 ChatBox 管道调用</summary>
public void Submit()
{
    if (!interactable) return;
    var result = TrySubmit();
    OnSubmitAttempted?.Invoke(result);
}
```

#### TrySubmit() 简化

只做颜色匹配评估 + 返回结果。不再调 ShowBossLine、QueueSuccessTransition。

### Step 5: 创建 ChatTaskDefinition 资产

**新建文件**: `Assets/Settings/DualWorld/ColorChatTask.asset`

数据从 ColorPuzzleController 硬编码值迁移：

```
taskId:         "color.right"
title:          "配色任务"
initialMessage: "排版可以了，写内容吧。"
failureMessages:
  - "写的什么玩意，审美呢？"
  - "211就这水平？"
  - "你是老太太吗，年轻点！"
successMessages:
  - "还得是高材生，可以可以。"
playerSubmitLines:
  - "老板，我改了一版，您看看。"
  - "这次我把颜色重新顺过了，您再看一眼？"
  - "我又调了一遍，这版会不会好点？"
  - "按您的意思往年轻一点改了，您看看行不行。"
  - "我再提一次，您帮我过下。"
```

### Step 6: 修复 Prefab 引用

**DualWorldWorkspace_ColorClean.prefab**:
| 改动 | 影响 |
|------|------|
| ColorSubLevel 的 `taskDefinition` → ColorChatTask.asset | 新增引用 |
| `successSceneName` = "Level2Cutscene" (默认值，不动) | 已在 .cs 设默认 |
| ChatTaskPanel `m_IsActive` → 1 | 恢复激活 |

**AlignmentSubLevel 相关 prefab**:
| 改动 | 影响 |
|------|------|
| `taskDefinition` 字段移到基类 → 序列化路径变了需重挂 | ⚠️ 需要重新指向 |
| 其他 ChatBox 字段移到基类（submitToNpcReplyDelaySec 等） | prefab 里自动继承默认值 |

### Step 7: Level2·2 场景加 ChatBox

拖 `Assets/Prefabs/DualWorld/ChatBox.prefab` 到场景根，如果 ChatBox 没有 Canvas 则补 Canvas + CanvasScaler + GraphicRaycaster。

---

## 4. 改动文件汇总

| 文件 | 改动 | 量 |
|------|------|---|
| `DualWorld/Flow/BaseSubLevelFlow.cs` | **ChatBox 管道提取** | +60 行 |
| `DualWorld/Flow/AlignmentSubLevelFlow.cs` | 瘦身，ChatBox 委托给基类 | -50 行 |
| `DualWorld/Flow/ColorSubLevelFlow.cs` | 补全 ChatBox 管道，瘦身 | -40 行 |
| `DualWorld/Color/ColorPuzzleController.cs` | 删 ChatBox / SceneRouter 逻辑 | -100 行 |
| `Settings/DualWorld/ColorChatTask.asset` | **新建** | +30 行 |
| `DualWorld/2/DualWorldWorkspace_ColorClean.prefab` | 挂引用 | 序列化 |
| `Scenes/Level/Level2·2.unity` | 加 ChatBox 实例 | 编辑 |

### 序列化影响分析

| 产物 | 序列化影响 |
|------|-----------|
| BaseSubLevelFlow.cs | 新增字段有默认值，不影响现有 prefab（除了 taskDefinition 引用路径变了需要重挂） |
| AlignmentSubLevelFlow.cs | 删除的字段 prefab 静默忽略，新增字段继承基类默认值 |
| ColorSubLevelFlow.cs | 同上 |
| ColorPuzzleController.cs | 删除字段 prefab 静默忽略 — **UI 位置/布局零改动** |
| ColorClean.prefab | 只挂一个 taskDefinition 引用 + ChatTaskPanel 激活 |
| Level2·2.unity | 加一个 ChatBox 实例，不动已有 GameObject |

**所有视觉元素（UI 位置、精灵、布局、碰撞体）保持不变。**

---

## 5. 不变的文件

- `ChatBoxUI.cs` / `ChatTaskController.cs` / `ChatTaskPanelUI.cs` — 已是通用组件
- `ChatTaskDefinition.cs` / `ChatLogEntry.cs` / `ChatLogEntryView.cs` — 数据结构完整
- `DreamColorCollectController.cs` / `DreamColorPickup.cs` — 与 ChatBox 无关
- `BossFeedbackPanel.cs` — 保留文件，不再被引用
- `DualWorldWorkspace.cs` — 不受影响

---

## 6. 验证

1. `dotnet build Game-create3.sln` 通过
2. DW_Test_Workspace → Play → 对齐子关 ChatBox 行为不变
3. Level2·2 → Play → ChatBox 可见，Submit 可用
4. 提交失败 → NPC reject 消息出现在 ChatBox log
5. 提交成功 → NPC approve 消息 → 延迟 1.1s → 跳转 Level2Cutscene
