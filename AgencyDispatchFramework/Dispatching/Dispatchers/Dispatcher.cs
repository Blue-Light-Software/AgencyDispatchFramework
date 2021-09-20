﻿using AgencyDispatchFramework.Scripting;
using AgencyDispatchFramework.Simulation;
using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// A base class that is responsible for dispatching officer units for
    /// a single <see cref="Simulation.Agency"/> within its jurisdiction.
    /// </summary>
    internal abstract class Dispatcher : IDisposable
    {
        /// <summary>
        /// Our lock object to prevent threading issues
        /// </summary>
        protected object _threadLock = new object();

        /// <summary>
        /// Indicates whether this instance is disposed or not
        /// </summary>
        internal bool IsDisposed { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Dispatching.Agency"/> that this
        /// instance is dispatching for
        /// </summary>
        public Agency Agency { get; set; }

        /// <summary>
        /// Gets a list of calls this instance is responsible for handling
        /// </summary>
        public HashSet<ActiveEvent> CallQueue { get; set; }

        /// <summary>
        /// Gets a list of calls this instance has raised
        /// </summary>
        public HashSet<ActiveEvent> RaisedCalls { get; set; }

        /// <summary>
        /// Event fired when a <see cref="ActiveEvent"/> needs additional resources
        /// that the <see cref="Agency"/> is unable to provide
        /// </summary>
        public static event CallRaisedHandler OnCallRaised;

        /// <summary>
        /// Method called every tick to manage calls
        /// </summary>
        public abstract void Process(DateTime currentTime);

        /// <summary>
        /// Creates a new instance of <see cref="Dispatcher"/>
        /// </summary>
        /// <exception cref="ArgumentException">thrown if the <paramref name="agency"/> param is null</exception>
        public Dispatcher(Agency agency)
        {
            // Set internals
            Agency = agency ?? throw new ArgumentNullException(nameof(agency));
            CallQueue = new HashSet<ActiveEvent>(12);
            RaisedCalls = new HashSet<ActiveEvent>();
        }

        /// <summary>
        /// Adds the call to the <see cref="CallQueue"/> safely
        /// </summary>
        /// <param name="call"></param>
        /// <returns>true if the call was added, false if the call already existed in the call queue.</returns>
        public virtual bool AddCall(ActiveEvent call)
        {
            // Stop if we are disposed
            if (IsDisposed) throw new ObjectDisposedException(nameof(Dispatcher));

            // Add call to priority Queue
            lock (_threadLock)
            {
                // Returns false if the call already exists in the queue
                if (CallQueue.Add(call))
                {
                    // Register for the end event
                    call.OnEnded += Call_OnCallEnded;

                    // Log
                    Log.Debug($"{Agency.ScriptName.ToUpper()} Dispatcher: Added Call to Queue '{call.ScenarioMeta.ScenarioName}' in zone '{call.Location.Zone.DisplayName}'");
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Removes the call to the <see cref="CallQueue"/> safely
        /// </summary>
        /// <param name="call"></param>
        public virtual void RemoveCall(ActiveEvent call)
        {
            // Stop if we are disposed
            if (IsDisposed) throw new ObjectDisposedException(nameof(Dispatcher));

            // Add call to priority Queue
            lock (_threadLock)
            {
                // This should always be true, but...
                if (CallQueue.Remove(call))
                {
                    // Unregister
                    call.OnEnded -= Call_OnCallEnded;

                    // Log
                    Log.Debug($"{Agency.ScriptName.ToUpper()} Dispatcher: Removed Call from Queue '{call.ScenarioMeta.ScenarioName}' in zone '{call.Location.Zone.DisplayName}'");
                }

                // Attempt to remove from raised
                RaisedCalls.Remove(call);
            }
        }

        /// <summary>
        /// Raises a call to <see cref="Dispatch"/> so that other <see cref="Dispatching.Agency"/>
        /// instances can assist in the call.
        /// </summary>
        /// <param name="call"></param>
        /// <param name="args"></param>
        protected virtual void RaiseCall(ActiveEvent call, CallRaisedEventArgs args)
        {
            // Ensure we dont spam
            if (!RaisedCalls.Contains(call))
            {
                // Add the call, no need to lock
                RaisedCalls.Add(call);

                // Raise this up!
                OnCallRaised?.Invoke(Agency, call, args);
            }
        }

        /// <summary>
        /// Event method called when a call is ended by the <see cref="Dispatch"/> class
        /// </summary>
        /// <param name="call"></param>
        /// <param name="closeFlag"></param>
        protected virtual void Call_OnCallEnded(ActiveEvent call, EventClosedFlag closeFlag)
        {
            RemoveCall(call);
        }

        /// <summary>
        /// Dispatches the provided <see cref="OfficerUnit"/> to the provided
        /// <see cref="ActiveEvent"/>. If the <paramref name="officer"/> is
        /// the Player, then the callout is started
        /// </summary>
        /// <param name="officer"></param>
        /// <param name="call"></param>
        public virtual void AssignUnitToCall(OfficerUnit officer, ActiveEvent call)
        {
            // Stop if we are disposed
            if (IsDisposed) throw new ObjectDisposedException(nameof(Dispatcher));

            // TODO: Secondary dispatching
            // Assign the officer to the call
            officer.AssignToCall(call, !officer.IsAIUnit);

            // If player, tell dispatch so it can play the radio
            if (!officer.IsAIUnit)
            {
                Dispatch.AssignedPlayerToCall(call);
            }
        }

        /// <summary>
        /// Disposes this instance
        /// </summary>
        public virtual void Dispose()
        {
            // Check
            if (IsDisposed)
                return;

            // Flag
            IsDisposed = true;

            // Remove call log, and unregister for events
            foreach (var call in CallQueue)
            {
                call.OnEnded -= Call_OnCallEnded;
            }

            // Clear call queue
            CallQueue.Clear();
            CallQueue = null;

            RaisedCalls.Clear();
            RaisedCalls = null;
        }
    }
}
