using System.Collections.Generic;

using WatchDog.Lite.Models;

namespace WatchDog.Lite.Helpers;
public interface IDBHelper {
    bool ClearAllLogs();
    int ClearLogs();
    int ClearWatchExceptionLog();
    int ClearWatchLog();
    bool DeleteWatchExceptionLog(int id);
    bool DeleteWatchLog(int id);
    IEnumerable<WatchLoggerModel> GetAllLogs();
    IEnumerable<WatchExceptionLog> GetAllWatchExceptionLogs();
    IEnumerable<WatchLog> GetAllWatchLogs();
    WatchExceptionLog GetWatchExceptionLogById(int id);
    WatchLog GetWatchLogById(int id);
    int InsertLog(WatchLoggerModel log);
    int InsertWatchExceptionLog(WatchExceptionLog log);
    int InsertWatchLog(WatchLog log);
    bool UpdateWatchExceptionLog(WatchExceptionLog log);
    bool UpdateWatchLog(WatchLog log);
}