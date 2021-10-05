using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;

namespace AgencyDispatchFramework.Simulation
{
    internal struct SimulationSettings
    {
        public bool Supervisor { get; set; }

        public District SelectedDistrict { get; set; }

        public bool FastForward { get; set; }

        public bool SyncTime { get; set; }

        public bool SyncDate { get; set; }

        public bool RandomWeather { get; set; }

        public bool RealisticWeather { get; set; }

        public bool ForceWeather { get; set; }

        public Weather SelectedWeather { get; set; }

        public int Beat { get; set; }

        public ShiftRotation SelectedShift { get; set; }

        public UnitType PrimaryRole { get; set; }

        public int TimeScaleMult { get; set; }
    }
}
