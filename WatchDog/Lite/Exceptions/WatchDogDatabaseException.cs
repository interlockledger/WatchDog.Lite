using System;

namespace WatchDog.Lite.Exceptions {
    internal class WatchDogDatabaseException : Exception {
        internal WatchDogDatabaseException() { }

        internal WatchDogDatabaseException(string message)
            : base(string.Format("WatchDog Database Exception: {0} Ensure you have passed the right SQLDriverOption at .AddWatchDogServices() as well as all required parameters for the database connection string", message)) {

        }
    }
}
