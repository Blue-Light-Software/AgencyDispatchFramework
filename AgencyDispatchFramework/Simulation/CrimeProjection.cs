using AgencyDispatchFramework.Game;
using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// A class that contains crime information for a <see cref="WorldZone"/>
    /// </summary>
    public class CrimeProjection
    {
        /// <summary>
        /// Gets the <see cref="WorldZone"/> for this data
        /// </summary>
        public WorldZone Zone { get; }

        /// <summary>
        /// Contains a dictionary of the average number of calls per time of day in this zone
        /// </summary>
        public IReadOnlyDictionary<TimePeriod, int> CrimeProbability { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        protected ProbabilityGenerator<Spawnable<TimePeriod>> Generator { get; set; }

        /// <summary>
        /// Gets the average daily crime calls
        /// </summary>
        public int AverageCalls { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="CrimeProjection"/>
        /// </summary>
        /// <param name="zone"></param>
        internal CrimeProjection(WorldZone zone, int averageCalls, Dictionary<TimePeriod, int> probabilities)
        {
            Zone = zone;
            AverageCalls = averageCalls;
            CrimeProbability = probabilities;
            Generator = new ProbabilityGenerator<Spawnable<TimePeriod>>();

            // Fill generator
            foreach (var item in probabilities)
            {
                // Skip if not probable at all
                if (item.Value == 0)
                    continue;

                // Add item
                var spawnable = new Spawnable<TimePeriod>(item.Value, item.Key);
                Generator.Add(spawnable);
            }
        }

        /// <summary>
        /// Using a <see cref="ProbabilityGenerator{T}"/>, returns the number of expected calls with a 
        /// given <see cref="TimePeriod"/>. This method may return different values everytime it is
        /// called, because it uses probability to determine the result.
        /// </summary>
        /// <param name="period"></param>
        public int GetNextAverageCallCount(TimePeriod period)
        {
            int counter = 0;
            for (int i = 0; i < AverageCalls; i++)
            {
                if (Generator.TrySpawn(out Spawnable<TimePeriod> result) && result.Value == period)
                    counter++;
            }

            return counter;
        }

        /// <summary>
        /// Returns the exact average of calls expected in a <see cref="TimePeriod"/>, rounded 2 decimal places.
        /// </summary>
        /// <param name="period"></param>
        /// <returns></returns>
        public double GetTrueAverageCallCount(TimePeriod period)
        {
            double result = CrimeProbability[period] / (double)Generator.CumulativeProbability;
            return Math.Round(AverageCalls * result, 2);
        }

        /// <summary>
        /// Gets the <see cref="WorldStateCrimeProjection"/> of this <see cref="WorldZone"/> based on the 
        /// specified <see cref="TimePeriod"/> and <see cref="Game.Weather"/>
        /// </summary>
        /// <param name="period"></param>
        /// <param name="weather"></param>
        /// <returns></returns>
        public WorldStateCrimeProjection CalculateCrimeProbabilities(TimePeriod period, Weather weather)
        {
            return new WorldStateCrimeProjection(period, weather, Zone);
        }
    }
}
