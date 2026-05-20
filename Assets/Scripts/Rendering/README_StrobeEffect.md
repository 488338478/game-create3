# 频闪效果（Strobe Post-Process）使用说明

第一篇章末尾"画面闪频/雪花屏"效果的实现。基于 URP 14 ScriptableRendererFeature
的全屏后处理，五个子效果一个 shader：亮度闪烁 / 雪花噪点 / RGB 偏移 / 扫描线 / 画面撕裂。

> Unity 2022.3 LTS + URP 14.0.12 + URP 2D Renderer。

---

## 文件清单

| 文件 | 说明 |
|------|------|
| `Assets/Shaders/Strobe2D.shader` | HLSL 后处理 shader |
| `Assets/Scripts/Rendering/StrobeRendererFeature.cs` | URP RendererFeature + Pass |
| `Assets/Scripts/SideScroll/VFX/StrobeConfig.cs` | ScriptableObject 预设 |
| `Assets/Scripts/SideScroll/VFX/StrobeEffectController.cs` | 运行时控制器（单例） |
| `Assets/Scripts/SideScroll/VFX/StrobeStoryEventBinder.cs` | StoryEventSystem → Controller 桥接 |
| `Assets/Scripts/StoryPlayer/StoryData.cs` | 新增 `StoryEventType.PostProcessEffect` |
| `Assets/Scripts/StoryPlayer/StoryEventSystem.cs` | 新增 `OnPostProcessEffectRequested` 事件 |

---

## 一次性设置（5 步）

### 1. 在 Renderer2DData 上注册 RendererFeature

> 这一步是手动操作，无法纯代码完成。

1. Project 窗口里找到 `Assets/Settings/Renderer2D.asset` 双击打开 Inspector
2. 在 Inspector 底部点击 `Add Renderer Feature`
3. 选择 `Strobe Renderer Feature`
4. 展开新增项，确认：
   - **Strobe Shader**：留空（运行时按名查找 `Game/PostProcess/Strobe2D`）
   - **Injection Point**：`After Rendering`（最末端 — UI 也会被频闪）
5. 保存项目（Ctrl+S）

### 2. 在场景里放置 Controller

在 BootScene 或主场景里：

1. 新建空 GameObject，命名 `[StrobeController]`
2. 添加组件 `Strobe Effect Controller`
3. 勾上 `Dont Destroy On Load`（建议保留默认）

### 3. 挂 Binder（任何持久 GameObject 都行）

> **不要挂在 StoryEventSystem 上** — 它是 StoryPlayer 动态创建的，挂上去会因为时序问题抓不到事件。

推荐做法：直接挂在步骤 2 的 `[StrobeController]` GameObject 上：

1. 选中 `[StrobeController]`
2. Inspector 里 `Add Component` → `Strobe Story Event Binder`
3. 保留默认的 `unsubscribeOnDisable = true`

Binder 监听的是 `StoryEventSystem.OnPostProcessEffectRequested` 静态事件，
不依赖任何具体的 StoryEventSystem 实例。无论 StoryPlayer 何时动态创建/销毁
StoryEventSystem，都能正确收到事件。

### 4. 创建预设

1. Project 窗口空白处右键 → `Create > Game > VFX > Strobe Preset`
2. 把生成的资产放到 `Assets/Resources/VFX/Strobe/` 目录下
3. 命名为 `Chapter1Transition`（即资产文件名为 `Chapter1Transition.asset`）
4. Inspector 里调参数（推荐参考下方"预设范例"）

### 5. 在 StorySequence 里添加事件

打开第一篇章末"奖状变方案"那页的 StorySequence 资产：

在该页的 `Page Events` 列表加一项：

| 字段 | 值 |
|------|---|
| Event Type | `Post Process Effect` |
| Trigger Time | `4.5`（这页播到 4.5 秒触发） |
| Event Data | `Chapter1Transition` |

保存即可。

---

## EventData 字符串格式

| 写法 | 说明 |
|------|------|
| `Chapter1Transition` | 用预设默认参数 |
| `Chapter1Transition?intensity=0.7&duration=3` | 预设 + 覆盖参数 |
| `?duration=1&intensity=0.8&noise=0.5` | 完全内联（不需要预设） |
| `stop` | 立即停止当前效果 |
| `fadeout=0.5` | 0.5 秒渐隐停止 |

### 可覆盖的参数键

| key | 含义 | 范围 |
|-----|------|------|
| `duration`     | 总时长（秒）         | > 0 |
| `fadein`       | 入场渐显（秒）       | ≥ 0 |
| `fadeout`      | 出场渐隐（秒）       | ≥ 0 |
| `intensity`    | 主强度               | 0–1 |
| `flicker`      | 亮度闪烁强度         | 0–1 |
| `frequency`    | 闪烁频率（Hz）       | > 0 |
| `noise`        | 雪花噪点强度         | 0–1 |
| `rgb`          | RGB 通道偏移（像素） | ≥ 0 |
| `scanline`     | 扫描线强度           | 0–1 |
| `scanlinefreq` | 扫描线数量           | > 0 |
| `tear`         | 撕裂强度             | 0–1 |
| `tearfreq`     | 撕裂频率（Hz）       | > 0 |
| `color`        | 偏色 HTML 颜色码     | `#RRGGBBAA` |

---

## 预设范例

### `Chapter1Transition.asset` — 第一章末闪频

```
duration: 2.5
fadeIn:   0.05
fadeOut:  0.4
intensity: 1.0
flickerStrength: 0.7
flickerFrequency: 18
noiseDensity: 0.55
rgbShiftAmount: 8
scanlineIntensity: 0.4
scanlineFrequency: 220
tearAmount: 0.25
tearFrequency: 12
colorShift: (1, 1, 1, 0)   // 不偏色，保留原画面
```

### `SubtleGlitch.asset` — 轻度抖动（玩偶发光等小转场用）

```
duration: 0.8
fadeIn:   0.0
fadeOut:  0.2
intensity: 0.6
flickerStrength: 0.3
flickerFrequency: 10
noiseDensity: 0.15
rgbShiftAmount: 3
scanlineIntensity: 0.1
scanlineFrequency: 180
tearAmount: 0.05
tearFrequency: 6
```

### `HeavyStatic.asset` — 重度雪花屏

```
duration: 1.5
fadeIn:   0.0
fadeOut:  0.3
intensity: 1.0
flickerStrength: 0.4
flickerFrequency: 25
noiseDensity: 0.85
rgbShiftAmount: 12
scanlineIntensity: 0.5
scanlineFrequency: 240
tearAmount: 0.35
tearFrequency: 15
```

---

## 代码调用

如果不通过 StoryPlayer 而是直接在代码里触发：

```csharp
using GameCreate3.SideScroll.VFX;
using UnityEngine;

// 用预设
var preset = Resources.Load<StrobeConfig>("VFX/Strobe/Chapter1Transition");
StrobeEffectController.Instance.Play(preset);

// 预设 + 覆盖
var overrides = new Dictionary<string, string> { {"intensity", "0.5"}, {"duration", "1"} };
StrobeEffectController.Instance.Play(preset, overrides);

// 实时调整
StrobeEffectController.Instance.SetIntensity(0.3f);

// 停止
StrobeEffectController.Instance.Stop();
StrobeEffectController.Instance.FadeOut(0.5f);
```

---

## 常见问题

**Q：场景里什么都没看到？**
- 检查 Renderer2DData 是否注册了 `Strobe Renderer Feature`（步骤 1）
- 检查场景里是否有 `StrobeEffectController` 组件
- 检查 Console 是否报 `Shader 'Game/PostProcess/Strobe2D' 未找到`

**Q：UI 也被频闪了，我不想要 UI 参与？**
- 把 RendererFeature 的 `Injection Point` 改成 `AfterRenderingPostProcessing`
- 注：URP 2D 的 UI 通常走 Overlay Canvas（独立相机），改注入点可能不够，需要让 UI 相机不挂这个 Feature

**Q：场景切换后效果消失？**
- Controller 设了 `DontDestroyOnLoad`，但 RendererFeature 是绑定在 Renderer2DData 上的全局资产，正常应该不受场景影响
- 如果 UI 是独立 Canvas + 独立相机，新场景的相机可能没共享同一个 Renderer2DData

**Q：效果太强/太弱？**
- 调预设里 `intensity` 总强度
- 单项太强：调对应子项的 `strength`/`density`/`amount`

**Q：可以同时叠加多个频闪效果？**
- 不能。`Play()` 会打断当前正在执行的效果。要叠加请改一个支持多 instance 的 Controller。

---

## 性能

- 单 Pass，2 次 Blit（temp ping-pong）
- 1080p 下额外开销约 0.3–0.6 ms（取决于显卡）
- 不启用时（`IsActive = false`）零开销，Pass 直接跳过

---

## 第一章触发位置参考

剧情顺序（参考 `策划文档/玩法文档.docx` 第一篇章）：

```
Page chapter1_award:           主角举着奖状欢呼
  → 0.0s   PlayMusic "victory_bgm"

Page chapter1_glitch:          音乐突然出现噪音
  → 0.0s   PlaySound "noise_burst"
  → 0.0s   PostProcessEffect "Chapter1Transition"   ← 这里触发频闪
  → 2.0s   StopMusic                                ← BGM 突然停止
  → 2.5s   PostProcessEffect "fadeout=0.3"          ← 频闪渐隐

Page chapter1_reality:         画面切回现实（灰暗调）
  → 0.0s   TriggerEffect "scene_to_reality"
```
