using AgencyDispatchFramework.Extensions;
using System;
using System.Collections.Generic;
using System.Xml;

namespace AgencyDispatchFramework.Xml
{
    internal class DistrictsFile : XmlFileBase
    {
        /// <summary>
        /// 
        /// </summary>
        public Dictionary<string, Dictionary<string, Tuple<int, HashSet<string>>>> Districts { get; private set; }

        /// <summary>
        /// Creates a new instance of <see cref="DistrictsFile"/> used to parse the Districts.xml file
        /// </summary>
        /// <param name="filePath"></param>
        public DistrictsFile(string filePath) : base(filePath)
        {
            Districts = new Dictionary<string, Dictionary<string, Tuple<int, HashSet<string>>>>();
        }

        /// <summary>
        /// Parses the XML in the Districts.xml file
        /// </summary>
        public void Parse()
        {
            // Grab root node
            XmlNode rootNode = Document.SelectSingleNode("Districts");
            if (rootNode == null || !rootNode.HasChildNodes)
            {
                throw new FormatException("DistrictsFile.Parse(): Missing district data ");
            }

            // Ensure we have agencies and districts!
            var agencies = rootNode.SelectNodes("Agency");
            if (agencies.Count == 0)
            {
                throw new FormatException("DistrictsFile.Parse(): Missing district data ");
            }

            // Extract agency district data
            foreach (XmlNode agencyNode in agencies)
            {
                // Log and skip if we are missing a name
                if (!agencyNode.TryGetAttribute("name", out string name))
                {
                    Log.Error("DistrictsFile.Parse(): Missing Agency name attribbute. Skipping Agency...");
                    continue;
                }

                var districts = ExtractDistricts(agencyNode, name);
                Districts.AddOrUpdate(name, districts);
            }
        }

        /// <summary>
        /// Extracts the District nodes from an Agency node
        /// </summary>
        /// <param name="agencyNode"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private Dictionary<string, Tuple<int, HashSet<string>>> ExtractDistricts(XmlNode agencyNode, string name)
        {
            var hashTable = new Dictionary<string, Tuple<int, HashSet<string>>>();

            // Ensure we have district nodes
            var districts = agencyNode.SelectNodes("District");
            if (districts.Count == 0)
            {
                Log.Warning($"DistrictsFile.ExtractDistricts(): Agency with name '{name}' does not have any Districts defined");
                return hashTable;
            }

            // Extract district data
            int i = 1;
            foreach (XmlNode districtNode in districts)
            {
                // Log and skip if we are missing a name
                if (!districtNode.TryGetAttribute("name", out string dName))
                {
                    Log.Error($"DistrictsFile.ExtractDistricts(): Missing District name attribute for Agency {name}");
                    continue;
                }

                // Ensure we have zone nodes
                var zoneNodes = districtNode.SelectNodes("Zone");
                if (zoneNodes.Count == 0)
                {
                    Log.Warning($"DistrictsFile.ExtractDistricts(): Agency '{name}' has a District with name '{dName}' that does not define any zones");
                    continue;
                }

                // Extract zones
                var zoneList = new HashSet<string>(zoneNodes.Count);
                foreach (XmlNode zoneNode in zoneNodes)
                {
                    string value = zoneNode.InnerText.Trim().ToUpperInvariant();
                    if (!String.IsNullOrEmpty(value))
                    {
                        zoneList.Add(value);
                    }
                }

                // Add district
                hashTable.AddOrUpdate(dName, new Tuple<int, HashSet<string>>(i++, zoneList));
            }

            return hashTable;
        }
    }
}
