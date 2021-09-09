using Rage;
using System;
using System.Drawing;
using static Rage.Native.NativeFunction;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// Represents a checkpoint within the game world and methods to manipulate it.
    /// </summary>
    public class Checkpoint : ISpatial, IDeletable, IEquatable<Checkpoint>
    {
        /// <summary>
        /// Gets or sets the reference handle of the checkpoint
        /// </summary>
        private int Handle { get; }

        /// <summary>
        /// Gets the position of this <see cref="Checkpoint"/> in the world
        /// </summary>
        public Vector3 Position { get; }

        /// <summary>
        /// Gets the direction this <see cref="Checkpoint"/> points towards.
        /// </summary>
        public Vector3 PointingTo { get; }

        /// <summary>
        /// Gets the current color of the <see cref="Checkpoint"/>
        /// </summary>
        public Color Color { get; protected set; }

        /// <summary>
        /// Indicates whether this checkpoint has been deleted in game already.
        /// </summary>
        public bool Deleted { get; protected set; }

        /// <summary>
        /// Gets or sets a custom object that contains data about this <see cref="Checkpoint"/>.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Gets the position of this <see cref="Checkpoint"/> in the world
        /// </summary>
        Vector3 ISpatial.Position
        {
            get => Position;
            set
            {
                // Do nothing since we are read only 
            }
        }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="handle">The handle of the checkpoint.</param>
        /// <param name="position">The position of the checkpoint. </param>
        /// <param name="pointingTo">The position of the next checkpoint to point to.</param>
        private Checkpoint(int handle, Vector3 position, Vector3 pointingTo, Color color)
        {
            Handle = handle;
            Position = position;
            PointingTo = pointingTo;
            Color = color;
        }

        /// <summary>
        /// Sets the cylinder height of the checkpoint. 
        /// </summary>
        /// <param name="nearHeight">The height of the checkpoint when inside of the radius. </param>
        /// <param name="farHeight">The height of the checkpoint when outside of the radius. </param>
        /// <param name="radius">The radius of the checkpoint. </param>
        public void SetCylinderHeight(float nearHeight, float farHeight, float radius)
        {
            Natives.SetCheckpointCylinderHeight(Handle, nearHeight, farHeight, radius);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="checkpoint"></param>
        /// <param name="scale"></param>
        public void SetScale(float scale)
        {
            Natives.SetCheckpointScale(Handle, scale);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="checkpoint"></param>
        /// <param name="scale"></param>
        public void SetIconScale(float scale)
        {
            Natives.SetCheckpointIconScale(Handle, scale);
        }

        /// <summary>
        /// Sets the checkpoint color.  
        /// </summary>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <param name="alpha"></param>
        public void SetColor(int red, int green, int blue, int alpha)
        {
            this.Color = Color.FromArgb(alpha, red, green, blue);
            Natives.SetCheckpointRgba(Handle, red, green, blue, alpha);
        }

        /// <summary>
        /// Sets the checkpoint color.  
        /// </summary>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <param name="alpha"></param>
        public void SetColor(Color color)
        {
            this.Color = color;
            Natives.SetCheckpointRgba(Handle, color.R, color.G, color.B, color.A);
        }

        /// <summary>
        /// Sets the checkpoint icon color. 
        /// </summary>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <param name="alpha"></param>
        public void SetIconColor(int red, int green, int blue, int alpha)
        {
            Natives.SetCheckpointRgba2(Handle, red, green, blue, alpha);
        }

        /// <summary>
        /// Sets the checkpoint icon color.
        /// </summary>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <param name="alpha"></param>
        public void SetIconColor(Color color)
        {
            Natives.SetCheckpointRgba2(Handle, color.R, color.G, color.B, color.A);
        }

        #region Contract Methods

        public float DistanceTo(Vector3 position)
        {
            return Position.DistanceTo(position);
        }

        public float DistanceTo(ISpatial spatialObject)
        {
            return Position.DistanceTo(spatialObject);
        }

        public float DistanceTo2D(Vector3 position)
        {
            return Position.DistanceTo2D(position);
        }

        public float DistanceTo2D(ISpatial spatialObject)
        {
            return Position.DistanceTo2D(spatialObject);
        }

        public float TravelDistanceTo(Vector3 position)
        {
            return Position.TravelDistanceTo(position);
        }

        public float TravelDistanceTo(ISpatial spatialObject)
        {
            return Position.TravelDistanceTo(spatialObject);
        }

        /// <summary>
        /// Deletes a checkpoint
        /// </summary>
        public void Delete()
        {
            Deleted = true;
            Natives.DeleteCheckpoint(Handle);
        }

        #endregion

        /// <summary>
        /// Creates a checkpoint at the specified location, and returns the handle
        /// </summary>
        /// <remarks>
        /// Checkpoints are already handled by the game itself, so you must not loop it like markers.
        /// </remarks>
        /// <seealso cref="https://docs.fivem.net/docs/game-references/checkpoints/"/>
        /// <param name="type">The type of checkpoint to create.</param>
        /// <param name="pos">The position of the checkpoint</param>
        /// <param name="radius">The radius of the checkpoint cylinder</param>
        /// <param name="color">The color of the checkpoint</param>
        /// <returns>returns the handle of the checkpoint</returns>
        public static Checkpoint Create(Vector3 pos, Color color, int type = 47, float radius = 5f, float nearHeight = 3f, float farHeight = 3f, bool forceGround = false, int number = 0)
        {
            if (forceGround)
            {
                var level = World.GetGroundZ(pos, true, false);
                if (level.HasValue)
                    pos.Z = level.Value;
            }

            // Create checkpoint
            int handle = Natives.CreateCheckpoint<int>(type, pos.X, pos.Y, pos.Z, pos.X, pos.Y, pos.Z, 1f, color.R, color.G, color.B, color.A, number);

            // Set hieght and radius of the cylinder
            Natives.SetCheckpointCylinderHeight(handle, nearHeight, farHeight, radius);

            // return handle
            return new Checkpoint(handle, pos, pos, color);
        }

        #region overrides

        public override bool Equals(object obj)
        {
            return Equals(obj as Checkpoint);
        }

        /// <summary>
        /// Compares the handles of the two objects
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(Checkpoint other)
        {
            return other?.Handle == Handle;
        }

        public override int GetHashCode()
        {
            return Handle.GetHashCode();
        }

        public override string ToString()
        {
            return $"[Checkpoint # {Handle}; {Position}]";
        }

        #endregion
    }
}
