using System;
using System.Linq;

using WatchDog.Lite.Enums;

namespace WatchDog.Lite.Models {
    internal class WatchDogConfigModel {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string[] Blacklist { get; set; }
        public bool LogExceptions { get; set; }

        public bool IsBlackListed(string requestPath) =>
            Blacklist.Contains(requestPath.Remove(0, 1), StringComparer.OrdinalIgnoreCase);

    }
    public class WatchDogOptionsModel {
        public string WatchPageUsername { get; set; }
        public string WatchPagePassword { get; set; }
        public string Blacklist { get; set; }
        public bool LogExceptions { get; set; }
    }

    public class WatchDogSettings {
        public bool IsAutoClear { get; set; }
        public WatchDogAutoClearScheduleEnum ClearTimeSchedule { get; set; } = WatchDogAutoClearScheduleEnum.Weekly;
        public string DatabaseFolder { get; set; }
    }

    public static class AutoClearModel {
        public static bool IsAutoClear { get; set; }
        public static WatchDogAutoClearScheduleEnum ClearTimeSchedule { get; set; } = WatchDogAutoClearScheduleEnum.Weekly;
    }
}
