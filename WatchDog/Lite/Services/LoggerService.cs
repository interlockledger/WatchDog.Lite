using WatchDog.Lite.Helpers;
using WatchDog.Lite.Interfaces;
using WatchDog.Lite.Models;

namespace WatchDog.Lite.Services {
    internal class LoggerService : ILoggerService {
        private readonly IDBHelper liteDBHelper;

        public LoggerService(IDBHelper liteDBHelper) {
            this.liteDBHelper = liteDBHelper;
        }

        public void ClearWatchLogs() {
            if (AutoClearModel.IsAutoClear) {
                liteDBHelper.ClearWatchLog();
                liteDBHelper.ClearWatchExceptionLog();
            }

        }
    }
}
