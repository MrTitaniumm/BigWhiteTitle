using System.IO;
using System.Reflection;

namespace BigWhiteTitle
{
    /// <summary>
    /// 模组根目录的定位。
    ///
    /// 不能用“DLL 位置往上两级”来推：1.6 的 DLL 在 &lt;mod&gt;\Assemblies，
    /// 1.7 起在 &lt;mod&gt;\1.7\Assemblies，上两级会变成 &lt;mod&gt;\1.7，
    /// 于是 Sounds\ 和 Textures\ 全部找不到（音效会静默退化成合成音）。
    ///
    /// 改为向上找带 About 目录的那一级——About 是每个模组的标志。
    /// </summary>
    public static class BwtPaths
    {
        static string sModRoot;

        public static string ModRoot
        {
            get
            {
                if (sModRoot != null) return sModRoot;

                string start = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string dir = start;

                for (int i = 0; i < 4 && !string.IsNullOrEmpty(dir); i++)
                {
                    if (Directory.Exists(Path.Combine(dir, "About")))
                    {
                        sModRoot = dir;
                        return sModRoot;
                    }
                    dir = Path.GetDirectoryName(dir);
                }

                // 兜底：退回旧行为（上两级）
                sModRoot = Path.GetDirectoryName(start ?? "") ?? "";
                return sModRoot;
            }
        }
    }
}
