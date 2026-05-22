# StorySequence 资产使用教程

## 它是什么

StorySequence 是一个 ScriptableObject，说白了就是一个“剧情脚本文件”。一段剧情由很多页组成，每页可以有背景图、对话、音效、转场。你在 Inspector 里把这些填好，运行时播放器就按顺序一页页放出来。

新手记住一句话就行：**一个 StorySequence = 一段完整剧情；一个 Page = 屏幕上的一个画面；一个 TextBlock = 这个画面里的一句话/一段话。** 整个结构就是这么三层套下去的。

## 怎么新建

在 Project 窗口右键 → Create → GameCreate3 → StoryPlayer → Story Sequence。文件名默认是 `StorySequence_`，后面接你自己起的名字，比如 `StorySequence_Level1Intro`。

建好后点中它，所有字段都在 Inspector 里。

---

## 第一层：StorySequence 本体的字段

| 字段 | 类型 | 干什么用 |
|---|---|---|
| Sequence Id | string | 这段剧情的唯一名字，给代码找它用的。随便起，但别和别的重名，比如 `level1_intro` |
| Pages | 列表 | 核心。所有页面都堆在这里，从上到下就是播放顺序 |
| Default Playback Mode | 枚举 | 默认怎么翻页。`ClickToAdvance` 是点一下翻一页，`AutoAdvance` 是自动定时翻 |
| Allow Skip | bool | 玩家能不能跳过这段剧情。勾上就允许 |
| Auto Advance Delay | float | 只在自动翻页模式下生效。每页停留几秒，默认 3 秒 |
| End Callback Type | 枚举 | 这段剧情放完之后干什么（见下表） |
| End Callback Parameter | string | 配合上面那个回调用的参数，比如要进哪一关就填关卡名 |

**End Callback Type 的几个选项：**
- `None` — 放完就完，啥也不干
- `EnterLevel` — 进入某个关卡，关卡名写在 End Callback Parameter 里
- `EnterSideScroller` — 进入横版玩法
- `EnterMainMenu` — 回主菜单
- `EnterDialogue` — 进入对话
- `TriggerEvent` — 触发一个自定义事件，事件名写在参数里

举个例子：开场动画放完要直接进第一关，那就把 End Callback Type 设成 `EnterLevel`，参数填 `Level1`。

---

## 第二层：StoryPage（每一页）

点开 Pages 列表里的一个元素，就是一页的全部设置。

| 字段 | 干什么用 |
|---|---|
| Page Id | 这一页的名字，方便你自己找，比如 `page_01` |
| Page Type | 这页是什么类型：`Static`(纯静态画面)、`Text`(纯文字)、`CG`(过场图)、`Mixed`(混合，最常用) |
| Background Image | 背景图（Sprite） |
| Foreground Image | 前景图，盖在背景上面，比如角色立绘 |
| Text Blocks | 这一页要显示的文字，可以有好几段，见第三层 |
| Display Duration | 这页显示多久。填 `-1` 表示不限时，等玩家操作 |
| Wait For Input | 是否等玩家点击才继续。勾上就停下来等点击 |
| Elements | 更细的元素（角色、特效等），高级用法，见后面 |
| Transition In | 这页**进场**用什么转场效果 |
| Transition Out | 这页**退场**用什么效果 |
| Transition Duration | 转场动画放多久，默认 0.5 秒 |
| Page Events | 在这页的特定时间点触发的事件，见后面 |
| Audio Config | 这页的声音设置，见后面 |

**Display Duration 和 Wait For Input 的关系**容易绕，说清楚：如果你想让玩家自己点击翻页，就把 Display Duration 留成 `-1`、Wait For Input 勾上。如果想让画面停 5 秒自动过，就 Display Duration 填 5、Wait For Input 取消勾选。

**转场类型（Transition Type）有这些：** None(无)、Fade(淡入淡出)、SlideLeft/Right/Up/Down(四个方向滑动)、Scale(缩放)、CrossFade(交叉淡化)。不确定填什么就用 Fade，最稳。

---

## 第三层：StoryTextBlock（每一句话）

这是玩家真正读到的文字。一页里可以放多段，按顺序出现。

| 字段 | 干什么用 |
|---|---|
| Text Id | 这段文字的标识，可留空 |
| Speaker | 说话的人名，比如 `小明`。旁白可以留空 |
| Content | 正文。这是一个多行文本框，长对话也能塞 |
| Display Mode | 文字怎么出现：`Instant`(瞬间全显)、`Typewriter`(打字机逐字)、`FadeIn`(淡入) |
| Typewriter Speed | 打字机模式下每个字的间隔，默认 0.05 秒。数字越小打得越快 |
| Delay Before Show | 这段文字延迟几秒才出现。想做“停顿一下再说话”就用它 |
| Duration | 这段文字显示多久后消失，填 `-1` 就一直留着 |

最常见的对话设置：Speaker 填名字，Content 填台词，Display Mode 用 Typewriter，其余默认。这样就是经典的逐字打字对话效果。

---

## 声音：StoryAudioConfig

每页的 Audio Config 里：

- **Bgm** — 这页的背景音乐（AudioClip）
- **Loop Bgm** — 背景音乐是否循环，默认开
- **Bgm Volume** — 音量，0 到 1，默认 1
- **Voice Over** — 配音/旁白语音
- **Sound Effects** — 一组音效，每个音效有三项：`Clip`(音频)、`Trigger Time`(进这页后第几秒播)、`Volume`(音量)

注意 BGM 是跟着页走的。如果连续几页想用同一首 BGM 不断掉，得每页都填同一个 Bgm（具体看播放器实现，必要时确认 StoryPlayer 的播放逻辑）。

---

## 高级：Elements 和 Page Events

这两个新手刚上手可以先不碰，知道有就行。

**Elements** 是比 TextBlock 更细的舞台元素——一个角色立绘、一个特效、一段单独的音频，都能当成一个 Element，各自带自己的出现延迟、持续时间和动画（FadeIn/SlideIn/ScaleIn/Typewriter/Shake）。适合做复杂演出，比如“角色从左边滑进来同时抖一下”。

每个 Element 的字段：

| 字段 | 干什么用 |
|---|---|
| Element Type | 类型：Background/Character/DialogueText/NarrationText/Effect/Audio |
| Element Id | 标识，可留空 |
| Image | 图片（Sprite） |
| Text | 文字 |
| Audio Clip | 音频 |
| Delay | 延迟几秒出现 |
| Duration | 持续多久，-1 为一直 |
| Animation Type | 出现动画：None/FadeIn/SlideIn/ScaleIn/Typewriter/Shake |
| Animation Duration | 动画时长，默认 0.5 秒 |

**Page Events** 是定时事件。在这页的第几秒，触发某个动作。`Trigger Time` 是时间点，`Event Data` 是附带数据。事件类型有：

- `PlaySound` — 播音效
- `PlayMusic` — 播音乐
- `StopMusic` — 停音乐
- `TriggerEffect` — 触发特效
- `SetVariable` — 设置变量
- `Branch` — 剧情分支
- `PostProcessEffect` — 后处理特效

这套是给做剧情分支和精细演出用的。

---

## 一个最简单的上手流程

想先跑通一段两句话的对话，照这个来：

1. 新建一个 Story Sequence，Sequence Id 填 `test`
2. Pages 加一个元素（第一页）
3. 这页给个 Background Image，Page Type 选 Mixed，Wait For Input 勾上，Display Duration 留 -1
4. Text Blocks 加一个：Speaker 填 `小明`，Content 填台词，Display Mode 选 Typewriter
5. 想要第二句就再加一个 Text Block，或者再加一页
6. End Callback Type 先留 None

这样就能跑了。等熟了再去玩转场、音效和 Elements。
