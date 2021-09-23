using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Scripting;
using Rage;
using System;

namespace AgencyDispatchFramework.Simulation
{
    internal static class Simulation
    {
        public static bool IsRunning { get; private set; }

        /// <summary>
        /// Starts the virtual simulation
        /// </summary>
        /// <param name="settings"></param>
        public static bool Begin(SimulationSettings settings)
        {
            // Do we fade the screen?
            bool fadeScreen = settings.ForceWeather || settings.RandomWeather || settings.FastForward || settings.SyncTime;

            // local variables to fallback onto
            bool changedTimeScale = false;
            int currentMult = TimeScale.GetCurrentTimeScaleMultiplier();
            var now = DateTime.Now;
            var gameNow = World.DateTime;
            var time = gameNow.TimeOfDay;

            // Wrap to catch unexpected errors
            try
            {
                // Show message
                Rage.Game.DisplayHelp("Begining Simulation...");
                
                // Fade screen?
                if (fadeScreen)
                {
                    // Fade the screen out and wait
                    Rage.Game.FadeScreenOut(1500);
                    GameFiber.Sleep(1500);
                }

                // Set timescale if not default first, since the RegionCrimeGenerator
                // uses the TimeScale in game for its sleep timer
                if (settings.TimeScaleMult != currentMult)
                {
                    changedTimeScale = TimeScale.SetTimeScaleMultiplier(settings.TimeScaleMult);
                }

                // Set world time
                if (settings.FastForward)
                {
                    time = GetShiftStartTime(settings.SelectedShift);
                    World.TimeOfDay = time;
                }
                else if (settings.SyncTime)
                {
                    time = DateTime.Now.TimeOfDay;
                    World.TimeOfDay = time;
                }

                // Initialize dispatch script
                if (!Dispatch.Start(settings))
                {
                    // Set the timescale back to original
                    if (changedTimeScale)
                    {
                        TimeScale.SetTimeScaleMultiplier(currentMult);
                    }

                    // Report back that we failed
                    return false;
                }

                // Start the event manager
                ScriptEngine.Begin();

                // Set world Date
                if (settings.SyncDate)
                {
                    World.DateTime = new DateTime(now.Year, now.Month, now.Day, time.Hours, time.Minutes, time.Seconds);
                }

                // Transition weather
                if (settings.ForceWeather)
                {
                    GameWorld.SetWeather(settings.SelectedWeather);
                }
                else if (settings.RandomWeather)
                {
                    var rnd = new CryptoRandom();
                    if (settings.RealisticWeather)
                    {
                        var weathers = GameWorld.GetRealisticWeatherListByDateTime();
                        GameWorld.SetWeather(rnd.PickOne(weathers));
                    }
                    else
                    {
                        GameWorld.RandomizeWeather();
                    }
                }

                // Tell GameWorld to begin listening. Stops automatically when player goes off duty
                GameWorld.BeginFibers();

                // Fade the screen back in
                if (fadeScreen)
                {
                    Rage.Game.FadeScreenIn(1000);
                    Rage.Game.HideHelp();
                }

                // Report back
                IsRunning = true;
                return true;
            }
            catch (Exception e)
            {
                // Hide the help message
                Rage.Game.HideHelp();

                // Log the exception
                Log.Exception(e);

                // Show player an error message
                // Display notification to the player
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "~o~Initialization Failed.",
                    $"~y~Please check your Game.log for errors."
                );

                // Make sure screen is faded in
                if (fadeScreen)
                {
                    Rage.Game.FadeScreenIn(500);
                }

                // Stop the events
                Shutdown();

                // Report back
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Shutdown()
        {
            IsRunning = false;

            // Tell dispatch to stop
            Dispatch.Shutdown();

            // Stop GameWorld fibers

            // Stop events
            ScriptEngine.Shutdown();
        }

        /// <summary>
        /// Gets the shift start time 
        /// </summary>
        /// <param name="shift"></param>
        /// <returns></returns>
        private static TimeSpan GetShiftStartTime(ShiftRotation shift)
        {
            switch (shift)
            {
                default:
                case ShiftRotation.Day: return TimeSpan.FromHours(6);
                case ShiftRotation.Swing: return TimeSpan.FromHours(15);
                case ShiftRotation.Night: return TimeSpan.FromHours(21);
            }
        }
    }
}
