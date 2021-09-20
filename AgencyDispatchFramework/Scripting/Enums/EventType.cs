namespace AgencyDispatchFramework.Scripting
{
    public enum EventType
    {
        /// <summary>
        /// Indicates that the <see cref="IEventController"/> is an event that is dispatched
        /// primarily to the Police
        /// </summary>
        Crime,

        /// <summary>
        /// Indicates that the <see cref="IEventController"/> is an event that is dispatched
        /// primarily to the Fire Department
        /// </summary>
        Fire,

        /// <summary>
        /// Indicates that the <see cref="IEventController"/> is an event that is dispatched
        /// primarily to EMS 
        /// </summary>
        Medical,

        /// <summary>
        /// not implemented yet
        /// </summary>
        AgencyAssist
    }
}
