using LiteDB;
using Rage;
using System.Collections.Generic;
using System.Linq;

namespace AgencyDispatchFramework.Game.Locations
{
    /// <summary>
    /// A <see cref="WorldLocation"/> that represents a position on a paved or dirt road,
    /// used to spawn a <see cref="Vehicle"/>
    /// </summary>
    public class RoadPosition : WorldLocation
    {
        [BsonIgnore]
        public override LocationTypeCode LocationType => LocationTypeCode.RoadPosition;

        /// <summary>
        /// Gets the id of this location
        /// </summary>
        [BsonId]
        public int Id { get; set; }

        /// <summary>
        /// Gets the heading of an object <see cref="Entity"/> at this location, if any
        /// </summary>
        public float Heading { get; set; }

        /// <summary>
        /// Gets or sets the Speed limit on the <see cref="RoadPosition"/>
        /// </summary>
        public int SpeedLimit { get; set; }

        /// <summary>
        /// Gets an array of RoadFlags that describe this <see cref="RoadShoulder"/>
        /// </summary>
        public List<RoadFlags> Flags { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="RoadPosition"/>
        /// </summary>
        public RoadPosition()
        {

        }

        /// <summary>
        /// Converts our <see cref="ResidenceFlags"/> to intergers and returns them
        /// </summary>
        /// <remarks>
        /// Used for filtering locations based on flags
        /// </remarks>
        /// <returns>An array of filters as integers</returns>
        public override int[] GetIntFlags()
        {
            return Flags?.Select(x => (int)x).ToArray();
        }
    }
}
