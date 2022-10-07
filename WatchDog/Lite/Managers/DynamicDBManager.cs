using System.Collections.Generic;
using System.Threading.Tasks;

using WatchDog.Lite.Helpers;

using WatchDog.Lite.Models;

namespace WatchDog.Lite.Managers {
    internal static class DynamicDBManager {
        public static Task<bool> ClearLogs() =>
             Task.FromResult(LiteDBHelper.ClearAllLogs());

        public static Task<IEnumerable<WatchLog>> GetAllWatchLogs() =>
            Task.FromResult(LiteDBHelper.GetAllWatchLogs());

        public static Task InsertWatchLog(WatchLog log) {
            LiteDBHelper.InsertWatchLog(log);
            return Task.CompletedTask;
        }

        // WATCH EXCEPTION OPERATIONS
        public static Task<IEnumerable<WatchExceptionLog>> GetAllWatchExceptionLogs() =>
            Task.FromResult(LiteDBHelper.GetAllWatchExceptionLogs());

        public static Task InsertWatchExceptionLog(WatchExceptionLog log) {
            LiteDBHelper.InsertWatchExceptionLog(log);
            return Task.CompletedTask;
        }

        // LOG OPERATIONS
        public static Task<IEnumerable<WatchLoggerModel>> GetAllLogs() =>
            Task.FromResult(LiteDBHelper.GetAllLogs());

        public static Task InsertLog(WatchLoggerModel log) {
            LiteDBHelper.InsertLog(log);
            return Task.CompletedTask;
        }
    }
}
