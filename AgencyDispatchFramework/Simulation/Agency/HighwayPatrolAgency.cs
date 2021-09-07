using AgencyDispatchFramework.Dispatching;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// Represents a policing agency that is statewide
    /// </summary>
    public class HighwayPatrolAgency : PoliceAgency
    {
        public override AgencyType AgencyType => AgencyType.HighwayPatrol; 

        internal HighwayPatrolAgency(string scriptName, string friendlyName, StaffLevel staffLevel, CallSignStyle signStyle)
            : base(scriptName, friendlyName, staffLevel, signStyle)
        {
        }

        protected override void AssignZones()
        {
            // Get our zones of jurisdiction, and ensure each zone has the primary agency set
            foreach (var zone in Zones)
            {
                // Set agencies. Order is important here!
                zone.SetPoliceAgencies(this);
            }
        }
    }
}
