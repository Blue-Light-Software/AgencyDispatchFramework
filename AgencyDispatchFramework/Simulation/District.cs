using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// Represents a patrol area for <see cref="OfficerUnit"/>s
    /// </summary>
    internal class District
    {
        /// <summary>
        /// A static randomizer
        /// </summary>
        protected static CryptoRandom RNG => new CryptoRandom();

        /// <summary>
        /// Gets the index of this district. Unique to each <see cref="Agency"/>
        /// </summary>
        public int Index { get; internal set; }

        /// <summary>
        /// Gets the name of this <see cref="District"/>
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Contains a list of zones in this jurisdiction
        /// </summary>
        public WorldZone[] Zones { get; set; }

        // <summary>
        /// 
        /// </summary>
        internal Dictionary<ShiftRotation, List<OfficerUnit>> OfficersByShift { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal CallSignGenerator CallSignGenerator { get; set; }

        /// <summary>
        /// Gets the optimum patrol count based on <see cref="TimePeriod" />
        /// </summary>
        internal Dictionary<UnitType, Dictionary<TimePeriod, double>> OptimumPatrols { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="District"/>
        /// </summary>
        public District(int uniqueId, string name, WorldZone[] zones, CallSignGenerator generator)
        {
            // Set properties
            Index = uniqueId;
            Name = name;
            Zones = zones;
            CallSignGenerator = generator;
            OptimumPatrols = new Dictionary<UnitType, Dictionary<TimePeriod, double>>();

            // Fill officer hash table
            OfficersByShift = new Dictionary<ShiftRotation, List<OfficerUnit>>();
            foreach (ShiftRotation shift in Enum.GetValues(typeof(ShiftRotation)))
            {
                OfficersByShift.Add(shift, new List<OfficerUnit>());
            }
        }

        /// <summary>
        /// Assigns the unit to this district
        /// </summary>
        /// <param name="unit"></param>
        /// <param name="shift"></param>
        public void Assign(OfficerUnit unit, ShiftRotation shift)
        {
            OfficersByShift[shift].Add(unit);
            unit.District = this;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="locationType"></param>
        /// <returns></returns>
        public WorldLocation GetRandomWorldLocation(LocationTypeCode locationType)
        {
            // Someone fucked up
            if (Zones.Length == 0) return null;

            // Get a list of unique indexes
            List<int> indexes = Enumerable.Range(0, Zones.Length - 1).ToList();

            // shuffle
            indexes.Shuffle();

            // Try and fetch a zone with this location type
            for (int i = 0; i < indexes.Count; i++)
            {
                // Grab zone at this index
                var zone = Zones.ElementAt(i);

                // Grab random location of type
                var loc = zone.GetRandomLocation(locationType, FlagFilterGroup.Default, true);
                if (loc != null)
                {
                    return loc;
                }
            }

            // If we are here, we failed
            return null;
        }
    }
}
