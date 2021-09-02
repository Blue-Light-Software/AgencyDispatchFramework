using AgencyDispatchFramework.Dispatching;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Simulation
{
    public class SheriffAgency : PoliceAgency
    {
        public override AgencyType AgencyType => AgencyType.CountySheriff;

        internal SheriffAgency(string scriptName, string friendlyName, StaffLevel staffLevel, CallSignStyle signStyle)
            : base(scriptName, friendlyName, staffLevel, signStyle)
        {

        }

        protected override void AssignZones()
        {
            // Get our zones of jurisdiction, and ensure each zone has the primary agency set
            foreach (var zone in Zones)
            {
                // Set agencies. Order is important here!
                zone.PoliceAgencies = new List<Agency>()
                {
                    this,
                    GetAgencyByName("sahp")
                };
            }
        }
    }
}
