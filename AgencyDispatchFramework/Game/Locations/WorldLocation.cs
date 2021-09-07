using LiteDB;
using Rage;
using System;
using System.Linq;
using System.Text;

namespace AgencyDispatchFramework.Game.Locations
{
    /// <summary>
    /// Contains a <see cref="Vector3"/> position within the GTA V world, and information
    /// about the surrounding area.
    /// </summary>
    public abstract class WorldLocation : ISpatial, IEquatable<WorldLocation>
    {
        /// <summary>
        /// Gets the <see cref="LocationType"/> for this <see cref="WorldLocation"/>
        /// </summary>
        [BsonIgnore]
        public abstract LocationTypeCode LocationType { get; }

        /// <summary>
        /// Gets the <see cref="Vector3"/> coordinates of this location
        /// </summary>
        [BsonIgnore]
        public Vector3 Position
        {
            get => new Vector3(X, Y,Z);
            set
            {
                X = value.X;
                Y = value.Y;
                Z = value.Z;
            }
        }

        /// <summary>
        /// Gets the X value coordinate of this location
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Gets the Y value coordinate of this location
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Gets the Z value coordinate of this location
        /// </summary>
        public float Z { get; set; }

        /// <summary>
        /// Gets the address to be used in the CAD
        /// </summary>
        public string StreetName { get; set; } = String.Empty;

        /// <summary>
        /// Gets the hint or description text of this location if any
        /// </summary>
        public string Hint { get; set; } = String.Empty;

        /// <summary>
        /// Gets the zone of this location, if known
        /// </summary>
        [BsonRef("WorldZones")]
        public WorldZone Zone { get; set; }

        /// <summary>
        /// Gets the postal address if any
        /// </summary>
        [BsonIgnore]
        public Postal Postal => Postal.FromVector(Position);

        /// <summary>
        /// Gets an array of flags used to describe this <see cref="WorldLocation"/>
        /// </summary>
        public abstract int[] GetIntFlags();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual string GetAddress()
        {
            StringBuilder builder = new StringBuilder();

            if (!String.IsNullOrWhiteSpace(StreetName))
                builder.Append(StreetName);

            return builder.ToString();
        }

        /// <summary>
        /// Determines whether this <see cref="WorldLocation"/> instance contains all the specified flags.
        /// This method is used for filtering locations based on Callout location requirements.
        /// </summary>
        /// <param name="requiredFlags"></param>
        /// <returns></returns>
        public bool HasAllFlags(int[] requiredFlags)
        {
            var flags = GetIntFlags();
            return requiredFlags.All(i => flags.Contains(i));
        }

        /// <summary>
        /// Determines whether this <see cref="WorldLocation"/> instance contains any of the specified flags.
        /// This method is used for filtering locations based on Callout location requirements.
        /// </summary>
        /// <param name="flags"></param>
        /// <returns></returns>
        public bool HasAnyFlag(int[] flags)
        {
            var f = GetIntFlags();
            return flags.Any(i => f.Contains(i));
        }

        /// <summary>
        /// Enables casting to a <see cref="Vector3"/>
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator Vector3(WorldLocation w)
        {
            return w.Position;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(WorldLocation other)
        {
            if (other == null) return false;
            return (other.X == X && other.Y == Y && other.Z == Z);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return Equals(obj as WorldLocation);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{X}, {Y}, {Z}";

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode() => (X, Y, Z).GetHashCode();

        #region ISpatial contracts

        public float DistanceTo(Vector3 position) => Position.DistanceTo(position);

        public float DistanceTo(ISpatial spatialObject) => Position.DistanceTo(spatialObject);

        public float DistanceTo2D(Vector3 position) => Position.DistanceTo2D(position);

        public float DistanceTo2D(ISpatial spatialObject) => Position.DistanceTo2D(spatialObject);

        public float TravelDistanceTo(Vector3 position) => Position.TravelDistanceTo(position);

        public float TravelDistanceTo(ISpatial spatialObject) => Position.TravelDistanceTo(spatialObject);

        #endregion
    }
}
