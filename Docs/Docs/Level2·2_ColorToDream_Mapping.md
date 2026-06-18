# Level2·2 Reality 填色 → 点亮 Dream 物体 机制分析

## 1. 整体数据流

```
┌─────────────────────────────────────────────────────────────────────┐
│                        ColorSubLevelFlow                            │
│  ResolveHintRouter() 初始化 DreamColorHintRouter                    │
│  传入: workspace, dreamCollector, realityTask, alignmentTask        │
└────────┬────────┬────────┬────────┬────────┬────────┬──────────────┘
         │        │        │        │        │        │
         ▼        ▼        ▼        ▼        ▼        ▼
┌──────────┐ ┌───────┐ ┌──────────┐ ┌───────────┐ ┌──────────┐ ┌───────────────┐
│ Dream    │ │Color  │ │Reality   │ │DreamColor │ │Interact  │ │ Dream 物体     │
│ Collector│ │Slot   │ │Alignment │ │HintRouter │ │Trigger   │ │ SpriteRenderer │
│          │ │       │ │Task      │ │           │ │(disabled)│ │               │
└────┬─────┘ └───┬───┘ └────┬─────┘ └─────┬─────┘ └────┬─────┘ └───────┬───────┘
     │           │           │             │            │              │
     │  流星掉落  │  targetVariantId        │  unlockMap  │  interactId  │  被灰度/恢复
     │  ↓        │  ↓        │  ↓          │  ↓          │  ↓           │  ↓
     │ Palette   │ blockIndex│ interactId  │ 桥接两端     │ 仅作ID标签   │ 视觉目标
     │ ColorOption│ ← → → → →│ ← → → → → →│ ← → → → → → │ ← → → → → →  │
     └───────────┘           │             │            │              │
```

---

## 2. 映射层级（四层）

### 第 1 层：PaletteColorOption → blockIndex

```
┌────────────────────────────────────────────────────────────────┐
│  DreamColorHintRouter.TryResolveBlockIndex(option)             │
│                                                                │
│  ① ColorPuzzleController.TryResolveBlockIndex()                │
│     └→ 遍历 ColorSlot，找 MatchesOption(option) 的 slot        │
│        └→ ColorSlot.TryGetBlockIndex()                         │
│                                                                │
│  ② (fallback) option.variantId - 1                             │
│  ③ (fallback) int.Parse(option.colorId) - 1                    │
└────────────────────────────────────────────────────────────────┘
```

**ColorSlot.TryGetBlockIndex() 优先级：**

```
优先级 1: blockIndex 字段 ≥ 0          → 直接用
优先级 2: targetVariantId > 0          → blockIndex = targetVariantId - 1
优先级 3: GameObject 名 "Target_N_*"   → 解析数字部分
```

### 第 2 层：blockIndex → interactId

```
┌──────────────────────────────────────────────────────┐
│  RealityAlignmentTask.unlockMap                      │
│                                                      │
│  struct InteractUnlock {                             │
│      string interactId;    // 如 "book", "phone"     │
│      int    blockIndex;    // 对应 ColorSlot 的 block │
│  }                                                   │
│                                                      │
│  GetInteractIdsForBlockIndex(N) → List<string>       │
│  返回所有 interactId，其 blockIndex == N              │
│  (一个 blockIndex 可对应多个 interactId)              │
└──────────────────────────────────────────────────────┘
```

### 第 3 层：interactId → InteractTrigger（SpriteRenderer）

```
┌──────────────────────────────────────────────────────────┐
│  DreamColorHintRouter.RebuildTargetCache()               │
│                                                          │
│  ① mappingTask.GetConfiguredInteractIds()                │
│     └→ 收集 unlockMap 中所有不重复的 interactId           │
│                                                          │
│  ② workspace.GetComponentsInChildren<InteractTrigger>()  │
│     └→ 筛选: interactId 在 configuredInteractIds 中      │
│     └→ 缓存: trigger.gameObject 的 SpriteRenderer[]      │
│                                                          │
│  ③ 不在 configuredInteractIds 中的 InteractTrigger       │
│     └→ 被跳过，永远不会被灰度/恢复/脉冲                  │
└──────────────────────────────────────────────────────────┘
```

### 第 4 层：SpriteRenderer → 视觉状态

```
┌───────────────────────────────────────────────────────┐
│  Shader: "Game/Sprites/GrayscaleTint"                 │
│                                                       │
│  三种视觉状态:                                         │
│                                                       │
│  ██ 灰度 (muted)                                      │
│     _GrayscaleAmount = 1                              │
│     _Brightness      = 1                              │
│     → dream 物体黑白褪色                               │
│                                                       │
│  ██ 脉冲 (pulse)                                      │
│     _GrayscaleAmount = 1                              │
│     _FlashColor      = accent color                   │
│     _FlashAmount     = sin(t) 波                      │
│     → 掉落对应颜色流星时闪烁提示                        │
│                                                       │
│  ██ 恢复 (restored)                                   │
│     Material 切回原始 sharedMaterial                   │
│     PropertyBlock 清空                                │
│     → dream 物体全彩 = "点亮"                          │
└───────────────────────────────────────────────────────┘
```

---

## 3. 运行时事件流

```
时间线 ──────────────────────────────────────────────────────────────────→

[阶段1: RealityTaskActive]
  │
  │ DreamColorHintRouter.RefreshMutedState()
  │   └→ dream 物体全部灰度
  │
  ├─ [玩家在梦境接住颜色流星]
  │    │
  │    dreamCollector.ItemCollected(option)
  │    ├── ColorSubLevelFlow.HandleDreamCollectorItemCollected()
  │    │     ├─ realityTask.SetCurrentPalette(option)     ← 现实色板更新
  │    │     └─ realityTask.FlashTargetsForOption(option)  ← 现实色板闪烁
  │    │
  │    └── DreamColorHintRouter.HandleItemCollected(option)
  │          ├─ TryResolveBlockIndex(option) → blockIndex
  │          ├─ GetInteractIdsForBlockIndex(blockIndex) → interactIds
  │          └─ PulseTarget(state, accentColor)
  │               └→ dream 物体用该颜色的 accent color 脉冲闪烁 (提示)
  │
  ├─ [玩家在 reality 点击 ColorSlot 填入颜色]
  │    │
  │    ColorSlot.ApplyPaletteColor(option)
  │    ├─ ColorApplyTarget.ApplyVariant(option, isCorrect)
  │    │    └─ 切换 Sprite / 着色 (reality 侧视觉更新)
  │    │
  │    └─ StateChanged 事件
  │         │
  │         DreamColorHintRouter.HandleColorSlotStateChanged(slot)
  │         ├─ slot.IsCorrectColor() ? 
  │         │    ├─ YES → IsBlockSolved(blockIndex) → YES
  │         │    │        IsInteractTargetFullySolved(interactId) ?
  │         │    │          ├─ YES → 所有关联 block 都解了
  │         │    │          │        StartStateTransition(gray=0, restore=true)
  │         │    │          │        └→ dream 物体恢复全彩 = "点亮" ✅
  │         │    │          │
  │         │    │          └─ NO  → 还有 block 没解，保持灰度
  │         │    │
  │         │    └─ NO  → 保持灰度 / 重新 muted
  │         │
  │         └─ 未关联任何 interactId 的 blockIndex
  │              └→ GetInteractIdsForBlockIndex() 返回空
  │              └→ 没有任何 dream 物体受影响 ⚠️
  │
  ├─ [所有 ColorSlot 填对，提交成功]
  │    │
  │    ColorPuzzleController.Submit() → success
  │    └→ ColorSubLevelFlow.EnterPhase(RealityTaskCompleted)
  │         └→ SubLevelCompleted → 过关
```

---

## 4. Level2·2 场景结构

```
Level2·2.unity (1309 行)
│
├── SceneEssentials (Camera, etc.)
│     prefab: guid:6c65d716...
│
├── DualWorldWorkspace_ColorClean  ← 核心 prefab
│     prefab: guid:28cf15f0...
│     │
│     ├── Canvas
│     │   ├── ColorSlot × 8  (各带 targetVariantId 1-8)
│     │   │   └── ColorApplyTarget (Sprite/Variant 切换)
│     │   └── ColorPaletteSwatch (当前拾取的颜色显示)
│     │
│     ├── RealityAlignmentTask  ← unlockMap 在此，但不做对齐
│     │   ├── blocks: size=0     (被清空)
│     │   ├── targetRects: size=0 (被清空)
│     │   └── unlockMap: size=10  (仅作映射表)
│     │
│     ├── ColorPuzzleController
│     │
│     ├── DreamColorCollectController (流星生成)
│     │
│     ├── InteractTrigger × 14  (dream 物体 ID 标签)
│     │   ├── album
│     │   ├── book
│     │   ├── radio          ← unlockMap[2], blockIndex=4
│     │   ├── vase
│     │   ├── blinds_rope
│     │   ├── blinds
│     │   ├── phone
│     │   └── small_shelf, shelf, wall_l, box,
│     │       wall_r, desk, castle_extra  (仅 observationId，无 interactId)
│     │
│     └── DreamColorHintRouter  (桥接器)
│           ├── mappingTask → RealityAlignmentTask
│           ├── colorPuzzle → ColorPuzzleController
│           ├── dreamCollector → DreamColorCollectController
│           └── disableLegacyPressEInteractions = true
│
├── backgroundCanvas
├── CustomCursor
├── SceneRouterHooks
├── AudioService
└── StoryAutoPlay (disabled)
```

---

## 5. 当前 unlockMap 配置

`RealityCanvas.prefab`（base）默认有 3 条：
```
[0] observationId=book,  blockIndex=0
[1] observationId=phone, blockIndex=1
[2] observationId=radio, blockIndex=2
```

`DualWorldWorkspace_ColorClean.prefab` 将 unlockMap 扩至 10 条并覆盖了各字段。未被显式覆盖的字段（如 `observationId`）仍从 base 继承。

| Index | interactId / observationId | blockIndex | 对应 ColorSlot | 来源 |
|-------|---------------------------|------------|----------------|------|
| 0 | `vase` | 3 | targetVariantId=8 (小熊) | override (覆盖 base [0]) |
| 1 | `phone` | 6 | targetVariantId=6 | observationId 继承 base [1] |
| 2 | `radio` | 4 | targetVariantId=5 | observationId 继承 base [2] |
| 3 | `blinds_rope` | 2 | targetVariantId=2 | override |
| 4 | `phone` | 7 | targetVariantId=7 | override |
| 5 | `blinds_rope` | 1 | targetVariantId=3 | override |
| 6 | `book` | 5 | targetVariantId=4 | override |
| 7 | `album` | 0 | targetVariantId=1 | override |
| 8 | `blinds` | 2 | targetVariantId=2 | override (与[3]重复) |
| 9 | `blinds` | 1 | targetVariantId=3 | override (与[5]重复) |

所有在场景中设了 `interactId` 的 InteractTrigger（`album`/`book`/`radio`/`vase`/`blinds_rope`/`blinds`/`phone`）均在 unlockMap 中。仅设 `observationId` 但无 `interactId` 的物体（`small_shelf`/`shelf`/`wall_l`/`box`/`wall_r`/`desk`/`castle_extra`）不在 unlockMap 内，不受填色影响。

---

## 6. 颜色 → 色槽 → 梦境物体 对应表

调色盘共有 8 种颜色，每种颜色编了号（`variantId` + `colorId` 均为 1~8）。流星掉落时携带着色号，玩家接到后去现实侧的对应色槽填入。每个色槽接受任意颜色填入，但只有色号匹配才算"正确"解锁。

```
调色盘颜色(1-8) ──→ ColorSlot(targetVariantId = 颜色号) ──→ blockIndex ──→ 梦境物体
```

| 颜色号 | 色槽名称 | blockIndex | 填对后点亮的梦境物体 |
|--------|----------|------------|---------------------|
| ① | Target_0 | 0 | **album**（相册） |
| ② | Target_2 | 2 | **blinds_rope + blinds**（百叶窗绳+百叶窗） |
| ③ | Target_1 | 1 | **blinds_rope + blinds**（同上，②③共享） |
| ④ | Target_5_副标题 | 5 | **book**（书） |
| ⑤ | Target_4_文字上 | 4 | **radio**（收音机） |
| ⑥ | Target_6_文字中 | 6 | **phone**（电话） |
| ⑦ | Target_7_文字下 | 7 | **phone**（电话，⑥⑦共享） |
| ⑧ | Target_3_小熊 | 3 | **vase**（花瓶） |

**关键规则：**

- 颜色②和③对应百叶窗组（blinds_rope + blinds）。因为 `blinds` 同时关联 blockIndex 1 和 2，**必须②和③两个槽都填对，blinds 才会点亮**。`blinds_rope` 同理。
- 颜色⑥和⑦都对应 phone。`phone` 同时关联 blockIndex 6 和 7，**两个槽都填对才会亮**。
- 其余物体（album / book / radio / vase）各自只关联一个 blockIndex，填对对应的那一种颜色就亮。

---

## 7. 排查清单

如果实际游玩时某个 ColorSlot 填对了颜色但对应 dream 物体没被点亮：

```
① ColorSlot.blockIndex 或者 targetVariantId 填对了吗？
   └→ TryGetBlockIndex() 推导值是否与预期一致？

② 推导出的 blockIndex 在 unlockMap 里有对应的 interactId 吗？
   └→ GetInteractIdsForBlockIndex(N) 返回非空？

③ unlockMap 里的 interactId 在 dream 场景中有对应的 InteractTrigger 物体吗？
   └→ targetStatesByInteractId.ContainsKey(interactId) ?
   └→ 该物体有 SpriteRenderer 吗？

④ IsInteractTargetFullySolved(interactId) 的判定
   └→ 该 interactId 关联的所有 blockIndex 都被填对了？
   └→ 如果 "blinds" 同时关联 blockIndex 1 和 2，
       必须两个 ColorSlot 都填对才会点亮
```

---

## 8. 关键代码索引

| 文件 | 关键方法 |
|------|---------|
| `Assets/Scripts/DualWorld/Color/DreamColorHintRouter.cs` | `Initialize()`, `HandleItemCollected()`, `HandleColorSlotStateChanged()`, `RebuildTargetCache()` |
| `Assets/Scripts/DualWorld/Color/ColorSlot.cs:148-169` | `TryGetBlockIndex()` |
| `Assets/Scripts/DualWorld/Color/ColorPuzzleController.cs:161-173` | `TryResolveBlockIndex()` |
| `Assets/Scripts/DualWorld/Reality/RealityAlignmentTask.cs:68-92` | `GetInteractIdsForBlockIndex()` |
| `Assets/Scripts/DualWorld/Flow/ColorSubLevelFlow.cs:277-289` | `ResolveHintRouter()` |
| `Assets/Prefabs/DualWorld/2/DualWorldWorkspace_ColorClean.prefab` | unlockMap & ColorSlot 序列化数据 |
| `Assets/Prefabs/DualWorld/1/RealityCanvas.prefab` | unlockMap base 值 |
