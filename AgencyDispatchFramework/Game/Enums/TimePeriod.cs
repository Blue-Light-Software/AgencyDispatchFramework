namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// An enumeration to describe different time periods throughout the day
    /// </summary>
    public enum TimePeriod
    {
        /// <summary>
        /// Describes a period of time between the hours of 04:00 and 08:00
        /// </summary>
        EarlyMorning,

        /// <summary>
        /// Describes a period of time between the hours of 08:00 and 12:00
        /// </summary>
        LateMorning,

        /// <summary>
        /// Describes a period of time between the hours of 12:00 and 16:00
        /// </summary>
        Afternoon,

        /// <summary>
        /// Describes a period of time between the hours of 16:00 and 20:00
        /// </summary>
        EarlyEvening,

        /// <summary>
        /// Describes a period of time between the hours of 20:00 and 23:59
        /// </summary>
        LateEvening,

        /// <summary>
        /// Describes a period of time between the hours of 00:01 and 04:00
        /// </summary>
        Night
    }
}
