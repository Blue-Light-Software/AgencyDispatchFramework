namespace AgencyDispatchFramework.Game.Locations
{
    /// <summary>
    /// Represents a coordinate within the <see cref="GameWorld"/> with no specific
    /// location type.
    /// </summary>
    public class WorldPosition : WorldLocation
    {
        /// <summary>
        /// Gets the heading of an object <see cref="Entity"/> at this location, if any
        /// </summary>
        public float Heading { get; set; }

        /// <summary>
        /// Gets the <see cref="Locations.LocationTypeCode"/>
        /// </summary>
        /// <summary>
        /// Gets the <see cref="LocationType"/> for this <see cref="WorldLocation"/>
        /// </summary>
        public override LocationTypeCode LocationType => LocationTypeCode.Coordinate;

        /// <summary>
        /// Returns nothing
        /// </summary>
        public override int[] GetIntFlags()
        {
            return new int[0];
        }
    }
}
