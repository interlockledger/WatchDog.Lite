using System;

namespace WatchDog.Lite.Exceptions {
    internal class WatchDogDBDriverException : Exception {
        internal WatchDogDBDriverException(string message)
            : base(string.Format("WatchDog Database Exception: {0}", message)) {

        }
    }
}
