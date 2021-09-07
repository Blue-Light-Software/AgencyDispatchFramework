using LiteDB;
using Rage;

namespace AgencyDispatchFramework.Game.Locations
{
    /// <summary>
    /// Represents a coordinate within the game world with a directional heading. 
    /// This data is used to spawn an <see cref="Entity"/> in its place.
    /// </summary>
    public class SpawnPoint
    {
        /// <summary>
        /// Gets the <see cref="Vector3"/> coordinates of this location
        /// </summary>
        [BsonIgnore]
        public Vector3 Position
        {
            get => new Vector3(X, Y, Z);
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
        /// Gets the heading of an object <see cref="Entity"/> at this location, if any
        /// </summary>
        public float Heading { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public SpawnPoint()
        {

        }

        /// <summary>
        /// Creates a new instance of <see cref="SpawnPoint"/>
        /// </summary>
        /// <param name="position">The <see cref="Vector3"/> location</param>
        /// <param name="heading">The directional heading for an <see cref="Entity"/> to face</param>
        public SpawnPoint(Vector3 position, float heading = 0f)
        {
            this.Position = position;
            this.Heading = heading;
        }

        /// <summary>
        /// Enables casting to a <see cref="Vector3"/>
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator Vector3(SpawnPoint s)
        {
            return s.Position;
        }

        /// <summary>
        /// Enables casting to a <see cref="Vector3"/>
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator Vector4(SpawnPoint s)
        {
            return new Vector4(s.Position, s.Heading);
        }

        /// <summary>
        /// Enables casting to  a <see cref="float"/>
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator float(SpawnPoint s)
        {
            return s.Heading;
        }

        /// <summary>
        /// Enables casting to  a <see cref="float"/>
        /// </summary>
        /// <param name="s"></param>
        public static implicit operator SpawnPoint(Vector4 vector)
        {
            return new SpawnPoint() { X = vector.X, Y = vector.Y, Z = vector.Z, Heading = vector.W };
        }
    }
}
