namespace AgencyDispatchFramework.Scripting
{
    public interface IEventController
    {
        /// <summary>
        /// Gets the <see cref="Scripting.ActiveEvent"/> of this instance
        /// </summary>
        ActiveEvent Event { get; }

        /// <summary>
        /// On tick processing method
        /// </summary>
        void Process();
    }
}
