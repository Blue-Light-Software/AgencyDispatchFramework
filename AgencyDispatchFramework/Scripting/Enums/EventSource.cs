namespace AgencyDispatchFramework.Scripting
{
    /// <summary>
    /// Indicates how an <see cref="IEventController"/> was created
    /// </summary>
    public enum EventSource
    {
        /// <summary>
        /// Indicates that an <see cref="IEventController"/> was initiated from a 911 call
        /// </summary>
        Reported,

        /// <summary>
        /// Indicates that an <see cref="IEventController"/> was a crime that happened
        /// in front of the player character. If not radioed in by the player,
        /// the <see cref="IEventController"/> will expire.
        /// </summary>
        Ambient,

        /// <summary>
        /// Indicates that an <see cref="IEventController"/> was a crime that happened
        /// in front of an <see cref="Simulation.AIOfficerUnit"/>, and radioed
        /// in by that officer.
        /// </summary>
        OfficerInitiated
    }
}
