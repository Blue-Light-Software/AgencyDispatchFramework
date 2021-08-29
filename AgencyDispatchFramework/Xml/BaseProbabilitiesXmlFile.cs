using AgencyDispatchFramework.Simulation;
using System;
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

            // Grab base crime probabilities
            var node = rootElement.SelectSingleNode("Crime/Probabilities");
            RegionCrimeGenerator.BaseCrimeMultipliers = XmlHelper.ExtractWorldStateMultipliers(node);
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
