using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.Simulation;
using Rage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace AgencyDispatchFramework.Xml
{
    /// <summary>
    /// Loads and parses the XML data from a location.xml
    /// </summary>
    public class WorldZoneFile : XmlFileBase
    {
        /// <summary>
        /// Gets the Zone name
        /// </summary>
        public WorldZone Zone { get; protected set; }

        /// <summary>
        /// Creates a new instance of <see cref="WorldZoneFile"/>
        /// </summary>
        /// <param name="filePath"></param>
        public WorldZoneFile(string filePath) : base(filePath)
        {

        }

        /// <summary>
        /// Parses the zone XML data in the file, and throws an exception on failure
        /// </summary>
        /// <exception cref="FormatException">thrown if the XML is missing nodes or attributes</exception>
        public void Parse(WorldZone zone = null)
        {
            // Get zone name
            var zoneName = Path.GetFileNameWithoutExtension(FilePath);

            // Grab zone node
            XmlNode node = Document.SelectSingleNode(zoneName);
            if (node == null || !node.HasChildNodes)
            {
                throw new FormatException($"WorldZoneFile.Parse(): Missing data for zone '{zoneName}'");
            }

            Zone = zone;
            if (zone == null)
            {
                // Load zone info
                XmlNode catagoryNode = node.SelectSingleNode("Name");
                var displayName = catagoryNode?.InnerText ?? throw new FormatException("Name");
                var scriptName = node.Name;

                // Extract size
                catagoryNode = node.SelectSingleNode("Size");
                if (String.IsNullOrWhiteSpace(catagoryNode?.InnerText) || !Enum.TryParse(catagoryNode.InnerText, out ZoneSize size))
                {
                    throw new FormatException("Size");
                }

                // Extract type
                catagoryNode = node.SelectSingleNode("Type");
                if (String.IsNullOrWhiteSpace(catagoryNode?.InnerText))
                {
                    throw new FormatException("Type");
                }
                var flags = ParseZoneFlags(catagoryNode);

                // Extract social class
                catagoryNode = node.SelectSingleNode("Class");
                if (String.IsNullOrWhiteSpace(catagoryNode?.InnerText) || !Enum.TryParse(catagoryNode.InnerText, out SocialClass sclass))
                {
                    throw new FormatException("Class");
                }

                // Extract population
                catagoryNode = node.SelectSingleNode("Population");
                if (String.IsNullOrWhiteSpace(catagoryNode?.InnerText) || !Enum.TryParse(catagoryNode.InnerText, out Population pop))
                {
                    throw new FormatException("Population");
                }

                // Extract county
                catagoryNode = node.SelectSingleNode("County");
                if (String.IsNullOrWhiteSpace(catagoryNode?.InnerText) || !Enum.TryParse(catagoryNode.InnerText, out County c))
                {
                    throw new FormatException("County");
                }

                // Create new instance
                Zone = new WorldZone(scriptName, displayName, c, pop, size, sclass, flags);
            }

            // Extract crime level
            XmlNode crimeNode = node.SelectSingleNode("Crime");
            if (crimeNode == null || !crimeNode.HasChildNodes)
            {
                throw new FormatException("Crime");
            }

            // Load crime probabilites
            Zone.CallCategoryGenerator = new WorldStateProbabilityGenerator<CallCategory>();

            // Get average calls by time of day
            var subNode = crimeNode.SelectSingleNode("AverageCalls");
            Zone.CrimeInfo = CreateCrimeProjection(subNode, Zone);
            int totalProbability = Zone.CrimeInfo.CrimeProbability.Values.Sum();

            // Does this zone get any calls?
            if (totalProbability > 0 && Zone.CrimeInfo.AverageCalls > 0)
            {
                crimeNode = crimeNode.SelectSingleNode("Probabilities");
                if (crimeNode == null || !crimeNode.HasChildNodes)
                {
                    throw new FormatException("Probabilities");
                }

                // Extract crime probabilites
                foreach (CallCategory calloutType in Enum.GetValues(typeof(CallCategory)))
                {
                    var nodeName = Enum.GetName(typeof(CallCategory), calloutType);
                    XmlNode n = crimeNode.SelectSingleNode(nodeName);

                    // Try and parse the crime type from the node name
                    if (n == null)
                    {
                        continue;
                    }

                    // See if this calloutType is possible in this zone
                    if (bool.TryParse(n.Attributes["possible"]?.Value, out bool possible))
                    {
                        if (!possible) continue;
                    }

                    // Try and parse the probability levels, cloning from the base settings
                    var multipliers = (WorldStateMultipliers)RegionCrimeGenerator.BaseCrimeMultipliers[calloutType].Clone();

                    // Override settings
                    XmlHelper.UpdateWorldStateMultipliers(n, multipliers);
                    Zone.CallCategoryGenerator.Add(calloutType, multipliers);
                }
            }
        }

        /// <summary>
        /// Reads and parses an <see cref="XmlNode"/> containing <see cref="ZoneFlags"/>
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static List<ZoneFlags> ParseZoneFlags(XmlNode node)
        {
            // Default return value
            string val = node?.InnerText;

            // Check for empty strings
            if (String.IsNullOrWhiteSpace(val))
            {
                return new List<ZoneFlags>();
            }

            // Parse comma seperated values
            return val.CSVToEnumList<ZoneFlags>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node">The AverageCalls node</param>
        /// <returns></returns>
        private CrimeProjection CreateCrimeProjection(XmlNode node, WorldZone zone)
        {
            // If attributes is null, we know... we know...
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            // Get average calls value
            if (!Int32.TryParse(node.GetAttribute("value"), out int averageCalls))
            {
                throw new FormatException($"[{node.GetFullPath()}]: Missing the 'value' attribute");
            }

            // Itterate through each time of day
            var probabilities = new Dictionary<TimePeriod, int>(6);
            foreach (TimePeriod period in Enum.GetValues(typeof(TimePeriod)))
            {
                var name = Enum.GetName(typeof(TimePeriod), period);
                var todNode = node.SelectSingleNode(name);
                if (todNode == null || !Int32.TryParse(todNode.GetAttribute("probability"), out int val))
                {
                    throw new Exception($"[{node.GetFullPath()}]: Unable to extract 'probability' attribute from XmlNode '{name}'");
                }

                probabilities.Add(period, val);
            }

            return new CrimeProjection(zone, averageCalls, probabilities);
        }
    }
}
