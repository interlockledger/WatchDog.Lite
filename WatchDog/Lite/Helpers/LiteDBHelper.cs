using LiteDB;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using WatchDog.Lite.Models;

namespace WatchDog.Lite.Helpers {
    internal class LiteDBHelper : IDBHelper {
        public static string DefaultFolder {
            get {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Assembly.GetEntryAssembly().GetName().Name);
            }
        }

        readonly LiteDatabase db;
        readonly ILiteCollection<WatchLog> _watchLogs;
        readonly ILiteCollection<WatchExceptionLog> _watchExLogs;
        readonly ILiteCollection<WatchLoggerModel> _logs;

        public LiteDBHelper(string folder) {
            folder ??= DefaultFolder;
            Directory.CreateDirectory(folder);
            db = new(Path.Combine(folder, "watchlogs.db"));
            _watchLogs = db.GetCollection<WatchLog>("WatchLogs");
            _watchExLogs = db.GetCollection<WatchExceptionLog>("WatchExceptionLogs");
            _logs = db.GetCollection<WatchLoggerModel>("Logs");
        }

        public IEnumerable<WatchLog> GetAllWatchLogs() => _watchLogs.FindAll();

        public bool ClearAllLogs() {
            var watchLogs = ClearWatchLog();
            var exLogs = ClearWatchExceptionLog();
            var logs = ClearLogs();


            return watchLogs > 1 && exLogs > 1 && logs > 1;
        }

        //WATCH lOGS OPERATION
        public WatchLog GetWatchLogById(int id) => _watchLogs.FindById(id);

        public int InsertWatchLog(WatchLog log) => _watchLogs.Insert(log);

        public bool UpdateWatchLog(WatchLog log) => _watchLogs.Update(log);

        public bool DeleteWatchLog(int id) => _watchLogs.Delete(id);

        public int ClearWatchLog() => _watchLogs.DeleteAll();


        //Watch Exception Operations
        public IEnumerable<WatchExceptionLog> GetAllWatchExceptionLogs() => _watchExLogs.FindAll();

        public WatchExceptionLog GetWatchExceptionLogById(int id) => _watchExLogs.FindById(id);

        public int InsertWatchExceptionLog(WatchExceptionLog log) => _watchExLogs.Insert(log);

        public bool UpdateWatchExceptionLog(WatchExceptionLog log) => _watchExLogs.Update(log);

        public bool DeleteWatchExceptionLog(int id) => _watchExLogs.Delete(id);
        public int ClearWatchExceptionLog() => _watchExLogs.DeleteAll();

        //LOGS OPERATION
        public int InsertLog(WatchLoggerModel log) => _logs.Insert(log);
        public int ClearLogs() => _logs.DeleteAll();
        public IEnumerable<WatchLoggerModel> GetAllLogs() => _logs.FindAll();
    }
}
