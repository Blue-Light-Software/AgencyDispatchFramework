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
        /// Indcates that the call has been assigned to an <see cref="OfficerUnit"/>,
        /// but not actively dispatched yet (could be due to busy radio)
        /// </summary>
        Assigned,

        /// <summary>
        /// Indicates that the call is currently assigned to the player, and waiting for 
        /// the player to accept it. If the player does not accept the callout, then
        /// progress goes back to <see cref="Reported"/>
        /// </summary>
        Waiting,

        /// <summary>
        /// Indicates that the call has been assigned and successfully dispatched to an 
        /// <see cref="OfficerUnit"/>
        /// </summary>
        Dispatched,

        /// <summary>
        /// Indicates that an <see cref="OfficerUnit"/> is on the scene of the event
        /// </summary>
        OnScene,

        /// <summary>
        /// Indicates that the call has been completed
        /// </summary>
        Completed
    }
}
