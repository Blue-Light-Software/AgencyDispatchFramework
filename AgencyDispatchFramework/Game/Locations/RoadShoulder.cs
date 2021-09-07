using LiteDB;
using Rage;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AgencyDispatchFramework.Game.Locations
{
    /// <summary>
    /// A <see cref="WorldLocation"/> that is a shoulder of a road, used for pullover locations
    /// to spawn a <see cref="Vehicle"/> or a <see cref="Ped"/>
    /// </summary>
    public class RoadShoulder : WorldLocation
    {
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
        /// Gets an array of RoadFlags that describe this <see cref="RoadShoulder"/>
        /// </summary>
        public List<RoadFlags> Flags { get; set; }

        /// <summary>
        /// Gets an array of <see cref="IntersectionFlags"/> for an intersection in front
        /// of this <see cref="WorldLocation.Position"/>
        /// </summary>
        public List<IntersectionFlags> BeforeIntersectionFlags { get;  set; }

        /// <summary>
        /// Gets an array of <see cref="IntersectionFlags"/> for an intersection behind
        /// this <see cref="WorldLocation.Position"/>
        /// </summary>
        public List<IntersectionFlags> AfterIntersectionFlags { get; set; }

        /// <summary>
        /// If the intersection in front this <see cref="RoadShoulder"/> location is a 
        /// <see cref="IntersectionFlags.ThreeWayIntersection"/>, then this property 
        /// describes the relative direction of the ajoining road.
        /// </summary>
        public RelativeDirection BeforeIntersectionDirection { get; set; }

        /// <summary>
        /// If the intersection behind this <see cref="RoadShoulder"/> location is a 
        /// <see cref="IntersectionFlags.ThreeWayIntersection"/>, then this property 
        /// describes the relative direction of the ajoining road.
        /// </summary>
        public RelativeDirection AfterIntersectionDirection { get; set; }

        /// <summary>
        /// Containts a list of spawn points for <see cref="Entity"/> types
        /// </summary>
        public Dictionary<RoadShoulderPosition, SpawnPoint> SpawnPoints { get; set; }

        /// <summary>
        /// Gets the <see cref="Locations.LocationTypeCode"/>
        /// </summary>
        /// <summary>
        /// Gets the <see cref="LocationType"/> for this <see cref="WorldLocation"/>
        /// </summary>
        [BsonIgnore]
        public override LocationTypeCode LocationType => LocationTypeCode.RoadShoulder;

        /// <summary>
        /// Creates a new instance of <see cref="RoadShoulder"/>
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="vector"></param>
        /// <param name="heading"></param>
        public RoadShoulder()
        {
            SpawnPoints = new Dictionary<RoadShoulderPosition, SpawnPoint>();
        }

        /// <summary>
        /// Returns whether the <see cref="SpawnPoint"/> collection is complete
        /// for this <see cref="WorldLocation"/> instance.
        /// </summary>
        /// <returns>true if all spawn points are set, false otherwise</returns>
        internal bool IsValid()
        {
            // Ensure spawn points is full
            foreach (RoadShoulderPosition type in Enum.GetValues(typeof(RoadShoulderPosition)))
            {
                if (!SpawnPoints.ContainsKey(type))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets an identifiable <see cref="SpawnPoint"/> by name for this <see cref="RoadShoulder"/>
        /// </summary>
        /// <param name="id">The <see cref="SpawnPoint"/> id</param>
        /// <returns>a <see cref="SpawnPoint"/> on success, false otherwise</returns>
        public SpawnPoint GetSpawnPositionById(RoadShoulderPosition id)
        {
            if (!SpawnPoints.ContainsKey(id))
                return null;

            return SpawnPoints[id];
        }

        /// <summary>
        /// Converts our <see cref="RoadFlags"/> to intergers and returns them
        /// </summary>
        /// <remarks>
        /// Used for filtering locations based on flags
        /// </remarks>
        /// <returns>An array of filters as integers</returns>
        public override int[] GetIntFlags()
        {
            return Flags?.Select(x => (int)x).ToArray();
        }

        /// <summary>
        /// Enables casting to a <see cref="Vector3"/>
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator SpawnPoint(RoadShoulder s)
        {
            //return new SpawnPoints.Position, s.Heading);
            return null;
        }

        /// <summary>
        /// Enables casting to a <see cref="Vector3"/>
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator Vector3(RoadShoulder s)
        {
            return s.Position;
        }
    }
}
