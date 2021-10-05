using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.Xml;
using LSPD_First_Response.Mod.API;
using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace AgencyDispatchFramework.Scripting
{
    /// <summary>
    /// A class that handles the processing of <see cref="Events.AmbientEvent"/>(s)
    /// as well as keeping persistent data related to <see cref="WorldLocation"/>(s)
    /// that are current in use by <see cref="ActiveEvent"/>s
    /// </summary>
    public static class ScriptEngine
    {
        /// <summary>
        /// An event counter used to assign unique ambient event ids
        /// </summary>
        private static int NextEventId = 1;

        /// <summary>
        /// Our lock object to prevent multi-threading issues
        /// </summary>
        private static object _threadLock = new object();

        /// <summary>
        /// Contains a hashset of active events in the <see cref="Rage.World"/> from this mod
        /// </summary>
        private static HashSet<IEventController> ActiveEvents { get; set; }

        /// <summary>
        /// Contains a hash table of <see cref="WorldLocation"/>s that are currently in use by the Call Queue
        /// </summary>
        /// <remarks>HashSet{T}.Contains is an O(1) operation</remarks>
        private static HashSet<WorldLocation> ActiveLocations { get; set; }

        /// <summary>
        /// Contains the active <see cref="GameFiber"/> or null
        /// </summary>
        private static GameFiber EventsFiber { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal static ScenarioPool Callouts { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal static ScenarioPool PlayerEvents { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal static ScenarioPool AIEvents { get; set; }

        /// <summary>
        /// Event called when a callout scenario is added to the list
        /// </summary>
        public static event ScenarioListUpdateHandler OnScenarioAdded;

        /// <summary>
        /// Event called when a callout is added and registered with LSPDFR through this <see cref="ScenarioPool"/> class.
        /// </summary>
        public static event CalloutListUpdateHandler OnCalloutRegistered;

        /// <summary>
        /// Event called when a call to the method <see cref="RegisterCalloutsFromPath(string, Assembly)"/>
        /// has completed adding callouts and scenarios to the pool.
        /// </summary>
        public static event CalloutPackLoadedHandler OnCalloutPackLoaded;

        /// <summary>
        /// Static constructor
        /// </summary>
        static ScriptEngine()
        {
            // Initialize scenario pools
            Callouts = new ScenarioPool();
            PlayerEvents = new ScenarioPool();
            AIEvents = new ScenarioPool();

            // Intialize a hash table of active events and locations
            ActiveEvents = new HashSet<IEventController>();
            ActiveLocations = new HashSet<WorldLocation>();
        }

        /// <summary>
        /// Used internally to get a new id for an ambient event
        /// </summary>
        /// <returns></returns>
        internal static int GetNewEventId()
        {
            return Interlocked.Increment(ref NextEventId);
        }

        /// <summary>
        /// Gets the closest event to the player within viewing range, or null
        /// </summary>
        /// <returns></returns>
        internal static IEventController GetClosestAmbientEvent()
        {
            // Prevent threading issues
            lock (_threadLock)
            {
                var playerPos = GamePed.Player.Position;
                return (
                    from x in ActiveEvents
                    let distance = x.Event.Location.DistanceTo(playerPos)
                    where distance < 50f
                    orderby distance
                    select x
                ).FirstOrDefault();
            }
        }

        /// <summary>
        /// Removes, activates and processes active <see cref="AmbientEvent"/>s
        /// </summary>
        internal static void Process()
        {
            // Loop
            while (Main.OnDutyLSPDFR)
            {
                // Prevent threading issues
                lock (_threadLock)
                {
                    // If we have no events, quit
                    if (ActiveEvents.Count != 0)
                    {
                        // Remove old
                        int removed = ActiveEvents.RemoveWhere(x => x.Event.HasEnded);
                        if (removed > 0)
                        {
                            // Log how many we removed
                            Log.Info($"Cleaned up {removed} AmbientEvent(s)");
                        }

                        // Ticking action
                        var events = ActiveEvents.ToArray();
                        foreach (var evnt in events)
                        {
                            evnt.Process();
                        }
                    }
                }

                // Sleep for a half of a second
                GameFiber.Sleep(500);
            }
        }

        internal static void CreateAmbientEvent()
        {

        }

        /// <summary>
        /// TODO still needs a lot of work! 
        /// 
        /// Starts an event at an AI officers location
        /// </summary>
        /// <param name="officerLocation"></param>
        internal static bool TryCreateOfficerEvent(WorldLocation officerLocation, out ActiveEvent activeEvent)
        {
            // Default
            activeEvent = default(ActiveEvent);
            EventScenarioMeta scenario = null;

            // Prevent crashing
            try
            {
                // Grab zone
                var zone = GetZoneAtLocation(officerLocation);

                // Get a call category @todo make this better
                var category = zone.CallCategoryGenerator.Spawn();

                // Try and spawn a scenario
                if (AIEvents.ScenariosByCategory[category].TrySpawn(out scenario))
                {
                    // Start event
                    activeEvent = CreateEvent(scenario, officerLocation);
                    return activeEvent != null;
                }
            }
            catch (Exception e)
            {
                Log.Exception(e, $"Failed to create AmbientEvent scenario '{scenario?.ScenarioName}'");
            }

            // If we are here, we failed
            return false;
        }

        /// <summary>
        /// Adds and begins processing the specified event
        /// </summary>
        /// <param name="event"></param>
        internal static ActiveEvent CreateEvent(EventScenarioMeta scenario, WorldLocation location)
        {
            // Ensure we are actively running!
            if (EventsFiber == null) return null;

            try
            {
                // Create the event handle
                var activeEvent = new ActiveEvent(GetNewEventId(), scenario, location);

                // WE do not handle callouts here, so return the event
                if (scenario.ScriptType == ScriptType.Event)
                {
                    // Create controller instance
                    IEventController instance = Activator.CreateInstance(activeEvent.ScenarioMeta.ControllerType, new[] { activeEvent }) as IEventController;
                    if (instance == null)
                    {
                        var name = scenario.ControllerType.Name;
                        throw new Exception($"Unable to create instance of {name} and cast as an IEventController");
                    }

                    // Prevent threading issues
                    lock (_threadLock)
                    {
                        // Add event
                        ActiveEvents.Add(instance);

                        // Add event location. Do this manually since we have already locked the thread
                        if (ActiveLocations.Add(activeEvent.Location))
                        {
                            // Register for events
                            activeEvent.OnEnded += ActiveEvent_OnEnded;
                        }
                        else
                        {
                            // Failed to add?
                            var zName = activeEvent.Location.Zone.DisplayName;
                            var message = $"Tried to register an ActiveEvent Location in zone '{zName}', but the location selected was already in use.";
                            Log.Error($"ScriptEngine.RegisterEventLocation(): {message}");
                        }
                    }

                    // Log
                    Log.Info($"Started AmbientEvent scenario '{scenario.ScenarioName}'");
                }
                else
                {
                    // Call method, since it locks the thread accordingly
                    RegisterEventLocation(activeEvent);

                    // Log
                    Log.Info($"Created ActiveEvent scenario '{scenario.ScenarioName}'");
                }

                // Return event
                return activeEvent;
            }
            catch (Exception e)
            {
                Log.Exception(e, "Attempted to create an ActiveEvent");
                return null;
            }
        }

        /// <summary>
        /// Begins all internal <see cref="GameFiber"/> instances
        /// </summary>
        internal static void Begin()
        {
            // Return if we are already running
            if (EventsFiber != null) return;

            // Start new
            EventsFiber = GameFiber.StartNew(Process);
        }

        /// <summary>
        /// Shutsdown the <see cref="ScriptEngine"/>
        /// </summary>
        internal static void Shutdown()
        {
            EventsFiber?.Abort();
            EventsFiber = null;

            // Reset scenario pools

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rootPath">The full directory path to the callout pack root folder</param>
        /// <param name="assembly">The calling assembly that contains the callout scripts</param>
        /// <returns></returns>
        public static int RegisterCalloutsFromPath(string rootPath, Assembly assembly, bool yieldFiber = true)
        {
            // Load directory
            var directory = new DirectoryInfo(rootPath);
            if (!directory.Exists)
            {
                Log.Error($"ScriptEngine.RegisterCalloutsFromPath(): Callouts directory is missing: {rootPath}");
                return 0;
            }

            // Initialize vars
            int itemsAdded = 0;

            // Load callout scripts
            foreach (var calloutDirectory in directory.GetDirectories())
            {
                // Get callout name and type
                string calloutDirName = calloutDirectory.Name;

                // ensure CalloutMeta.xml exists
                string path = Path.Combine(calloutDirectory.FullName, "CalloutMeta.xml");
                if (!File.Exists(path))
                {
                    Log.Warning($"ScriptEngine.RegisterCalloutsFromPath(): Directory does not contain a CalloutMeta.xml: {path}");
                    continue;
                }

                // Wrap in a try/catch. Exceptions are thrown in here
                try
                {
                    // Load meta file
                    using (var metaFile = new CalloutMetaFile(path))
                    {
                        // Parse file
                        metaFile.Parse(assembly, yieldFiber);

                        // Yield fiber?
                        if (yieldFiber) GameFiber.Yield();

                        // Add each scenario
                        foreach (var scenario in metaFile.Scenarios.OrderBy(x => x.ScenarioName))
                        {
                            // Add to the pool
                            Callouts.Add(assembly, scenario);

                            // Statistics trackins
                            itemsAdded++;

                            // Call event
                            OnScenarioAdded?.Invoke(scenario);

                            // Yield fiber?
                            if (yieldFiber) GameFiber.Yield();
                        }

                        // Register the callout
                        Functions.RegisterCallout(metaFile.CalloutType);

                        // Call event
                        OnCalloutRegistered?.Invoke(metaFile.CalloutType);
                    }

                }
                catch (FileNotFoundException)
                {
                    Log.Error($"ScriptEngine.RegisterCalloutsFromPath(): Missing CalloutMeta.xml in directory '{calloutDirName}' for Assembly: '{assembly.FullName}'");
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }
            }

            // Call events
            OnCalloutPackLoaded?.Invoke(rootPath, assembly, itemsAdded);

            // Log and return
            Log.Debug($"Added {itemsAdded} Callout Scenarios to the ScenarioPool from {assembly.FullName}");
            return itemsAdded;
        }

        /// <summary>
        /// Gets an array of <see cref="WorldLocation"/>s currently in use based
        /// on the specified type <typeparamref name="T"/>
        /// </summary>
        /// <param name="type"></param>
        /// <exception cref="InvalidCastException">thrown if the <paramref name="type"/> does not match the <typeparamref name="T"/></exception>
        /// <typeparam name="T">A type that inherits from <see cref="WorldLocation"/></typeparam>
        /// <returns></returns>
        public static T[] GetActiveLocationsOfType<T>() where T : WorldLocation
        {
            return (from x in ActiveLocations where x is T select (T)x).ToArray();
        }

        /// <summary>
        /// Gets an array of locations that are not currently in use from the provided
        /// <see cref="WorldLocation"/> pool
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pool"></param>
        /// <returns></returns>
        public static T[] GetInactiveLocationsFromPool<T>(IEnumerable<T> pool) where T : WorldLocation
        {
            return (from x in pool where !ActiveLocations.Contains(x) select x).ToArray();
        }

        /// <summary>
        /// Thread-Safe. Adds the location in an internal <see cref="HashSet{T}"/> to prevent
        /// other <see cref="ActiveEvent"/>(s) from using the same location at the same time.
        /// </summary>
        /// <param name="activeEvent"></param>
        /// <returns></returns>
        public static bool RegisterEventLocation(ActiveEvent activeEvent)
        {
            // Add call to priority Queue
            lock (_threadLock)
            {
                if (ActiveLocations.Add(activeEvent.Location))
                {
                    // Register for events
                    activeEvent.OnEnded += ActiveEvent_OnEnded;
                    return true;
                }
                else
                {
                    // Failed to add?
                    var zName = activeEvent.Location.Zone.DisplayName;
                    var message = $"Tried to register an ActiveEvent Location in zone '{zName}', but the location selected was already in use.";
                    Log.Error($"ScriptEngine.RegisterEventLocation(): {message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="officerLocation"></param>
        /// <returns></returns>
        private static WorldZone GetZoneAtLocation(WorldLocation officerLocation)
        {
            // Return if zone is not null
            if (officerLocation.Zone != null)
                return officerLocation.Zone;

            // We must find the zone
            return GameWorld.GetZoneAtLocation(officerLocation.Position);
        }

        /// <summary>
        /// When the <see cref="ActiveEvent"/> has ended, this method frees the <see cref="WorldLocation"/>
        /// </summary>
        /// <param name="details"></param>
        /// <param name="closeFlag"></param>
        private static void ActiveEvent_OnEnded(ActiveEvent details, EventClosedFlag closeFlag)
        {
            lock (_threadLock)
            {
                ActiveLocations.Remove(details.Location);
            }
        }
    }
}
