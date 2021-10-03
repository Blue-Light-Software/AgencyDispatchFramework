using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// Represents a police type agency
    /// </summary>
    public class PoliceAgency : Agency
    {

        public override AgencyType AgencyType => AgencyType.CityPolice;

        public override ServiceSector Sector => ServiceSector.Police; 

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scriptName"></param>
        /// <param name="friendlyName"></param>
        /// <param name="staffLevel"></param>
        /// <param name="signStyle"></param>
        internal PoliceAgency(string scriptName, string friendlyName, StaffLevel staffLevel, CallSignStyle signStyle, string[] zoneNames) 
            : base(scriptName, friendlyName, staffLevel, signStyle, zoneNames)
        {
            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal override Dispatcher CreateDispatcher()
        {
            return new PoliceDispatcher(this);
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void AssignZones()
        {
            // Get our zones of jurisdiction, and ensure each zone has the primary agency set
            foreach (var zone in Zones)
            {
                // Grab county
                var name = zone.County == County.Blaine ? "bcso" : "lssd";

                // Set agencies. Order is important here!
                zone.SetPoliceAgencies(this, GetAgencyByName(name), GetAgencyByName("sahp"));
            }
        }

        /// <summary>
        /// Calculates the optimum patrols for this agencies jurisdiction
        /// </summary>
        /// <param name="zoneNames"></param>
        /// <returns></returns>
        protected override void CalculateAgencySize()
        {
            // --------------------------------------------------
            // Get number of optimal patrols, and total calls per period
            // --------------------------------------------------
            CallsByPeriod = new Dictionary<TimePeriod, double>();
            double idealRosterSize = 0;

            // Calculate each TimePeriod of the day
            Array timePeriods = Enum.GetValues(typeof(TimePeriod));
            foreach (TimePeriod period in timePeriods)
            {
                CallsByPeriod.Add(period, 0);
            }

            // Define which responsibilities that Patrol is the primary responder for
            var patrolHandlingCalls = new HashSet<CallCategory>((CallCategory[])Enum.GetValues(typeof(CallCategory)));

            // If we have a traffic unit, remove traffic responsibilities from patrol
            if (Units.ContainsKey(UnitType.Traffic))
                patrolHandlingCalls.Remove(CallCategory.Traffic);

            // Calculate each district
            foreach (var district in Districts.Values)
            {
                // Add Agency units types
                foreach (UnitType type in Units.Keys)
                {
                    var counts = new Dictionary<TimePeriod, double>();
                    foreach (TimePeriod period in timePeriods)
                    {
                        counts.Add(period, 0);
                    }

                    district.OptimumPatrols.Add(type, counts);
                }

                // Calculate each TimePeriod of the day
                foreach (TimePeriod period in timePeriods)
                {
                    // Define local variables
                    double optimumPatrols = 0;

                    // Add data from each zone in the Jurisdiction
                    foreach (var zone in district.Zones)
                    {
                        // Get average calls per period
                        var calls = zone.CrimeInfo.GetTrueAverageCallCount(period);
                        CallsByPeriod[period] += calls;
                        AverageDailyCalls += zone.CrimeInfo.AverageCalls;

                        // Grab crime report of this zone
                        var report = zone.CrimeInfo.CalculateCrimeProbabilities(period, Weather.Clear);

                        // --------------------------------------------------
                        // Add Patrol units
                        // --------------------------------------------------
                        if (Units.ContainsKey(UnitType.Patrol))
                        {
                            // Get average number of calls
                            var callCount = report.GetExpectedCallCountsOf(patrolHandlingCalls.ToArray());
                            optimumPatrols += GetOptimumPatrolCountForZone(callCount, period, zone);

                            idealRosterSize += optimumPatrols;
                            Units[UnitType.Patrol].OptimumPatrols[period] += optimumPatrols;
                            district.OptimumPatrols[UnitType.Patrol][period] += optimumPatrols;
                        }

                        // --------------------------------------------------
                        // Add Traffic units
                        // --------------------------------------------------
                        if (Units.ContainsKey(UnitType.Traffic))
                        {
                            // Get average number of calls
                            var callCount = report.GetExpectedCallCountsOf(CallCategory.Traffic);
                            optimumPatrols += GetOptimumTrafficCountForZone(callCount, period, zone);

                            idealRosterSize += optimumPatrols;
                            Units[UnitType.Traffic].OptimumPatrols[period] += optimumPatrols;
                            district.OptimumPatrols[UnitType.Traffic][period] += optimumPatrols;
                        }

                        // --------------------------------------------------
                        // Add Gang units
                        // --------------------------------------------------
                        // @todo Not implemented yet
                    }
                }
            }

            // Do stuff
            IdealRosterSize = (int)Math.Round(idealRosterSize, 0);
        }

        /// <summary>
        /// Determines the optimal number of patrols this zone should have based
        /// on <see cref="ZoneSize"/> and <see cref="CrimeLevel"/>
        /// </summary>
        /// <param name="averageCalls"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        protected virtual double GetOptimumPatrolCountForZone(double averageCalls, TimePeriod period, WorldZone zone)
        {
            double callsPerOfficerPerShift = 2d;
            double baseCount = Math.Max(0.5d, averageCalls / callsPerOfficerPerShift);

            switch (zone.Size)
            {
                case ZoneSize.VerySmall:
                case ZoneSize.Small:
                    baseCount--;
                    break;
                case ZoneSize.Medium:
                case ZoneSize.Large:
                    break;
                case ZoneSize.VeryLarge:
                case ZoneSize.Massive:
                    baseCount += 1;
                    break;
            }

            return Math.Max(0.25d, baseCount);
        }

        /// <summary>
        /// Determines the optimal number of patrols this zone should have based
        /// on <see cref="ZoneSize"/>, <see cref="Population"/> and <see cref="CrimeLevel"/> rate
        /// </summary>
        /// <param name="averageCalls"></param>
        /// <param name="zone"></param>
        /// <returns></returns>
        protected virtual double GetOptimumTrafficCountForZone(double averageCalls, TimePeriod period, WorldZone zone)
        {
            double callsPerOfficerPerShift = 2d;
            double baseCount = Math.Max(0.5d, averageCalls / callsPerOfficerPerShift);

            if (Units.ContainsKey(UnitType.Traffic))
            {
                switch (zone.Size)
                {
                    case ZoneSize.VerySmall:
                    case ZoneSize.Small:
                        baseCount--;
                        break;
                    case ZoneSize.Medium:
                    case ZoneSize.Large:
                        break;
                    case ZoneSize.VeryLarge:
                    case ZoneSize.Massive:
                        baseCount += 1;
                        break;
                }

                switch (zone.Population)
                {
                    default: // None
                        return 0;
                    case Population.Scarce:
                        baseCount *= 0.75;
                        break;
                    case Population.Moderate:
                        // No adjustment
                        break;
                    case Population.Dense:
                        baseCount *= 1.25;
                        break;
                }
            }

            return Math.Max(0.25d, baseCount);
        }
    }
}
