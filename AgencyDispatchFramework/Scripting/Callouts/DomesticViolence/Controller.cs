using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game.Locations;
using LSPD_First_Response.Mod.Callouts;
using System;

namespace AgencyDispatchFramework.Scripting.Callouts.DomesticViolence
{
    /// <summary>
    /// A callout representing a domestic violence call with multiple scenarios
    /// </summary>
    /// <remarks>
    /// All AgencyCallout type callouts must have a CalloutProbability of Never!
    /// This is due to a reliance on the <see cref="Dispatch"/> class for location
    /// and <see cref="CalloutScenarioMeta"/> information.
    /// </remarks>
    [CalloutInfo("AgencyCallout.DomesticViolence", CalloutProbability.Never)]
    internal class Controller : AgencyCallout
    {
        /// <summary>
        /// Gets the script info name of this <see cref="Callout"/>
        /// </summary>
        public override string Name => ScriptInfo.Name;

        /// <summary>
        /// 
        /// </summary>
        public override PriorityCall Call { get; protected set; }

        /// <summary>
        /// Stores the <see cref="API.Residence"/> where the accident occured
        /// </summary>
        public Residence Location { get; protected set; }

        /// <summary>
        /// Stores the current randomized scenario
        /// </summary>
        private CalloutScenario Scenario;

        /// <summary>
        /// Event called right before the Callout is displayed to the player
        /// </summary>
        /// <remarks>
        /// Lets do all of our heavy loading here, BEFORE the callout begins for the Player
        /// </remarks>
        /// <returns>false on failure, true otherwise</returns>
        public override bool OnBeforeCalloutDisplayed()
        {
            // Grab the priority call dispatched to player
            Call = Dispatch.RequestPlayerCallInfo(this);
            if (Call == null)
            {
                Log.Error("AgencyCallout.DomesticViolence: This is awkward... No PriorityCall of this type for player");
                return false;
            }

            try
            {
                // Get location
                Location = Call.EventHandle.Location as Residence;

                // Create scenario class handler
                Scenario = CreateScenarioInstance();

                // Show are blip and message
                ShowCalloutAreaBlipBeforeAccepting(Location.Position, 40f);
                CalloutMessage = Call.EventHandle.ScenarioMeta.CADEventText;
                CalloutPosition = Location.Position;
            }
            catch (Exception e)
            {
                // Log exception
                Log.Exception(e);

                // Tell LSPDFR we failed to setup correctly
                return false;
            }

            // Return base
            return base.OnBeforeCalloutDisplayed();
        }

        /// <summary>
        /// Event called right after the Callout is accepted by the player
        /// </summary>
        /// <returns>false on failure, true otherwise</returns>
        public override bool OnCalloutAccepted()
        {
            try
            {
                // Setup active scene
                Scenario.Setup();
            }
            catch (Exception e)
            {
                // Log exception
                Log.Exception(e);

                // Clean up entities
                Scenario.Cleanup();

                // Clear call
                base.OnCalloutNotAccepted();

                // Tell LSPDFR we failed to setup the scenario
                return false;
            }

            // AgencyCallout base class will handle the Dispatch stuff
            return base.OnCalloutAccepted();
        }

        /// <summary>
        /// Event called right if the callout is not accepted by the player
        /// </summary>
        public override void OnCalloutNotAccepted()
        {
            // AgencyCallout base class will handle the Dispatch stuff
            base.OnCalloutNotAccepted();
        }

        /// <summary>
        /// Method called every tick
        /// </summary>
        public override void Process()
        {
            Scenario.Process();
            base.Process();
        }

        /// <summary>
        /// Method called when the callout has ended
        /// </summary>
        public override void End()
        {
            Scenario.Cleanup();
            base.End();
        }

        /// <summary>
        /// Creates a new instance of the chosen scenario by name
        /// </summary>
        /// <returns></returns>
        private CalloutScenario CreateScenarioInstance()
        {
            switch (Event.ScenarioMeta.ScenarioName)
            {
                case "ReportsOfArguingThreats":
                    return new ReportsOfArguingThreats(this, Event.ScenarioMeta);
                default:
                    throw new Exception($"Unsupported DomesticViolence Scenario '{Event.ScenarioMeta.ScenarioName}'");
            }
        }
    }
}
