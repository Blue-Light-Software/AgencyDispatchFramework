using AgencyDispatchFramework.Dispatching.Assignments;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Scripting;
using AgencyDispatchFramework.Simulation;
using LSPD_First_Response.Engine.Scripting.Entities;
using Rage;
using System;

namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// Represents an Officer unit that a <see cref="Dispatcher"/> will send to 
    /// respond to <see cref="ActiveEvent"/>(s)
    /// </summary>
    public abstract class OfficerUnit : IDisposable, IEquatable<OfficerUnit>
    {
        private static int OfficerCounter = 0;

        /// <summary>
        /// A unique officer ID
        /// </summary>
        protected int OfficerId { get; private set; }

        /// <summary>
        /// Indicates whether this instance is disposed
        /// </summary>
        public bool IsDisposed { get; private set; } = false;

        /// <summary>
        /// Indicates whether this <see cref="OfficerUnit"/> is an AI
        /// ped or the <see cref="Rage.Game.LocalPlayer"/>
        /// </summary>
        public abstract bool IsAIUnit { get; }

        /// <summary>
        /// Gets the first name of this <see cref="OfficerUnit"/>
        /// </summary>
        public Persona Persona { get; internal set; }

        /// <summary>
        /// Gets the <see cref="Agency"/> of this <see cref="OfficerUnit"/>
        /// </summary>
        public Agency Agency { get; internal set; }

        /// <summary>
        /// Gets the formatted Division-UnitType-Beat for this unit to be used in strings
        /// </summary>
        public CallSign CallSign { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public UnitType PrimaryRole { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public UnitType SecondaryRole { get; internal set; }

        /// <summary>
        /// Gets the officers current <see cref="OfficerStatus"/>
        /// </summary>
        public OfficerStatus Status { get; internal set; }

        /// <summary>
        /// Gets or sets the current assignment this <see cref="OfficerUnit"/>
        /// </summary>
        public BaseAssignment Assignment { get; internal set; }

        /// <summary>
        /// Gets the last <see cref="Rage.Game.DateTime"/> this officer was tasked with something
        /// </summary>
        public DateTime LastStatusChange { get; internal set; }

        /// <summary>
        /// Gets the current <see cref="ActiveEvent"/> if any this unit is assigned to
        /// </summary>
        public ActiveEvent CurrentCall { get; protected set; }

        /// <summary>
        /// 
        /// </summary>
        internal District District { get; set; }

        /// <summary>
        /// Temporary
        /// </summary>
        internal DateTime NextStatusChange { get; set; }

        /// <summary>
        /// Used internally by Dispatch when deciding to pull officer units
        /// from thier current assignments to send to higher priorty calls
        /// </summary>
        internal DispatchPriority Priority { get; set; }

        /// <summary>
        /// Contains the Shift hours for this unit
        /// </summary>
        protected bool EndingDuty { get; set; } = false;

        /// <summary>
        /// Gets the position of this <see cref="OfficerUnit"/>
        /// </summary>
        protected Vector3 Position { get; set; }

        /// <summary>
        /// Gets the <see cref="ShiftRotation"/> for this <see cref="OfficerUnit"/>
        /// </summary>
        internal ShiftRotation Shift { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="OfficerUnit"/> for an AI unit
        /// </summary>
        internal OfficerUnit(Agency agency, CallSign callSign, ShiftRotation shift, UnitType primaryRole, District district)
        {
            Agency = agency;
            CallSign = callSign;
            Shift = shift;
            PrimaryRole = primaryRole;
            District = district;

            OfficerId = OfficerCounter++;
        }

        /// <summary>
        /// Gets the position of this <see cref="OfficerUnit"/>
        /// </summary>
        public virtual Vector3 GetPosition()
        {
            return Position;
        }

        /// <summary>
        /// Starts the Task unit fiber for this AI Unit
        /// </summary>
        internal virtual void StartDuty(Vector3 startPosition)
        {
            EndingDuty = false;
            Position = startPosition;
            LastStatusChange = World.DateTime;
            Status = OfficerStatus.Available;

            Log.Info($"Unit {CallSign} starting duty");
        }

        /// <summary>
        /// Ends duty for this officer unit
        /// </summary>
        internal virtual void EndDuty()
        {
            EndingDuty = true;
        }

        /// <summary>
        /// Method called every tick on the AI Fiber Thread
        /// </summary>
        /// <param name="gameTime"></param>
        internal abstract void OnTick(DateTime gameTime);

        /// <summary>
        /// Returns whether this <see cref="OfficerUnit"/> is on shift based
        /// off the passed <see cref="TimeSpan.Hours"/>
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public virtual bool IsOnShift(DateTime now)
        {
            switch (Shift)
            {
                default:
                case ShiftRotation.Day: return now.IsBetween(TimeSpan.FromHours(6), TimeSpan.FromHours(16));
                case ShiftRotation.Night: return now.IsBetween(TimeSpan.FromHours(21), TimeSpan.FromHours(7));
                case ShiftRotation.Swing: return now.IsBetween(TimeSpan.FromHours(15), TimeSpan.FromHours(1));
            }
        }

        /// <summary>
        /// Returns whether this <see cref="OfficerUnit"/> is off duty or ending shift within
        /// the next hour
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public virtual bool IsNearingEndOfShift(DateTime now)
        {
            if (EndingDuty) return true;

            switch (Shift)
            {
                default:
                case ShiftRotation.Day: return !now.IsBetween(TimeSpan.FromHours(6), TimeSpan.FromHours(15));
                case ShiftRotation.Night: return !now.IsBetween(TimeSpan.FromHours(21), TimeSpan.FromHours(6));
                case ShiftRotation.Swing: return !now.IsBetween(TimeSpan.FromHours(15), new TimeSpan(23, 59, 59));
            }
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~OfficerUnit()
        {
            Dispose();
        }

        /// <summary>
        /// Our Dispose method
        /// </summary>
        public virtual void Dispose()
        {
            if (!IsDisposed)
            {
                IsDisposed = true;
            }
        }

        /// <summary>
        /// Assigns this officer to the specified call
        /// </summary>
        /// <param name="call"></param>
        internal virtual void AssignToCall(ActiveEvent call, bool forcePrimary = false)
        {
            // Did we get called on for a more important assignment?
            if (CurrentCall != null)
            {
                // Put this here to remove a potential null problem later
                var currentCall = CurrentCall;

                // Is this unit the primary on a lesser important call?
                if (currentCall.PrimaryOfficer == this && (int)currentCall.CurrentPriority > 2)
                {
                    if (currentCall.Status == EventStatus.OnScene)
                    {
                        // @todo : If more than 50% complete, close call
                        var flag = (call.CurrentPriority < currentCall.CurrentPriority) ? EventClosedFlag.Premature : EventClosedFlag.Forced;
                        CompleteCall(flag);
                    }
                }

                // Back out of call
                currentCall.RemoveOfficer(this);
            }

            // Set flags
            call.AssignOfficer(this, forcePrimary);
            call.Status = EventStatus.Dispatched;

            Assignment = new AssignedToCall(call);
            CurrentCall = call;
            Status = OfficerStatus.Dispatched;
            LastStatusChange = World.DateTime;
        }

        /// <summary>
        /// Clears the current call, DOES NOT SIGNAL DISPATCH
        /// </summary>
        /// <remarks>
        /// This method is called by <see cref="Dispatch"/> for the Player ONLY.
        /// AI units call it themselves
        /// </remarks>
        internal virtual void CompleteCall(EventClosedFlag flag)
        {
            // Clear last call
            CurrentCall = null;
            LastStatusChange = World.DateTime;
        }

        internal void SetCallSign(CallSign callSign)
        {
            CallSign = callSign;
        }

        public override int GetHashCode()
        {
            return OfficerId;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as OfficerUnit);
        }

        public bool Equals(OfficerUnit other)
        {
            if (other == null) return false;
            return other.OfficerId == OfficerId;
        }
    }
}
