# 雷霆白字 / Big White Title

进入特殊地图、特殊群系，或首领登场时，一块巨大的白色标题压上画面——类似《Control》或《明日方舟：终末地》的出场方式。

*Huge white titles slam onto the screen when you enter a special map or biome, or when a boss arrives.*

- 支持 **RimWorld 1.6 与 1.7**
- 界面与卡片跟随游戏语言（中文 / English）
- 其他模组可通过 `TitleCardDef` 追加自己的标题卡，无需依赖本模组代码

工坊页面：<https://steamcommunity.com/sharedfiles/filedetails/?id=3829573390>

---

## 目录结构

```
BigWhiteTitle/
├─ LoadFolders.xml          按游戏版本选择子目录
├─ About/                   About.xml、Preview.png
├─ Defs/                    TitleCardDef 定义
├─ Languages/               中英文 Keyed 翻译
├─ Sounds/thud.wav          音效
├─ Textures/Titles/         标题卡图片，按来源分子目录
│    Vanilla/               原版地图、群系、首领
│    AncientAmorphousThreat/
│    DeadMansSwitch/
│    AlphaMechs/
├─ 1.6/Assemblies/          1.6 用的 DLL（Unity 引擎）
├─ 1.7/Assemblies/          1.7 用的 DLL（Waylith 引擎）
└─ Source/                  C# 源码与构建脚本
```

### 为什么有两个 DLL

1.6 与 1.7 的运行时完全不同：

| | 1.6 | 1.7 |
|---|---|---|
| 引擎 | Unity | Waylith（Ludeon 自研） |
| 运行时 | Mono / .NET Framework | .NET 10 |
| 游戏程序集位置 | `RimWorldWin64_Data/Managed` | 游戏根目录 |
| Unity 兼容层 | `UnityEngine.*.dll` | `UnityStubs.dll`（编译用桩） |

同一个 DLL 无法同时在两边加载，因此 `LoadFolders.xml` 按版本二选一：

```xml
<loadFolders>
  <v1.6><li>/</li><li>1.6</li></v1.6>
  <v1.7><li>/</li><li>1.7</li></v1.7>
</loadFolders>
```

---

## 构建

需要 [.NET SDK](https://dotnet.microsoft.com/download)（用于调用 Roslyn 编译器）。不依赖 MSBuild / NuGet。

```powershell
cd Source

# 两个版本都编
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Target all

# 只编其中一个
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Target 1.7
```

参数：

| 参数 | 说明 |
|---|---|
| `-Target` | `1.6` / `1.7` / `all`（默认 all） |
| `-GameDir` | 游戏安装目录，默认 `D:\steam\steamapps\common\RimWorld` |
| `-Dotnet` | dotnet 可执行文件路径 |

### 1.6 的特殊要求

游戏更新到 1.7 后，1.6 的 `RimWorldWin64_Data/Managed` 会被删除。此时构建脚本会退回到本地备份目录：

```
<仓库上级>/refs/1.6/Managed/
```

取得方式：Steam → RimWorld → 属性 → 测试版 → 切到 1.6 分支，复制该目录，再切回来。

---

## 添加标题卡

### 1. 生成图片

```powershell
python tools/add_card.py <key> 中文名 "ENGLISH NAME" boss
```

`key` 可带子目录，例如 `MyMod/MyBoss`。输出到 `Textures/Titles/<key>.png` 与 `<key>_en.png`。
不加 `boss` 参数则使用地图卡的黑色投影（首领卡用红色投影）。

需要 `pip install pillow` 与字体文件（见下方「字体」）。

### 2. 添加 Def

在 `Defs/TitleCardDefs/` 下新建或追加：

```xml
<BigWhiteTitle.TitleCardDef>
  <defName>BWT_Card_MyBoss</defName>
  <kind>Boss</kind>                        <!-- Map / Biome / Boss -->
  <target>MyMod_MyBossKind</target>        <!-- MapGeneratorDef / BiomeDef / PawnKindDef 的 defName -->
  <texture>MyMod/MyBoss</texture>          <!-- 图片路径，不带 .png -->
  <text>我的首领</text>
  <textEn>MY BOSS</textEn>
</BigWhiteTitle.TitleCardDef>
```

`texture` 省略时退回用 `target` 作路径。英文图会自动找 `<texture>_en.png`，缺失则回退中文图。

### 3. 生效

重启游戏，或在开发者模式下执行 `Reload defs`。

### 关于第三方模组

- `kind` / `target` 指向任意模组的 Def 即可，**没有硬依赖**：目标不存在时条目不匹配任何东西，不会报错
- 图片可以放在**你自己的模组**里：`Textures/Titles/<路径>.png`，`texture` 填 `Titles/<路径>`
- 触发判断只看 Def 是否存在条目，不依赖 `isBoss` 标记

---

## 字体

卡片图片使用 [HarmonyOS Sans](https://developer.harmonyos.com/cn/docs/design/font-0000001157868583) 渲染，**字体文件不随仓库分发**（体积大，且许可要求使用前自行确认）。

需要生成新卡时，把字体放到：

```
tools/fonts/hmos/HarmonyOS_Sans_SC_Black.ttf   （中文）
tools/fonts/hmos/HarmonyOS_Sans_Black.ttf      （英文 / 拉丁）
```

替代方案（OFL 许可，可自由分发）：思源黑体 / Noto Sans SC 的 Heavy 或 Black 字重，改 `tools/add_card.py` 里的 `FONT_ZH` / `FONT_EN` 即可。

---

## 已知说明

- **1.6 的 DLL 未包含最新修复**：`TitleRegistry` 首次调用会抛一次空引用（自愈，不影响功能）。重编 1.6 需要上文所述的 `refs/1.6/Managed` 备份
- 演出期间会接管镜头并可选暂停；设置页可调整冷却、镜头时长、「每种只显示一次」等

---

## 许可

本项目采用 **[CC BY-NC-SA 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/)** 授权，完整法律文本见 [LICENSE](LICENSE)。

简单说：

- ✅ 可以随意使用、修改、再分发
- ✅ 可以放进自己的整合包
- ⚠️ 必须署名（MrTitanium / 雷霆白字）
- ❌ 不得用于商业用途
- ⚠️ 修改后的版本必须使用相同的许可发布

### 例外

**`Sounds/thud.wav`** 来自 [Pixabay](https://pixabay.com/)，遵循 [Pixabay Content License](https://pixabay.com/service/license-summary/)，**不适用**上述 CC 条款。该许可允许将其作为作品的一部分分发，但不允许单独再分发原始音频文件。

**卡片图片中使用的 HarmonyOS Sans 字体**不随本项目分发，仅有渲染后的 PNG 随附。字体本身的授权请参阅 [HarmonyOS 开发者文档](https://developer.harmonyos.com/cn/docs/design/font-0000001157868583)。
