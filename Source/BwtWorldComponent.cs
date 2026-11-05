using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace BigWhiteTitle
{
    /// <summary>
    /// 存档级持久化：记住哪些首领类型已经演出过。
    ///
    /// WorldComponent 会随世界存档一起序列化，所以读档、重启都不会丢。
    /// 若当前世界没有这个组件（老存档、或首次加载），Get() 会补建一个
    /// 并塞进 World.components（公开字段），之后照常存档。
    /// </summary>
    public class BwtWorldComponent : WorldComponent
    {
        public List<string> shownBossKinds = new List<string>();
        public List<string> shownMapDefs = new List<string>();
        public List<string> shownBiomes = new List<string>();

        public BwtWorldComponent(World world) : base(world) { }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref shownBossKinds, "bwt_shownBossKinds", LookMode.Value);
            Scribe_Collections.Look(ref shownMapDefs, "bwt_shownMapDefs", LookMode.Value);
            Scribe_Collections.Look(ref shownBiomes, "bwt_shownBiomes", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (shownBossKinds == null) shownBossKinds = new List<string>();
                if (shownMapDefs == null) shownMapDefs = new List<string>();
                if (shownBiomes == null) shownBiomes = new List<string>();
            }
        }

        public static BwtWorldComponent Get()
        {
            World w = Find.World;
            if (w == null) return null;

            BwtWorldComponent c = w.GetComponent<BwtWorldComponent>();
            if (c == null)
            {
                c = new BwtWorldComponent(w);
                w.components.Add(c);
                BwtLog.Info("[BWT] 已为当前世界创建持久化组件");
            }
            return c;
        }

        public bool HasShown(string kind)
        {
            return shownBossKinds != null && shownBossKinds.Contains(kind);
        }

        public void MarkShown(string kind)
        {
            if (shownBossKinds == null) shownBossKinds = new List<string>();
            if (!shownBossKinds.Contains(kind)) shownBossKinds.Add(kind);
        }

        public int Count { get { return shownBossKinds == null ? 0 : shownBossKinds.Count; } }

        public int MapCount { get { return shownMapDefs == null ? 0 : shownMapDefs.Count; } }

        public bool HasShownMap(string defName)
        {
            return shownMapDefs != null && shownMapDefs.Contains(defName);
        }

        public void MarkShownMap(string defName)
        {
            if (shownMapDefs == null) shownMapDefs = new List<string>();
            if (!shownMapDefs.Contains(defName)) shownMapDefs.Add(defName);
        }

        public bool HasShownBiome(string defName)
        {
            return shownBiomes != null && shownBiomes.Contains(defName);
        }

        public void MarkShownBiome(string defName)
        {
            if (shownBiomes == null) shownBiomes = new List<string>();
            if (!shownBiomes.Contains(defName)) shownBiomes.Add(defName);
        }

        public int BiomeCount { get { return shownBiomes == null ? 0 : shownBiomes.Count; } }

        public void Clear()
        {
            if (shownBossKinds == null) shownBossKinds = new List<string>();
            else shownBossKinds.Clear();

            if (shownMapDefs == null) shownMapDefs = new List<string>();
            else shownMapDefs.Clear();

            if (shownBiomes == null) shownBiomes = new List<string>();
            else shownBiomes.Clear();
        }
    }
}
