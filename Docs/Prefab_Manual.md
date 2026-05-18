# Prefab 使用手册

> 配套 `GameCreate3` Editor 工具一键生成的全部 prefab。
> 关卡搭建只需要看这一份。

---

## 0. 一图速览

```
Assets/Prefabs/
├── SceneEssentials.prefab                          ← 每个新场景先拖
├── Core/
│   ├── AudioService.prefab                         ← 全局音频服务
│   ├── SaveProgressService.prefab                  ← 存档/配置服务
│   └── SceneRouterHooks.prefab                     ← SceneRouter 的可选 Hook 容器
│   (SceneRouter 本身是 static class 无 prefab — 见 §2.3)
├── UI/
│   ├── System/
│   │   ├── UIControlSystem.prefab                  ← UI 页面/弹窗层级壳
│   │   └── UISettingsService.prefab                ← UI 设置服务
│   ├── Pages/
│   │   ├── MainMenuPage.prefab                     ← 主入口菜单
│   │   ├── SettingsPage.prefab                     ← 设置页
│   │   ├── PausePage.prefab                        ← 暂停菜单
│   │   ├── InGameHUDPage.prefab                    ← 游戏内 HUD
│   │   ├── ResultPage.prefab                       ← 结算页
│   │   ├── GalleryPage.prefab                      ← CG 图鉴页
│   │   ├── StoryOverlayPage.prefab                 ← 剧情覆盖层
│   │   └── LoadingPage.prefab                      ← 全局加载遮罩
│   ├── Popups/
│   │   └── ConfirmDialogPage.prefab                ← 通用确认弹窗
│   └── Atoms/
│       ├── CGGallerySlot.prefab                    ← CG 图鉴格子
│       └── VolumeSlider.prefab                     ← 音量滑条
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

## 2. Core 模块

Core 下面放的是从 `Assets/Scripts/Core` 梳理出的**全局 Logic 单例 prefab**。它们都是最小原子服务：一个 prefab 只承担一种全局职责，场景里最多一份。

### 2.1 AudioService.prefab

**全局音频服务**。裸 GameObject 上挂 `GameAudioService`。

#### 能力
- 播放 BGM：`PlayBGM(bgmId)`
- 播放环境音：`PlayAmbient(ambientId)`
- 播放音效：`PlaySFX(sfxId, channel)`
- 设置音量：`SetVolume(GameAudioChannel, value01)`
- 淡入淡出：`FadeIn` / `FadeOut`

#### 资源约定
| 类型 | Resources 路径 |
|---|---|
| BGM | `Resources/Audio/BGM/{bgmId}` |
| Ambient | `Resources/Audio/Ambient/{ambientId}` |
| SFX / UI SFX | `Resources/Audio/SFX/{sfxId}` |

#### 内部引用
`bgmSource`、`ambientSource`、`sfxSource`、`uiSource`、`voiceSource` 可以 prefab 内预配；如果为空，运行时会自动创建 `Core_BGM`、`Core_Ambient`、`Core_SFX`、`Core_UI`、`Core_Voice` 子节点并挂 `AudioSource`。

#### 注意
- 场景里**最多一份**。默认 `dontDestroyOnLoad = true`
- 和 `UISettingsService` 使用同一套 PlayerPrefs 音量 key，会自动同步设置页音量

---

### 2.2 SaveProgressService.prefab

**存档 / 配置服务**。裸 GameObject 上挂 `GameSaveProgressService`。

#### 能力
- `SetConfig` / `GetConfig`：保存全局配置
- `SetProgress` / `GetProgress`：保存进度标记
- `Save()` / `Load()`：写入和读取 JSON 存档
- `DeleteSaveFile()`：删除当前存档

#### 可调字段
| 字段 | 默认值 | 用途 |
|---|---|---|
| `saveFileName` | `game_persistence.json` | 存档文件名 |
| `loadOnAwake` | true | Awake 时自动读档 |
| `dontDestroyOnLoad` | true | 跨场景保留 |

#### 注意
场景里**最多一份**。如果只是某个关卡内的临时状态，不要写进这里，优先放工作区自己的运行时字段。

---

### 2.3 SceneRouter（无 prefab，static 门面）

**全局场景切换**。`SceneRouter` 是 static class，**不需要拖任何 prefab**，任何代码任何地方直接调。

#### 能力
- `Go(routeId, payload?)` / `GoAsync(...)`：按语义 ID 切场景，从 catalog 查 sceneName
- `GoScene(sceneName, payload?)` / `GoSceneAsync(...)`：直接按场景名切（调试 / 临时场景）
- `Reload()` / `ReloadAsync()`：重载当前场景
- `OnBeforeChange` / `OnAfterChange`：切换前 / 后回调，下发 `SceneRouteContext`
  - 上下文里有 `FromScene / ToRouteId / ToScene / Payload / UseLoading`
  - 存档、音频淡出、Loading 遮罩等系统都做成订阅者，SceneRouter 自己不直接依赖

#### 配置：`Assets/Resources/SceneRoutes.asset`
ScriptableObject，列表里每条 = `{ routeId, sceneName, useLoading }`。`SceneRouter` 第一次被调用时自动从 `Resources/SceneRoutes` 加载。要换 catalog 调 `SceneRouter.SetCatalog(other)`。

#### 用法
```csharp
// UI 按钮 / 剧情结束 / 任何 MonoBehaviour
SceneRouter.Go("level1_intro");                          // 走 catalog
SceneRouter.Go("level1_intro", payload: stats);          // 带数据
SceneRouter.GoScene("DebugSandbox");                     // 不进 catalog
SceneRouter.Reload();                                    // 重载当前场景
await SceneRouter.GoAsync("level1_dream");               // 等加载完
```

挂 loading 遮罩 / 存档 / BGM 淡出，写一个组件订阅事件即可（**SceneRouter 自己不引这些模块**）：
```csharp
SceneRouter.OnBeforeChange += ctx => {
    if (ctx.UseLoading) UIControlSystem.Instance?.OpenPage("loading");
    GameSaveProgressService.Instance?.Save();
};
SceneRouter.OnAfterChange += ctx => {
    UIControlSystem.Instance?.ClosePage("loading");
};
```

#### 注意
- 未注册的 routeId → `Debug.LogError` 不切场景（避免拼错静默失败）
- 当前已在切换中 → 后续请求被丢弃 + warning
- 场景名必须在 Build Settings 里
- SceneRouter 自动在首次使用时创建一个 DontDestroyOnLoad 的 `[SceneRouterRunner]` 跑协程，无需配置

#### 配套 Hook：`Assets/Prefabs/Core/SceneRouterHooks.prefab`
三个独立 MonoBehaviour，挂在子节点上，订阅 SceneRouter 事件实现常见副作用：

| Hook | 行为 | 关键字段 |
|---|---|---|
| `SceneRouterLoadingHook` | `UseLoading=true` 的路由切换前打开 `loading` 页，切换后关闭 | `loadingPageId`(默认 `loading`) |
| `SceneRouterSaveHook` | 切场景前自动 `GameSaveProgressService.Save()` | `excludeRouteIds`(调试场景排除) |
| `SceneRouterAudioHook` | 切场景前淡出 BGM；切完后按 routeId → bgmId 映射表 `PlayBGM` | `routeBgm`(列表), `fadeOutOnLeave` |

**用法**：把 `SceneRouterHooks.prefab` 拖进**首场景**（推荐主菜单或 Bootstrap 场景），因 SceneRouter 是 static、事件订阅持续到游戏退出，**只拖一次即全局生效**。不需要的 Hook 直接 disable 子节点即可。

> 完全不拖也能跑：Router 本身不依赖 Hook。Hook 缺席时切场景就没有 loading / 自动存档 / BGM 处理，需要的地方自己写。

---

### 2.4 不做 prefab 的 Core 服务

| 服务 | 原因 |
|---|---|
| `GameEventBus` | 纯 static 事件总线，没有场景对象 |
| `ResourceConfigService` | 纯 static Resources 加载服务，没有可挂组件 |

---

## 3. UI 模块

UI 下面按最小职责拆成 System / Pages / Popups / Atoms。System 是 UI 层级和设置服务，Pages / Popups 是可被 `UIControlSystem` 创建的界面，Atoms 是页面内部复用件。

### 3.1 System/UIControlSystem.prefab

**UI 页面 / 弹窗层级壳**。根节点挂 `UIControlSystem`。

#### 子结构
```
UIControlSystem
├── MainRoot
├── HudRoot
├── MenuRoot
├── PopupRoot
└── OverlayRoot
```

#### 包含
- Canvas (ScreenSpaceOverlay) + GraphicRaycaster
- `UIControlSystem`
- 5 个页面层级根节点
- 可选 `HUD CanvasGroup`（绑定到 `hudGroup`）

#### 引用要求
| 字段 | 来源 | 是否需手挂 |
|---|---|---|
| `mainRoot` | 子节点 MainRoot | prefab 内已配 ✓ |
| `hudRoot` | 子节点 HudRoot | prefab 内已配 ✓ |
| `menuRoot` | 子节点 MenuRoot | prefab 内已配 ✓ |
| `popupRoot` | 子节点 PopupRoot | prefab 内已配 ✓ |
| `overlayRoot` | 子节点 OverlayRoot | prefab 内已配 ✓ |
| `pagePrefabs` | Pages 下的页面 prefab | 按项目需要配置 |
| `popupPrefabs` | Popups 下的弹窗 prefab | 按项目需要配置 |

#### 用法
拖到使用菜单 / HUD / 弹窗的场景中。打开页面时：

```csharp
UIControlSystem.Instance.OpenPage(UIPageIds.CGGallery, galleryData);
UIControlSystem.Instance.PushPopup(UIPageIds.ConfirmPopup, promptData);
```

#### 注意
- 场景里**最多一份**。默认 `dontDestroyOnLoad = true`
- `ensureEventSystem = true` 时，如果场景里没有 EventSystem，会自动创建一个
- 如果已经使用 `SceneEssentials.prefab`，EventSystem 通常已经存在

---

### 3.2 System/UISettingsService.prefab

**UI 设置服务**。裸 GameObject 上挂 `UISettingsService`。

#### 能力
- 读取 / 保存 Master、BGM、SFX、Voice 音量
- 音量变化时广播 `OnVolumeSettingsChanged`
- 刷新 StoryPlayer 的音频适配器

#### 配套
和 `Atoms/VolumeSlider.prefab` 搭配使用。`VolumeSlider` 会自动找 `UISettingsService.Instance` 并同步滑条值。

#### 注意
- 场景里**最多一份**
- 它只管理 UI 设置状态；真正播放 BGM/SFX 的服务是 `AudioService.prefab`

---

### 3.3 Pages 页面 prefab

Pages 下放**可被 `UIControlSystem.OpenPage` 打开的完整页面**。所有页面根节点都应该继承 / 挂 `UIPageController`，并配置好 `pageId`、`layer`、`CanvasGroup`。

#### 页面一览
| Prefab | pageId | 建议 Layer | 当前脚本状态 | 职责 |
|---|---|---|---|---|
| `MainMenuPage` | `UIPageIds.MainMenu` | Menu | 需补 `UIMainMenuPageController` | 新游戏、继续游戏、设置、画廊、退出 |
| `SettingsPage` | `UIPageIds.Settings` | Menu / Popup | 需补 `UISettingsPageController` | 全局设置入口，重点是音量 |
| `PausePage` | `UIPageIds.PauseMenu` | Popup | 需补 `UIPausePageController` | 继续、重开、设置、回主菜单 |
| `InGameHUDPage` | `UIPageIds.InGameHud` | HUD | 需补 `UIInGameHUDPageController` | 任务、交互提示、暂停按钮、临时消息 |
| `ResultPage` | `UIPageIds.VictorySettlement` / `UIPageIds.FailureRetry` | Menu | 需补 `UIResultPageController` | 关卡 / 阶段完成后的结算展示 |
| `GalleryPage` | `UIPageIds.CGGallery` | Menu | 已有 `UICGGalleryPageController` 可承接 | CG 网格、解锁状态、大图预览 |
| `StoryOverlayPage` | 可新增 `story_overlay` | Overlay | 需补 `UIStoryOverlayPageController` | 剧情继续 / 跳过提示 |
| `LoadingPage` | 可新增 `loading` | Overlay | 需补 `UILoadingPageController` | 场景 / 流程切换遮罩 |

#### MainMenuPage.prefab

**主入口页面**。

推荐结构：
```
MainMenuPage
├── BackgroundRoot
├── LogoRoot
├── MenuButtonRoot
│   ├── StartButton
│   ├── ContinueButton
│   ├── SettingsButton
│   ├── GalleryButton
│   └── ExitButton
└── FooterRoot
    ├── VersionText
    └── CopyrightText
```

脚本职责：
- 初始化按钮事件
- 根据 `GameSaveProgressService` / 存档接口判断 Continue 是否可点
- 调用 `SceneRouter.Go("start_new_game")` / `Go("continue_game")` 进入游戏
- 调用 `UIControlSystem.OpenPage` 打开 Settings / Gallery

不负责：
- 不直接加载场景
- 不直接读写复杂存档内容
- 不直接播放剧情

---

#### SettingsPage.prefab

**全局设置页面**。

推荐结构：
```
SettingsPage
└── Panel
    ├── TitleText
    ├── AudioGroup
    │   ├── MasterSliderItem
    │   ├── MusicSliderItem
    │   ├── SFXSliderItem
    │   └── UISliderItem
    └── ButtonsGroup
        ├── ResetButton
        └── BackButton
```

每个 SliderItem 推荐结构：
```
SliderItem
├── LabelText
├── Slider
└── ValueText
```

脚本职责：
- 页面打开时读取当前设置
- 初始化 slider 值
- 监听 slider 改动
- 改动时实时同步到音频服务
- 返回 / 关闭时保存设置

注意：
- 当前 `UIVolumeChannel` 只有 Master / Bgm / Sfx / Voice；如果要单独支持 UI 音量，需要补 `UIVolumeChannel.Ui` 并接到 `GameAudioChannel.Ui`
- UI 页不要直接写 PlayerPrefs，统一走 `UISettingsService` / `GameAudioService`
- 不直接操作 AudioSource

---

#### PausePage.prefab

**游戏中断时的暂停菜单**。

推荐结构：
```
PausePage
├── DimMask
└── Panel
    ├── TitleText
    ├── ResumeButton
    ├── RestartButton
    ├── SettingsButton
    └── MainMenuButton
```

脚本职责：
- 打开时通知暂停控制器暂停游戏
- 关闭时恢复游戏
- Resume 关闭自己
- Restart 调 `SceneRouter.Reload()`
- Settings 打开 SettingsPage
- MainMenu 先弹 ConfirmDialog，再调 `SceneRouter.Go("main_menu")` 返回主菜单

注意：暂停建议由统一的 `GamePauseController` 负责，不要只依赖 `Time.timeScale = 0`，后续剧情、UI 动画、音频都可能需要更细的暂停规则。

---

#### InGameHUDPage.prefab

**游戏中常驻 HUD**。

推荐结构：
```
InGameHUDPage
├── TopRoot
│   ├── TaskText
│   └── PhaseText
├── CenterHintRoot
│   └── InteractionHintText
├── TemporaryMessageRoot
└── PauseButton
```

推荐模式：
```csharp
public enum HUDMode
{
    Gameplay,
    Dream,
    Story,
    Hidden
}
```

脚本职责：
- 提供外部接口刷新文案
- 支持 Gameplay / Dream / Story / Hidden 显示模式
- 不直接决定什么时候显示什么，由关卡流程系统调用

推荐接口：
- `SetMode(HUDMode mode)`
- `SetTaskText(string text)`
- `SetPhaseText(string text)`
- `ShowInteractionHint(string text)`
- `HideInteractionHint()`
- `ShowTemporaryMessage(string text, float duration)`

---

#### ResultPage.prefab

**关卡或阶段完成后的结算页**。

推荐结构：
```
ResultPage
└── ResultPanel
    ├── TitleText
    ├── StageNameText
    ├── UnlockRoot
    │   ├── UnlockTitle
    │   └── UnlockPreview
    └── ButtonRoot
        ├── NextButton
        └── MainMenuButton
```

数据建议：
```csharp
public sealed class UIResultPageData
{
    public string title;
    public string stageName;
    public bool hasUnlockedCG;
    public string unlockedCGId;
    public bool hasNextStage;
}
```

脚本职责：
- `OnOpened` 时接收 `UIResultPageData`
- 根据数据刷新标题、关卡名、解锁内容
- NextButton 调 `SceneRouter.Go("next_stage")` 进入下一段
- MainMenuButton 返回主菜单

---

#### GalleryPage.prefab

**CG 图鉴页面**。当前已有 `UICGGalleryPageController` 可作为第一版页面脚本。

推荐结构：
```
GalleryPage
├── ListRoot
│   └── ScrollView
│       └── Content
├── PreviewRoot
│   ├── FullImage
│   ├── TitleText
│   └── ClosePreviewButton
└── BackButton
```

当前已支持：
- 打开页面时传入 `UICGGalleryData`
- 动态生成 `Atoms/CGGallerySlot.prefab`
- 未解锁 CG 显示 `???` 并禁用点击

还建议补：
- `FullImage` 大图预览
- `BackButton`
- `CGConfig` ScriptableObject 配置表
- 存档里的 `UnlockedCGIds`

---

#### StoryOverlayPage.prefab

**剧情机 UI 覆盖层**。

推荐结构：
```
StoryOverlayPage
├── ContinueHint
└── SkipHint
```

脚本职责：
- 只负责显示，不负责剧情推进
- 接收剧情机下发的数据
- 根据剧情机指令更新继续 / 跳过提示

推荐接口：
- `ShowContinueHint(bool show)`
- `ShowSkipHint(bool show)`

---

#### LoadingPage.prefab

**场景切换或流程切换时的全局遮罩**。

推荐结构：
```
LoadingPage
├── BackgroundMask
├── Spinner
└── LoadingText
```

注意：
- 应挂在 OverlayLayer
- 优先级高于绝大多数 UI
- 可以支持黑屏淡入淡出，但不要在这里直接决定加载哪个场景

---

### 3.4 Popups/ConfirmDialogPage.prefab

**通用确认弹窗**。当前可直接用 `UIPromptPopup`（继承 `UIPageController`）承接。

#### 包含
- 标题文本 `titleLabel`
- 正文文本 `messageLabel`
- 确认按钮 `confirmButton`
- 取消按钮 `cancelButton`
- 两个按钮文本 `confirmLabel` / `cancelLabel`

#### 数据
打开时传入 `UIPromptPopupData`，等价于确认框数据：

```csharp
var data = new UIPromptPopupData
{
    title = "提示",
    message = "确认继续？",
    confirmText = "确定",
    cancelText = "取消",
    showCancel = true,
    onConfirm = () => Debug.Log("confirm")
};

UIControlSystem.Instance.PushPopup(UIPageIds.ConfirmPopup, data);
```

#### 注意
- `showCancel = false` 时会隐藏取消按钮
- 关闭时会优先走 `UIControlSystem.Instance.ClosePage(PageId)`
- Action 直接传对象可以，但跨场景或缓存弹窗时要注意引用时机；担心引用失效时可改成 eventId 模式

---

### 3.5 Atoms 单组件件

| Prefab | 组件 | 必填字段 | 行为 |
|---|---|---|---|
| `CGGallerySlot` | UICGGallerySlotView | titleLabel, thumbnailImage, lockedGroup, button | CG 图鉴单格；由 CGGalleryPage 动态生成 |
| `VolumeSlider` | UIVolumeSliderBinder + Slider | channel, slider | 设置页音量滑条；channel 可设 Master / Bgm / Sfx / Voice |

---

## 4. SideScroll 模块

### 4.1 SideScrollPlayer.prefab

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

### 4.2 CameraRig.prefab

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

### 4.3 Shells/SideScrollStoryWorkspace.prefab

**剧情型工作区壳**。空 GameObject 上挂 `SideScrollStoryWorkspace`（继承 `SideScrollWorkspaceBase`）。

#### 多出来的能力
`SetStoryInputLocked(bool)` —— 演出期间锁玩家输入。

#### 用法
作为关卡根。玩家、镜头组、地形、触发器都放在它下面。

---

### 4.4 Shells/SideScrollGameplayWorkspace.prefab

**解谜型工作区壳**。挂 `SideScrollGameplayWorkspace`。

#### 多出来的能力
- `EvaluateCompletion()` —— 检查通关条件（捡齐物品 / 触发齐目标 / 走到出口）
- 达成时广播 `workspace.completed` 事件

#### 用法
同 Story 工作区，区别在于关卡设计要配套用 GoalTriggerZone / PickupObject 凑够通关条件。

---

### 4.5 Atoms 单组件件

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

## 5. DualWorld 模块

### 5.1 DualWorldWorkspace.prefab

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

### 5.2 RealityCanvas.prefab

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

### 5.3 DreamWorld.prefab

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

### 5.4 ChatTaskPanel.prefab

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

### 5.5 Logic 单例 prefab

下面 5 个都是**裸 GameObject + 单组件**。直接拖会"看起来是空的"，但**挂到 DualWorldWorkspace 子树下**会通过 `GetComponentInParent` 找到 workspace。

| Prefab | 组件 | 必须的位置 |
|---|---|---|
| `AlignmentSubLevelFlow` | AlignmentSubLevelFlow | 任意子节点；引用 RealityCanvas + DreamWorld + ChatTaskDefinition |
| `LevelInGameFlowController` | LevelInGameFlowController | 顶层（subLevels 列表填子关流程机） |
| `DreamToRealityEnhancer` | DreamToRealityEnhancer | DualWorldWorkspace 子树下任意层级 |
| `RealityToDreamRepair` | RealityToDreamRepair | 同上 |
| `ChatTaskController` | ChatTaskController | 同上（建议放在 ChatTaskPanel 同级或父级，方便 panel 自查） |

---

### 5.6 Atoms 单组件件

| Prefab | 用途 |
|---|---|
| `DraggableBlock` | RectTransform + Image + DraggableAlignmentBlock；放在 Canvas 下，target 字段拖一个 AlignmentTarget 进去 |
| `AlignmentTarget` | RectTransform + Image（半透明黄色目标位） |
| `SubmitButton` | RectTransform + Image + Button + Label 子节点 |
| `PushableBlock` | 物理盒 + DreamPushable 标记 |
| `DreamPushTarget` | 触发器盒 + DreamPushTarget（dwell 计时） |

---

## 6. StoryPlayer 模块

### 6.1 StoryPlayerRig.prefab

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

### 6.2 Triggers 触发器 prefab（生产用，**关卡里直接拖**）

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

### 6.3 Atoms

| Prefab | 用途 |
|---|---|
| `Background.prefab` | 全屏 Image，留作剧情背景 |
| `FadeOverlay.prefab` | 全屏黑 + CanvasGroup（默认 alpha=0），转场用 |
| `InputBlocker.prefab` | 透明 Image + Button，演出期间吞掉点击 |

这些都是 UI 子件，**只在自定义剧情画布时用**，常规剧情触发不需要。

---

## 7. ScriptableObject 资产

| 资产路径 | 类型 | 修改影响 |
|---|---|---|
| `Settings/Defaults/DefaultCharacterMoveConfig.asset` | CharacterMoveConfig | 玩家走路速度、加减速度 |
| `Settings/Defaults/DefaultCharacterJumpConfig.asset` | CharacterJumpConfig | 跳跃高度、coyote、buffer |
| `Settings/Defaults/DefaultCameraConfig.asset` | CameraConfig | 相机偏移、damping |
| `Settings/DualWorld/AlignmentChatTask.asset` | ChatTaskDefinition | 对齐子关 5 段聊天文本 |
| 自建 `*.asset` | StorySequence | 各段剧情数据 |

修改这些**不需要重生成 prefab** —— prefab 引用的就是这些 asset 文件，立刻生效。

---

## 8. 实际关卡搭建场景

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

## 9. 常见操作模板

### 9.1 在某个位置插一段剧情

```
方案 A：拖 StoryAutoPlay.prefab，sequence 字段挂剧情 → 场景加载即播
方案 B：拖 StoryTriggerZone.prefab，调位置和大小 → 走到这里播
方案 C：拖 StoryInteractable.prefab → 按 E 播
方案 D：从代码任意位置 StoryPlayerService.Play(seq) → 立即播
方案 E：拖 StoryWorkspaceEventTrigger.prefab，eventId 填 "puzzle.solved" → 解谜后播
```

### 9.2 让玩家拾取物品

```
1. 拖 Atoms/PickupObject.prefab
2. Inspector 里改 pickupId（如 "key_red"）
3. 玩家走入 → 工作区记录到 collectedPickupIds
4. 后续门禁可用 ConditionTriggerZone 检查 pickupId 是否已收集
```

### 9.3 设置关卡出口

```
1. 拖 Atoms/ExitPoint.prefab
2. （可选）改 prompt 文本
3. 玩家走入 → 工作区 raise "workspace.exit" 事件
4. 监听处理：Workspace.WorkspaceEventRaised += (id) => { if(id=="workspace.exit") ... }
```

### 9.4 配置镜头跟随范围

```
1. 选中 CameraRig.prefab 实例下的 CameraBounds 子节点
2. 改 BoxCollider2D.size 调整可视范围
3. （可选）拖 Atoms/CameraTriggerZone.prefab 在路径上某点 → 切镜头到不同 config
```

### 9.5 剧情结束做某件事

```csharp
StoryPlayerService.Play(sequence, onComplete: () => {
    // 这里写剧情结束后的逻辑
    Debug.Log("剧情播完了");
});
```

---

## 10. 引用关系图（关键的）

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

## 11. 故障排查

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

## 12. 自定义你自己的 prefab

如果自动生成的不够用：

1. **从已有 prefab 派生 Variant**：右键 prefab → Create → Prefab Variant，然后改细节
2. **新增触发器类型**：实现自己的 `: TriggerZoneBase` 或 `: SideScrollInteractableBase` 子类，套这套 BindWorkspace 机制就行
3. **新增子关流程机**：实现 `: BaseSubLevelFlow` 子类，按 8 阶段枚举走

不要直接修改 `Assets/Editor/*` 生成的 prefab —— 重跑菜单会覆盖你的改动。

---

最后更新：2026-05-08
