using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace BigWhiteTitle
{
    [StaticConstructorOnStartup]
    public static class CinematicBootstrap
    {
        static CinematicBootstrap()
        {
            LongEventHandler.ExecuteWhenFinished(Setup);
        }

        static void Setup()
        {
            try
            {
                GameObject go = new GameObject("BWT_Cinematic");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<CinematicBehaviour>();
                BwtLog.Info(Prefs.DevMode
                    ? "[BWT] cinematic ready — Ctrl+Alt+Enter 入场 · N 首领 · C 仅缩放 · V 平移 · L 状态 · Y 重检地图 · D boss列表 · X 清记录"
                    : "[BWT] cinematic ready");
            }
            catch (Exception e)
            {
                Log.Error("[BWT] cinematic create failed: " + e);
            }
        }
    }

    /// <summary>
    /// 演出时间线。把相机、白字、暂停串成一条。
    ///
    /// 入场：暂停 → 拉远 3.0s → 白字 → 3.0s 后拉回 2.7s → 恢复（全长 8.7s）
    /// 首领：暂停 → 推向首领 2.0s（size 18）→ 白字 → 恢复（全长 5.0s）
    /// </summary>
    public class CinematicBehaviour : MonoBehaviour
    {
        enum Seq { None, MapEntry, Boss }

        // ---- 参数：全部读写模组设置，改了就存盘 ----
        static BwtSettings Cfg { get { return BwtMod.Settings; } }

        static float sZoomDur
        {
            get { return Cfg != null ? Cfg.zoomDur : 3.0f; }
            set { if (Cfg != null) Cfg.zoomDur = value; }
        }

        static float sPanDur
        {
            get { return Cfg != null ? Cfg.panDur : 2.0f; }
            set { if (Cfg != null) Cfg.panDur = value; }
        }

        static float sBossSize
        {
            get { return Cfg != null ? Cfg.bossSize : 18f; }
            set { if (Cfg != null) Cfg.bossSize = value; }
        }

        static float sBossDelay
        {
            get { return Cfg != null ? Cfg.bossDelay : 1.0f; }
            set { if (Cfg != null) Cfg.bossDelay = value; }
        }

        static float sMapDelay
        {
            get { return Cfg != null ? Cfg.mapDelay : 2.0f; }
            set { if (Cfg != null) Cfg.mapDelay = value; }
        }

        static bool sOncePerBoss
        {
            get { return Cfg == null || Cfg.oncePerBoss; }
            set { if (Cfg != null) Cfg.oncePerBoss = value; }
        }

        static bool sOncePerMap
        {
            get { return Cfg == null || Cfg.oncePerMap; }
            set { if (Cfg != null) Cfg.oncePerMap = value; }
        }

        static bool sOncePerBiome
        {
            get { return Cfg == null || Cfg.oncePerBiome; }
            set { if (Cfg != null) Cfg.oncePerBiome = value; }
        }

        static float sMapCooldown
        {
            get { return Cfg != null ? Cfg.mapCooldown : 30f; }
            set { if (Cfg != null) Cfg.mapCooldown = value; }
        }

        static float sBossCooldown
        {
            get { return Cfg != null ? Cfg.bossCooldown : 30f; }
            set { if (Cfg != null) Cfg.bossCooldown = value; }
        }

        /// <summary>两种演出各算各的冷却，互不影响。</summary>
        static float sLastMapSeqEnd = -999f;
        static float sLastBossSeqEnd = -999f;

        static bool InCooldown(float end, float dur)
        {
            if (dur <= 0.01f) return false;
            return Time.realtimeSinceStartup - end < dur;
        }

        static float CooldownLeft(float end, float dur)
        {
            return Mathf.Max(0f, dur - (Time.realtimeSinceStartup - end));
        }

        static bool MapInCooldown() { return InCooldown(sLastMapSeqEnd, sMapCooldown); }
        static bool BossInCooldown() { return InCooldown(sLastBossSeqEnd, sBossCooldown); }
        static float MapCooldownLeft() { return CooldownLeft(sLastMapSeqEnd, sMapCooldown); }
        static float BossCooldownLeft() { return CooldownLeft(sLastBossSeqEnd, sBossCooldown); }

        /// <summary>记录当前序列的结束时刻（要在把 sSeq 置空之前调用）。</summary>
        static void MarkSeqEnd()
        {
            float now = Time.realtimeSinceStartup;
            if (sSeq == Seq.MapEntry) sLastMapSeqEnd = now;
            else if (sSeq == Seq.Boss) sLastBossSeqEnd = now;
        }

        /// <summary>地图至少运行这么多游戏刻才开演（60 刻 ≈ 游戏内 1 秒）。</summary>
        const int MapSettleTicks = 30;

        // ---- 时间线 ----
        static Seq sSeq = Seq.None;
        static int sStep;
        static float sT0;
        static Vector3 sHomePos;
        static float sHomeSize = 21f;
        static Vector3 sTarget;
        static string sCardText = "机械巢穴";
        static string sCardKey;

        // ---- 自动触发 ----
        static bool sAuto
        {
            get { return Cfg == null || Cfg.enabled; }
            set { if (Cfg != null) Cfg.enabled = value; }
        }
        static Map sLastMap;
        static bool sFirstMapSeen;
        static Map sPendingMap;
        static float sPendingAt;
        static TitleCardDef sPendingDef;
        static float sNextBossScan;
        static Pawn sPendingBoss;
        static float sPendingBossAt;

        void Update()
        {
            CameraPan.Tick();
            CheckAuto();

            if (sSeq == Seq.None) return;
            float t = Time.realtimeSinceStartup - sT0;

            if (sSeq == Seq.MapEntry)
            {
                if (sStep == 1 && t >= sZoomDur)
                {
                    CardMockBehaviour.PlayCard(sCardKey, sCardText);
                    BwtLog.Info("[BWT] 序列：白字「" + sCardText + "」 key=" + sCardKey);
                    sStep = 2;
                }
                else if (sStep == 2 && t >= sZoomDur + 3.0f)
                {
                    float back = sZoomDur * 0.9f;
                    CameraPan.Start(sHomePos, sHomeSize, back);
                    MusicDuck.FadeTo(1f, 1.5f);
                    BwtLog.Info("[BWT] 序列：拉回 " + back.ToString("0.0") + "s，BGM 恢复");
                    sStep = 3;
                }
                else if (sStep == 3 && t >= sZoomDur * 1.9f + 3.0f)
                {
                    Finish("入场");
                }
            }
            else if (sSeq == Seq.Boss)
            {
                if (sStep == 1 && t >= sPanDur)
                {
                    CardMockBehaviour.PlayCard(sCardKey, sCardText);
                    BwtLog.Info("[BWT] 序列：白字「" + sCardText + "」 key=" + sCardKey);
                    sStep = 2;
                }
                else if (sStep == 2 && t >= sPanDur + 3.0f)
                {
                    MusicDuck.FadeTo(1f, 1.2f);
                    Finish("首领");
                }
            }
        }

        void LateUpdate()
        {
            // 放在 LateUpdate：排在引擎的 MusicUpdate 之后，写入才不会被覆盖
            MusicDuck.Tick();
        }

        // ---------- 序列 ----------

        static void PlayMapEntry(Map map, string key, string title)
        {
            if (map == null || map != Find.CurrentMap) { Log.Warning("[BWT] 没有当前地图"); return; }
            CameraDriver cd = Find.CameraDriver;
            if (cd == null) return;

            Abort();

            sHomePos = CameraPan.CurrentPos();
            sHomeSize = cd.RootSize;
            sTarget = map.Center.ToVector3();
            sCardKey = key;
            sCardText = title;

            float safe = SafeZoomSize(map);
            BwtLog.Info(string.Format(
                "[BWT] 入场序列开始：安全 size={0:0.0}，缩放 {1:0.0}s，白字 3.0s，拉回 {2:0.0}s",
                safe, sZoomDur, sZoomDur * 0.9f));

            PauseGuard.Hold();
            MusicDuck.FadeTo(0f, sZoomDur);
            CameraPan.Start(sTarget, safe, sZoomDur);

            sSeq = Seq.MapEntry;
            sStep = 1;
            sT0 = Time.realtimeSinceStartup;
        }

        static void PlayBoss()
        {
            Map map = Find.CurrentMap;
            if (map == null) { Log.Warning("[BWT] 没有当前地图"); return; }

            Pawn boss = NearestHostile(map);
            if (boss == null) { BwtLog.Info("[BWT] 地图上没有敌对单位"); return; }

            PlayBoss(boss);
        }

        static void PlayBoss(Pawn boss)
        {
            if (boss == null || boss.Map == null || boss.Map != Find.CurrentMap) return;

            Abort();

            sTarget = boss.Position.ToVector3();
            TitleCardDef bossDef = boss.kindDef != null ? TitleRegistry.Boss(boss.kindDef.defName) : null;
            sCardKey = bossDef != null ? CardKey(bossDef.TextureKey) : null;
            sCardText = bossDef != null ? bossDef.TextFor(IsChinese) : boss.LabelShort;
            BwtLog.Info(string.Format(
                "[BWT] 首领序列开始：{0} @ {1}，推向 size={2:0}，{3:0.0}s，白字 3.0s",
                boss.LabelShort, boss.Position, sBossSize, sPanDur));

            PauseGuard.Hold();
            MusicDuck.FadeTo(0f, sPanDur);
            CameraPan.Start(sTarget, sBossSize, sPanDur);

            sSeq = Seq.Boss;
            sStep = 1;
            sT0 = Time.realtimeSinceStartup;
        }

        static void ZoomOnly()
        {
            Map map = Find.CurrentMap;
            if (map == null) { Log.Warning("[BWT] 没有当前地图"); return; }
            Abort();
            BwtLog.Info("[BWT] 仅缩放 " + sZoomDur.ToString("0.0") + "s");
            CameraPan.Start(map.Center.ToVector3(), SafeZoomSize(map), sZoomDur);
        }

        static void Abort()
        {
            if (sSeq != Seq.None) BwtLog.Info("[BWT] 上一个序列被打断");
            MarkSeqEnd();
            sSeq = Seq.None;
            sStep = 0;
            PauseGuard.Release();
            MusicDuck.Reset();
            CameraPan.Cancel();
        }

        static void Finish(string name)
        {
            BwtLog.Info("[BWT] 序列结束：" + name);
            MarkSeqEnd();
            sSeq = Seq.None;
            sStep = 0;
            PauseGuard.Release();
        }

        // ---------- 自动触发 ----------

        static readonly HashSet<int> sSeenBosses = new HashSet<int>();

        /// <summary>地图已经运行了多少游戏刻（加载期间游戏暂停，tick 不走）。</summary>
        static int TicksSinceGen(Map m)
        {
            if (m == null) return 0;
            TickManager tm = Find.TickManager;
            if (tm == null) return 0;
            return tm.TicksGame - m.generationTick;
        }

        /// <summary>
        /// 进地图时决定演哪张卡：特殊地图优先，其次特殊群系；两者都没有就不演。
        /// 比如进"机械主巢"，它的群系是"轨道"，只演地图卡。
        /// </summary>
        static void TryQueueEntryCard(Map m)
        {
            string gen = m.generatorDef != null ? m.generatorDef.defName : "";
            TitleCardDef def = TitleRegistry.Map(gen);
            if (def == null)
                def = TitleRegistry.Biome(m.Biome != null ? m.Biome.defName : "");
            if (def == null) return;

            // 有定义但没图片时不自动播——保证演出观感一致（纯文字条目留给手动/后续扩展）
            if (!TitleTexture.Has(CardKey(def.TextureKey))) return;

            bool isBiome = def.kind == TitleCardKind.Biome;
            BwtWorldComponent wc = BwtWorldComponent.Get();
            bool shown = isBiome
                ? (sOncePerBiome && wc != null && wc.HasShownBiome(def.target))
                : (sOncePerMap && wc != null && wc.HasShownMap(def.target));

            if (shown)
            {
                BwtLog.Info(def.target + " 本存档已展示过，跳过");
                return;
            }

            sPendingMap = m;
            sPendingAt = Time.realtimeSinceStartup + sMapDelay;
            sPendingDef = def;
            BwtLog.Info("进入 " + def.target + " -> " + def.TextFor(IsChinese));
        }

        /// <summary>
        /// 调试用：把当前地图所在格子的群系依次切成四个特殊群系之一，然后强制重判。
        /// Tile.PrimaryBiome 是公开可写的，比在开发者菜单里翻 Set biome 快。
        /// </summary>
        static void CycleSpecialBiome()
        {
            Map map = Find.CurrentMap;
            if (map == null) { Log.Warning("[BWT] 没有当前地图"); return; }

            string[] keys = { "Glowforest", "Scarlands", "LavaField", "GlacialPlain" };
            string cur = map.Biome != null ? map.Biome.defName : "";
            int idx = System.Array.IndexOf(keys, cur);
            idx = (idx + 1) % keys.Length;

            BiomeDef def = DefDatabase<BiomeDef>.GetNamedSilentFail(keys[idx]);
            if (def == null) { Log.Warning("[BWT] 找不到群系 " + keys[idx]); return; }

            RimWorld.Planet.Tile tile = Find.WorldGrid[map.Tile];
            if (tile == null) { Log.Warning("[BWT] 找不到地图所在地块"); return; }

            tile.PrimaryBiome = def;
            sLastMap = null;    // 强制下一帧重新判定
            BwtLog.Info("[BWT] 当前地图群系改为 " + def.defName
                + "（「" + def.label + "」），即将重新判定");
        }

        /// <summary>当前游戏语言是不是中文（中文走中文卡，其余走 _en 卡）。</summary>
        static bool IsChinese
        {
            get
            {
                LoadedLanguage lang = LanguageDatabase.activeLanguage;
                string f = lang != null ? lang.folderName : "";
                return !string.IsNullOrEmpty(f) && f.StartsWith("Chinese");
            }
        }

        /// <summary>把基础 key 解析成实际图片文件名：中文用原 key，其他语言用 key_en。
        /// 缺哪个就退回另一个，保证任何语言下都有卡可显示。</summary>
        static string CardKey(string baseKey)
        {
            if (string.IsNullOrEmpty(baseKey)) return baseKey;

            string primary = IsChinese ? baseKey : baseKey + "_en";
            if (TitleTexture.Has(primary)) return primary;

            string fallback = IsChinese ? baseKey + "_en" : baseKey;
            if (TitleTexture.Has(fallback)) return fallback;

            return primary;
        }

        static void CheckAuto()
        {
            if (!sAuto) return;

            // --- 地图切换 ---
            Map cur = Find.CurrentMap;
            if (cur != sLastMap)
            {
                sLastMap = cur;
                if (cur == null) return;

                SuppressExistingBosses(cur);

                if (!sFirstMapSeen)
                {
                    // 读档 / 开局的那一次不算"进入特殊地图"
                    sFirstMapSeen = true;
                    BwtLog.Info("[BWT] 首个地图不触发（" +
                        (cur.generatorDef != null ? cur.generatorDef.defName : "?") + "）");
                }
                else if (sSeq == Seq.None)
                {
                    TryQueueEntryCard(cur);
                }
            }

            if (sPendingMap != null
                && Time.realtimeSinceStartup >= sPendingAt
                && TicksSinceGen(sPendingMap) >= MapSettleTicks)
            {
                Map m = sPendingMap;
                sPendingMap = null;

                if (m != Find.CurrentMap || sSeq != Seq.None)
                {
                    // 地图已经换走或正在演别的，作废
                }
                else if (MapInCooldown())
                {
                    BwtLog.Info(string.Format("地图冷却中（还剩 {0:0.0}s），跳过 {1}",
                        MapCooldownLeft(), sPendingDef.target));
                }
                else
                {
                    bool isBiome = sPendingDef.kind == TitleCardKind.Biome;
                    BwtWorldComponent wc = BwtWorldComponent.Get();
                    bool already = isBiome
                        ? (sOncePerBiome && wc != null && wc.HasShownBiome(sPendingDef.target))
                        : (sOncePerMap && wc != null && wc.HasShownMap(sPendingDef.target));

                    if (already)
                    {
                        BwtLog.Info(sPendingDef.target + " 本存档已展示过，跳过");
                    }
                    else
                    {
                        if (wc != null)
                        {
                            if (isBiome) wc.MarkShownBiome(sPendingDef.target);
                            else wc.MarkShownMap(sPendingDef.target);
                        }
                        PlayMapEntry(m, CardKey(sPendingDef.TextureKey), sPendingDef.TextFor(IsChinese));
                    }
                }
            }

            // --- 首领出现：每 0.5 秒扫一次 ---
            if (Time.realtimeSinceStartup >= sNextBossScan)
            {
                sNextBossScan = Time.realtimeSinceStartup + 0.5f;
                ScanBosses();
            }

            // --- 延迟到点才开演 ---
            if (sPendingBoss != null && Time.realtimeSinceStartup >= sPendingBossAt)
            {
                Pawn b = sPendingBoss;
                sPendingBoss = null;

                if (b != null && !b.Dead && !b.Destroyed && b.Map == Find.CurrentMap && sSeq == Seq.None)
                    PlayBoss(b);
                else
                    BwtLog.Info("[BWT] 首领已消失或不在当前地图，取消演出");
            }
        }

        static void DumpBosses()
        {
            Map map = Find.CurrentMap;
            if (map == null) { BwtLog.Info("[BWT] 没有当前地图"); return; }

            int n = 0;
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if (p == null || p.Dead || p.kindDef == null) continue;
                if (!p.kindDef.isBoss) continue;

                bool hostile = p.Faction != null && p.HostileTo(Faction.OfPlayer);
                BwtLog.Info(string.Format("[BWT] boss: kind={0}  typeLabel=「{1}」  pawnName=「{2}」  hostile={3}",
                    p.kindDef.defName, p.kindDef.label, p.LabelShort, hostile));
                n++;
            }
            BwtLog.Info("[BWT] 本图 isBoss 单位共 " + n + " 个");
        }

        /// <summary>
        /// 进图时把场上已有的首领记为"已见过"。
        /// 它们没有"登场"，是你走进去看见的——播登场卡等于把"它一直在那儿"说成"它刚来"。
        /// 之后新刷出来的首领不受影响。
        /// </summary>
        static void SuppressExistingBosses(Map m)
        {
            if (m == null) return;

            int n = 0;
            foreach (Pawn p in m.mapPawns.AllPawnsSpawned)
            {
                if (p == null || p.Dead || p.kindDef == null) continue;
                if (!p.kindDef.isBoss) continue;
                if (!TitleTexture.Has(p.kindDef.defName)) continue;
                if (sSeenBosses.Add(p.thingIDNumber)) n++;
            }

            if (n > 0)
                BwtLog.Info("[BWT] 进图时场上已有 " + n + " 个首领，已标记为已见过（不播登场卡）");
        }

        static void ScanBosses()
        {
            if (sSeq != Seq.None) return;
            Map map = Find.CurrentMap;
            if (map == null) return;

            BwtWorldComponent wc = BwtWorldComponent.Get();

            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if (p == null || p.Dead) continue;
                if (p.Faction == null || !p.HostileTo(Faction.OfPlayer)) continue;
                if (p.kindDef == null) continue;

                // 哪些首领会被演，完全由 TitleCardDef 决定。
                // 别的模组给它自己的 PawnKindDef 加一条 Boss 条目就能接入，
                // 不需要改这里的代码，也不依赖 isBoss 标记。
                TitleCardDef def = TitleRegistry.Boss(p.kindDef.defName);
                if (def == null) continue;
                if (!TitleTexture.Has(CardKey(def.TextureKey))) continue;

                if (sOncePerBoss && wc != null && wc.HasShown(def.target)) continue;
                if (sSeenBosses.Contains(p.thingIDNumber)) continue;

                sSeenBosses.Add(p.thingIDNumber);
                if (wc != null) wc.MarkShown(def.target);

                // 冷却期内直接丢弃：已经记进"已显示"，不会再补播
                if (BossInCooldown())
                {
                    BwtLog.Info(string.Format("首领冷却中（还剩 {0:0.0}s），跳过 {1}（{2}）",
                        BossCooldownLeft(), p.LabelShort, def.target));
                    continue;
                }

                sPendingBoss = p;
                sPendingBossAt = Time.realtimeSinceStartup + sBossDelay;
                BwtLog.Info(string.Format("检测到首领 {0}（{1}），{2:0.0}s 后演出",
                    p.LabelShort, def.target, sBossDelay));
                return;
            }
        }

        static Pawn NearestHostile(Map map)
        {
            Vector3 cam = CameraPan.CurrentPos();
            Pawn best = null;
            float bestD = float.MaxValue;
            foreach (Pawn p in map.mapPawns.AllPawnsSpawned)
            {
                if (p == null || p.Dead || p.Downed) continue;
                if (p.Faction == null || !p.HostileTo(Faction.OfPlayer)) continue;
                float d = (p.Position.ToVector3() - cam).sqrMagnitude;
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        /// <summary>不会露出地图外底色的最大缩放。实测：可见高 = 3×size，可见宽 = 3×size×宽高比。</summary>
        static float SafeZoomSize(Map map)
        {
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            float byWidth = map.Size.x / (3f * aspect);
            float byHeight = map.Size.z / 3f;
            return Mathf.Clamp(Mathf.Min(byWidth, byHeight) * 0.95f, 15f, 65f);
        }

        // ---------- 日志 ----------

        static void LogParams()
        {
            BwtLog.Info(string.Format("[BWT] 缩放 {0:0.0}s · 平移 {1:0.0}s · 首领 size {2:0} · 首领延迟 {3:0.0}s · 只显示一次 {4}",
                sZoomDur, sPanDur, sBossSize, sBossDelay, sOncePerBoss ? "开" : "关"));
        }

        static void LogState()
        {
            CameraDriver cd = Find.CameraDriver;
            Map map = Find.CurrentMap;
            if (cd == null) { BwtLog.Info("[BWT] 相机: 无"); return; }

            float cellPx = cd.CellSizePixels;
            float visW = cellPx > 0.0001f ? Screen.width / cellPx : -1f;
            float visH = cellPx > 0.0001f ? Screen.height / cellPx : -1f;

            BwtWorldComponent wc = BwtWorldComponent.Get();
            BwtLog.Info(string.Format(
                "[BWT] 状态: size={0:0.00} 可见≈{1:0.0}x{2:0.0}格 地图={3} 序列={4} 暂停={5} | " +
                "缩放 {6:0.0}s 平移 {7:0.0}s 首领 {8:0} 延迟 {9:0.0}s 只一次 {10} 已记 {11}",
                cd.RootSize, visW, visH,
                map == null ? "-" : map.Size.x + "x" + map.Size.z,
                sSeq, PauseGuard.Enabled ? "开" : "关",
                sZoomDur, sPanDur, sBossSize, sBossDelay,
                sOncePerBoss ? "开" : "关",
                wc == null ? 0 : wc.Count));
        }
    }
}
