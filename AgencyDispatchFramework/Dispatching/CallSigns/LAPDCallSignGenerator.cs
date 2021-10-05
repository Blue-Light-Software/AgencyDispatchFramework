using AgencyDispatchFramework.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// 
    /// </summary>
    internal class LAPDCallSignGenerator : CallSignGenerator
    {
        /// <summary>
        /// 
        /// </summary>
        public int DistrictIndex { get; private set; }

        /// <summary>
        /// Gets a Queue of unused callsigns for supervisor units based on <see cref="UnitType"/>
        /// </summary>
        private Dictionary<UnitType, Queue<int>> SupervisorCallSigns { get; set; }

        /// <summary>
        /// Gets a Queue of unused callsigns for officer units based on <see cref="UnitType"/>
        /// </summary>
        private Dictionary<UnitType, Queue<int>> OfficerCallSigns { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public LAPDCallSignGenerator(int districtIndex)
        {
            DistrictIndex = districtIndex;

            // Create officer unit callsigns
            OfficerCallSigns = new Dictionary<UnitType, Queue<int>>();
            foreach (UnitType type in Enum.GetValues(typeof(UnitType)))
            {
                // Supervisors start at 1-10
                var beats = Enumerable.Range(11, 88).ToList();

                // Shuffle beats
                beats.Shuffle();

                // Add a queue of unused beats
                OfficerCallSigns.Add(type, new Queue<int>(beats));
            }

            // Create supervisor unit callsigns
            SupervisorCallSigns = new Dictionary<UnitType, Queue<int>>();
            foreach (UnitType type in Enum.GetValues(typeof(UnitType)))
            {
                // Supervisors start at 1-10
                var beats = Enumerable.Range(1, 10).ToList();

                // Shuffle beats
                beats.Shuffle();

                // Add a queue of unused beats
                SupervisorCallSigns.Add(type, new Queue<int>(beats));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unitType"></param>
        /// <returns></returns>
        public override CallSign Next(UnitType unitType, bool supervisor)
        {
            // Get next beat
            var callSignQueue = (supervisor) ? SupervisorCallSigns[unitType] : OfficerCallSigns[unitType];
            if (callSignQueue.Count == 0)
            {
                return null;
            }

            // Get alpha index of the unit
            var unit = GetUnitTypeChar(unitType);
            int unitTypeIndex = (int)unit % 32;

            return new LAPDStyleCallsign(DistrictIndex, unitTypeIndex, callSignQueue.Dequeue());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unitType"></param>
        /// <param name="supervisor"></param>
        /// <returns></returns>
        public override HashSet<int> GetAvailableBeats(UnitType unitType, bool supervisor)
        {
            // Get next beat
            var callSignQueue = (supervisor) ? SupervisorCallSigns[unitType] : OfficerCallSigns[unitType];
            if (callSignQueue.Count == 0)
            {
                return new HashSet<int>();
            }

            return callSignQueue.ToHashSet();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="unitType"></param>
        /// <returns></returns>
        public static char GetUnitTypeChar(UnitType unitType)
        {
            switch (unitType)
            {
                default: return 'X';
                case UnitType.Patrol: return 'L';
                case UnitType.Traffic: return 'E';
                case UnitType.Gang: return 'G';
                case UnitType.K9: return 'K';
            }
        }
    }
}
