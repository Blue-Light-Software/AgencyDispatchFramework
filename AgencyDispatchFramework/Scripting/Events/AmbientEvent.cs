using System;

namespace AgencyDispatchFramework.Scripting.Events
{
    /// <summary>
    /// Base class for an ambient event that happens in the world near the player
    /// </summary>
    public abstract class AmbientEvent : IEventController, IDisposable, IEquatable<AmbientEvent>
    {
        /// <summary>
        /// Stores the current <see cref="Scripting.ActiveEvent"/>
        /// </summary>
        public ActiveEvent Event { get; set; }

        /// <summary>
        /// Gets a bool indicating whether this instance is disposed
        /// </summary>
        public bool IsDisposed { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        public abstract void Process();

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public virtual void Dispose()
        {
            IsDisposed = true;
        }

        public override int GetHashCode() => Event.GetHashCode();

        public override bool Equals(object obj) => Equals(obj as AmbientEvent);

        public bool Equals(AmbientEvent other) => (other == null) ? false : other.Event == Event;
    }
}
