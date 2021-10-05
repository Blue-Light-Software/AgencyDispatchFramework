namespace AgencyDispatchFramework.Scripting
{
    public interface IEventController
    {
        /// <summary>
        /// 
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the <see cref="ActiveEvent"/> of this instance
        /// </summary>
        ActiveEvent Event { get; }

        /// <summary>
        /// On tick processing method
        /// </summary>
        void Process();

        /// <summary>
        /// 
        /// </summary>
        void End();
    }
}
