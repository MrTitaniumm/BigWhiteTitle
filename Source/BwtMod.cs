using UnityEngine;
using Verse;

namespace BigWhiteTitle
{
    /// <summary>
    /// Mod 设置页。所有文案走 Keyed 翻译，跟随游戏语言。
    /// </summary>
    public class BwtMod : Mod
    {
        public static BwtSettings Settings;

        static string sStatus;

        public BwtMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<BwtSettings>();
        }

        public override string SettingsCategory()
        {
            return "BWT_Category".Translate();
        }

        static void Line(Listing_Standard list, string text)
        {
            Widgets.Label(list.GetRect(24f), text);
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            BwtSettings s = Settings;
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);

            list.CheckboxLabeled("BWT_Enabled".Translate(), ref s.enabled, "BWT_EnabledTip".Translate());
            list.CheckboxLabeled("BWT_AutoPause".Translate(), ref s.autoPause, "BWT_AutoPauseTip".Translate());
            list.CheckboxLabeled("BWT_DuckMusic".Translate(), ref s.duckMusic, "BWT_DuckMusicTip".Translate());

            list.GapLine(14f);

            list.CheckboxLabeled("BWT_OncePerBoss".Translate(), ref s.oncePerBoss, "BWT_OncePerBossTip".Translate());
            list.CheckboxLabeled("BWT_OncePerMap".Translate(), ref s.oncePerMap, "BWT_OncePerMapTip".Translate());
            list.CheckboxLabeled("BWT_OncePerBiome".Translate(), ref s.oncePerBiome, "BWT_OncePerBiomeTip".Translate());

            list.GapLine(16f);

            Line(list, "BWT_ZoomDur".Translate(s.zoomDur.ToString("0.0")));
            s.zoomDur = list.Slider(s.zoomDur, 0.6f, 6f);

            Line(list, "BWT_PanDur".Translate(s.panDur.ToString("0.0")));
            s.panDur = list.Slider(s.panDur, 0.3f, 6f);

            Line(list, "BWT_BossSize".Translate(s.bossSize.ToString("0")));
            s.bossSize = list.Slider(s.bossSize, 15f, 45f);

            list.GapLine(16f);

            Line(list, "BWT_BossDelay".Translate(s.bossDelay.ToString("0.0")));
            s.bossDelay = list.Slider(s.bossDelay, 0f, 5f);

            Line(list, "BWT_MapDelay".Translate(s.mapDelay.ToString("0.0")));
            s.mapDelay = list.Slider(s.mapDelay, 0f, 8f);

            list.GapLine(16f);

            Line(list, "BWT_MapCooldown".Translate(s.mapCooldown.ToString("0")));
            s.mapCooldown = list.Slider(s.mapCooldown, 0f, 180f);

            Line(list, "BWT_BossCooldown".Translate(s.bossCooldown.ToString("0")));
            s.bossCooldown = list.Slider(s.bossCooldown, 0f, 180f);

            list.GapLine(16f);

            BwtWorldComponent wc = BwtWorldComponent.Get();
            Line(list, wc != null
                ? "BWT_Recorded".Translate(wc.Count, wc.MapCount, wc.BiomeCount)
                : "BWT_RecordedNone".Translate());

            if (list.ButtonText("BWT_Reset".Translate(), null, 1f))
            {
                if (wc != null)
                {
                    wc.Clear();
                    sStatus = "BWT_ResetDone".Translate();
                    BwtLog.Info("[BWT] 玩家重置了本存档的显示记录");
                }
                else
                {
                    sStatus = "BWT_ResetNoSave".Translate();
                }
            }

            if (!string.IsNullOrEmpty(sStatus))
                Line(list, sStatus);

            list.GapLine(16f);

            if (list.ButtonText("BWT_RestoreMusic".Translate(), null, 1f))
                MusicDuck.RestoreNow();

            list.End();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();

            if (Settings != null && !Settings.duckMusic)
                MusicDuck.RestoreNow();
        }
    }
}
