using System;

namespace AgencyDispatchFramework.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Returns whether the the current time falls within a time range
        /// </summary>
        /// <param name="current"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static bool IsBetween(this DateTime current, TimeSpan start, TimeSpan end)
        {
            var now = current.TimeOfDay;

            // Start and stop times are in the same day?
            if (start <= end)
            {
                if (now >= start && now <= end)
                {
                    // Current time is between start and stop
                    return true;
                }
            }
            // Start and stop times are in different days
            else if (now >= start || now <= end)
            {
                // Current time is between start and stop
                return true;
            }

            return false;
        }
    }
}
