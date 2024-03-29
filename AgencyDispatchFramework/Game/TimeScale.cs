﻿using System;
using static Rage.Native.NativeFunction;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// A class that translate game time to real time and vise-versa using the game's TimeScale
    /// </summary>
    public static class TimeScale
    {
        /// <summary>
        /// Event fired when the <see cref="Rage.Game.TimeScale"/> is modified using this class.
        /// </summary>
        public static TimeScaleChangedEventHandler OnTimeScaleChanged;

        /// <summary>
        /// Converts seconds in real life to seconds in game
        /// </summary>
        /// <param name="realSeconds"></param>
        /// <returns></returns>
        public static double GameSecondsFromRealSeconds(int realSeconds)
        {
            return realSeconds * GetCurrentTimeScaleMultiplier();
        }

        /// <summary>
        /// Converts seconds in game to real life seconds
        /// </summary>
        /// <param name="gameSeconds"></param>
        /// <returns></returns>
        public static double RealSecondsFromGameSeconds(int gameSeconds)
        {
            if (gameSeconds == 0) return 0;
            return Math.Round(gameSeconds / (double)GetCurrentTimeScaleMultiplier(), 5);
        }

        /// <summary>
        /// Converts a <see cref="TimeSpan"/> in real life to
        /// a <see cref="TimeSpan"/> in game
        /// </summary>
        /// <param name="realTime"></param>
        /// <returns></returns>
        public static TimeSpan ToGameTime(TimeSpan realTime)
        {
            var total = realTime.TotalSeconds * GetCurrentTimeScaleMultiplier();
            return TimeSpan.FromSeconds(total);
        }

        /// <summary>
        /// Converts a <see cref="TimeSpan"/> in game time to
        /// a <see cref="TimeSpan"/> in real time
        /// </summary>
        /// <param name="realTime"></param>
        /// <returns></returns>
        public static TimeSpan ToRealTime(TimeSpan gameTime)
        {
            var total = gameTime.TotalSeconds / GetCurrentTimeScaleMultiplier();
            return TimeSpan.FromSeconds(total);
        }

        /// <summary>
        /// Returns the number of milliseconds that equals to one game minute.
        /// </summary>
        /// <returns></returns>
        internal static int GetMillisecondsPerGameMinute()
        {
            return Natives.GetMillisecondsPerGameMinute<int>();
        }

        /// <summary>
        /// Sets how many real milliseconds are equal to one game minute.
        /// </summary>
        /// <param name="value">The time in milliseconds</param>
        internal static bool SetMillisecondsPerGameMinute(int value)
        {
            // Check for setting of the same value
            int oldValue = GetCurrentTimeScaleMultiplier();
            if (oldValue == value) return true;

            // Catch errors
            try
            {
                // Natives.SetMillisecondsPerGameMinute does not work, so we need to
                // edit memory manually
                unsafe
                {
                    var address = NativeMemory.FindPattern("\x66\x0F\x6E\x05\x00\x00\x00\x00\x0F\x57\xF6", "xxxx????xxx");
                    int* millisecondsPerGameMinuteAddress = (int*)(*(int*)(address + 4) + address + 8);
                    *millisecondsPerGameMinuteAddress = value;
                }

                // Invoke event
                int newValue = GetCurrentTimeScaleMultiplier();
                OnTimeScaleChanged?.Invoke(oldValue, value);

                // Temp logging
                Log.Debug($"TimeScale changed from {oldValue} to {newValue}");
                return (newValue == value);
            }
            catch (Exception e)
            {
                // Temp logging
                Log.Exception(e, $"Failed to set TimeScale to value of {value}");
                return false;
            }
        }

        /// <summary>
        /// Sets the time scale multiplier in game
        /// </summary>
        /// <param name="value">default value is 30</param>
        public static bool SetTimeScaleMultiplier(int value)
        {
            var realMsPerMin = 60000;
            var msPerGameMin = realMsPerMin / value;
            return SetMillisecondsPerGameMinute(msPerGameMin);
        }

        /// <summary>
        /// Using Natives, gets the current time scale multiplier in game
        /// </summary>
        /// <returns></returns>
        public static int GetCurrentTimeScaleMultiplier()
        {
            var realMsPerMin = 60000;
            var msPerMinute = GetMillisecondsPerGameMinute();
            return (realMsPerMin / msPerMinute);
        }
    }
}
