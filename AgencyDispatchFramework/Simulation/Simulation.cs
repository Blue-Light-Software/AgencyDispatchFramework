using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.NativeUI;
using AgencyDispatchFramework.Scripting;
using Rage;
using System;
using System.Windows;

namespace AgencyDispatchFramework.Simulation
{
    internal static class Simulation
    {
        /// <summary>
        /// Starts the virtual simulation
        /// </summary>
        /// <param name="settings"></param>
        public static bool Begin(SimulationSettings settings)
        {
            // Do we fade the screen?
            bool fadeScreen = settings.ForceWeather || settings.RandomWeather || settings.FastForward;
            bool changedTimeScale = false;

            // local variables to fallback onto
            int currentMult = TimeScale.GetCurrentTimeScaleMultiplier();
            var time = World.TimeOfDay;
            var weather = GameWorld.CurrentWeather;

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
                    World.TimeOfDay = GetShiftStartTime(settings.SelectedShift);
                }

                // Initialize dispatch script
                if (!Dispatch.Start(settings))
                {
                    // Set back
                    if (changedTimeScale)
                    {
                        TimeScale.SetTimeScaleMultiplier(currentMult);
                    }

                    // Report back that we failed
                    return false;
                }

                // Start the event manager
                Scripting.ScriptEngine.Begin();

                // Transition weather
                if (settings.ForceWeather)
                {
                    GameWorld.TransitionToWeather(settings.SelectedWeather, 0f);
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
