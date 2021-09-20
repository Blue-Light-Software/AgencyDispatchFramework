using AgencyDispatchFramework.Game;
using Rage;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AgencyDispatchFramework.Scripting
{
    /// <summary>
    /// A class that handles the processing of <see cref="IEventController"/>s
    /// </summary>
    public static class ScriptEngine
    {
        /// <summary>
        /// An event counter used to assign unique ambient event ids
        /// </summary>
        private static int EventCounter = 0;

        /// <summary>
        /// Contains a hashset of active events in the <see cref="Rage.World"/> from this mod
        /// </summary>
        private static HashSet<IEventController> ActiveEvents { get; set; }

        /// <summary>
        /// Contains the active <see cref="GameFiber"/> or null
        /// </summary>
        private static GameFiber EventsFiber { get; set; }

        /// <summary>
        /// Our lock object to prevent multi-threading issues
        /// </summary>
        private static object _threadLock = new object();

        /// <summary>
        /// Static constructor
        /// </summary>
        static ScriptEngine()
        {
            ActiveEvents = new HashSet<IEventController>();
        }

        /// <summary>
        /// Used internally to get a new id for an ambient event
        /// </summary>
        /// <returns></returns>
        internal static int GetNewEventId()
        {
            return Interlocked.Increment(ref EventCounter);
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

        /// <summary>
        /// Adds and begins processing the specified event
        /// </summary>
        /// <param name="event"></param>
        internal static void StartNewAmbientEvent(IEventController @event)
        {
            if (@event.Event.ScenarioMeta.ScriptType == ScriptType.Callout)
            {
                // WE do not handle callouts here
                return;
            }

            // Check if ID equals zero
            if (@event.Event.EventId == 0)
            {
                @event.Event.EventId = GetNewEventId();
            }

            // Prevent threading issues
            lock (_threadLock)
            {
                // Add event
                ActiveEvents.Add(@event);
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
        }
    }
}
