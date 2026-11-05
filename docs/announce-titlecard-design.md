# Big White Title · 大字演出 — 设计 v0.1

- 目标游戏：RimWorld **1.6**（1.6.4871 public，全 DLC：Core / Royalty / Ideology / Biotech / Anomaly / Odyssey）
- 目标平台：Steam 创意工坊
- 状态：规划中，等待字体技术验证（M0）
- 参考：《Control》FBC 区域标题卡 / 《明日方舟：终末地》初探索大字

---

## 0. 一句话

**玩家踏入某些地方、或某些东西踏进来的时候，屏幕上砸下一块巨大的白色名字。**

---

## 1. 已定决策

| 项 | 决定 | 备注 |
|---|---|---|
| 形态 | **无底白字，直接压在游戏画面上** | 不用全屏黑幕 |
| 内容 | **只有名字**（地图名 / boss 名） | 不写正文、不写旁白；**只显示中文**，英文副标题默认关闭（保留开关） |
| 首版范围 | **2 张卡**（1 boss + 1 地图）验证观感 | 观感过关后再铺剩下的 9 张 |
| 技术路线 | 优先 TextMeshPro，退路 IMGUI | 见 §6 |

### 这个决定的代价（必须记住）

内容极其单薄，**mod 的成败 100% 在排版上**。同样的名字，字号/字重/字距/节奏差一档，就会从「机构宣告」掉成「调试日志」。

推论：**「同一触发只弹一次」几乎是必须的**。只有名字的情况下，重复出现会立刻暴露内容之薄。

---

## 2. 触发点清单

### Boss（Biotech，`BossgroupDef`，共 3 个）

| defName | 备注 |
|---|---|
| `Diabolus` | **MVP** —— 压迫感最强，通讯台可召唤，便于测试 |
| `Warqueen` | |
| `Apocriton` | |

登场信件走 `LetterArrive_Bossgroup`。

### 特殊地图（`MapGeneratorDef`，共 8 个）

| DLC | defName | 场景 |
|---|---|---|
| Odyssey | `Mechhive` | **MVP** —— 机械巢穴 |
| Odyssey | `AncientStockpile` | 古代仓库 |
| Odyssey | `InsectLair` | 虫巢 |
| Odyssey | `Asteroid` / `AsteroidBasic` | 小行星 |
| Odyssey | `OrbitalItemStash` | 轨道物资 |
| Odyssey | `OrbitalRelay` | 轨道中继 |
| Odyssey | `SettlementPlatform` | 定居点平台 |
| Anomaly | `MetalHell` | 金属地狱 |

触发键：`Find.CurrentMap.generatorDef.defName`。

> 注意：Anomaly 的「实体」不走 `BossgroupDef`，是另一套系统。若要给异常实体做卡，触发方式需单独设计（按 `PawnKindDef` 或实体 codex）。

---

## 3. 数据驱动：TitleCardDef

**文案不进 C#。** 理由：改文案不用重编译；支持 modded boss 只需加一个 Def；别人也能给本 mod 写卡。

```xml
<TitleCardDef>
  <defName>TC_Mechhive</defName>
  <trigger>
    <mapGenerator>Mechhive</mapGenerator>
    <!-- 或 <bossgroup>Diabolus</bossgroup> -->
  </trigger>
  <title>机械巢穴</title>
  <subtitle></subtitle>   <!-- 可选，默认留空：只显示中文 -->
  <accent>location</accent>   <!-- location=白字黑影 | hostile=白字红影 -->
  <oncePerGame>true</oncePerGame>
  <holdSeconds>2.2</holdSeconds>
</TitleCardDef>
```

---

## 4. 排版规格（1920×1080 基准，按屏高比例缩放）

| 元素 | 规格 |
|---|---|
| 主标题字号 | **230px 基准（屏高 21.3%）**，按屏高等比缩放，不写死像素 |
| 字重 | Black / Heavy |
| 字距 | 中文 +0.08em；含拉丁字母 +0.18em |
| 颜色 | `#FFFFFF`，不透明度 1.0 |
| 位置 | 水平居中，垂直 50% |
| 宽度钳制 | 最长名字超过屏宽 86% 时自动缩字号（防 4:3 / 长名字溢出） |
| 投影 | 偏移 (0,4)、模糊 18、黑 55% |
| 描边 | 1px，黑 25%（浅色地形兜底） |
| 细线 | **不画**（已否决） |
| 副标题 | **默认不显示**；需要时 26px、全大写、字距 +0.35em |
| 入场 | **瞬现**（无淡入、无缩放） |
| 停留 | **3.0s** |
| 退场 | **瞬灭**（无淡出） |
| 音效 | **暂时搁置**，见下面「音效」一节 |

### 关键坑：白字会在浅色地形上消失

雪地、沙漠、冰原上纯白字不可读。**解法不是加黑幕**（那就不是"无底"了），而是：

1. 字自带投影 + 1px 描边
2. **只在字周围一圈**加极轻的径向渐变压暗，不是全屏

### 语义：用颜色区分 boss 与地点

- 地点：纯白字 + 黑色投影
- Boss：白字 + **暗红投影**（`#8C0D14` @ 75%），字本身仍是白的

红色是 Control 的标志元素。副标题和细线都去掉之后，**投影是唯一的语义载体**——是否够用待观感验证。若不够，备选方案是字本身带一圈极淡的红晕，或干脆取消 boss / 地点的视觉区分。

---

## 5. 中断策略

| 场景 | 暂停 | 停留 | 说明 |
|---|---|---|---|
| 进入地图 | 可暂停 | 3.0s | 进场时玩家无压力 |
| Boss 登场 | **不暂停** | 3.0s | 正在被护卫追打，长停留会被骂；实测后可能缩短到 1.5–2s |

统一规则：**任意键跳过**，且记住"该玩家跳过了这一张"，同类以后走短版本。

### 音效

**运行时合成，不打包任何音频文件**：

- 130Hz → 42Hz 低频下滑正弦，指数衰减（主体）
- 60ms 噪声冲击层（"砸"的毛刺）
- 总长 0.9s，归一化到 0.9 峰值，尾部 80ms 淡出
- 播放走 2D `AudioSource`（`spatialBlend = 0`），音量 × `Prefs.VolumeGame`

好处：零授权风险、零资产管线、零体积。
风险：合成音的质感上限低于采样。若听感不合格，再考虑打包 CC0 采样。

### 音效：暂时搁置（2026-10-23）

参考视频（B 站 `BV1GcZXB1EKG`，《将〈控制〉〈心灵杀手2〉的大字糊脸效果放进终末地》）逆向分析结论：

| 时刻 | 起音 | 中高频/低频 | 判断 |
|---|---|---|---|
| 8.44s | 267ms | −29.9 dB | **配乐的下潜，不是音效** |
| 14.57s | 41ms | −13.3 dB | 像打击音效 |
| 33.12s | 41ms | −16.9 dB | 像打击音效，更亮 |
| 40.86s | 238ms | −23.8 dB | 又是配乐 |

**教训**：先确认参考对象是"音效"还是"配乐"，再动手仿。起音慢于 100ms 的基本都是配乐渐强。

已按 8.44s 的测量值合成 R1（双分音 43/75Hz、不扫频、τ=1.1s），频段分布贴合到 1–3dB，但听感仍不像——**频段对了不等于音色对了**，纯正弦做不出物理感。

现状：截取版（带 BGM）只在 `tools/ref/local-only/` 与 `BigWhiteTitle/Sounds/thud_local.wav`，仅供本地试听；**发布前必须删除**，否则工坊包里会带 Control/终末地的音频。

下一步备选：① 改仿 14.6s / 33.1s 的硬起音打击声；② 放弃参考自己设计；③ 找 CC0 的 cinematic boom 采样。

**已解决（2026-10-24）**：采用 Pixabay 的 `Cinematic boom & low rumble`（id `521720`，Pixabay Content License，可商用、免署名）。处理为 **4.0s / 单声道 44.1k / 峰值 0.89**，放在 `Sounds/thud.wav`，随 mod 发布。原先那个带 BGM 的 `thud_local.wav` 已删除。

实测对比：

| | 起音 | 到 −20dB | 20–80Hz | 80–200Hz | 200–500Hz |
|---|---|---|---|---|---|
| 参考（视频 8.44s） | 267ms | 1387ms | −0.1 | −16.5 | −30.9 |
| Pixabay 521720 | **41ms** | 1498ms | −1.2 | −6.4 | −19.9 |

差异是刻意保留的：参考那一下是**配乐渐强**，采样是**真实撞击**。我们要的是撞击，所以没把中频压下去强行对齐。

### 字体（已实现三级回退）

1. 系统字体，按候选列表找：`Microsoft YaHei` → `Microsoft YaHei UI` → `SimHei` → `Noto Sans SC` → `Source Han Sans SC` → `DengXian` → `SimSun`
2. 用 `Font.GetCharacterInfo('机')` 实测有没有中文字形（不能只看字体名）
3. 全失败 → 回退到 `GUI.skin.font`（游戏自带字体，**必定含中文字形**，否则游戏自己的中文界面就是方块）

**已知限制**：Unity 运行时无法从 TTF 文件创建 `Font`，只能用系统字体或已加载的字体对象。所以随 mod 打包字体这条路走不通（除非做 AssetBundle，需 Unity Editor）。英文版 Windows 若没装中文字体包，会走回退，字重可能只有常规体——**这是本 mod 的真实限制，需写进工坊说明**。

---

## 6. 技术路线与风险

### 结论（2026-10-22 实测）：走 IMGUI

TMP 路线**已排除**，实测证据：

- 三个 TMP shader 都存在（`TextMeshPro/Distance Field`、`Mobile/Distance Field`、`Sprite` 全部 `Shader.Find != null`）
- 但 `TMP_FontAsset.CreateFontAsset(Font)` 返回 `null`，日志给出原因：
  `Unable to load font face for [Microsoft YaHei]. Make sure "Include Font Data" is enabled in the Font Import Settings.`

原因是 `Font.CreateDynamicFontFromOSFont` 造出的是**动态字体**，没有可读的字体数据，TMP 的 `FontEngine.LoadFontFace` 读不了。要用 TMP 必须提供 Unity Editor 烘焙的字体资源（或 AssetBundle），本项目没有 Unity 环境，不做。

### 路线 B：IMGUI（已采用）

- `GUI.Label` + `GUIStyle.fontSize` 可以放大，但**不支持字距**
- 变通：逐字符绘制、手动计算 x 偏移，可伪造 letter-spacing
- 代价：质感掉一档，中文描边要靠多偏移绘制

### 字体

- 候选：**思源黑体 Heavy**（Source Han Sans Heavy）/ **Noto Sans SC Black**，均为 OFL，可随 mod 打包
- 打包为 AssetBundle 或 TMP SDF font asset
- **不可依赖** `Font.CreateDynamicFontFromOSFont`（依赖玩家系统字体，发布不可靠）

### 必须最先验证的

1. TMP 能否在 RimWorld 中初始化并绘制中文
2. 120px 中文的重叠笔画在游戏分辨率下是否糊
3. 浅色地形上的可读性（雪地/沙漠实测）

---

### 相机运镜（2026-10-23 实测）

定稿参数：**缩放 3.0s / 平移 2.0s / 推敌 size 18 / 运镜期间自动暂停**

| 结论 | 证据 |
|---|---|
| 不能用 `PanToMapLocAndSize` | 它按移动距离缩放时长（`FullDurationPanDistance`）；纯缩放时位置几乎不动，传 3 秒实际只跑 1 秒。改为自己逐帧 `SetRootPosAndSize` + smoothstep |
| **"缩放到整张地图"不可能** | `MaxAltitude` 是 `const 65f`，反射报 "Cannot set a constant field"。250×250 地图需要 size 125 |
| 安全的缩放上限 | `size × cellPx = 屏高 / 3` → 可见高 = 3·size，可见宽 = 3·size·宽高比；故 `safe = min(图宽/(3·宽高比), 图高/3) × 0.95`，钳制在 [15, 65] |
| 超出安全值会看到什么 | 地图外的**暗红底色**铺满画面 |
| `CurrentViewRect` 不能判越界 | 它被钳制在地图范围内，永远显示"没越界" |
| 自动暂停 | `TickManager.CurTimeSpeed = TimeSpeed.Paused`，结束恢复原速度；动画用 `unscaledDeltaTime`，暂停期间照常推进 |
| 暂停的安全意义 | 镜头被抢走时玩家看不见战场，暂停才不会有人因此送命 |

已知待决：暂停时长叠加问题——运镜 3s + 白字 3s = 游戏停 6 秒，玩家可能觉得"卡住了"。备选：白字期间不暂停，或缩短暂停。

---

## 7. 里程碑

| 阶段 | 内容 | 预估 | 状态 |
|---|---|---|---|
| M0 | 字体探针：同屏对比 TMP / IMGUI 各画一次大字，定技术路线 | 半天 | **完成**：IMGUI 胜出，TMP 排除 |
| M1 | 2 张卡跑通：`Diabolus` + `Mechhive`，含触发、动画、跳过、once 逻辑 | 3–5 天 | |
| M2 | 观感评审 → 调参数 → 铺满剩下 9 张 | 3 天 | |
| M3 | 音效、设置项（字号/开关/时长）、中英文、工坊发布 | 3 天 | |

---

## 8. 待定

1. ~~mod 名~~ 已定：**Big White Title**，当前 packageId `bigwhitetitle.mod`
2. ~~作者名~~ 已定：**MrTitanium**，packageId `MrTitanium.BigWhiteTitle`（定稿，发布后不可改）
3. 音效做不做（做则需找 CC0 低频音源）
4. 是否做「档案馆」（11/11 收集界面）——把一次性演出变成可回看的内容，工作量 +30%
5. 是否支持 modded boss（如 RimWorld of Magic 等）——需实测别的 mod 的 boss 如何生成

---

## 9. 与归途的关系

独立项目，无代码复用。归档项目见 [guitu-return-service-design.md](guitu-return-service-design.md)。
