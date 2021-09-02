using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using Rage;
using System;

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
            try
            {
                // Show message
                Rage.Game.DisplayHelp("Begining Simulation...");

                // Fade the screen out and wait
                Rage.Game.FadeScreenOut(1500);
                GameFiber.Sleep(1500);

                // Initialize dispatch script
                if (!Dispatch.Start(settings))
                {
                    return false;
                }

                // Set timescale if not default
                if (settings.TimescaleMult != 30)
                {
                    TimeScale.SetTimeScaleMultiplier(settings.TimescaleMult);
                }

                // Transition weather
                if (settings.ForceWeather)
                {
                    GameWorld.TransitionToWeather(settings.SelectedWeather, 0f);
                }

                // Set world time
                if (settings.FastForward)
                {
                    World.TimeOfDay = GetShiftStartTime(settings.SelectedShift);
                }

                // Fade the screen back in
                Rage.Game.FadeScreenIn(1000);
                Rage.Game.HideHelp();

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
                if (Rage.Game.IsScreenFadingOut || Rage.Game.IsScreenFadedOut)
                {
                    Rage.Game.FadeScreenIn(500);
                }

                // Call just to be sure
                Dispatch.Shutdown();

                // Report back
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static void Shutdown()
        {

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
