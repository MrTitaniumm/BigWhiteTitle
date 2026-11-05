using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Verse;

namespace BigWhiteTitle
{
    /// <summary>
    /// 标题图缓存。图片放在 模组根/Textures/Titles/&lt;key&gt;.png，
    /// 用 File.ReadAllBytes + Texture2D.LoadImage 加载——不需要 AssetBundle，
    /// 也不用走 RimWorld 的 ContentFinder 路径规则。
    /// </summary>
    public static class TitleTexture
    {
        static readonly Dictionary<string, Texture2D> sCache = new Dictionary<string, Texture2D>();

        public static Texture2D Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            Texture2D tex;
            if (sCache.TryGetValue(key, out tex)) return tex;

            tex = Load(key);
            sCache[key] = tex;      // 失败也缓存 null，避免每帧重试
            return tex;
        }

        public static bool Has(string key)
        {
            return Get(key) != null;
        }

        static Texture2D Load(string key)
        {
            try
            {
                // key 可以带子目录，例如 "Vanilla/Mechhive" 或 "DeadMansSwitch/DMS_Mech_Geochelone"
                string rel = key.Replace('\\', '/');
                string path = Path.Combine(BwtPaths.ModRoot, "Textures", "Titles",
                    rel.Replace('/', Path.DirectorySeparatorChar) + ".png");

                if (File.Exists(path))
                {
                    byte[] bytes = File.ReadAllBytes(path);
                    Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    if (!tex.LoadImage(bytes))
                    {
                        Log.Warning("[BWT] 图片解码失败: " + path);
                        return null;
                    }

                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Bilinear;
                    tex.name = "BWT_" + rel;
                    BwtLog.Info(string.Format("图 {0}  {1}x{2}", rel, tex.width, tex.height));
                    return tex;
                }

                // 自己的文件夹里没有 → 去所有已加载模组的 Textures/ 里找。
                // 路径要带上 Titles/ 前缀，才是 Textures/ 下的完整相对路径。
                Texture2D found = ContentFinder<Texture2D>.Get("Titles/" + rel, false);
                if (found != null)
                {
                    BwtLog.Info("图 " + rel + " 来自模组资源");
                    return found;
                }

                // 缺图是常态（比如没有做英文版的卡），静默返回，由调用方决定回退
                return null;
            }
            catch (System.Exception e)
            {
                Log.Warning("[BWT] 加载标题图 " + key + " 失败: " + e.Message);
                return null;
            }
        }
    }
}
