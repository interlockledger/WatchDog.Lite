using System.Threading.Tasks;

using WatchDog.Lite.Models;

namespace WatchDog.Lite.Interfaces {
    public interface IBroadcastHelper {
        Task BroadcastWatchLog(WatchLog log);
        Task BroadcastExLog(WatchExceptionLog log);
        Task BroadcastLog(WatchLoggerModel log);
    }
}
