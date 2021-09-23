using AgencyDispatchFramework.Conversation;
using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.Scripting;
using AgencyDispatchFramework.Simulation;
using LSPD_First_Response.Mod.Callouts;
using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;

namespace AgencyDispatchFramework.Xml
{
    public class CalloutMetaFile : XmlFileBase
    {
        /// <summary>
        /// Gets whether this file has been loaded
        /// </summary>
        public bool IsLoaded { get; private set; }

        /// <summary>
        /// Contains a list Scenarios by name
        /// </summary>
        public List<EventScenarioMeta> Scenarios { get; set; }

        /// <summary>
        /// Gets the class type that controls the scenarios
        /// </summary>
        public Type CalloutType { get; internal set; }

        /// <summary>
        /// Creates a new instance of <see cref="CalloutMetaFile"/> using the specified
        /// file
        /// </summary>
        /// <param name="filePath">The full file path to the FlowSequence xml file</param>
        public CalloutMetaFile(string filePath) : base(filePath)
        {
            // === Document loaded successfully by base class if we are here === //
            // 
            Scenarios = new List<EventScenarioMeta>();
        }

        /// <summary>
        /// Parses the XML in the callout meta
        /// </summary>
        /// <param name="assembly">The assembly containing the callout class type for the contained scenarios</param>
        public void Parse(Assembly assembly, bool yieldFiber)
        {
            // Setup some vars
            var calloutDirName = Path.GetDirectoryName(FilePath);

            // Ensure proper format at the top
            var rootElement = Document.SelectSingleNode("CalloutMeta");
            if (rootElement == null)
            {
                throw new Exception($"CalloutMetaFile.Parse(): Unable to load CalloutMeta data in directory '{calloutDirName}' for Assembly: '{assembly.FullName}'");
            }

            // Get callout type name
            var typeName = rootElement.SelectSingleNode("Controller/Script")?.InnerText;
            if (String.IsNullOrWhiteSpace(typeName))
            {
                throw new Exception($"CalloutMetaFile.Parse(): Unable to extract Controller value in CalloutMeta.xml in directory '{calloutDirName}' for Assembly: '{assembly.FullName}'");
            }

            // Get callout type name
            CalloutType = assembly.GetType(typeName);
            if (CalloutType == null)
            {
                throw new Exception($"CalloutMetaFile.Parse(): Unable to find Callout class '{typeName}' in Assembly '{assembly.FullName}'");
            }

            // Get official callout name
            string calloutName = CalloutType.GetAttributeValue((CalloutInfoAttribute attr) => attr.Name);
            if (String.IsNullOrWhiteSpace(calloutName))
            {
                throw new Exception($"CalloutMetaFile.Parse(): Callout class '{typeName}' in Assembly '{assembly.FullName}' is missing the CalloutInfoAttribute!");
            }

            // Extract script type
            var scriptTypeName = rootElement.SelectSingleNode("Controller/Type")?.InnerText;
            if (!Enum.TryParse(scriptTypeName, out ScriptType scriptType))
            {
                throw new Exception($"CalloutMetaFile.Parse(): Unable to extract Controller ScriptType value in CalloutMeta.xml in directory '{calloutDirName}' for Assembly: '{assembly.FullName}'");
            }

            // Yield fiber?
            if (yieldFiber) GameFiber.Yield();

            // Process the XML scenarios
            foreach (XmlNode scenarioNode in rootElement.SelectSingleNode("Scenarios")?.ChildNodes)
            {
                // Skip all but elements
                if (scenarioNode.NodeType == XmlNodeType.Comment) continue;         

                // ============================================== //
                // Grab probabilities
                // ============================================== //
                XmlNode probNode = scenarioNode.SelectSingleNode("Probabilities");
                if (probNode == null || !probNode.HasChildNodes)
                {
                    Log.Error($"CalloutMetaFile.Parse(): Unable to load probabilities in CalloutMeta for '{calloutDirName}'");
                    continue;
                }

                // Parse World State Probabilities
                WorldStateMultipliers multipliers = null;
                try
                {
                    multipliers = XmlHelper.ExtractWorldStateMultipliers(probNode);
                }
                catch (Exception e)
                {
                    Log.Warning("CalloutMetaFile.Parse(): " + e.Message);
                    continue;
                }

                // Create scenario
                var scene = new EventScenarioMeta()
                {
                    ScenarioName = scenarioNode.Name,
                    ControllerName = calloutName,
                    ControllerType = CalloutType,
                    ScriptType = scriptType,
                    ProbabilityMultipliers = multipliers
                };

                // Parse locations
                if (!ExtractLocations(scene, scenarioNode, calloutDirName)) continue;

                // Yield fiber?
                if (yieldFiber) GameFiber.Yield();

                // Parse Dispatching info
                if (!ExtractDispatchData(scene, scenarioNode, calloutDirName)) continue;

                // Yield fiber?
                if (yieldFiber) GameFiber.Yield();

                // Parse Scanner data
                if (!ExtractScannerData(scene, scenarioNode, calloutDirName)) continue;

                // Yield fiber?
                if (yieldFiber) GameFiber.Yield();

                // Parse simulation info
                if (!ExtractSimulationData(scene, scenarioNode, calloutDirName)) continue;

                // Yield fiber?
                if (yieldFiber) GameFiber.Yield();

                // Parse simulation info
                if (!ExtractCircumstances(scene, scenarioNode, calloutDirName)) continue;

                // Yield fiber?
                if (yieldFiber) GameFiber.Yield();

                // Add scenario to the pools
                Scenarios.Add(scene);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scenario"></param>
        /// <param name="scenarioNode"></param>
        /// <param name="calloutDirName"></param>
        /// <returns></returns>
        private bool ExtractCircumstances(EventScenarioMeta scenario, XmlNode scenarioNode, string calloutDirName)
        {
            // Fetch each FlowOutcome item
            var scenarioNodes = scenarioNode.SelectSingleNode("Circumstances")?.SelectNodes("Circumstance");
            if (scenarioNodes == null || scenarioNodes.Count == 0)
            {
                Log.Error($"CalloutMetaFile.Parse(): Unable to load Circumstance nodes in CalloutMeta for '{calloutDirName}'");
                return false;
            }

            // Evaluate each flow outcome, and add it to the probability generator
            List<Circumstance> outcomes = new List<Circumstance>();
            foreach (XmlNode n in scenarioNodes)
            {
                // Ensure we have attributes
                if (n.Attributes == null)
                {
                    Log.Warning($"Circumstance item has no attributes in 'CalloutMeta.xml->{scenarioNode.Name}->Circumstances'");
                    continue;
                }

                // Try and extract type value
                if (n.Attributes["id"]?.Value == null)
                {
                    Log.Warning($"Unable to extract the 'id' attribute value in 'CalloutMeta.xml->{scenarioNode.Name}->Circumstances'");
                    continue;
                }

                // Try and extract probability value
                if (n.Attributes["probability"]?.Value == null || !int.TryParse(n.Attributes["probability"].Value, out int probability))
                {
                    Log.Warning($"Unable to extract probability value of a Circumstance in 'CalloutMeta.xml->{scenarioNode.Name}->Circumstances'");
                    continue;
                }

                // Add
                outcomes.Add(new Circumstance(n.Attributes["id"].Value, probability)
                {
                    ConditionStatement = n.Attributes["if"]?.Value ?? String.Empty
                });
            }

            if (outcomes.Count == 0)
            {
                Log.Warning($"Unable to extract any Circumstances in 'CalloutMeta.xml' for '{calloutDirName}' for scenario {scenarioNode.Name}");
                return false;
            }

            scenario.Circumstances = outcomes.ToArray();
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scenario"></param>
        /// <param name="scenarioNode"></param>
        /// <param name="calloutDirName"></param>
        /// <returns></returns>
        private bool ExtractSimulationData(EventScenarioMeta scenario, XmlNode scenarioNode, string calloutDirName)
        {
            // Extract simulation time
            int min = 0, max = 0;
            XmlNode childNode = scenarioNode.SelectSingleNode("Simulation")?.SelectSingleNode("CallTime");
            if (childNode == null)
            {
                Log.Warning(
                    $"CalloutMetaFile.Parse(): Unable to extract Simulation->CallTime element for '{calloutDirName}->Scenarios->{scenarioNode.Name}'"
                );
                return false;
            }
            else if (childNode.Attributes == null || !Int32.TryParse(childNode.Attributes["min"]?.Value, out min) || !Int32.TryParse(childNode.Attributes["max"]?.Value, out max))
            {
                Log.Warning(
                    $"CalloutMetaFile.Parse(): Unable to extract CallTime[min] or CallTime[max] attribute for '{calloutDirName}->Scenarios->{scenarioNode.Name}'"
                );
                return false;
            }

            // Set properties
            scenario.SimulationTime = new Range<int>(min, max);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scenario"></param>
        /// <param name="scenarioNode"></param>
        /// <param name="calloutDirName"></param>
        /// <returns></returns>
        private bool ExtractScannerData(EventScenarioMeta scenario, XmlNode scenarioNode, string calloutDirName)
        {
            // Grab the Scanner data
            var scannerNode = scenarioNode.SelectSingleNode("Scanner");
            if (scannerNode == null)
            {
                Log.Warning(
                    $"CalloutMetaFile.Parse(): Unable to extract scenario Scanner node for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                );
                return false;
            }

            // Local Vars
            var messages = new Dictionary<string, RadioMessageMeta>();

            // Add each scanner event callback
            foreach (XmlNode eventNode in scannerNode.SelectNodes("Event"))
            {
                string scanner = String.Empty;
                XmlNode childNode = eventNode.SelectSingleNode("AudioString");
                if (String.IsNullOrWhiteSpace(eventNode.InnerText))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract ScannerAudioString value for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}' -> Scanner"
                    );
                    return false;
                }
                else
                {
                    scanner = eventNode.InnerText;
                }

                // Extract event name
                var name = eventNode.GetAttribute("name");
                if (String.IsNullOrEmpty(name))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to parse Event['name'] value for '{calloutDirName}/CalloutMeta.xml -> '{scenarioNode.Name}' -> Scanner"
                    );
                    continue;
                }

                // Extract message priority
                if (!Enum.TryParse(eventNode.GetAttribute("priority"), out RadioMessage.MessagePriority priority))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to parse RadionMessagePriority value for '{calloutDirName}/CalloutMeta.xml -> '{scenarioNode.Name}' -> Scanner"
                    );
                    continue;
                }

                // Try to extract scanner prefix and suffix information
                childNode = eventNode.SelectSingleNode("PrefixCallSign");
                bool.TryParse(childNode?.InnerText, out bool prefix);

                childNode = eventNode.SelectSingleNode("UsePosition");
                bool.TryParse(childNode?.InnerText, out bool suffix);

                // Create new
                messages.AddOrUpdate(name, new RadioMessageMeta()
                {
                    AudioString = scanner,
                    PrefixCallSign = prefix,
                    UsePosition = suffix,
                    Priority = priority
                });
            }

            // Set properties
            scenario.RadioMessages = messages;
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scenario"></param>
        /// <param name="scenarioNode"></param>
        /// <param name="calloutDirName"></param>
        /// <returns></returns>
        private bool ExtractDispatchData(EventScenarioMeta scenario, XmlNode scenarioNode, string calloutDirName)
        {
            // Get the Dispatch Node
            XmlNode dispatchNode = scenarioNode.SelectSingleNode("Dispatch");
            if (dispatchNode == null)
            {
                Log.Warning(
                    $"CalloutMetaFile.Parse(): Unable to extract scenario Dispatch node for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                );
                return false;
            }

            var agencies = new Dictionary<ServiceSector, int>();
            var descriptions = new ProbabilityGenerator<EventDescription>();

            // Grab agency list
            XmlNode agenciesNode = dispatchNode.SelectSingleNode("Agencies");
            if (agenciesNode == null || !agenciesNode.HasChildNodes)
            {
                Log.Error($"CalloutMetaFile.Parse(): Unable to load agency data in CalloutMeta for '{calloutDirName}'");
                return false;
            }

            // Itterate through items
            foreach (XmlNode n in agenciesNode.SelectNodes("Agency"))
            {
                // Try and extract type value
                if (!Enum.TryParse(n.GetAttribute("target"), out ServiceSector agencyType))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to parse AgencyType value for '{calloutDirName}/CalloutMeta.xml -> '{scenarioNode.Name}'"
                    );
                    continue;
                }

                // Try and extract type value
                if (!Int32.TryParse(n.GetAttribute("unitCount"), out int unitCount))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to parse unitCount of AgencyType value for '{calloutDirName}/CalloutMeta.xml -> '{scenarioNode.Name}'"
                    );
                    continue;
                }

                agencies.Add(agencyType, unitCount);
            }

            // Grab the Callout Catagory
            XmlNode catagoryNode = dispatchNode.SelectSingleNode("Category");
            if (!Enum.TryParse(catagoryNode?.InnerText, out CallCategory crimeType))
            {
                Log.Warning(
                    $"CalloutMetaFile.Parse(): Unable to extract Callout Category value for '{calloutDirName}/CalloutMeta.xml -> '{scenarioNode.Name}'"
                );
                return false;
            }

            // Try and extract priority value
            XmlNode childNode = dispatchNode.SelectSingleNode("Priority");
            if (!Enum.TryParse(childNode.InnerText, out EventPriority priority))
            {
                Log.Warning(
                    $"CalloutMetaFile.Parse(): Unable to extract scenario priority value for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                );
                return false;
            }

            // Try and extract event type
            childNode = dispatchNode.SelectSingleNode("EventType");
            if (!Enum.TryParse(childNode.InnerText, out EventType eventType))
            {
                Log.Warning(
                    $"CalloutMetaFile.Parse(): Unable to extract scenario eventType value for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                );
                return false;
            }

            // Try and extract event source
            childNode = dispatchNode.SelectSingleNode("EventSource");
            if (!Enum.TryParse(childNode.InnerText, out EventSource eventSource))
            {
                Log.Warning(
                    $"CalloutMetaFile.Parse(): Unable to extract scenario eventSource value for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                );
                return false;
            }

            // Try and extract response code value
            childNode = dispatchNode.SelectSingleNode("Response");
            if (!Enum.TryParse(childNode.InnerText, out ResponseCode code))
            {
                Log.Warning(
                    $"CalloutMetaFile.Parse(): Unable to extract scenario response code value for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                );
                return false;
            }

            // Try and extract descriptions
            XmlNode cadNode = dispatchNode.SelectSingleNode("MDT");
            if (cadNode == null || !cadNode.HasChildNodes)
            {
                Log.Warning(
                    $"CalloutMetaFile.Parse(): Unable to extract scenario MDT values for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                );
                return false;
            }

            // Try and extract descriptions
            childNode = cadNode.SelectSingleNode("Descriptions");
            if (childNode == null || !childNode.HasChildNodes)
            {
                Log.Warning(
                    $"CalloutMetaFile.Parse(): Unable to extract scenario descriptions values for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                );
                return false;
            }
            else
            {
                // Clear old descriptions
                foreach (XmlNode descNode in childNode.SelectNodes("Description"))
                {
                    // Ensure we have attributes
                    if (descNode.Attributes == null || !int.TryParse(descNode.Attributes["probability"]?.Value, out int prob))
                    {
                        Log.Warning(
                            $"CalloutMetaFile.Parse(): Unable to extract probability value for Description in '{calloutDirName}->Scenarios->{scenarioNode.Name}'->Dispatch->Descriptions"
                        );
                        continue;
                    }

                    // Extract desc text value
                    var descTextNode = descNode.SelectSingleNode("Text");
                    if (descTextNode == null)
                    {
                        Log.Warning(
                            $"CalloutMetaFile.Parse(): Unable to extract Text value for Description in '{calloutDirName}->Scenarios->{scenarioNode.Name}'->Dispatch->Descriptions"
                        );
                        continue;
                    }

                    descriptions.Add(new EventDescription(prob, descTextNode.InnerText.Trim()));
                }

                // If we have no descriptions, we failed
                if (descriptions.ItemCount == 0)
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Scenario has no Descriptions '{calloutDirName}->Scenarios->{scenarioNode.Name}'->Dispatch"
                    );
                    return false;
                }
            }

            // Grab the CAD Texture
            childNode = cadNode.SelectSingleNode("Texture");
            if (String.IsNullOrWhiteSpace(childNode.InnerText))
            {
                Log.Warning(
                    $"CalloutMetaFile.Parse(): Unable to extract CADTexture value for '{calloutDirName}->Scenarios->{scenarioNode.Name}'"
                );
                return false;
            }
            else if (childNode.Attributes == null || String.IsNullOrWhiteSpace(childNode.Attributes["dictionary"]?.Value))
            {
                Log.Warning(
                    $"CalloutMetaFile.Parse(): Unable to extract CADTexture[dictionary] attribute for '{calloutDirName}->Scenarios->{scenarioNode.Name}'"
                );
                return false;
            }

            string textName = childNode.InnerText;
            string textDict = childNode.Attributes["dictionary"].Value;

            // Grab the Incident
            childNode = cadNode.SelectSingleNode("IncidentType");
            if (String.IsNullOrWhiteSpace(childNode.InnerText))
            {
                Log.Warning(
                    $"CalloutMetaFile.Parse(): Unable to extract IncidentType value for '{calloutDirName}->Scenarios->{scenarioNode.Name}'"
                );
                return false;
            }
            else if (childNode.Attributes == null || childNode.Attributes["abbreviation"]?.Value == null)
            {
                Log.Warning(
                    $"CalloutMetaFile.Parse(): Unable to extract Incident abbreviation attribute for '{calloutDirName}->Scenarios->{scenarioNode.Name}'"
                );
                return false;
            }

            // Set properties
            scenario.Type = eventType;
            scenario.Source = eventSource;
            scenario.Category = crimeType;
            scenario.Priority = priority;
            scenario.ResponseCode = code;
            scenario.InitialDispatch = agencies;
            scenario.Descriptions = descriptions;
            scenario.CADEventText = childNode.InnerText;
            scenario.CADEventAbbreviation = childNode.Attributes["abbreviation"].Value;
            scenario.CADSpriteName = textName;
            scenario.CADSpriteTextureDict = textDict;

            // Report success
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scenario"></param>
        /// <param name="scenarioNode"></param>
        /// <param name="calloutDirName"></param>
        /// <returns></returns>
        private bool ExtractLocations(EventScenarioMeta scenario, XmlNode scenarioNode, string calloutDirName)
        {
            // Grab the Location info
            var locationsNode = scenarioNode.SelectSingleNode("Locations");
            if (locationsNode == null)
            {
                Log.Warning(
                    $"CalloutMetaFile.Parse(): Unable to extract Locations node for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                );
                return false;
            }

            // Initialize
            scenario.PossibleLocationMetas = new ProbabilityGenerator<PossibleLocationMeta>();

            // Add each scanner event callback
            foreach (XmlNode locationNode in locationsNode.SelectNodes("Location"))
            {
                // Try and extract probability value
                if (!Int32.TryParse(locationNode.GetAttribute("probability"), out int probability))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to parse Location['probability'] value for '{calloutDirName}/CalloutMeta.xml -> '{scenarioNode.Name}'"
                    );
                    continue;
                }

                // Parse location type
                string type = locationNode.SelectSingleNode("Type")?.InnerText ?? "";
                if (!Enum.TryParse(type, out LocationTypeCode locationType))
                {
                    Log.Warning(
                        $"CalloutMetaFile.Parse(): Unable to extract Location Type value for '{calloutDirName}/CalloutMeta.xml -> {scenarioNode.Name}'"
                    );
                    continue;
                }

                // Extract location flags
                var filter = new FlagFilterGroup();
                XmlNode childNode = locationNode.SelectSingleNode("RequiredFlags");
                if (childNode != null && childNode.HasChildNodes)
                {
                    // Fetch filter type
                    if (!Enum.TryParse(childNode.GetAttribute("mode"), out SelectionOperator filterType))
                    {
                        filterType = SelectionOperator.All;
                    }
                    filter.Mode = filterType;

                    // Get child requirements
                    var nodes = childNode.SelectNodes("Requirement");
                    foreach (XmlNode n in nodes)
                    {
                        // Fetch filter type
                        if (!Enum.TryParse(n.GetAttribute("mode"), out SelectionOperator fType))
                        {
                            fType = SelectionOperator.All;
                        }

                        // Fetch inverse mode?
                        if (!bool.TryParse(n.GetAttribute("inverse"), out bool inverse))
                        {
                            inverse = false;
                        }

                        // Parse flags
                        int[] flags = GetFlagCodesFromLocationType(locationType, n, out Type t);
                        filter.Requirements.Add(new Requirement(t)
                        {
                            Flags = flags,
                            Inverse = inverse,
                            Mode = fType
                        });
                    }
                }

                // Add location
                scenario.PossibleLocationMetas.Add(new PossibleLocationMeta
                {
                    Probability = probability,
                    LocationTypeCode = locationType,
                    LocationFilters = filter,
                });
            }

            // Report back
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="innerText"></param>
        /// <returns></returns>
        private int[] GetFlagCodesFromLocationType(LocationTypeCode locationType, XmlNode node, out Type type)
        {
            type = default(Type);
            if (String.IsNullOrEmpty(node.InnerText))
            {
                return new int[0];
            }

            string[] vals = node.InnerText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            int[] items;

            switch (locationType)
            {
                case LocationTypeCode.Residence:
                    type = typeof(ResidenceFlags);
                    items = ParseFlags<ResidenceFlags>(vals, node);
                    break;
                case LocationTypeCode.RoadShoulder:
                    type = typeof(RoadFlags);
                    items = ParseFlags<RoadFlags>(vals, node);
                    break;
                case LocationTypeCode.Intersection:
                    type = typeof(IntersectionFlags);
                    items = ParseFlags<IntersectionFlags>(vals, node);
                    break;
                default:
                    throw new Exception($"Cannot parse flags from {type}");
            }

            return items;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="vals"></param>
        /// <param name="node"></param>
        /// <returns></returns>
        private int[] ParseFlags<T>(string[] vals, XmlNode node) where T : struct
        {
            var items = new List<int>(vals.Length);
            foreach (string input in vals)
            {
                if (Enum.TryParse(input.Trim(), out T flag))
                {
                    int value = Convert.ToInt32(flag);
                    items.Add(value);
                }
                else
                {
                    Log.Debug($"Unable to parse enum value of '{input}' for type '{typeof(T).Name}' in CalloutMeta.xml ->  {node.GetFullPath()}");
                }
            }

            return items.ToArray();
        }
    }
}
