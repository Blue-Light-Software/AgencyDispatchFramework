using AgencyDispatchFramework.Dispatching;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AgencyDispatchFramework.Scripting
{
    /// <summary>
    /// Provides an interface to load and store <see cref="EventScenarioMeta"/> instances
    /// </summary>
    internal class ScenarioPool
    {
        /// <summary>
        /// Contains a list Scenarios by name
        /// </summary>
        internal Dictionary<string, EventScenarioMeta> ScenariosByName { get; set; }

        /// <summary>
        /// Contains a list Scenarios by name
        /// </summary>
        internal Dictionary<string, List<EventScenarioMeta>> ScenariosByAssembly { get; set; }

        /// <summary>
        /// Contains a list Scenarios seperated by CalloutType that will be used
        /// to populate the calls board
        /// </summary>
        internal Dictionary<CallCategory, WorldStateProbabilityGenerator<EventScenarioMeta>> ScenariosByCategory { get; set; }

        /// <summary>
        /// Contains a list of scenario's by callout name
        /// </summary>
        internal Dictionary<string, WorldStateProbabilityGenerator<EventScenarioMeta>> ScenariosByCalloutName { get; set; }

        /// <summary>
        /// Static method called the first time this class is referenced anywhere
        /// </summary>
        public ScenarioPool()
        {
            // Initialize callout types
            ScenariosByName = new Dictionary<string, EventScenarioMeta>();
            ScenariosByAssembly = new Dictionary<string, List<EventScenarioMeta>>();
            ScenariosByCalloutName = new Dictionary<string, WorldStateProbabilityGenerator<EventScenarioMeta>>();
            ScenariosByCategory = new Dictionary<CallCategory, WorldStateProbabilityGenerator<EventScenarioMeta>>();
            foreach (CallCategory type in Enum.GetValues(typeof(CallCategory)))
            {
                ScenariosByCategory.Add(type, new WorldStateProbabilityGenerator<EventScenarioMeta>());
            }
        }
        
        /// <summary>
        /// Adds the <see cref="EventScenarioMeta"/> to the internal lists of scenarios
        /// </summary>
        /// <param name="assembly"></param>
        /// <param name="scenario"></param>
        public void Add(Assembly assembly, EventScenarioMeta scenario)
        {
            // Add assembly
            var name = assembly.GetName().Name;
            if (!ScenariosByAssembly.ContainsKey(name))
            {
                ScenariosByAssembly.Add(name, new List<EventScenarioMeta>());
            }

            // Create entry if not already
            if (!ScenariosByCalloutName.ContainsKey(scenario.ControllerName))
            {
                ScenariosByCalloutName.Add(scenario.ControllerName, new WorldStateProbabilityGenerator<EventScenarioMeta>());
            }

            // Add scenario to the pools
            ScenariosByName.Add(scenario.ScenarioName, scenario);
            ScenariosByAssembly[name].Add(scenario);
            ScenariosByCalloutName[scenario.ControllerName].Add(scenario, scenario.ProbabilityMultipliers);
            ScenariosByCategory[scenario.Category].Add(scenario, scenario.ProbabilityMultipliers);
        }

        /// <summary>
        /// Clears the current list of scenario's from the pool
        /// </summary>
        internal void Reset()
        {
            ScenariosByName.Clear();
            ScenariosByCalloutName.Clear();
            foreach (CallCategory type in Enum.GetValues(typeof(CallCategory)))
            {
                ScenariosByCategory[type].Clear();
            }
        }
    }
}
