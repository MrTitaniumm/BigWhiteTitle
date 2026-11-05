using System.Reflection;
using UnityEngine;
using Verse;

namespace BigWhiteTitle
{
    /// <summary>
    /// 自建相机补间。
    ///
    /// 为什么不用原版 PanToMapLocAndSize：它按移动距离缩放时长
    /// （CameraDriver.FullDurationPanDistance），而"拉远"只改 size、位置几乎不动，
    /// 于是传 3 秒实际只跑 1 秒。
    ///
    /// 这里用 unscaledDeltaTime 逐帧插值，时长精确，且暂停期间照常推进。
    /// </summary>
    public static class CameraPan
    {
        static FieldInfo sPosField;

        /// <summary>私有字段可能改名（1.7 换了引擎），按顺序试几个常见名字。</summary>
        static readonly string[] PosFieldNames = { "rootPos", "rootPosition", "position", "curRootPos" };
        static int sPosFieldTried = -1;
        static bool sActive;
        static float sT, sDur;
        static Vector3 sFrom, sTo;
        static float sFromSize, sToSize;

        public static bool Active { get { return sActive; } }

        public static Vector3 CurrentPos()
        {
            CameraDriver cd = Find.CameraDriver;
            if (cd == null) return Vector3.zero;
            try
            {
                if (sPosFieldTried < 0)
                {
                    sPosFieldTried = 0;
                    BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                    for (int i = 0; i < PosFieldNames.Length; i++)
                    {
                        FieldInfo f = typeof(CameraDriver).GetField(PosFieldNames[i], flags);
                        if (f != null && f.FieldType == typeof(Vector3)) { sPosField = f; break; }
                    }
                }
                return sPosField != null ? (Vector3)sPosField.GetValue(cd) : Vector3.zero;
            }
            catch { return Vector3.zero; }
        }

        public static float CurrentSize()
        {
            CameraDriver cd = Find.CameraDriver;
            return cd != null ? cd.RootSize : 21f;
        }

        public static void Start(Vector3 toPos, float toSize, float dur)
        {
            CameraDriver cd = Find.CameraDriver;
            if (cd == null) return;

            sFrom = CurrentPos();
            sFromSize = cd.RootSize;
            sTo = toPos;
            sToSize = toSize;
            sT = 0f;
            sDur = Mathf.Max(0.05f, dur);
            sActive = true;
        }

        public static void Cancel()
        {
            sActive = false;
        }

        public static void Tick()
        {
            if (!sActive) return;

            CameraDriver cd = Find.CameraDriver;
            if (cd == null) { sActive = false; return; }

            sT += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(sT / sDur);
            float e = t * t * (3f - 2f * t);          // smoothstep

            cd.SetRootPosAndSize(
                Vector3.Lerp(sFrom, sTo, e),
                Mathf.Lerp(sFromSize, sToSize, e));

            if (t >= 1f) sActive = false;
        }
    }

    /// <summary>
    /// 演出期间暂停游戏，结束恢复原速度。
    /// 只在我们自己按下去时才负责恢复——玩家本来就暂停的不碰。
    /// </summary>
    public static class PauseGuard
    {
        static bool sHeld;
        static TimeSpeed sPrev = TimeSpeed.Normal;

        public static bool Enabled
        {
            get { return BwtMod.Settings == null || BwtMod.Settings.autoPause; }
            set { if (BwtMod.Settings != null) BwtMod.Settings.autoPause = value; }
        }

        public static void Hold()
        {
            TickManager tm = Find.TickManager;
            if (tm == null || !Enabled || sHeld) return;
            if (tm.CurTimeSpeed == TimeSpeed.Paused) return;

            sPrev = tm.CurTimeSpeed;
            tm.CurTimeSpeed = TimeSpeed.Paused;
            sHeld = true;
            BwtLog.Info("[BWT] 演出暂停（原速度 " + sPrev + "）");
        }

        public static void Release()
        {
            if (!sHeld) return;
            sHeld = false;
            TickManager tm = Find.TickManager;
            if (tm == null) return;
            tm.CurTimeSpeed = sPrev;
            BwtLog.Info("[BWT] 恢复速度 " + sPrev);
        }
    }
}
