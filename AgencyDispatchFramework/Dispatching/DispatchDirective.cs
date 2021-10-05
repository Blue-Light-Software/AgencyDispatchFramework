namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// Contains meta data on how this plugin should dispatch this <see cref="PriorityCall"/>
    /// for this <see cref="ServiceSector"/>
    /// </summary>
    public class DispatchDirective
    {
        /// <summary>
        /// Gets the number of <see cref="OfficerUnit"/>s required for this call
        /// </summary>
        public int TotalRequiredUnits { get; internal set; }

        /// <summary>
        /// The original <see cref="CallPriority"/>
        /// </summary>
        public CallPriority Priority { get; internal set; }

        /// <summary>
        /// Indicates wether the OfficerUnit should repsond code 3
        /// </summary>
        public ResponseCode ResponseCode { get; internal set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public DispatchDirective(int requiredUnits, CallPriority priority, ResponseCode response)
        {
            Priority = priority;
            ResponseCode = response;
            TotalRequiredUnits = requiredUnits;
        }
    }
}
