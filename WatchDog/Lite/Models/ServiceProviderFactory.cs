using WatchDog.Lite.Helpers;
using WatchDog.Lite.Interfaces;

namespace WatchDog.Lite.Models {
    public static class ServiceProviderFactory {
        public static IBroadcastHelper BroadcastHelper { get; set; }
        public static IDBHelper DBHelper { get; set; }
    }
}
