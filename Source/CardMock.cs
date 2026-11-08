using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Verse;

namespace BigWhiteTitle
{
    [StaticConstructorOnStartup]
    public static class MockBootstrap
    {
        static MockBootstrap()
        {
            LongEventHandler.ExecuteWhenFinished(Setup);
        }

        static void Setup()
        {
            try
            {
                GameObject go = new GameObject("BigWhiteTitle_Mock");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<CardMockBehaviour>();
                BwtLog.Info(Prefs.DevMode
                    ? "[BWT] card ready — Ctrl+Alt+B 播放 · M 首领 · [ ] 字号 · P 常显 · K 诊断 · J 音效"
                    : "[BWT] card ready");
            }
            catch (Exception e)
            {
                Log.Error("[BWT] mock create failed: " + e);
            }
        }
    }

    /// <summary>
    /// 排版 + 时序 + 音效样张。
    ///
    /// 已定：
    ///   - 瞬现 → 停 3 秒 → 瞬灭（没有淡入淡出，没有缩放）
    ///   - IMGUI + 系统动态字体；系统字体不可用时回退到游戏自带字体（一定有中文字形）
    ///   - 音效运行时合成，不依赖任何外部音频资源
    /// </summary>
    public class CardMockBehaviour : MonoBehaviour
    {
        const string LocTitle = "机械巢穴";
        const string LocSub = "MECHHIVE";
        const string BossTitle = "战争女王";
        const string BossSub = "WARQUEEN";

        /// 已知最长的名字，用于宽度钳制（超过屏宽 86% 就缩字号）。
        const string LongestTitle = "阿波克里顿";

        /// 1080p 下的基准字号；实际 = 基准 × (屏高/1080)
        const int BasePxDefault = 230;
        const int BasePxMin = 80;
        const int BasePxMax = 360;

        /// 停留时长：到点直接消失
        const float HoldDur = 3.0f;

        static readonly Color Accent = new Color(0.70f, 0.13f, 0.16f, 1f);
        static readonly Color ShadowNormal = new Color(0f, 0f, 0f, 0.55f);
        static readonly Color ShadowHostile = new Color(0.55f, 0.05f, 0.08f, 0.75f);

        static readonly Vector2[] OutlineOffsets =
        {
            new Vector2(-1, 0), new Vector2(1, 0), new Vector2(0, -1), new Vector2(0, 1),
            new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1)
        };

        static readonly string[] FontCandidates =
        {
            "Microsoft YaHei", "Microsoft YaHei UI", "微软雅黑", "SimHei",
            "Noto Sans SC", "Source Han Sans SC", "DengXian", "SimSun"
        };

        static Font sFont;
        static string sFontSource = "(未初始化)";
        static GUIStyle sTitle;
        static GUIStyle sSub;
        static GUIStyle sDiag;
        static bool sFailed;
        static int sLastEff = -1;

        static AudioSource sSource;
        static AudioClip sThud;

        static int sBasePx = BasePxDefault;
        static bool sPreview = false;    // 默认不常显：只在触发时出现
        static bool sHostile = false;
        static bool sShowSub = false;
        static float sPlayT = -1f;
        static string sOverrideTitle;
        static string sOverrideKey;

        /// <summary>播放一次白字（含音效），使用当前选中的示例文字。</summary>
        public static void PlayCard()
        {
            sOverrideKey = null;
            sOverrideTitle = null;
            sPlayT = 0f;
            PlayThud();
        }

        /// <summary>播放一次白字：优先用 key 对应的标题图，没有图就显示 fallbackText。</summary>
        public static void PlayCard(string key, string fallbackText)
        {
            sOverrideKey = key;
            sOverrideTitle = fallbackText;
            sPlayT = 0f;
            PlayThud();
        }

        /// <summary>立即收起白字。</summary>
        public static void StopCard()
        {
            sPlayT = -1f;
            sOverrideTitle = null;
            sOverrideKey = null;
        }

        void Awake()
        {
            try
            {
                sSource = gameObject.AddComponent<AudioSource>();
                sSource.playOnAwake = false;
                sSource.spatialBlend = 0f;      // 2D：跟着镜头，不做空间衰减

                sThud = TryLoadLocalWav();
                // 找不到 wav 就静音，不再用程序化合成的替身音
                if (sThud == null)
                    BwtLog.Info("[BWT] 音效：thud.wav 未找到，本次静音");
                BwtLog.Info("[BWT] thud clip = " + (sThud == null ? "NULL" : sThud.length.ToString("0.00") + "s"));
            }
            catch (Exception e)
            {
                Log.Error("[BWT] audio init failed: " + e);
            }
        }

        void Update()
        {
            if (sPlayT < 0f) return;
            sPlayT += Time.unscaledDeltaTime;
            if (sPlayT >= HoldDur) sPlayT = -1f;
        }

        void OnGUI()
        {
            try
            {
                Build();
                if (sFailed) return;

                if (sPlayT >= 0f || sPreview) DrawCard();
            }
            catch (Exception ex)
            {
                Log.ErrorOnce("[BWT] OnGUI: " + ex, 823712);
            }
        }

        // ---------- 字体 ----------

        static bool FontHasCjk(Font f)
        {
            if (f == null) return false;
            try
            {
                f.RequestCharactersInTexture("机械巢穴", 64, FontStyle.Bold);
                CharacterInfo ci;
                return f.GetCharacterInfo('机', out ci, 64, FontStyle.Bold);
            }
            catch
            {
                return false;
            }
        }

        static void ResolveFont()
        {
            Font os = Font.CreateDynamicFontFromOSFont(FontCandidates, 120);
            if (FontHasCjk(os))
            {
                sFont = os;
                sFontSource = "系统字体: " + os.name;
                return;
            }

            // 回退：游戏自带的字体一定有中文字形（否则游戏的中文界面也渲染不出来）
            Font game = GUI.skin != null ? GUI.skin.font : null;
            if (FontHasCjk(game))
            {
                sFont = game;
                sFontSource = "游戏自带: " + game.name;
                return;
            }

            sFontSource = "无可用中文字体";
            Log.Error("[BWT] 系统字体和游戏字体都不含中文字形，卡片无法绘制");
            sFailed = true;
        }

        static void Build()
        {
            if (sFailed) return;

            if (sFont == null)
            {
                ResolveFont();
                if (sFailed) return;

                BwtLog.Info("[BWT] font = " + sFontSource);
                sTitle = NewStyle(sFont, FontStyle.Bold, TextAnchor.MiddleLeft);
                sSub = NewStyle(sFont, FontStyle.Normal, TextAnchor.MiddleLeft);
                sDiag = NewStyle(sFont, FontStyle.Normal, TextAnchor.UpperLeft);
                sDiag.fontSize = 20;
                sDiag.normal.textColor = new Color(0.92f, 0.92f, 0.92f, 1f);
            }

            int eff = Mathf.Max(24, Mathf.RoundToInt(sBasePx * (Screen.height / 1080f)));
            if (eff == sLastEff) return;
            sLastEff = eff;

            sTitle.fontSize = eff;
            sSub.fontSize = Mathf.Max(10, Mathf.RoundToInt(eff * 0.22f));

            float limit = Screen.width * 0.86f;
            float w = MeasureSpaced(LongestTitle, sTitle, 0.08f * eff);
            if (w > limit && w > 0f)
            {
                int clamped = Mathf.Max(24, Mathf.FloorToInt(eff * limit / w));
                sTitle.fontSize = clamped;
                sSub.fontSize = Mathf.Max(10, Mathf.RoundToInt(clamped * 0.22f));
                BwtLog.Info(string.Format("[BWT] 宽度钳制 {0}px -> {1}px", eff, clamped));
            }

            BwtLog.Info(string.Format("[BWT] title {0}px = {1:0.0}% 屏高 (base {2}, {3}x{4})",
                sTitle.fontSize, sTitle.fontSize * 100f / Screen.height, sBasePx, Screen.width, Screen.height));
        }

        static GUIStyle NewStyle(Font f, FontStyle fs, TextAnchor anchor)
        {
            GUIStyle s = new GUIStyle();
            s.font = f;
            s.fontStyle = fs;
            s.alignment = anchor;
            s.clipping = TextClipping.Overflow;
            s.richText = false;
            s.normal.textColor = Color.white;
            return s;
        }

        // ---------- 音效 ----------

        /// <summary>
        /// 模组目录下优先用 Sounds/thud.wav（正式音效），其次 Sounds/thud_local.wav（本地覆盖），
        /// 都没有就退回运行时合成音。
        /// </summary>
        static AudioClip TryLoadLocalWav()
        {
            try
            {
                string soundsDir = Path.Combine(BwtPaths.ModRoot, "Sounds");
                string path = Path.Combine(soundsDir, "thud.wav");
                if (!File.Exists(path)) path = Path.Combine(soundsDir, "thud_local.wav");
                if (!File.Exists(path)) return null;

                byte[] b = File.ReadAllBytes(path);
                if (b.Length < 44) return null;
                if (b[0] != 'R' || b[1] != 'I' || b[2] != 'F' || b[3] != 'F') return null;
                if (b[8] != 'W' || b[9] != 'A' || b[10] != 'V' || b[11] != 'E') return null;

                int channels = 0, rate = 0, bits = 0, dataOff = -1, dataLen = 0;
                int i = 12;
                while (i + 8 <= b.Length)
                {
                    string id = new string(new[] { (char)b[i], (char)b[i + 1], (char)b[i + 2], (char)b[i + 3] });
                    int sz = BitConverter.ToInt32(b, i + 4);
                    if (id == "fmt ")
                    {
                        channels = BitConverter.ToInt16(b, i + 10);
                        rate = BitConverter.ToInt32(b, i + 12);
                        bits = BitConverter.ToInt16(b, i + 22);
                    }
                    else if (id == "data")
                    {
                        dataOff = i + 8;
                        dataLen = Math.Min(sz, b.Length - dataOff);
                        break;
                    }
                    i += 8 + sz + (sz & 1);
                }

                if (dataOff < 0 || bits != 16 || channels < 1 || rate < 8000) return null;

                int frames = dataLen / (2 * channels);
                float[] data = new float[frames];
                for (int k = 0; k < frames; k++)
                {
                    float sum = 0f;
                    for (int c = 0; c < channels; c++)
                        sum += BitConverter.ToInt16(b, dataOff + (k * channels + c) * 2) / 32768f;
                    data[k] = sum / channels;
                }

                AudioClip clip = AudioClip.Create("BWT_Local", frames, 1, rate, false);
                clip.SetData(data, 0);
                BwtLog.Info("[BWT] 音效：加载 Sounds/" + Path.GetFileName(path) + " ("
                    + (frames / (float)rate).ToString("0.00") + "s)");
                return clip;
            }
            catch (Exception e)
            {
                Log.Warning("[BWT] 加载 thud_local.wav 失败：" + e.Message);
                return null;
            }
        }

        static void PlayThud()
        {
            if (sSource == null || sThud == null) return;
            float v = 0.75f;
            try { v *= Mathf.Clamp01(Prefs.VolumeGame); } catch { }
            sSource.PlayOneShot(sThud, v);
        }

        // ---------- 画卡片 ----------

        static void DrawCard()
        {
            float k = Screen.height / 1080f;

            // 优先用标题图；没有图才退回逐字绘制
            Texture2D tex = sOverrideKey != null ? TitleTexture.Get(sOverrideKey) : null;
            if (tex != null)
            {
                DrawTextureCard(tex, k);
                return;
            }

            string title = sOverrideTitle ?? (sHostile ? BossTitle : LocTitle);
            string sub = sHostile ? BossSub : LocSub;
            Color shadow = sHostile ? ShadowHostile : ShadowNormal;

            float titleSpace = 0.08f * sTitle.fontSize;
            float titleW = MeasureSpaced(title, sTitle, titleSpace);

            float cx = Screen.width * 0.5f;
            float cy = Screen.height * 0.5f;
            float titleH = sTitle.fontSize * 1.30f;
            float titleY = cy - titleH * 0.5f;

            DrawSpaced(title, sTitle, cx - titleW * 0.5f, titleY, titleSpace, true, shadow);

            if (sShowSub)
            {
                float subSpace = 0.35f * sSub.fontSize;
                float subW = MeasureSpaced(sub, sSub, subSpace);
                float subY = titleY + titleH + 18f * k;

                Color old = sSub.normal.textColor;
                sSub.normal.textColor = Accent;
                DrawSpaced(sub, sSub, cx - subW * 0.5f, subY, subSpace, false, ShadowNormal);
                sSub.normal.textColor = old;
            }
        }

        /// <summary>
        /// 标题图按 460px 字号渲染，对应屏幕上的 230px 基准，
        /// 所以显示缩放系数 = 0.5 × (屏高/1080)。
        /// 阴影已烘焙进图里，这里只管贴图和居中。
        /// </summary>
        static void DrawTextureCard(Texture2D tex, float k)
        {
            float scale = 0.5f * k;
            float w = tex.width * scale;
            float h = tex.height * scale;

            float maxW = Screen.width * 0.90f;
            if (w > maxW)
            {
                float f = maxW / w;
                w *= f;
                h *= f;
            }

            Rect r = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
            GUI.DrawTexture(r, tex, ScaleMode.StretchToFill, true);
        }

        static float MeasureSpaced(string text, GUIStyle style, float spacing)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            float total = spacing * (text.Length - 1);
            for (int i = 0; i < text.Length; i++)
                total += style.CalcSize(new GUIContent(text[i].ToString())).x;
            return total;
        }

        static void DrawSpaced(string text, GUIStyle style, float x0, float y, float spacing,
                               bool outline, Color shadowColor)
        {
            float k = Screen.height / 1080f;
            float h = style.fontSize * 1.50f;
            float o = Mathf.Max(1f, 1.2f * k);
            float shadowDy = 4f * k;

            Color original = style.normal.textColor;
            float x = x0;

            for (int i = 0; i < text.Length; i++)
            {
                string ch = text[i].ToString();
                float w = style.CalcSize(new GUIContent(ch)).x;

                if (ch != " ")
                {
                    if (outline)
                    {
                        style.normal.textColor = new Color(0f, 0f, 0f, 0.30f);
                        for (int d = 0; d < OutlineOffsets.Length; d++)
                        {
                            GUI.Label(new Rect(
                                x + OutlineOffsets[d].x * o,
                                y + OutlineOffsets[d].y * o,
                                w + 12f, h), ch, style);
                        }
                    }

                    style.normal.textColor = shadowColor;
                    GUI.Label(new Rect(x, y + shadowDy, w + 12f, h), ch, style);

                    style.normal.textColor = original;
                    GUI.Label(new Rect(x, y, w + 12f, h), ch, style);
                }

                x += w + spacing;
            }

            style.normal.textColor = original;
        }

        // ---------- 诊断 ----------

        static void DrawDiag()
        {
            float k = Screen.height / 1080f;
            string s = string.Format(
                "Ctrl+Alt : B 播放({0:0.00}s) · M 首领({1}) · [ ] 字号 {2} · P 常显({3}) · K 关闭\n" +
                "{4}x{5}  k={6:0.000}  标题 {7}px ({8:0.0}% 屏高)  {9}",
                sPlayT, sHostile ? "是" : "否", sBasePx, sPreview ? "开" : "关",
                Screen.width, Screen.height,
                k, sTitle.fontSize, sTitle.fontSize * 100f / Screen.height, sFontSource);
            GUI.Label(new Rect(16f, Screen.height - 72f, 1200f, 64f), s, sDiag);
        }
    }
}
