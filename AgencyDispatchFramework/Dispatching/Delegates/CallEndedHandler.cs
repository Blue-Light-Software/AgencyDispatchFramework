namespace AgencyDispatchFramework.Scripting
{
    /// <summary>
    /// A delegate to handle the closing of a call
    /// </summary>
    /// <param name="details">The call that has ended</param>
    /// <param name="closeFlag">How the call was ended</param>
    internal delegate void EventEndedHandler(ActiveEvent details, EventClosedFlag closeFlag);
}
