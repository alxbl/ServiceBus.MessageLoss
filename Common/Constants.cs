namespace Common
{
    public static class Constants
    {
        public const string ServiceBus = "<connstring>";
        public const string StorageAccount = "<connstring>";
        public const string TopicName = "numbers";
        public const string Subscription = "default";
        public const string StorageContainer = "numbers-container";

        /// <summary>
        /// Easily switch between async or blocking calls for blob I/O.
        /// </summary>
        public static bool SynchronousBlobStorage = false; // Not a constant so that the compiler doesn't complain about dead-code.
    }
}
