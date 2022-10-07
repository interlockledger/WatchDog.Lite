using System;

namespace WatchDog.Lite.Exceptions {
    internal class WatchDogAuthenticationException : Exception {
        internal WatchDogAuthenticationException() { }

        internal WatchDogAuthenticationException(string message)
            : base(string.Format("WatchDog Authentication Exception: {0}", message)) {

        }
    }
}
