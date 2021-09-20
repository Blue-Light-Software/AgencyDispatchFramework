using AgencyDispatchFramework.Game.Locations;

namespace AgencyDispatchFramework.Scripting
{
    public class PossibleLocationMeta : ISpawnable
    {
        /// <summary>
        /// Defines the location type of this call
        /// </summary>
        public LocationTypeCode LocationTypeCode { get; set; }

        /// <summary>
        /// Indicates the location flags required for this scenario, if any
        /// </summary>
        public FlagFilterGroup LocationFilters { get; set; }

        /// <summary>
        /// Gets the probabilty this location meta will be used
        /// </summary>
        public int Probability { get; set; }
    }
}
