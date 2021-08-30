using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Simulation;
using System;
using System.Collections.Generic;
using System.IO;

namespace AgencyDispatchFramework.Xml
{
    internal class BaseProbabilitiesXmlFile : XmlFileBase
    {
        public BaseProbabilitiesXmlFile(string filePath) : base(filePath)
        {

        }

        public void Parse()
        {
            // Ensure proper format at the top
            var rootElement = Document.SelectSingleNode("Probabilities");
            if (rootElement == null)
            {
                throw new Exception($"BaseProbabilitiesXmlFile.Parse(): Unable to load base probability data!");
            }

            // Grab crime probabilities @todo
            RegionCrimeGenerator.BaseCrimeMultipliers = new Dictionary<CallCategory, WorldStateMultipliers>();

            // Grab base crime probabilities
            var node = rootElement.SelectSingleNode("Crime/Probabilities");
            foreach (CallCategory category in Enum.GetValues(typeof(CallCategory)))
            {
                // Grab subnode
                var subNode = node.SelectSingleNode(category.ToString());
                RegionCrimeGenerator.BaseCrimeMultipliers.Add(category, XmlHelper.ExtractWorldStateMultipliers(subNode));
            }
        }

        public static void Load()
        {
            // Load base probabilities
            string filePath = Path.Combine(Main.FrameworkFolderPath, "BaseProbabilities.xml");
            using (var file = new BaseProbabilitiesXmlFile(filePath))
            {
                // Parse the file
                file.Parse();
            }
        }
    }
}
