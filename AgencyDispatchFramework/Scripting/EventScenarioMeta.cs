using AgencyDispatchFramework.Conversation;
using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Simulation;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Scripting
{
    /// <summary>
    /// Contains Scenario information for an <see cref="IEventScenario"/>
    /// </summary>
    public class EventScenarioMeta
    {
        /// <summary>
        /// Gets the <see cref="WorldStateMultipliers"/> for this scenario
        /// </summary>
        public WorldStateMultipliers ProbabilityMultipliers { get; set; }

        /// <summary>
        /// Gets the name of the Scenario
        /// </summary>
        public string ScenarioName { get; set; }

        /// <summary>
        /// Gets the name of the <see cref="IEventController"/> that processes the <see cref="ActiveEvent"/> 
        /// this meta describes.
        /// </summary>
        public string ControllerName { get; set; }

        /// <summary>
        /// Gets the <see cref="EventType"/>
        /// </summary>
        public EventType Type { get; internal set; }

        /// <summary>
        /// Gets the <see cref="EventSource"/>
        /// </summary>
        public EventSource Source { get; internal set; }

        /// <summary>
        /// Indicates whether this <see cref="Event"/> is a Callout or an AmbientEvent
        /// </summary>
        public ScriptType ScriptType { get; set; }

        /// <summary>
        /// Gets the priority level of the call
        /// </summary>
        public EventPriority Priority { get; set; }

        /// <summary>
        /// Indicates whether the responding unit should respond Code 3
        /// </summary>
        public ResponseCode ResponseCode { get; set; }

        /// <summary>
        /// Contains an array of <see cref="Agency"/>s that should be dispatched to
        /// the call, and the unit count of each agency
        /// </summary>
        public Dictionary<ServiceSector, int> InitialDispatch { get; internal set; }

        /// <summary>
        /// Contains a time range of how long this scenario could take when the AI is on scene
        /// in minutes (game time)
        /// </summary>
        public Range<int> SimulationTime { get; internal set; }

        /// <summary>
        /// Gets the <see cref="Dispatching.CallCategory"/> of this scenario
        /// </summary>
        public CallCategory Category { get; internal set; }

        /// <summary>
        /// Gets the incident text
        /// </summary>
        public string CADEventText { get; set; }

        /// <summary>
        /// Gets the incident abbreviation text
        /// </summary>
        public string CADEventAbbreviation { get; set; }

        /// <summary>
        /// Gets the texture sprite name to display in the CAD
        /// </summary>
        public string CADSpriteName { get; internal set; }

        /// <summary>
        /// Gets the texture sprite dictionary name to display in the CAD
        /// </summary>
        public string CADSpriteTextureDict { get; internal set; }

        /// <summary>
        /// Gets an array of call descriptions
        /// </summary>
        public ProbabilityGenerator<EventDescription> Descriptions { get; set; }

        /// <summary>
        /// Contains a list of <see cref="Circumstance"/> from the CalloutMeta.xml
        /// </summary>
        public Circumstance[] Circumstances { get; internal set; }

        /// <summary>
        /// Contains a hash table of radio messages to be played on the <see cref="Scanner"/>
        /// </summary>
        public Dictionary<string, RadioMessageMeta> RadioMessages { get; internal set; }

        /// <summary>
        /// Contains meta data for choosing locations for this <see cref="EventScenarioMeta"/>
        /// </summary>
        public ProbabilityGenerator<PossibleLocationMeta> PossibleLocationMetas { get; set; }

        /// <summary>
        /// Selects a random <see cref="Circumstance"/>, using an <see cref="ExpressionParser"/>
        /// to evaluate acceptable <see cref="Circumstance"/>s based on the conditions set for
        /// each <see cref="Circumstance"/>
        /// </summary>
        /// <param name="parser"></param>
        public bool GetRandomCircumstance(ExpressionParser parser, out Circumstance selected)
        {
            var gen = new ProbabilityGenerator<Circumstance>();
            foreach (var scenario in Circumstances)
            {
                if (scenario.Evaluate(parser))
                {
                    gen.Add(scenario);
                }
            }

            return gen.TrySpawn(out selected);
        }
    }
}
