using Verse;

namespace BigWhiteTitle
{
    public enum TitleCardKind
    {
        Map,     // MapGeneratorDef
        Biome,   // BiomeDef
        Boss     // PawnKindDef
    }

    /// <summary>
    /// 一张标题卡的定义。放在 Defs 里，任何模组都能用 PatchOperation 追加，
    /// 也可以用 XmlExtensions 之类做条件加载。
    ///
    /// 图片查找顺序：texture（若填了）→ target，再按语言加 _en 后缀。
    /// 找不到图片就退回用 text 现场渲染，所以纯文字条目也能用。
    /// </summary>
    public class TitleCardDef : Def
    {
        /// <summary>这张卡对应哪类目标。</summary>
        public TitleCardKind kind = TitleCardKind.Map;

        /// <summary>目标的 defName：MapGeneratorDef / BiomeDef / PawnKindDef。</summary>
        public string target;

        /// <summary>中文备用文字（无图时现场渲染）。</summary>
        public string text;

        /// <summary>英文备用文字。留空则用 text。</summary>
        public string textEn;

        /// <summary>可选：Textures 下的图片路径（不带扩展名，不带 Textures 前缀）。
        /// 留空则用 target 当路径。</summary>
        public string texture;

        public string TextureKey
        {
            get { return texture.NullOrEmpty() ? target : texture; }
        }

        public string TextFor(bool chinese)
        {
            if (chinese) return text.NullOrEmpty() ? label : text;
            return textEn.NullOrEmpty() ? TextFor(true) : textEn;
        }
    }
}
