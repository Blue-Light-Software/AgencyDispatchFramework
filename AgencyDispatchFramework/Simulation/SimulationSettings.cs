using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyDispatchFramework.Simulation
{
    internal struct SimulationSettings
    {
        public bool Supervisor { get; set; }

        public bool FastForward { get; set; }

        public bool RandomWeather { get; set; }

        public bool ForceWeather { get; set; }

        public Weather SelectedWeather { get; set; }

        public CallSign SetCallSign { get; set; }

        public ShiftRotation SelectedShift { get; set; }

        public UnitType PrimaryRole { get; set; }

        public int TimescaleMult { get; set; }
    }
}
