namespace AgencyDispatchFramework
{
    internal enum LogLevel
    {
        /// <summary>
        /// Messages that are verbose and used to debugging and testing purposes
        /// </summary>
        DEBUG,

        /// <summary>
        /// Messages that trace state the status of the plugin
        /// </summary>
        INFO,

        /// <summary>
        /// Messages that could lead to further problems in the plugin later
        /// </summary>
        WARN,

        /// <summary>
        /// Messages that prevent the plugin or any part of the plugig
        /// from working any further
        /// </summary>
        ERROR,
    }
}
