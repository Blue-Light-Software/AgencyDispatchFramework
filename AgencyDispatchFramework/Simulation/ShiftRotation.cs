namespace AgencyDispatchFramework.Dispatching
{
    internal enum ShiftRotation
    {
        /// <summary>
        /// Day shift ranges from 6am to 4pm
        /// </summary>
        Day,

        /// <summary>
        /// Swing shift ranges from 3pm to 1am
        /// </summary>
        Swing,

        /// <summary>
        /// Night shift ranges from 9pm to 7am
        /// </summary>
        Night,
    }
}