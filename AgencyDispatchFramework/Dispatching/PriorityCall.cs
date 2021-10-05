using AgencyDispatchFramework.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// 
    /// </summary>
    public class PriorityCall
    {
        /// <summary>
        /// Contains meta data on how this plugin should dispatch this <see cref="PriorityCall"/>
        /// for this <see cref="ServiceSector"/>
        /// </summary>
        internal DispatchDirective DispatchInfo { get; set; }

        /// <summary>
        /// Gets the handle of the <see cref="Scripting.ActiveEvent"/>
        /// </summary>
        public ActiveEvent EventHandle { get; set; }

        /// <summary>
        /// Gets a unique call id for this <see cref="PriorityCall"/>
        /// </summary>
        public int CallId { get; private set; }

        /// <summary>
        /// Gets the  <see cref="DateTime"/> when this call was created using Game Time
        /// </summary>
        public DateTime Created { get; internal set; }

        /// <summary>
        /// Gets the current <see cref="CallStatus"/> of this <see cref="PriorityCall"/>
        /// </summary>
        public CallStatus Status { get; internal set; }

        /// <summary>
        /// The current <see cref="CallPriority"/>
        /// </summary>
        public CallPriority Priority { get; internal set; }

        /// <summary>
        /// Indicates wether the OfficerUnit should repsond code 3
        /// </summary>
        public ResponseCode ResponseCode { get; set; }

        /// <summary>
        /// Gets the description of this event for the MDT
        /// </summary>
        public string MDTDescription { get; internal set; }

        /// <summary>
        /// Indicates whether this call was declined by the player
        /// </summary>
        public bool DeclinedByPlayer { get; internal set; }

        /// <summary>
        /// Gets the primary <see cref="OfficerUnit"/> assigned to this call
        /// </summary>
        public OfficerUnit PrimaryOfficer { get; private set; }

        /// <summary>
        /// Gets a list of officers attached to this <see cref="Scripting.ActiveEvent"/>
        /// </summary>
        private List<OfficerUnit> AttachedOfficers { get; set; }

        /// <summary>
        /// Gets the number of <see cref="OfficerUnit"/>s required for this call
        /// </summary>
        public int TotalRequiredUnits { get; internal set; }

        /// <summary>
        /// Indicates whether this Call needs more <see cref="OfficerUnit"/>(s)
        /// assigned to it.
        /// </summary>
        public bool NeedsMoreOfficers => AttachedOfficers.Count < TotalRequiredUnits;

        /// <summary>
        /// Gets the number of additional <see cref="OfficerUnit"/>s still required for this call
        /// </summary>
        public int NumberOfAdditionalUnitsRequired => Math.Max(TotalRequiredUnits - AttachedOfficers.Count, 0);

        /// <summary>
        /// Gets a value indicating whether this instance is disposed
        /// </summary>
        public bool Disposed { get; internal set; }

        /// <summary>
        /// An event fired when this call is closed. This event is used to remove the call from
        /// ever <see cref="Dispatcher"/> that has added this call to its <see cref="Dispatcher.CallQueue" />
        /// </summary>
        internal event CallEndedHandler OnEnded;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="directive"></param>
        internal PriorityCall(int callId, ActiveEvent activeEvent, DispatchDirective directive)
        {
            CallId = callId;
            AttachedOfficers = new List<OfficerUnit>(4);
            DispatchInfo = directive;
            TotalRequiredUnits = directive.TotalRequiredUnits;
            ResponseCode = directive.ResponseCode;
        }

        /// <summary>
        /// Assigns the provided <see cref="OfficerUnit"/> as the primary officer of the 
        /// call if there isnt one, or adds the officer to the <see cref="AttachedOfficers"/>
        /// list otherwise
        /// </summary>
        /// <param name="officer"></param>
        internal void AssignOfficer(OfficerUnit officer, bool forcePrimary)
        {
            // Do we have a primary? Or are we forcing one?
            if (PrimaryOfficer == null || forcePrimary)
            {
                PrimaryOfficer = officer;
            }

            // Attach officer
            AttachedOfficers.Add(officer);
        }

        /// <summary>
        /// Removes the specified <see cref="OfficerUnit"/> from the call. If the 
        /// <see cref="OfficerUnit"/> was the primary officer, and <see cref="AttachedOfficers"/>
        /// is populated, the topmost <see cref="OfficerUnit"/> will be the new
        /// <see cref="PrimaryOfficer"/>
        /// </summary>
        /// <param name="officer"></param>
        internal void RemoveOfficer(OfficerUnit officer)
        {
            // Do we need to assign a new primary officer?
            if (officer == PrimaryOfficer)
            {
                if (AttachedOfficers.Count > 1)
                {
                    // Dispatch one more AI unit to this call
                    var primary = AttachedOfficers.Where(x => x != PrimaryOfficer).FirstOrDefault();
                    if (primary != null)
                        PrimaryOfficer = primary;
                }
                else
                {
                    PrimaryOfficer = null;
                }
            }

            // Finally, remove
            AttachedOfficers.Remove(officer);
        }

        /// <summary>
        /// Ends the call for this <see cref="ServiceSector"/>
        /// </summary>
        /// <param name="flag"></param>
        internal void End(EventClosedFlag flag)
        {
            // Only dispose once, and on internal calls only
            if (Disposed) return;

            // Flag
            Disposed = true;

            // Fire event
            OnEnded?.Invoke(this, flag);

            // Clear
            AttachedOfficers.Clear();
            AttachedOfficers = null;
            PrimaryOfficer = null;
            MDTDescription = null;
        }

        /// <summary>
        /// Gets a list of officers attached to this <see cref="Scripting.ActiveEvent"/>
        /// </summary>
        public OfficerUnit[] GetAttachedOfficers() => AttachedOfficers.ToArray();
    }
}
