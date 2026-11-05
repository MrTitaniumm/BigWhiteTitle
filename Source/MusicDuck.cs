using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace BigWhiteTitle
{
    /// <summary>
    /// BGM 闪避（ducking）：演出时把音乐渐压到 0，白字结束后渐回。
    ///
    /// 基准音量用 Prefs.VolumeMusic（玩家设置里的音乐音量），不用
    /// audioSource.volume —— 后者在暂停时已被 pausedVolumeFactor 压小，
    /// 拿它当基准会导致"恢复"之后音乐一直很小。
    ///
    /// 写入放在 LateUpdate，排在引擎的 MusicUpdate 之后，否则同帧会被覆盖。
    /// </summary>
    public static class MusicDuck
    {
        static FieldInfo sSourceField;
        static bool sActive;
        static float sFrom = 1f;
        static float sTo = 1f;
        static float sCur = 1f;
        static float sT;
        static float sDur;

        public static float Current { get { return sCur; } }

        static float BaseVolume
        {
            get
            {
                float v = 1f;
                try { v = Prefs.VolumeMusic; } catch { }
                return Mathf.Clamp01(v);
            }
        }

        public static void FadeTo(float target, float dur)
        {
            // 玩家在设置里关掉了音乐闪避，就完全不接管音量
            if (BwtMod.Settings != null && !BwtMod.Settings.duckMusic) return;

            sFrom = sCur;
            sTo = Mathf.Clamp01(target);
            sT = 0f;
            sDur = Mathf.Max(0.01f, dur);
            sActive = true;
            BwtLog.Info(string.Format("[BWT] BGM {0:0.00} -> {1:0.00}，用时 {2:0.0}s（基准 {3:0.00}）",
                sFrom, sTo, sDur, BaseVolume));
        }

        public static void Reset()
        {
            FadeTo(1f, 0.8f);
        }

        /// <summary>立即把音乐恢复成设置里的音量，并停止接管。用于卡住时手动救场。</summary>
        public static void RestoreNow()
        {
            sCur = 1f;
            sActive = false;
            AudioSource src = GetSource();
            if (src != null) src.volume = BaseVolume;
            BwtLog.Info("[BWT] BGM 强制恢复 -> " + BaseVolume.ToString("0.00"));
        }

        public static void Tick()
        {
            if (!sActive) return;

            AudioSource src = GetSource();
            if (src == null) { sActive = false; sCur = 1f; return; }

            sT += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(sT / sDur);
            float e = k * k * (3f - 2f * k);          // smoothstep
            sCur = Mathf.Lerp(sFrom, sTo, e);

            src.volume = BaseVolume * sCur;

            // 压到 0 之后要继续压着（否则引擎下一帧就可能写回来）；
            // 完全恢复后才松手。
            if (k >= 1f && sTo >= 0.999f)
            {
                sCur = 1f;
                sActive = false;
            }
        }

        static AudioSource GetSource()
        {
            try
            {
                MusicManagerPlay mm = Find.MusicManagerPlay;
                if (mm == null) return null;
                if (sSourceField == null)
                    sSourceField = typeof(MusicManagerPlay).GetField(
                        "audioSource", BindingFlags.Instance | BindingFlags.NonPublic);
                return sSourceField != null ? sSourceField.GetValue(mm) as AudioSource : null;
            }
            catch { return null; }
        }
    }
}
