using System;

namespace AgencyDispatchFramework.Scripting.Events
{
    /// <summary>
    /// Base class for an ambient event that happens in the world near the player
    /// </summary>
    public abstract class AmbientEvent : IEventController, IEquatable<AmbientEvent>
    {
        /// <summary>
        /// Stores the current <see cref="Scripting.ActiveEvent"/>
        /// </summary>
        public ActiveEvent Event { get; set; }

        public abstract string Name { get; }

        /// <summary>
        /// 
        /// </summary>
        public abstract void Process();

        /// <summary>
        /// 
        /// </summary>
        public abstract void End();

        public override int GetHashCode() => Event.GetHashCode();

        public override bool Equals(object obj) => Equals(obj as AmbientEvent);

        public bool Equals(AmbientEvent other) => (other == null) ? false : other.Event == Event;
    }
}
