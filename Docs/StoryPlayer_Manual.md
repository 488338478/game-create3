# StoryPlayer 剧情机文档

最后更新：2026-04-26  
适用版本：Unity 2022.3.62f3c1

## 1. 模块定位
`StoryPlayer` 是面向绘本式、CG 式、轻演出式剧情段落的运行器。

目标用途：
- 播放一组连续剧情页。
- 支持背景图、前景图、说话人、正文、逐字显示、淡入显示。
- 支持页内定时事件，例如播放音效、播放音乐、停止音乐、触发特效、写入剧情变量、发起分支。
- 支持剧情结束后的流程回调，例如进入关卡、进入横版工作区、回到主菜单、进入对话、触发自定义事件。

与旧 `DialogueController` 的区别：
- `DialogueController` 偏“节点对话”：角色对话、选项、条件跳转。
- `StoryPlayer` 偏“剧情页播放”：绘本页、CG 页、片头片尾、章节转场、不可交互或轻交互演出。

## 2. 当前目录
核心代码目录：
- `Assets/Scripts/StoryPlayer`

测试场景：
- `Assets/Scenes/StoryPlayerTestScene.unity`

测试场景依赖资源：
- `Assets/chinese`
- `Assets/TextMesh Pro/Shaders`

## 3. 核心脚本职责

| 脚本 | 职责 |
|---|---|
| `StoryData` | 定义剧情数据结构、枚举、页、文本块、音频配置、页事件。 |
| `StoryPlayer` | 剧情播放主控，负责页推进、状态切换、等待输入、跳过、完成回调。 |
| `StoryPageRenderer` | 渲染单页内容，负责背景、前景、文本、页内元素、逐字显示、基础动画。 |
| `StoryInputController` | 处理点击、长按跳过、键盘推进和跳过。 |
| `StoryEventSystem` | 按页内时间轴触发事件。 |
| `StoryAudioAdapter` | 播放 BGM、SFX、VoiceOver，并处理淡入淡出和交叉淡入淡出。 |
| `SimpleTransitionController` | 基于 UI CanvasGroup / RectTransform 的轻量转场控制器。 |
| `TransitionController` | 基于材质 / Shader 的转场控制器预留实现。 |
| `StoryFlowBridge` | 剧情结束后连接关卡、横版工作区、主菜单、对话、事件。 |
| `StoryPlayerTestBootstrap` | 测试场景运行时搭建器；没有手动配置剧情资源时会生成测试数据。 |
| `StoryTestDataGenerator` | 生成用于测试的 `StorySequence` 数据。 |

接口：
- `IStoryPageRenderer`
- `ITransitionController`

状态：
- `StoryPlayerState`

## 4. 数据结构

### 4.1 StorySequence
`StorySequence` 是剧情机的主数据资产。

创建路径：
- Unity 菜单：`Create > Game > StoryPlayer > Story Sequence`

关键字段：
- `sequenceId`：剧情段 ID。
- `pages`：剧情页列表。
- `defaultPlaybackMode`：默认播放模式。
- `allowSkip`：是否允许跳过。
- `autoAdvanceDelay`：自动播放时的默认等待时间。
- `endCallbackType`：剧情结束后的流程回调类型。
- `endCallbackParameter`：回调参数。

### 4.2 StoryPage
`StoryPage` 是单个剧情页。

关键字段：
- `pageId`：页 ID。
- `pageType`：页类型。
- `backgroundImage`：背景图。
- `foregroundImage`：前景图或角色图。
- `textBlocks`：本页文本块。
- `displayDuration`：本页显示时长；小于等于 0 时使用 `StorySequence.autoAdvanceDelay`。
- `waitForInput`：本页是否等待玩家输入。
- `elements`：页内附加元素。
- `transitionIn` / `transitionOut`：入场 / 出场转场。
- `transitionDuration`：转场时长。
- `pageEvents`：页内事件。
- `audioConfig`：页级音频配置。

页类型：
- `Static`
- `Text`
- `CG`
- `Mixed`

### 4.3 StoryTextBlock
`StoryTextBlock` 是单段文本。

关键字段：
- `textId`：文本 ID。
- `speaker`：说话人。
- `content`：正文。
- `displayMode`：显示模式。
- `typewriterSpeed`：逐字显示速度。
- `delayBeforeShow`：显示前延迟。
- `duration`：文本块显示时长。

显示模式：
- `Instant`：立即显示。
- `Typewriter`：逐字显示。
- `FadeIn`：淡入显示。

### 4.4 StoryElement
`StoryElement` 是页内附加元素。

元素类型：
- `Background`
- `Character`
- `DialogueText`
- `NarrationText`
- `Effect`
- `Audio`

动画类型：
- `None`
- `FadeIn`
- `SlideIn`
- `ScaleIn`
- `Typewriter`
- `Shake`

当前 `StoryPageRenderer` 已实现：
- `FadeIn`
- `SlideIn`
- `ScaleIn`

`Typewriter` 和 `Shake` 当前只在枚举中预留，尚未在 `StoryPageRenderer` 的元素动画分支中实现。

## 5. 播放流程

### 5.1 初始化
推荐运行时结构：
- 一个 `StoryPlayerSystem` GameObject。
- 挂载 `StoryPlayer`。
- 挂载 `StoryPageRenderer`。
- 挂载 `SimpleTransitionController`。
- 挂载 `StoryInputController`。
- 挂载 `StoryEventSystem`。
- 挂载 `StoryAudioAdapter`。
- 挂载 `StoryFlowBridge`。

初始化调用：
```csharp
storyPlayer.Initialize(pageRenderer, transitionController);
inputController.Initialize(storyPlayer, pageRenderer);
flowBridge.BindStoryPlayer(storyPlayer);
```

输入桥接：
```csharp
inputController.OnNextPageRequested += storyPlayer.NextPage;
inputController.OnSkipSequenceRequested += storyPlayer.SkipSequence;
inputController.OnTextFastForwardRequested += pageRenderer.SkipCurrentAnimation;
```

### 5.2 播放一段剧情
```csharp
storyPlayer.Play(storySequence);
```

播放时主流程：
1. `StoryPlayer.Play()` 设置当前 `StorySequence`。
2. `AdvanceToNextPageAsync()` 推进到下一页。
3. 如果存在上一页，执行上一页 `transitionOut`。
4. 执行下一页 `transitionIn`。
5. `StoryAudioAdapter.ApplyPageAudioConfig()` 应用页级音频。
6. `StoryEventSystem.StartEventTracking()` 启动页内事件。
7. `StoryPageRenderer.RenderPageAsync()` 渲染当前页。
8. 根据播放模式和 `waitForInput` 决定等待输入或自动前进。
9. 播放完最后一页后调用 `CompleteSequenceAsync()`。
10. `StoryFlowBridge` 根据 `endCallbackType` 执行后续流程。

### 5.3 状态流转
状态枚举：
- `Idle`
- `PlayingPage`
- `WaitingInput`
- `Transitioning`
- `Completed`
- `Skipped`

常见状态链路：
```text
Idle
-> PlayingPage
-> Transitioning
-> PlayingPage
-> WaitingInput
-> Transitioning
-> Completed
```

说明：
- `StoryPlayer.SkipSequence()` 会触发 `OnSequenceSkipped`，并把播放速度切到 `fastForwardSpeed`。
- 当前跳过逻辑偏“快速推进”，不是立即硬切到结尾。
- `StoryInputController` 在 `Transitioning` 状态会关闭输入，避免转场中重复推进。

## 6. 输入约定
默认输入：
- 点击：推进文本 / 下一页。
- 长按约 1.5 秒：跳过剧情。
- `Space`：推进。
- `Esc`：跳过。

文本块推进规则：
- 若当前文本正在逐字显示，输入会先调用 `SkipCurrentAnimation()`，直接显示完整文本。
- 若本页还有后续 `StoryTextBlock`，输入会推进到下一个文本块。
- 若本页文本块已播放完，输入会通知 `StoryPlayer` 进入下一页。

## 7. 页事件

`StoryPageEvent` 字段：
- `eventType`
- `triggerTime`
- `eventData`

事件类型：
- `PlaySound`
- `PlayMusic`
- `StopMusic`
- `TriggerEffect`
- `SetVariable`
- `Branch`

### 7.1 PlaySound
格式：
```text
音效名|音量
```

示例：
```text
door_open|0.8
```

资源路径：
```text
Resources/Audio/SFX/door_open
```

### 7.2 PlayMusic
格式：
```text
音乐名|音量|是否循环
```

示例：
```text
chapter_intro|0.7|true
```

资源路径：
```text
Resources/Audio/BGM/chapter_intro
```

### 7.3 StopMusic
`eventData` 可留空。

### 7.4 TriggerEffect
格式：
```text
特效名
```

资源路径：
```text
Resources/Effects/特效名
```

### 7.5 SetVariable
格式：
```text
变量名=变量值
```

示例：
```text
StoryTestPassed=true
```

写入规则：
- `true` / `false` 写入 `NarrativeVariableStore.SetBool()`。
- 整数写入 `NarrativeVariableStore.SetInt()`。
- 其他值写入 `NarrativeVariableStore.SetString()`。

### 7.6 Branch
格式：
```text
分支或对话 ID
```

当前行为：
- 触发 `StoryEventSystem.OnDialogueRequested`。
- 具体接入方需要监听事件并执行跳转。

## 8. 音频方案
页级音频由 `StoryAudioConfig` 管理：
- `bgm`
- `loopBgm`
- `bgmVolume`
- `voiceOver`
- `soundEffects`

事件音频由 `StoryEventSystem` 和 `StoryAudioAdapter` 管理：
- `PlaySound` 会从 `Resources/Audio/SFX` 加载。
- `PlayMusic` 会从 `Resources/Audio/BGM` 加载。
- `StoryAudioAdapter` 支持 BGM 淡入、淡出、交叉淡入淡出。

全局音量键：
- `BGM_Volume`
- `SFX_Volume`
- `Voice_Volume`

这些值当前从 `PlayerPrefs` 读取。

## 9. 结束回调
`StorySequence.endCallbackType` 决定剧情结束后的流程。

可选值：
- `None`
- `EnterLevel`
- `EnterSideScroller`
- `EnterMainMenu`
- `EnterDialogue`
- `TriggerEvent`

参数：
- `endCallbackParameter`

回调执行者：
- `StoryFlowBridge`

说明：
- 如果场景中存在 `FlowController`，优先调用 `FlowController`。
- 如果没有 `FlowController`，部分回调会退回到 `SceneManager.LoadScene()` 或 `Debug.Log()`。
- `EnterLevel` 的空参数会使用 `defaultLevelSceneName`。
- `EnterMainMenu` 的场景名来自 `mainMenuSceneName`。

## 10. 测试入口
测试场景：
- `Assets/Scenes/StoryPlayerTestScene.unity`

运行方式：
1. 打开 `StoryPlayerTestScene.unity`。
2. 直接 Play。
3. 如果 `StoryPlayerTestBootstrap.testSequence` 为空，运行时会自动生成测试剧情。

测试内容：
- 第 1 页：自动推进。
- 第 2 页：手动推进、逐字显示、点击快进、长按跳过。
- 第 3 页：自动触发事件。
- 第 4 页：完成或跳过流程。

测试 UI 会显示：
- 操作说明。
- 当前播放状态。
- 页变化日志。
- 事件日志。

## 11. 接入步骤

### 11.1 配置 UI
最小 UI 需求：
- `Canvas`
- 背景 `Image`
- 说话人 `TMP_Text`
- 正文 `TMP_Text`
- 转场用 `CanvasGroup`

可选 UI：
- CG `Image`
- 前景 / 角色 `Image`
- 文本容器 `RectTransform`
- 跳过提示 `CanvasGroup`
- 跳过进度条 `Image`

### 11.2 创建剧情资源
1. 在 Project 面板创建 `StorySequence`。
2. 设置 `sequenceId`。
3. 添加 `pages`。
4. 为每页配置图片、文本、转场、事件、音频。
5. 设置 `endCallbackType` 和 `endCallbackParameter`。

### 11.3 运行剧情
```csharp
public class StoryLauncher : MonoBehaviour
{
    [SerializeField] private StoryPlayer storyPlayer;
    [SerializeField] private StorySequence openingSequence;

    public void PlayOpening()
    {
        storyPlayer.Play(openingSequence);
    }
}
```

## 12. 当前边界
- `StoryElementType.Effect` 当前没有在 `StoryPageRenderer.RenderElementAsync()` 中实例化特效。
- `StoryAnimationType.Typewriter` / `Shake` 当前未在元素动画中实现。
- `StoryEventSystem.Branch` 当前只抛出事件，不直接切换剧情页。
- `StoryPlayer.Pause()` 当前会取消播放任务并切到 `Idle`，恢复时以当前页索引继续推进；复杂暂停场景需要单独验证。
- `TransitionController` 依赖转场 Shader 名称，当前稳定测试入口使用 `SimpleTransitionController`。
- 测试场景里的 UI 由 `StoryPlayerTestBootstrap` 运行时搭建，正式场景建议手动配置 UI 引用。

## 13. 开发约束
- 新剧情页数据优先走 `StorySequence`，不要把剧情内容硬编码到运行器里。
- UI 展示逻辑放在 `StoryPageRenderer` 或专用 Renderer，`StoryPlayer` 只负责播放状态和流程。
- 事件格式需要保持可读、可追踪，复杂事件优先拆成明确的 `StoryEventType` 或桥接监听器。
- 正式流程跳转统一走 `StoryFlowBridge`，不要在剧情页渲染层直接切场景。
- 新增事件类型时同步更新：
  - `StoryEventType`
  - `StoryEventSystem.TriggerEvent()`
  - 本文档第 7 节
- 新增动画类型时同步更新：
  - `StoryAnimationType`
  - `StoryPageRenderer.PlayElementAnimationAsync()`
  - 本文档第 4.4 节

## 14. 验收清单
- 打开 `StoryPlayerTestScene.unity` 后可直接 Play。
- 测试剧情可自动生成并播放。
- 点击可快进逐字文本。
- 点击可推进到下一页。
- 长按可触发跳过。
- `Esc` 可触发跳过。
- 页转场能正常显示。
- BGM / SFX / VoiceOver 不报空引用。
- `SetVariable` 能写入 `NarrativeVariableStore`。
- `endCallbackType` 能被 `StoryFlowBridge` 接收并执行。
- 控制台无 Missing Script、Missing Font、Missing Shader 报错。
