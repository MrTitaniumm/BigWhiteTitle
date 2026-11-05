namespace BigWhiteTitle
{
    /// <summary>
    /// 安静的日志入口。
    ///
    /// 调试阶段所有状态都往日志里写，正式版玩家不需要看到这些——
    /// 所以统一改走这里。想临时恢复输出，把 Verbose 改成 true 即可。
    ///
    /// 真正的错误仍然走 Log.Error / Log.Warning，不受这里影响。
    /// </summary>
    public static class BwtLog
    {
        public static bool Verbose = false;

        public static void Info(object msg)
        {
            if (Verbose) Verse.Log.Message("[BWT] " + msg);
        }
    }
}
