using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Scripting
{
    /// <summary>
    /// Contains the details to a specific <see cref="IEventScenario"/> that is active in the <see cref="GameWorld"/> 
    /// </summary>
    public sealed class ActiveEvent : IEquatable<ActiveEvent>
    {
        /// <summary>
        /// The unique call ID
        /// </summary>
        public int EventId { get; internal set; }

        /// <summary>
        /// The callout scenario for this call
        /// </summary>
        public EventScenarioMeta ScenarioMeta { get; set; }

        /// <summary>
        /// Gets the Callout handle
        /// </summary>
        internal IEventController Controller { get; set; }

        /// <summary>
        /// Gets the  <see cref="DateTime"/> when this call was created using Game Time
        /// </summary>
        public DateTime Created { get; internal set; }

        /// <summary>
        /// Gets the current <see cref="EventStatus"/> of this <see cref="ActiveEvent"/>
        /// </summary>
        public EventStatus Status { get; internal set; }

        /// <summary>
        /// The <see cref="WorldLocation"/> that this callout takes place or begins at
        /// </summary>
        public WorldLocation Location { get; internal set; }

        /// <summary>
        /// Gets the description of this event for the MDT
        /// </summary>
        public EventDescription Description { get; internal set; }

        /// <summary>
        /// Contains a list of all attached <see cref="PriorityCall"/>(s) to this event
        /// </summary>
        internal Dictionary<ServiceSector, PriorityCall> AttachedCalls { get; set; }

        /// <summary>
        /// Gets a value indicated whether the <see cref="ActiveEvent"/> has ended
        /// </summary>
        public bool HasEnded { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed
        /// </summary>
        public bool Disposed { get; internal set; }

        /// <summary>
        /// An event fired when this call is closed. This event is used to remove the call from
        /// ever <see cref="Dispatcher"/> that has added this call to its <see cref="Dispatcher.CallQueue" />
        /// </summary>
        internal event EventEndedHandler OnEnded;

        /// <summary>
        /// Creates a new instance of <see cref="ActiveEvent"/>
        /// </summary>
        /// <param name="id"></param>
        /// <param name="scenarioInfo"></param>
        internal ActiveEvent(int id, EventScenarioMeta scenarioInfo, WorldLocation location)
        {
            EventId = id;
            Created = Rage.World.DateTime;
            ScenarioMeta = scenarioInfo ?? throw new ArgumentNullException(nameof(scenarioInfo));
            Description = scenarioInfo.Descriptions.Spawn();
            Status = EventStatus.Created;
            Location = location;
            AttachedCalls = new Dictionary<ServiceSector, PriorityCall>();
        }

        /// <summary>
        /// Makes <see cref="Dispatch"/> aware of this <see cref="ActiveEvent"/>,
        /// and dispatch accordingly
        /// </summary>
        public void Report()
        {
            // Check to see if we have a call already!
            if (Status == EventStatus.Created)
            {
                Dispatch.Report(this);
                Status = EventStatus.Reported;
            }
        }

        /// <summary>
        /// Attaches the <see cref="PriorityCall"/> to this <see cref="ActiveEvent"/>
        /// </summary>
        /// <param name="sector"></param>
        /// <param name="call"></param>
        internal void AttachCall(ServiceSector sector, PriorityCall call)
        {
            AttachedCalls.AddOrUpdate(sector, call);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sector"></param>
        internal void RemoveCall(ServiceSector sector, EventClosedFlag flag)
        {
            // Attempt to remove the call
            if (AttachedCalls.Remove(sector))
            {
                // Was that the last sector?
                if (AttachedCalls.Count > 0) return;

                // End the event
                End(flag);
            }
        }

        /// <summary>
        /// Ends the call and calls the event <see cref="OnEnded"/>
        /// </summary>
        /// <param name="flag"></param>
        internal void End(EventClosedFlag flag)
        {
            if (!HasEnded)
            {
                HasEnded = true;

                // Fire event
                OnEnded?.Invoke(this, flag);

                // Dispose of this instance
                Dispose();
            }
        }

        internal void Dispose()
        {
            // Only dispose once, and on internal calls only
            if (Disposed || !HasEnded) return;

            // Flag
            Disposed = true;

            // Clear
            Controller = null;
            ScenarioMeta = null;
            Location = null;
            Description = null;
        }

        public override string ToString()
        {
            return ScenarioMeta?.ScenarioName;
        }

        public bool Equals(ActiveEvent other)
        {
            if (other == null) return false;
            return other.EventId == EventId;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as ActiveEvent);
        }

        public override int GetHashCode()
        {
            return EventId.GetHashCode();
        }
    }
}
