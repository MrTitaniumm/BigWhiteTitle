using System.Collections.Generic;
using Verse;

namespace BigWhiteTitle
{
    /// <summary>
    /// 把 TitleCardDef 按 kind 分组成三张查找表。
    /// 首次访问时才构建，避免和 Defs 加载顺序打架。
    /// </summary>
    public static class TitleRegistry
    {
        static Dictionary<string, TitleCardDef> sMaps;
        static Dictionary<string, TitleCardDef> sBiomes;
        static Dictionary<string, TitleCardDef> sBosses;

        static int sBuiltCount = -1;

        static void EnsureBuilt()
        {
            // 数量没变就复用。这样开发者模式里 Reload defs 加了新卡也能立刻生效。
            int count = DefDatabase<TitleCardDef>.DefCount;
            if (sMaps != null && sBuiltCount == count) return;
            sBuiltCount = count;

            sMaps = new Dictionary<string, TitleCardDef>();
            sBiomes = new Dictionary<string, TitleCardDef>();
            sBosses = new Dictionary<string, TitleCardDef>();

            foreach (TitleCardDef d in DefDatabase<TitleCardDef>.AllDefs)
            {
                if (d.target.NullOrEmpty())
                {
                    Log.Warning("[BWT] TitleCardDef " + d.defName + " 缺少 target，已忽略");
                    continue;
                }

                Dictionary<string, TitleCardDef> dict;
                switch (d.kind)
                {
                    case TitleCardKind.Biome: dict = sBiomes; break;
                    case TitleCardKind.Boss: dict = sBosses; break;
                    default: dict = sMaps; break;
                }

                if (dict.ContainsKey(d.target))
                {
                    Log.Warning("[BWT] TitleCardDef " + d.defName + " 与已有条目重复（target=" + d.target + "），已忽略");
                    continue;
                }
                dict[d.target] = d;
            }

            BwtLog.Info(string.Format("卡片库：地图 {0} · 群系 {1} · 首领 {2}",
                sMaps.Count, sBiomes.Count, sBosses.Count));
        }

        static TitleCardDef Lookup(Dictionary<string, TitleCardDef> dict, string target)
        {
            if (dict == null || target.NullOrEmpty()) return null;

            TitleCardDef def;
            return dict.TryGetValue(target, out def) ? def : null;
        }

        // 注意：EnsureBuilt 必须在取字段之前调用。
        // 写成 Lookup(sMaps, ...) 时，sMaps 会在 EnsureBuilt 执行前就被求值，
        // 首次调用拿到的是 null，导致 NullReferenceException。
        public static TitleCardDef Map(string defName) { EnsureBuilt(); return Lookup(sMaps, defName); }
        public static TitleCardDef Biome(string defName) { EnsureBuilt(); return Lookup(sBiomes, defName); }
        public static TitleCardDef Boss(string defName) { EnsureBuilt(); return Lookup(sBosses, defName); }
    }
}
