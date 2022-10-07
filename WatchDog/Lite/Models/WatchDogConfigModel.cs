using System.Linq;
using System;

using WatchDog.Lite.Enums;

namespace WatchDog.Lite.Models {
    public static class WatchDogConfigModel {
        public static string UserName { get; set; }
        public static string Password { get; set; }
        public static string[] Blacklist { get; set; }

        public static bool IsBlackListed(string requestPath) => 
            Blacklist.Contains(requestPath.Remove(0, 1), StringComparer.OrdinalIgnoreCase);

    }

    public class WatchDogSettings {
        public bool IsAutoClear { get; set; }
        public WatchDogAutoClearScheduleEnum ClearTimeSchedule { get; set; } = WatchDogAutoClearScheduleEnum.Weekly;
    }

    public static class AutoClearModel {
        public static bool IsAutoClear { get; set; }
        public static WatchDogAutoClearScheduleEnum ClearTimeSchedule { get; set; } = WatchDogAutoClearScheduleEnum.Weekly;
    }
}
