﻿using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Scripting;
using Rage;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// Handles the simultion of a scene when the player is in view
    /// </summary>
    public class AISceneSimulation
    {
        /// <summary>
        /// Gets or sets the see active <see cref="Event"/>
        /// </summary>
        protected ActiveEvent Event { get; set; }

        /// <summary>
        /// Indicates whether or this <see cref="AISceneSimulation"/> is currently running
        /// </summary>
        public bool IsPlaying { get; protected set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        /// <param name="call"></param>
        public AISceneSimulation(Scripting.ActiveEvent call)
        {
            Event = call;
        }

        /// <summary>
        /// Indicates whether the player is in range to draw the scene
        /// </summary>
        /// <param name="playerLocation"></param>
        /// <returns></returns>
        public bool IsPlayerInRange(Vector3 playerLocation)
        {
            return playerLocation.DistanceTo(Event.Location.Position) < 50f;
        }

        public virtual void Begin()
        {
            // Tell AI officer to get out of his car

            // Walk to the front door
        }

        /// <summary>
        /// Main Logic called OnTick
        /// </summary>
        public virtual void Process()
        {
            


        }

        public virtual void End()
        {

        }
    }
}
