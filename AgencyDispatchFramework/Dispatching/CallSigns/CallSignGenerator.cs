using System.Collections.Generic;

namespace AgencyDispatchFramework.Dispatching
{
    internal abstract class CallSignGenerator
    {
        public abstract CallSign Next(UnitType unitType, bool supervisor);

        public abstract HashSet<int> GetAvailableBeats(UnitType unitType, bool supervisor);
    }
}
