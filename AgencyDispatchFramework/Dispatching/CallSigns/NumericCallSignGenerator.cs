using AgencyDispatchFramework.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace AgencyDispatchFramework.Dispatching
{
    /// <summary>
    /// 
    /// </summary>
    internal class NumericCallSignGenerator : CallSignGenerator
    {
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
        public NumericCallSignGenerator()
        {
            /**
             * 1-19 = Upper Management (Sheriff, Chief of Police etc etc)
             * 20-69 = Supervisors (Sergeants)
             * 70-99 = Detectives
             * 100-119 = Special Teams
             * 120-149 = K9
             * 150-199 = Gang
             * 200-299 = Traffic
             * 300-499 = Patrol
             */

            // Create officer unit callsigns
            OfficerCallSigns = new Dictionary<UnitType, Queue<int>>()
            {
                { UnitType.K9, new Queue<int>(GetShuffledQueue(120, 149)) },
                { UnitType.Gang, new Queue<int>(GetShuffledQueue(150, 199)) }, 
                { UnitType.Traffic, new Queue<int>(GetShuffledQueue(200, 299)) },
                { UnitType.Patrol, new Queue<int>(GetShuffledQueue(300, 499)) },
            };

            // Create officer unit callsigns
            var q = new Queue<int>(GetShuffledQueue(20, 69));
            SupervisorCallSigns = new Dictionary<UnitType, Queue<int>>()
            {
                { UnitType.K9, q },
                { UnitType.Gang, q },
                { UnitType.Traffic, q },
                { UnitType.Patrol, q },
            };
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="minRange"></param>
        /// <param name="maxRange"></param>
        /// <returns></returns>
        private IEnumerable<int> GetShuffledQueue(int minRange, int maxRange)
        {
            // Supervisors start at 1-10
            var beats = Enumerable.Range(minRange, maxRange).ToList();

            // Shuffle beats
            beats.Shuffle();

            // Return shuffled beats
            return beats;
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

            // Create
            return new NumericStyleCallsign(callSignQueue.Dequeue());
        }
    }
}
