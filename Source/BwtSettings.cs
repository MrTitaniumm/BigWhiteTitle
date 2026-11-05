using Verse;

namespace BigWhiteTitle
{
    /// <summary>模组设置。随 Mod 一起序列化到 RimWorld 的设置文件里。</summary>
    public class BwtSettings : ModSettings
    {
        public bool enabled = true;      // 总开关：关闭后不再自动演演出
        public bool autoPause = true;    // 演出期间暂停游戏
        public bool duckMusic = true;    // 演出期间压低 BGM
        public bool oncePerBoss = true;  // 每种首领只显示一次
        public bool oncePerMap = true;   // 每张特殊地图只显示一次
        public bool oncePerBiome = true; // 每种特殊群系只显示一次

        public float zoomDur = 3.0f;     // 拉远/拉回时长
        public float panDur = 2.0f;      // 平移时长
        public float bossSize = 18f;     // 推首领时的镜头尺寸
        public float bossDelay = 1.0f;   // 检测到首领后延迟
        public float mapDelay = 2.0f;    // 检测到切图后延迟
        public float mapCooldown = 30f;  // 地图演出结束后，多久内不再演地图
        public float bossCooldown = 30f; // 首领演出结束后，多久内不再演首领

        public override void ExposeData()
        {
            Scribe_Values.Look(ref enabled, "enabled", true);
            Scribe_Values.Look(ref autoPause, "autoPause", true);
            Scribe_Values.Look(ref duckMusic, "duckMusic", true);
            Scribe_Values.Look(ref oncePerBoss, "oncePerBoss", true);
            Scribe_Values.Look(ref oncePerMap, "oncePerMap", true);
            Scribe_Values.Look(ref oncePerBiome, "oncePerBiome", true);

            Scribe_Values.Look(ref zoomDur, "zoomDur", 3.0f);
            Scribe_Values.Look(ref panDur, "panDur", 2.0f);
            Scribe_Values.Look(ref bossSize, "bossSize", 18f);
            Scribe_Values.Look(ref bossDelay, "bossDelay", 1.0f);
            Scribe_Values.Look(ref mapDelay, "mapDelay", 2.0f);
            Scribe_Values.Look(ref mapCooldown, "mapCooldown", 30f);
            Scribe_Values.Look(ref bossCooldown, "bossCooldown", 30f);
        }
    }
}
