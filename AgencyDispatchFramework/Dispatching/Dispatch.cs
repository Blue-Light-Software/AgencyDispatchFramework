﻿using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.NativeUI;
using AgencyDispatchFramework.Scripting;
using AgencyDispatchFramework.Simulation;
using LSPD_First_Response.Mod.API;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AgencyDispatchFramework
{
    /// <summary>
    /// A class that handles the dispatching of Callouts based on the current 
    /// <see cref="AgencyType"/> in thier Jurisdiction.
    /// </summary>
    public static class Dispatch
    {
        /// <summary>
        /// Our lock object to prevent multi-threading issues
        /// </summary>
        private static object _threadLock = new object();

        /// <summary>
        /// Do write to this property directly!! Not thread safe
        /// </summary>
        private static int _timeCalloutWaiting = 0;

        /// <summary>
        /// Do write to this property directly!! Not thread safe
        /// </summary>
        /// <remarks>
        /// Set high to start, so the player can immediatly be dispatched
        /// </remarks>
        private static int _timeSinceCalloutAttempt = 9999;

        /// <summary>
        /// Contains the last Call ID used
        /// </summary>
        private static int NextCallId { get; set; }

        /// <summary>
        /// Gets a value in seconds that have elapsed since a callout was declined by the player
        /// </summary>
        public static int TimeSinceLastCalloutAttempt
        {
            get => _timeSinceCalloutAttempt;
            private set => Interlocked.Exchange(ref _timeSinceCalloutAttempt, value);
        }

        /// <summary>
        /// Gets a value in seconds that have passed since a callout was last dispatched to the player,
        /// waiting for acceptance. If a call is not currently awaiting a players acceptance, this value
        /// will always read zero.
        /// </summary>
        public static int TimeCalloutWaiting
        {
            get => _timeCalloutWaiting;
            private set => Interlocked.Exchange(ref _timeCalloutWaiting, value);
        }

        /// <summary>
        /// Indicates whether an external LSPDFR callout is detected as running, and not handled by <see cref="Dispatch"/>
        /// </summary>
        public static bool IsExternalCalloutRunning { get; private set; }

        /// <summary>
        /// Indicates whether a callout is being displayed to the player currently
        /// </summary>
        public static bool IsCalloutBeingDisplayed { get; private set; }

        /// <summary>
        /// Indicates whether the player is actively on a callout, or is being displayed a callout for acceptance.
        /// </summary>
        public static bool IsPlayerOnACallout => (IsExternalCalloutRunning || ActivePlayerCall != null || IsCalloutBeingDisplayed);

        /// <summary>
        /// Gets the value of <see cref="Functions.IsPlayerAvailableForCalls()"/> value before dispatching
        /// a player to a <see cref="Scripting.ActiveEvent"/>, so we can set it back afterwards
        /// </summary>
        private static bool? PreviousLSPDFRAvailability { get; set; }

        /// <summary>
        /// Gets the player's current selected <see cref="Agency"/>
        /// </summary>
        public static Agency PlayerAgency { get; private set; }

        /// <summary>
        /// Event called when a call is added to the call list
        /// </summary>
        public static event CallListUpdateHandler OnCallAdded;

        /// <summary>
        /// Event called when a call is removed from the call list
        /// </summary>
        public static event CallListUpdateHandler OnCallCompleted;

        /// <summary>
        /// Event called when a call is removed from the call list due to expiration
        /// </summary>
        public static event CallListUpdateHandler OnCallExpired;

        /// <summary>
        /// Event called when a call is accepted by the player
        /// </summary>
        public static event CallListUpdateHandler OnPlayerCallAccepted;

        /// <summary>
        /// Event called when a call is completed by the player
        /// </summary>
        public static event CallListUpdateHandler OnPlayerCallCompleted;

        /// <summary>
        /// Event called when a shift is begining
        /// </summary>
        internal static event ShiftUpdateHandler OnShiftStart;

        /// <summary>
        /// Event called when a shift is begining
        /// </summary>
        internal static event ShiftUpdateHandler OnShiftEnd;

        /// <summary>
        /// Gets or sets the <see cref="RegionCrimeGenerator"/>, responsible for spawning
        /// <see cref="Scripting.ActiveEvent"/>s
        /// </summary>
        internal static RegionCrimeGenerator CrimeGenerator { get; set; }

        /// <summary>
        /// Contains a list of zones currently loaded and handled by the active agencies
        /// </summary>
        internal static List<WorldZone> Zones { get; set; }

        /// <summary>
        /// Gets the overall crime level definition for the current <see cref="Agency"/>
        /// </summary>
        public static CrimeLevel CurrentCrimeLevel => CrimeGenerator.CurrentCrimeLevel;

        /// <summary>
        /// The <see cref="GameFiber"/> that runs the logic for all the AI units
        /// </summary>
        private static GameFiber AISimulationFiber { get; set; }

        /// <summary>
        /// Contains the active agencies by type
        /// </summary>
        private static Dictionary<string, Agency> EnabledAgenciesByName { get; set; }

        /// <summary>
        /// Gets the player's <see cref="OfficerUnit"/> instance
        /// </summary>
        public static OfficerUnit PlayerUnit { get; private set; }

        /// <summary>
        /// Gets the current <see cref="ShiftRotation"/>
        /// </summary>
        internal static ShiftRotation CurrentShift { get; set; }

        /// <summary>
        /// Contains a hashtable of currently active <see cref="ShiftRotation"/>s
        /// </summary>
        private static Dictionary<ShiftRotation, bool> ActiveShifts { get; set; }

        /// <summary>
        /// Our open calls list
        /// </summary>
        private static Dictionary<CallPriority, List<PriorityCall>> OpenCalls { get; set; }

        /// <summary>
        /// Contains the priority call being dispatched to the player currently
        /// </summary>
        public static PriorityCall ActivePlayerCall { get; private set; }

        /// <summary>
        /// Contains the priority call that needs to be dispatched to the player on next Tick
        /// </summary>
        private static PriorityCall InvokeForPlayer { get; set; }

        /// <summary>
        /// Signals that the next callout should be given to the player, regardless of distance
        /// or player availability.
        /// </summary>
        private static bool SendNextCallToPlayer { get; set; } = false;

        /// <summary>
        /// Static method called the first time this class is referenced anywhere
        /// </summary>
        static Dispatch()
        {
            // Create call Queue
            // See also: https://grantpark.org/info/16029
            OpenCalls = new Dictionary<CallPriority, List<PriorityCall>>();
            foreach (CallPriority priority in Enum.GetValues(typeof(CallPriority)))
            {
                OpenCalls.Add(priority, new List<PriorityCall>());
            }

            // Create agency lookup
            EnabledAgenciesByName = new Dictionary<string, Agency>();
            ActiveShifts = new Dictionary<ShiftRotation, bool>();
            foreach (ShiftRotation period in Enum.GetValues(typeof(ShiftRotation)))
            {
                ActiveShifts.Add(period, false);
            }

            // Register for event
            Dispatcher.OnCallRaised += Dispatcher_OnCallRaised;

            // Create next random call ID
            var randomizer = new CryptoRandom();
            NextCallId = randomizer.Next(21234, 34567);
        }

        #region Public API Methods

        /// <summary>
        /// Sets the player's status
        /// </summary>
        /// <param name="status"></param>
        public static void SetPlayerStatus(OfficerStatus status)
        {
            // Ensure we are on duty before we do anything
            if (!Main.OnDutyLSPDFR) return;

            // Update player status
            PlayerUnit.Status = status;
            PlayerUnit.LastStatusChange = World.DateTime;

            // @todo: Tell LSPDFR whats going on 
        }

        /// <summary>
        /// Gets the <see cref="PlayerOfficerUnit"/>s status
        /// </summary>
        /// <returns></returns>
        public static OfficerStatus GetPlayerStatus()
        {
            if (!Main.OnDutyLSPDFR) return OfficerStatus.EndingDuty;
            return PlayerUnit?.Status ?? OfficerStatus.OutOfService;
        }

        /// <summary>
        /// Sets player's call sign.
        /// </summary>
        public static bool SetPlayerCallSign(CallSign callSign)
        {
            // Ensure we are on duty before we do anything
            if (!Main.OnDutyLSPDFR) return false;

            // Get player Agency, and ensure call sign type matches
            if (PlayerAgency.CallSignStyle != callSign.Style)
                return false;

            // Set new callsign
            PlayerUnit.SetCallSign(callSign);
            return true;
        }

        /// <summary>
        /// Gets all the calls by the specified priority
        /// </summary>
        /// <param name="priority"></param>
        /// <returns></returns>
        public static PriorityCall[] GetCallList(CallPriority priority)
        {
            return OpenCalls[priority].ToArray();
        }

        /// <summary>
        /// Gets the number of calls in an array by priority
        /// </summary>
        /// <returns></returns>
        public static Dictionary<CallPriority, int> GetCallCount()
        {
            return OpenCalls.ToDictionary(x => x.Key, y => y.Value.Count);
        }

        /// <summary>
        /// Gets an array of <see cref="Agency"/> instances that are currently
        /// being simulated
        /// </summary>
        /// <returns>If the player is not on duty, this method return null</returns>
        public static Agency[] GetEnabledAgencies()
        {
            return (Main.OnDutyLSPDFR) ? EnabledAgenciesByName.Values.ToArray() : null;
        }

        /// <summary>
        /// Determines if the specified <see cref="Agency"/> can be dispatched to a call.
        /// </summary>
        /// <param name="agency"></param>
        /// <param name="call"></param>
        /// <returns></returns>
        public static bool CanAssignAgencyToCall(Agency agency, PriorityCall call)
        {
            // Check
            switch (agency.AgencyType)
            {
                case AgencyType.CountySheriff:
                    // Sheriffs can take this call IF not assigned already, or the
                    // primary agency was dispatched already but needs support
                    return (call.Priority == CallPriority.Immediate || call.PrimaryOfficer == null || call.NeedsMoreOfficers);
                case AgencyType.CityPolice:
                    // City police can only take calls in their primary jurisdiction
                    return agency.Zones.Contains(call.EventHandle.Location.Zone);
                case AgencyType.HighwayPatrol:
                    return call.EventHandle.ScenarioMeta.Category == CallCategory.Traffic;
                default:
                    return call.EventHandle.ScenarioMeta.DispatchDirectives.ContainsKey(ServiceSector.Police);
            }
        }

        /// <summary>
        /// Invokes a random callout from the CallQueue to the player
        /// </summary>
        /// <returns></returns>
        public static bool InvokeAnyCalloutForPlayer()
        {
            if (CanInvokeAnyCalloutForPlayer(true))
            {
                // Cache player location
                var location = Rage.Game.LocalPlayer.Character.Position;

                // Try and find a call for the player
                foreach (var callList in OpenCalls)
                {
                    // Get call list
                    if (callList.Value.Count == 0)
                        continue;

                    // Filter calls
                    var list = callList.Value.Where(x => CanPlayerHandleCall(x) && x.NeedsMoreOfficers).ToArray();
                    if (list.Length == 0)
                        continue;

                    // Order calls by distance from player
                    var call = list.OrderBy(x => (x.DeclinedByPlayer) ? 1 : 0)
                        .ThenBy(x => x.EventHandle.Location.Position.DistanceTo(location))
                        .FirstOrDefault();

                    // Invoke callout
                    InvokeForPlayer = call;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Signals that the player is to be dispatched to a callout as soon as possible.
        /// </summary>
        /// <param name="dispatched">
        /// If true, then a call is immediatly dispatched to the player. If false, 
        /// the player will be dispatched to the very next incoming call.
        /// </param>
        /// <returns>Returns true if the player is available and can be dispatched to a call, false otherwise</returns>
        public static bool InvokeNextCalloutForPlayer(out bool dispatched)
        {
            if (CanInvokeAnyCalloutForPlayer(true))
            {
                if (!InvokeAnyCalloutForPlayer())
                {
                    // If we are here, we did not find a call. Signal dispatch to send next call
                    SendNextCallToPlayer = true;
                    dispatched = false;
                }
                else
                {
                    dispatched = true;
                }

                return true;
            }

            dispatched = false;
            return false;
        }

        /// <summary>
        /// On the next call check, the specified call will be dispatched to the player.
        /// If the player is already on a call that is in progress, this method does nothing
        /// and returns false.
        /// </summary>
        /// <param name="call"></param>
        /// <returns></returns>
        public static bool InvokeCallForPlayer(PriorityCall call)
        {
            if (CanPlayerHandleCall(call))
            {
                InvokeForPlayer = call;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Indicates whether or not a <see cref="Callout"/> can be invoked for the Player
        /// dependant on thier current call status, cooldown timer between declined callouts,
        /// and whether or not a callout is currenlty being displayed to the player.
        /// </summary>
        /// <param name="ignoreCalloutTimeout">If true, the result will not take into account the timeout between callout attempts.</param>
        /// <returns></returns>
        public static bool CanInvokeAnyCalloutForPlayer(bool ignoreCalloutTimeout = false)
        {
            // Do we ignore the cooldown period?
            if (!ignoreCalloutTimeout && TimeSinceLastCalloutAttempt < Settings.TimeoutBetweenCalloutAttempts)
            {
                return false;
            }

            // Acceptable status'
            CallStatus[] acceptable = { CallStatus.Completed, CallStatus.Dispatched, CallStatus.Waiting };

            // Is an external callout running?
            if (IsCalloutBeingDisplayed)
            {
                // Maybe an external call is being displayed?
                if (ActivePlayerCall == null)
                {
                    return false;
                }
                else
                {
                    // We will allow the by-pass
                    return acceptable.Contains(ActivePlayerCall.Status);
                }
            }
            else if (IsExternalCalloutRunning)
            {
                return false;
            }

            // default
            return ActivePlayerCall == null || acceptable.Contains(ActivePlayerCall.Status);
        }

        /// <summary>
        /// Indicates whether or not a <see cref="Callout"/> can be invoked for the Player
        /// dependant on thier current assignment, call status and jurisdiction limits.
        /// </summary>
        /// <returns></returns>
        public static bool CanPlayerHandleCall(PriorityCall call)
        {
            // If a call is less priority than a players current assignment
            if (PlayerUnit.Assignment != null && (int)call.Priority >= (int)PlayerUnit.Assignment.Priority)
            {
                return false;
            }

            // Check to make sure the agency can preform this type of call
            if (!call.EventHandle.ScenarioMeta.DispatchDirectives.ContainsKey(PlayerAgency.Sector))
            {
                return false;
            }

            // Check jurisdiction
            return call.EventHandle.Location.Zone.DoesAgencyHaveJurisdiction(PlayerAgency);
        }

        #endregion Public API Methods

        #region Public API Callout Methods

        /// <summary>
        /// Requests a <see cref="Scripting.ActiveEvent"/> from dispatch using the specified
        /// <see cref="Callout"/> type.
        /// </summary>
        /// <param name="calloutType"></param>
        /// <returns></returns>
        public static PriorityCall RequestPlayerCallInfo(IEventController controller)
        {
            // Extract Callout Name
            string controllerName = controller.Name;

            // Have we sent a call to the player?
            if (ActivePlayerCall != null)
            {
                // Do our types match?
                if (controllerName.Equals(ActivePlayerCall.EventHandle.ScenarioMeta.ControllerName))
                {
                    return ActivePlayerCall;
                }
                else if (ActivePlayerCall.Status != CallStatus.Waiting)
                {
                    // This call is currently running!
                    EndPlayerCallout();
                    ActivePlayerCall = null;
                    Log.Error($"Dispatch.RequestPlayerCallInfo: Player was already on a call out when a request from {controllerName} was recieved");
                }
                else
                {
                    // Cancel
                    ActivePlayerCall.DeclinedByPlayer = true;
                    ActivePlayerCall.Status = CallStatus.Created;
                    ActivePlayerCall = null;
                    Log.Warning($"Dispatch.RequestPlayerCallInfo: Player active call type does not match callout of type {controllerName}");
                }
            }

            // If we are here, maybe the player used a console command to start the callout.
            // At this point, see if we have anything in the call Queue
            if (ScriptEngine.Callouts.ScenariosByCalloutName.ContainsKey(controllerName))
            {
                CallStatus[] status = { CallStatus.Created, CallStatus.Waiting, CallStatus.Dispatched };
                var playerPosition = Rage.Game.LocalPlayer.Character.Position;

                // Lets see if we have a call already created
                foreach (var callList in OpenCalls)
                {
                    var calls = (
                        from x in callList.Value
                        where x.EventHandle.Controller.Name.Equals(controllerName) && status.Contains(x.Status)
                        orderby x.EventHandle.Location.Position.DistanceTo(playerPosition) ascending
                        select x
                    ).ToArray();

                    // Do we have any active calls?
                    if (calls.Length > 0)
                    {
                        ActivePlayerCall = calls[0];
                        return calls[0];
                    }
                }

                /*

                // Still here? Maybe we can create a call?
                if (ScriptEngine.Callouts.ScenariosByCalloutName[controllerName].TrySpawn(out EventScenarioMeta scenarioInfo))
                {
                    // Log
                    Log.Info($"Dispatch.RequestPlayerCallInfo: It appears that the player requested a callout of type '{controllerName}' using an outside source. Creating a call out of thin air");

                    // Create the call
                    var eventt = CrimeGenerator.CreateEventFromScenario(scenarioInfo);
                    if (eventt == null)
                    {
                        return null;
                    }

                    // Add call to priority Queue
                    lock (_threadLock)
                    {
                        OpenCalls[eventt.DispatchInfo.Priority].Add(eventt);
                        Log.Debug($"Dispatch: Added Call to Queue '{eventt.EventHandle.ScenarioMeta.ScenarioName}' in zone '{eventt.EventHandle.Location.Zone.DisplayName}'");

                        // Invoke the next callout for player
                        InvokeForPlayer = eventt;
                    }

                    return eventt;
                }

                */
            }

            // cant do anything at this point
            Log.Warning($"Dispatch.RequestCallInfo(): Unable to spawn a scenario for {controllerName}");
            return null;
        }

        /// <summary>
        /// Tells dispatch that the call is complete
        /// </summary>
        /// <param name="call"></param>
        public static void RegisterCallComplete(PriorityCall call)
        {
            // Ensure the call is not null. This can happen when trying
            // to initiate a scenario from a menu
            if (call == null)
            {
                Log.Error("Dispatch.RegisterCallComplete(): Tried to reference a call that is null");
                return;
            }

            // End call
            call.End(EventClosedFlag.Completed);
        }

        /// <summary>
        /// Tells dispatch that we are on scene
        /// </summary>
        /// <param name="call"></param>
        public static void RegisterOnScene(OfficerUnit unit, PriorityCall call)
        {
            if (unit == PlayerUnit)
            {
                SetPlayerStatus(OfficerStatus.OnScene);
            }
            else
            {
                unit.Status = OfficerStatus.OnScene;
                unit.LastStatusChange = World.DateTime;
            }

            // Update call status
            call.Status = CallStatus.OnScene;
        }

        /// <summary>
        /// Tells dispatch that we are on scene
        /// </summary>
        /// <param name="call"></param>
        /// <remarks>Used for Player only, not AI</remarks>
        public static void CalloutAccepted(PriorityCall call, IEventController callout)
        {
            // Clear radio block
            Scanner.IsWaitingPlayerResponse = false;
            TimeCalloutWaiting = 0;

            // Player is always primary on a callout
            PlayerUnit.AssignToCall(call, true);
            SetPlayerStatus(OfficerStatus.Dispatched);

            // Assign callout property
            call.EventHandle.Controller = callout;

            // Fire event
            OnPlayerCallAccepted?.Invoke(call);
        }

        /// <summary>
        /// Tells dispatch that did not accept the callout
        /// </summary>
        /// <param name="call"></param>
        /// <remarks>Used for Player only, not AI</remarks>
        public static void CalloutNotAccepted(PriorityCall call)
        {
            // Cancel callout
            if (ActivePlayerCall != null && call == ActivePlayerCall)
            {
                // Remove player from call
                call.Status = CallStatus.Created;
                call.DeclinedByPlayer = true;
                SetPlayerStatus(OfficerStatus.Available);

                // Remove player
                call.RemoveOfficer(PlayerUnit);

                // Ensure this is null
                ActivePlayerCall = null;
                TimeCalloutWaiting = 0;
                Log.Info($"Dispatch: Player declined callout scenario {call.EventHandle.ScenarioMeta.ScenarioName}");

                // Clear radio block
                Scanner.IsWaitingPlayerResponse = false;
            }
        }

        /// <summary>
        /// Tells dispatch that the current assignment given to the player is finished.
        /// </summary>
        public static void ClearPlayerAssignment()
        {
            // Set previous value
            if (PreviousLSPDFRAvailability != null)
                Functions.SetPlayerAvailableForCalls(PreviousLSPDFRAvailability.Value);

            // Reset
            PreviousLSPDFRAvailability = null;
        }

        /// <summary>
        /// Ends the current callout that is running for the player
        /// </summary>
        public static void EndPlayerCallout()
        {
            bool calloutRunning = Functions.IsCalloutRunning();
            if (ActivePlayerCall != null)
            {
                // Do we have a callout instance?
                if (ActivePlayerCall.EventHandle.Controller != null)
                {
                    ActivePlayerCall.EventHandle.Controller.End();
                }
                else if (calloutRunning)
                {
                    Log.Info("Dispatch.EndPlayerCallout(): No Player Active Callout. Using LSPDFR's Functions.StopCurrentCallout()");
                    Functions.StopCurrentCallout();
                }

                // If the call still isnt ended... do it manually
                // Need to do a null check!!! Callout.End() could set PlayerActiveCall to null
                if (ActivePlayerCall != null && !ActivePlayerCall.EventHandle.HasEnded)
                {
                    RegisterCallComplete(ActivePlayerCall);
                }
                
                ActivePlayerCall = null;
            }
            else if (calloutRunning)
            {
                Log.Warning("Dispatch.EndPlayerCallout(): Player does not have an Active Call. Using LSPDFR's Functions.StopCurrentCallout()");
                Functions.StopCurrentCallout();
            }
        }

        #endregion Public API Callout Methods

        #region Internal Callout Methods

        /// <summary>
        /// Reports the <see cref="ActiveEvent"/> so that a <see cref="PriorityCall"/> can
        /// be made for each <see cref="DispatchDirective"/>, and added to the Call Queue.
        /// </summary>
        /// <param name="activeEvent"></param>
        internal static void Report(ActiveEvent activeEvent)
        {
            // Create calls
            foreach (var item in activeEvent.ScenarioMeta.DispatchDirectives)
            {
                // Local vars
                var sector = item.Key;
                var directive = item.Value;

                // Create call
                var call = new PriorityCall(NextCallId++, activeEvent, directive);

                // Attempt to add the call
                if (AddIncomingCall(call, sector))
                {
                    activeEvent.AttachCall(sector, call);
                }
            }
        }

        /// <summary>
        /// Adds the specified <see cref="PriorityCall"/> to the Dispatch Queue
        /// </summary>
        /// <param name="call"></param>
        private static bool AddIncomingCall(PriorityCall call, ServiceSector sector)
        {
            // Ensure the call is not null. This can happen when trying
            // to initiate a scenario from a menu
            if (call?.EventHandle == null || call.EventHandle.Location == null)
            {
                Log.Error("Dispatch.AddIncomingCall(): Tried to add a call that is a null reference, or has a null location reference");
                return false;
            }

            // Grab cached zone
            //var zone = WorldZone.GetZoneByName(call.EventHandle.Location.Zone.ScriptName);
            var zone = call.EventHandle.Location.Zone;

            // Decide which agency gets this call
            switch (sector)
            {
                case ServiceSector.Police:
                    zone.GetPoliceAgencies()[0].Dispatcher.AddCall(call);
                    break;
                case ServiceSector.Fire:
                case ServiceSector.Medical:
                    throw new NotSupportedException();
            }

            // Prevent threading issues
            lock (_threadLock)
            {
                // Add call to list
                OpenCalls[call.DispatchInfo.Priority].Add(call);
            }

            // Register first for events
            call.OnEnded += EndCall;

            // Call event
            OnCallAdded?.Invoke(call);

            // Invoke the next callout for player?
            if (SendNextCallToPlayer && sector == PlayerAgency.Sector)
            {
                // Check jurisdiction
                if (call.EventHandle.Location.Zone.DoesAgencyHaveJurisdiction(PlayerAgency))
                {
                    // Unflag
                    SendNextCallToPlayer = false;

                    // Ensure player isnt in a callout already!
                    if (ActivePlayerCall == null)
                    {
                        // Set call to invoke
                        InvokeForPlayer = call;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Method called when a call is ended <see cref="Scripting.ActiveEvent.OnEnded"/>
        /// </summary>
        /// <param name="call"></param>
        /// <param name="flag"></param>
        internal static void EndCall(PriorityCall call, EventClosedFlag flag)
        {
            // Unregister
            call.OnEnded -= EndCall;

            // Call handle
            switch (flag)
            {
                case EventClosedFlag.Expired:
                    OnCallExpired?.Invoke(call);
                    break;
                // @Todo: handle more intances
            }

            // Remove call
            lock (_threadLock)
            {
                OpenCalls[call.DispatchInfo.Priority].Remove(call);
            }

            // Set player status
            if (call.PrimaryOfficer == PlayerUnit)
            {
                PlayerUnit.CompleteCall(EventClosedFlag.Completed);
                SetPlayerStatus(OfficerStatus.Available);

                // Reset
                if (PreviousLSPDFRAvailability != null)
                    Functions.SetPlayerAvailableForCalls(PreviousLSPDFRAvailability.Value);

                // Set active call to null
                ActivePlayerCall = null;

                // Fire event
                OnPlayerCallCompleted?.Invoke(call);
            }

            // Call event
            OnCallCompleted?.Invoke(call);
        }

        /// <summary>
        /// Internally used as part of a handshake with a <see cref="Dispatcher"/> when
        /// sending the player to a callout
        /// </summary>
        /// <param name="call"></param>
        internal static void AssignedPlayerToCall(PriorityCall call)
        {
            // wait for the radio to be clear before actually dispatching the player
            ActivePlayerCall = call;

            // Store this value for after our callout is ended, so we can
            // allows LSPFDFR the ability to still gives calls to the player
            if (PreviousLSPDFRAvailability == null)
                PreviousLSPDFRAvailability = Functions.IsPlayerAvailableForCalls();

            // If this is a callout requiring acceptance
            if (call.EventHandle.ScenarioMeta.ScriptType == ScriptType.Callout)
            {
                // Fetch meta if we have one
                if (call.EventHandle.ScenarioMeta.RadioMessages.TryGetValue("OnAssigned", out RadioMessageMeta meta))
                {
                    // Create radio message
                    var message = new RadioMessage(meta.AudioString);

                    // Are we using location?
                    if (meta.UsePosition)
                    {
                        message.LocationInfo = call.EventHandle.Location.Position;
                    }

                    // Add callsign?
                    if (meta.PrefixCallSign)
                    {
                        message.SetTarget(PlayerUnit);
                    }

                    // Set call status
                    ActivePlayerCall.Status = CallStatus.Waiting;

                    // Bind event
                    message.BeforePlayed += CalloutMessage_BeforePlayed;

                    // Queue radio
                    message.Priority = meta.Priority;
                    Scanner.QueueCalloutAudioToPlayer(message);
                }
                else
                {
                    // Double check, but this should never be true
                    if (Functions.IsCalloutRunning())
                    {
                        // Cancel the audio message
                        Log.Warning("Dispatch.AssignedPlayerToCall: Cannot dispatch player to call, already busy");
                        return;
                    }

                    // Start callout
                    TimeSinceLastCalloutAttempt = 0;
                    Functions.StartCallout(ActivePlayerCall.EventHandle.ScenarioMeta.ControllerName);
                }
            }
            else
            {
                // Set not available for calls
                Functions.SetPlayerAvailableForCalls(false);
            }
        }

        /// <summary>
        /// Method called when the <see cref="RadioMessage"/> for the callout is played
        /// for the player
        /// </summary>
        /// <param name="message"></param>
        /// <param name="args"></param>
        private static void CalloutMessage_BeforePlayed(RadioMessage message, RadioCancelEventArgs args)
        {
            // Double check, but this should never be true
            if (Functions.IsCalloutRunning())
            {
                // Cancel the audio message
                args.Cancel = true;
                Log.Warning("Dispatch.CalloutMessage_BeforePlayed: Cannot dispatch player to call, already busy");
                return;
            }

            // Clear radio for response
            Scanner.IsWaitingPlayerResponse = true;
            TimeSinceLastCalloutAttempt = 0;

            // Start callout
            Functions.StartCallout(ActivePlayerCall.EventHandle.ScenarioMeta.ControllerName);
        }

        /// <summary>
        /// Tells dispatch the AI officer has died
        /// </summary>
        /// <param name="officer"></param>
        internal static void RegisterDeadOfficer(OfficerUnit officer)
        {
            if (officer.IsAIUnit)
            {
                // Dispose
                officer.Dispose();

                // Show player notification
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "~g~An Officer has Died.",
                    $"Unit ~g~{officer.CallSign}~s~ is no longer with us"
                );

                //@todo Spawn new officer unit?
            }
        }

        /// <summary>
        /// Method called when a <see cref="Dispatcher"/> raises a call. This method analyzes
        /// what additional resources are needed for the call, and assigns the appropriate agencies 
        /// to assist.
        /// </summary>
        /// <remarks>
        /// Each <see cref="Dispatcher"/> instance is responsible for keeping track of which calls have
        /// been raised in the <see cref="Dispatcher.RaisedCalls"/> HashSet
        /// </remarks>
        /// <param name="agency">The agency that raised the call</param>
        /// <param name="call">The call that requires additional resources</param>
        /// <param name="args">Container that directs the dispatch of what resoures are being requested</param>
        private static void Dispatcher_OnCallRaised(Agency agency, PriorityCall call, CallRaisedEventArgs args)
        {
            // Of we are not on duty, then wth
            if (!Main.OnDutyLSPDFR) return;

            // @todo finish
            Agency newAgency = null;

            // So far this is all we support
            if (args.NeedsPolice)
            {
                switch (agency.AgencyType)
                {
                    case AgencyType.CityPolice:
                        // Grab county agency
                        newAgency = call.EventHandle.Location.Zone.GetPoliceAgencies().Where(x => x.AgencyType == AgencyType.CountySheriff).FirstOrDefault();
                        break;
                    case AgencyType.StateParks:
                    case AgencyType.CountySheriff:
                        // Grab state agency
                        newAgency = call.EventHandle.Location.Zone.GetPoliceAgencies().Where(x => x.IsStateAgency && x.AgencyType != AgencyType.StateParks).FirstOrDefault();
                        break;
                    case AgencyType.HighwayPatrol:
                    case AgencyType.StatePolice:
                        // We need to grab the county that has jurisdiction here
                        newAgency = call.EventHandle.Location.Zone.GetPoliceAgencies().Where(x => x.AgencyType == AgencyType.CountySheriff).FirstOrDefault();
                        break;
                    default:
                        throw new NotSupportedException();
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            // Add the call to the dispatcher of the next tier.
            // Both agencies should now be tracking the call.
            if (newAgency != null)
            {
                // Only log if the call was added successfully
                if (newAgency.Dispatcher.AddCall(call))
                {
                    Log.Debug($"{agency.ScriptName.ToUpper()} Dispatcher: Requested assistance with call '{call.EventHandle.ScenarioMeta.ScenarioName}' from {newAgency.ScriptName.ToUpper()}");
                }
            }
            else
            {
                Log.Debug($"{agency.ScriptName.ToUpper()} Dispatcher: Attemtped to request assitance with call '{call.EventHandle.ScenarioMeta.ScenarioName}', but there is other agency.");
            }
        }

        #endregion Internal Callout Methods

        #region Duty Methods

        /// <summary>
        /// Method called at the start of every duty
        /// </summary>
        internal static bool Start(SimulationSettings settings)
        {
            // Always call this first
            Shutdown();

            // Get players current agency
            Agency oldAgency = PlayerAgency;
            PlayerAgency = Agency.GetCurrentPlayerAgency();
            if (PlayerAgency == null)
            {
                PlayerAgency = oldAgency;
                Log.Error("Dispatch.Start(): Player Agency is null");
                return false;
            }

            // Did we change Agencies?
            if (oldAgency != null && !PlayerAgency.ScriptName.Equals(oldAgency.ScriptName))
            {
                // Clear agency data
                foreach (var ag in EnabledAgenciesByName)
                {
                    ag.Value.Disable();
                }

                // Clear calls
                foreach (var callList in OpenCalls)
                {
                    callList.Value.Clear();
                }

                // Clear enabled angencies
                EnabledAgenciesByName.Clear();
            }

            // ********************************************
            // Build a hashset of zones to load
            // ********************************************
            var zonesToLoad = new HashSet<string>(PlayerAgency.ZoneNames);
            if (zonesToLoad.Count == 0)
            {
                // Display notification to the player
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "~o~Initialization Failed.",
                    $"~y~Selected Agency does not have any zones in it's jurisdiction."
                );

                return false;
            }

            // ********************************************
            // Load all agencies that need to be loaded
            // ********************************************
            EnabledAgenciesByName.Add(PlayerAgency.ScriptName, PlayerAgency);

            // Next we need to determine which agencies to load alongside the players agency
            switch (PlayerAgency.AgencyType)
            {
                case AgencyType.CityPolice:
                    // Add county
                    var name = (PlayerAgency.BackingCounty == County.Blaine) ? "bcso" : "lssd";
                    EnabledAgenciesByName.Add(name, Agency.GetAgencyByName(name));
                    zonesToLoad.UnionWith(Agency.GetZoneNamesByAgencyName(name));

                    // Add state
                    EnabledAgenciesByName.Add("sahp", Agency.GetAgencyByName("sahp"));
                    zonesToLoad.UnionWith(Agency.GetZoneNamesByAgencyName("sahp"));
                    break;
                case AgencyType.CountySheriff:
                case AgencyType.StateParks:
                    // Add state
                    EnabledAgenciesByName.Add("sahp", Agency.GetAgencyByName("sahp"));
                    zonesToLoad.UnionWith(Agency.GetZoneNamesByAgencyName("sahp"));
                    break;
                case AgencyType.StatePolice:
                case AgencyType.HighwayPatrol:
                    // Add both counties
                    EnabledAgenciesByName.Add("bcso", Agency.GetAgencyByName("bcso"));
                    EnabledAgenciesByName.Add("lssd", Agency.GetAgencyByName("lssd"));

                    // Load both counties zones
                    zonesToLoad.UnionWith(Agency.GetZoneNamesByAgencyName("bcso"));
                    zonesToLoad.UnionWith(Agency.GetZoneNamesByAgencyName("lssd"));
                    break;
            }

            // ********************************************
            // Load locations based on current agency jurisdiction.
            // This method needs called everytime the player Agency is changed
            // ********************************************
            Zones = WorldZone.GetZonesByName(zonesToLoad.ToArray(), out int numZonesLoaded, out int totalLocations).ToList();
            if (Zones.Count == 0)
            {
                // Display notification to the player
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "~o~Initialization Failed.",
                    $"~r~Failed to load all zone XML files."
                );

                return false;
            }

            // Log
            Log.Info($"Loaded {numZonesLoaded} zones with {totalLocations} locations into memory'");

            // Yield to prevent freezing
            GameFiber.Yield();

            // ********************************************
            // Build the crime generator
            // ********************************************
            CrimeGenerator = new RegionCrimeGenerator(Zones.ToArray());

            // Create player, and initialize all agencies
            PlayerUnit = PlayerAgency.AddPlayerUnit(settings);

            // Log for debugging
            foreach (var agency in EnabledAgenciesByName.Values)
            {
                // Debugging
                Log.Debug($"Loading {agency.FullName} with the following data:");
                Log.Debug($"\t\tAgency Type: {agency.AgencyType}");
                Log.Debug($"\t\tAgency Staff Level: {agency.StaffLevel}");

                // Enable the agency
                agency.Enable();

                // Yield to prevent freezing
                GameFiber.Yield();

                // Log shift information
                Log.Debug($"\t\tAgency Unit Shift Data:");
                foreach (ShiftRotation shift in Enum.GetValues(typeof(ShiftRotation)))
                {
                    var shiftName = Enum.GetName(typeof(ShiftRotation), shift);
                    Log.Debug($"\t\t\t{shiftName} shift:");
                    foreach (var unit in agency.Units.Values)
                    {
                        var name = Enum.GetName(typeof(UnitType), unit.UnitType);
                        Log.Debug($"\t\t\t\t{name}: {unit.OfficersByShift[shift].Count}");
                    }
                }

                // Yield to prevent freezing
                GameFiber.Yield();

                // Loop through each time period and cache crime numbers
                Log.Debug($"\t\tAgency Projected Crime Data:");
                foreach (TimePeriod period in Enum.GetValues(typeof(TimePeriod)))
                {      
                    // Log crime logic;
                    string name = Enum.GetName(typeof(TimePeriod), period);
                    Log.Debug($"\t\t\tAverage Calls during the {name}: {agency.CallsByPeriod[period]}");
                }

                // Yield to prevent freezing
                GameFiber.Yield();

                // Log zone data
                Log.Debug($"\t\tAgency Primary Zone Count: {agency.Zones.Length}");
                Log.Debug($"\t\tAgency Primary Zones: ");
                foreach (var zone in agency.Zones)
                {
                    Log.Debug($"\t\t\t{zone.ScriptName}");
                }

                // Yield to prevent freezing
                GameFiber.Yield();
            }

            // Initialize CAD. This needs called everytime we go on duty
            ComputerAidedDispatchMenu.Initialize();

            // Register for LSPDFR events
            LSPD_First_Response.Mod.API.Events.OnCalloutDisplayed += LSPDFR_OnCalloutDisplayed;
            LSPD_First_Response.Mod.API.Events.OnCalloutAccepted += LSPDFR_OnCalloutAccepted;
            LSPD_First_Response.Mod.API.Events.OnCalloutNotAccepted += LSPDFR_OnCalloutNotAccepted;
            LSPD_First_Response.Mod.API.Events.OnCalloutFinished += LSPDFR_OnCalloutFinished; 

            // Start Dispatching logic fibers
            CrimeGenerator.Begin();
            Log.Debug($"Setting current crime level: {CrimeGenerator.CurrentCrimeLevel}");

            // Fire event
            CurrentShift = GetNewestShiftByTime(World.TimeOfDay);
            OnShiftStart?.Invoke(CurrentShift);
            ActiveShifts[CurrentShift] = true;

            // Start simulation fiber
            AISimulationFiber = GameFiber.StartNew(Process);

            // Report back
            return true;
        }

        /// <summary>
        /// Stops the internal thread and preforms clean up
        /// </summary>
        internal static void Shutdown()
        {
            // End crime generation
            CrimeGenerator?.End();

            // End fibers
            if (AISimulationFiber != null && (AISimulationFiber.IsAlive || AISimulationFiber.IsSleeping))
            {
                AISimulationFiber.Abort();
            }

            // Cancel CAD
            ComputerAidedDispatchMenu.Dispose();

            // Register for LSPDFR events
            LSPD_First_Response.Mod.API.Events.OnCalloutDisplayed -= LSPDFR_OnCalloutDisplayed;
            LSPD_First_Response.Mod.API.Events.OnCalloutAccepted -= LSPDFR_OnCalloutAccepted;
            LSPD_First_Response.Mod.API.Events.OnCalloutNotAccepted -= LSPDFR_OnCalloutNotAccepted;
            LSPD_First_Response.Mod.API.Events.OnCalloutFinished -= LSPDFR_OnCalloutFinished;

            // Deactivate all agencies
            foreach (var a in EnabledAgenciesByName.Values)
            {
                a.Disable();
            }
            
            // Set back to Default
            foreach (ShiftRotation period in Enum.GetValues(typeof(ShiftRotation)))
            {
                ActiveShifts[period] = false;
            }
        }

        #endregion Duty Methods

        #region GameFiber methods

        /// <summary>
        /// Main thread loop for Dispatch. Processes the Logic for AI officers every
        /// second, and every 2 seconds performs dispatching logic
        /// </summary>
        private static void Process()
        {
            // local vars
            bool skip = false;
            var orderedAgencies = EnabledAgenciesByName.Values.OrderBy(x => (int)x.AgencyType);

            // While on duty main loop
            while (Main.OnDutyLSPDFR)
            {
                // Get current datetime and timeperiod
                var date = World.DateTime;
                var tod = GameWorld.CurrentTimePeriod;

                // Check for Shift changes
                var currentShift = GetNewestShiftByTime(date.TimeOfDay);
                if (currentShift != CurrentShift)
                {
                    CurrentShift = currentShift;
                    ActiveShifts[currentShift] = true;

                    // Fire event
                    OnShiftStart?.Invoke(currentShift);
                }

                // Time to send a shift home?
                var activeShifts = GetActiveShifts(date.TimeOfDay);
                var difference = ActiveShifts.Where(x => x.Value && !activeShifts[x.Key]).Select(x => x.Key).ToArray();
                foreach (var shift in difference)
                {
                    OnShiftEnd?.Invoke(shift);
                }

                // Are we invoking a call to the player?
                if (InvokeForPlayer != null)
                {
                    // Tell the players dispatcher they are about to get a call
                    PlayerAgency.Dispatcher.AssignUnitToCall(PlayerUnit, InvokeForPlayer);
                    InvokeForPlayer = null;
                }

                // Every other loop, do dispatching logic
                skip = !skip;

                // Call OnTick for the VirtualAIUnit(s)
                foreach (var agency in orderedAgencies)
                {
                    // Do dispatching logic?
                    if (!skip)
                    {
                        agency.Dispatcher.Process(date);
                    }

                    // Be nice
                    GameFiber.Yield();

                    // On tick for every AI officer currently on duty
                    foreach (var officer in agency.GetOnDutyOfficers())
                    {
                        officer.OnTick(date);
                    }

                    // Be nice
                    GameFiber.Yield();
                }

                // Increment thread safe, since callouts can be initiated from other threads!
                Interlocked.Increment(ref _timeSinceCalloutAttempt);

                // Are we waiting for a player to accept a callout?
                if (ActivePlayerCall?.Status == CallStatus.Waiting)
                {
                    // A callout should call this method when expired, but just in
                    // case, we will check after a safe amount of time
                    if (TimeCalloutWaiting > 20)
                    {
                        // Ensure this is called, since the callout failed!
                        CalloutNotAccepted(ActivePlayerCall);
                    }
                    else
                    {
                        // Set safely, since Callout.OnCalloutAccepted is in a different thread
                        Interlocked.Increment(ref _timeCalloutWaiting);
                    }
                }

                // Stop this thread for one second
                GameFiber.Wait(1000);
            }
        }

        /// <summary>
        /// Returns a hash table of all shifts, and flags which ones are still active by the provided time
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        internal static Dictionary<ShiftRotation, bool> GetActiveShifts(TimeSpan time)
        {
            return new Dictionary<ShiftRotation, bool>()
            {
                { ShiftRotation.Day, (time.Hours.InRange(6, 16)) },
                { ShiftRotation.Night, (time.Hours >= 21 || time.Hours < 7) },
                { ShiftRotation.Swing, (time.Hours < 1 || time.Hours.InRange(15, 24)) }
            };
        }

        /// <summary>
        /// Returns the primary shift by time of day
        /// </summary>
        /// <returns></returns>
        internal static ShiftRotation GetNewestShiftByTime(TimeSpan time)
        {
            if (time.Hours.InRange(6, 16))
            {
                return ShiftRotation.Day;
            }
            else if (time.Hours >= 21 || time.Hours < 7)
            {
                return ShiftRotation.Night;
            }
            
            return ShiftRotation.Swing;
        }

        #endregion GameFiber methods

        #region LSPDFR event methods

        /// <summary>
        /// Method called whenever a player does not accept a callout in LSPDFR
        /// </summary>
        /// <param name="handle">The callout handle</param>
        private static void LSPDFR_OnCalloutNotAccepted(LHandle handle)
        {
            IsCalloutBeingDisplayed = false;
            IsExternalCalloutRunning = false;
        }

        /// <summary>
        /// Method called whenever a callout is displayed to the player for acceptance
        /// </summary>
        /// <param name="handle">The callout handle</param>
        private static void LSPDFR_OnCalloutDisplayed(LHandle handle)
        {
            IsCalloutBeingDisplayed = true;
        }

        /// <summary>
        /// Method called whenever a player finishes a callout
        /// </summary>
        /// <param name="handle">The callout handle</param>
        private static void LSPDFR_OnCalloutFinished(LHandle handle)
        {
            if (IsExternalCalloutRunning)
            {
                // @todo add more, such as player assignment changing
                IsExternalCalloutRunning = false;
            }
        }

        /// <summary>
        /// Event called when a callout is accepted in LSPDFR
        /// </summary>
        /// <param name="handle"></param>
        private static void LSPDFR_OnCalloutAccepted(LHandle handle)
        {
            // If we detect no active call, must be external
            if (ActivePlayerCall == null)
            {
                IsExternalCalloutRunning = true;
            }
            else if (ActivePlayerCall.EventHandle.Controller == null)
            {
                Log.Warning("Dispatch: Player call is not null, but the Callout property is not set!");
                IsExternalCalloutRunning = true;
            }
            else
            {
                // Check to ensure our callout is running!
                var runningName = Functions.GetCalloutName(handle);
                var calloutName = ((Callout)ActivePlayerCall.EventHandle.Controller).ScriptInfo.Name;
                if (!calloutName.Equals(runningName))
                {
                    // Log
                    Log.Warning($"Dispatch: Player active callout ({calloutName}) does not match the running callout name ({runningName})!");

                    // Close the player callout
                    ActivePlayerCall = null;

                    // Set flag
                    IsExternalCalloutRunning = true;
                }
                else
                {
                    // Log
                    Log.Debug($"Completed call handshake for scenario '{calloutName}'");

                    // Callout is verified
                    IsExternalCalloutRunning = false;
                }
            }

            // No longer displayed
            IsCalloutBeingDisplayed = false;

            // Set player assignment
            if (IsExternalCalloutRunning)
            {
                // @todo
            }
        }

        #endregion
    }
}
