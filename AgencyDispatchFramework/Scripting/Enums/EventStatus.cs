namespace AgencyDispatchFramework.Scripting
{
    public enum EventStatus
    {
        /// <summary>
        /// Indicates that the event has been created, but Dispatch is not aware
        /// </summary>
        Created,

        /// <summary>
        /// Indicates that the event has been reported to dispatch, and is not
        /// being shown in the MDT
        /// </summary>
        Reported,

        /// <summary>
        /// Indicates that the call has been completed
        /// </summary>
        Completed
    }
}
