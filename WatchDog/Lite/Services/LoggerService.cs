using WatchDog.Lite.Helpers;
using WatchDog.Lite.Interfaces;
using WatchDog.Lite.Models;

namespace WatchDog.Lite.Services {
    internal class LoggerService : ILoggerService {
        public void ClearWatchLogs() {
            if (AutoClearModel.IsAutoClear) {
                LiteDBHelper.ClearWatchLog();
                LiteDBHelper.ClearWatchExceptionLog();
            }

        }
    }
}
