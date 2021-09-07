using Rage;
using System;
using System.Drawing;
using static Rage.Native.NativeFunction;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// Represents a checkpoint within the game world and methods to manipulate it.
    /// </summary>
    public class Checkpoint : IDisposable
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
        public bool IsDisposed { get; protected set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="handle">The handle of the checkpoint.</param>
        /// <param name="position">The position of the checkpoint. </param>
        /// <param name="pointingTo">The position of the next checkpoint to point to.</param>
        internal Checkpoint(int handle, Vector3 position, Vector3 pointingTo, Color color)
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

        /// <summary>
        /// Deletes a checkpoint with the specified handle
        /// </summary>
        /// <param name="handle"></param>
        public void Dispose()
        {
            IsDisposed = true;
            Natives.DeleteCheckpoint(Handle);
        }
    }
}
