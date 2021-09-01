﻿using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Simulation;
using Rage;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Dynamic;
using System.Xml;

namespace AgencyDispatchFramework.Xml
{
    internal class AgenciesFile : XmlFileBase
    {
        /// <summary>
        /// A dictionary of agencies
        /// </summary>
        public Dictionary<string, Agency> Agencies { get; set; }

        /// <summary>
        /// Contains a string to Enum value look up for <see cref="PedComponent"/>s
        /// </summary>
        protected static Dictionary<string, PedComponent> ComponentLookUp { get; set; }

        /// <summary>
        /// Contains a string to Enum value look up for <see cref="PedPropIndex"/>s
        /// </summary>
        protected static Dictionary<string, PedPropIndex> PropLookUp { get; set; }

        /// <summary>
        /// Static constructor
        /// </summary>
        static AgenciesFile()
        {
            ComponentLookUp = new Dictionary<string, PedComponent>()
            {
                { "face", PedComponent.Head },
                { "beard", PedComponent.Mask },
                { "hair", PedComponent.Hair },
                { "shirt", PedComponent.Torso },
                { "pants", PedComponent.Legs },
                { "hands", PedComponent.Hands },
                { "shoes", PedComponent.Shoes },
                { "eyes", PedComponent.Eyes },
                { "tasks", PedComponent.Tasks },
                { "decals", PedComponent.Decals },
                { "shirtoverlay", PedComponent.ShirtOverlay },
            };

            PropLookUp = new Dictionary<string, PedPropIndex>()
            {
                { "hats", PedPropIndex.Hat },
                { "glasses", PedPropIndex.Glasses },
                { "ears", PedPropIndex.Ear },
                { "watches", PedPropIndex.Watch },
            };
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="filePath"></param>
        public AgenciesFile(string filePath) : base(filePath)
        {
            Agencies = new Dictionary<string, Agency>();
        }

        /// <summary>
        /// Parses the XML in the agency.xml file
        /// </summary>
        public void Parse()
        {
            var mapping = new Dictionary<string, string>();
            var agencyZones = new Dictionary<string, HashSet<string>>();

            // *******************************************
            // Load backup.xml for agency backup mapping
            // *******************************************
            string rootPath = Path.Combine(Main.GTARootPath, "lspdfr", "data");
            string path = Path.Combine(rootPath, "backup.xml");

            // Load XML document
            XmlDocument document = new XmlDocument();
            using (var file = new FileStream(path, FileMode.Open))
            {
                document.Load(file);
            }

            // Allow other plugins time to do whatever
            GameFiber.Yield();

            // cycle through each child node 
            foreach (XmlNode node in document.DocumentElement.SelectSingleNode("LocalPatrol").ChildNodes)
            {
                // Skip errors
                if (!node.HasChildNodes) continue;

                // extract needed data
                string nodeName = node.LocalName;
                string agency = node.FirstChild.InnerText.ToLowerInvariant();

                // add
                mapping.Add(nodeName, agency);
            }

            // *******************************************
            // Load regions.xml for agency jurisdiction zones
            // *******************************************
            path = Path.Combine(rootPath, "regions.xml");
            document = new XmlDocument();
            using (var file = new FileStream(path, FileMode.Open))
            {
                document.Load(file);
            }

            // Allow other plugins time to do whatever
            GameFiber.Yield();

            // cycle though regions
            foreach (XmlNode region in document.DocumentElement.ChildNodes)
            {
                string name = region.SelectSingleNode("Name").InnerText;
                string agency = mapping[name];
                var zones = new HashSet<string>();

                // Make sure we have zones!
                XmlNode node = region.SelectSingleNode("Zones");
                if (!node.HasChildNodes)
                {
                    continue;
                }

                // Load all zones of jurisdiction
                foreach (XmlNode zNode in node.ChildNodes)
                {
                    zones.Add(zNode.InnerText.ToUpperInvariant());
                }

                // Add or Update
                if (agencyZones.ContainsKey(agency))
                {
                    agencyZones[agency].UnionWith(zones);
                }
                else
                {
                    agencyZones.Add(agency, zones);
                }

                WorldZone.AddRegion(name, zones.ToList());
            }

            // Add Highway to highway patrol
            if (agencyZones.ContainsKey("sahp"))
            {
                agencyZones["sahp"].Add("HIGHWAY");
            }
            else
            {
                agencyZones.Add("sahp", new HashSet<string>() { "HIGHWAY" });
            }


            // Load each custom agency XML to get police car names!
            GameFiber.Yield();

            // *******************************************
            // Load Agencies.xml for agency types
            // *******************************************
            path = Path.Combine(Main.FrameworkFolderPath, "Agencies.xml");
            document = new XmlDocument();
            using (var file = new FileStream(path, FileMode.Open))
            {
                document.Load(file);
            }

            // cycle though agencies
            foreach (XmlNode agencyNode in document.DocumentElement.SelectNodes("Agency"))
            {
                // extract data
                string name = agencyNode.SelectSingleNode("Name")?.InnerText;
                string sname = agencyNode.SelectSingleNode("ScriptName")?.InnerText;
                string atype = agencyNode.SelectSingleNode("AgencyType")?.InnerText;
                string sLevel = agencyNode.SelectSingleNode("StaffLevel")?.InnerText;
                string csStyle = agencyNode.SelectSingleNode("CallSignStyle")?.InnerText;
                string county = agencyNode.SelectSingleNode("County")?.InnerText;
                string customBackAgency = agencyNode.SelectSingleNode("CustomBackingAgency")?.InnerText;
                XmlNode unitsNode = agencyNode.SelectSingleNode("Units");

                // Skip if we have no units
                if (unitsNode == null) continue;

                // Check name
                if (String.IsNullOrWhiteSpace(sname))
                {
                    Log.Warning($"Agency.Initialize(): Unable to extract ScriptName value for agency in Agencies.xml");
                    continue;
                }

                // Try and parse agency type
                if (String.IsNullOrWhiteSpace(atype) || !Enum.TryParse(atype, out AgencyType type))
                {
                    Log.Warning($"Agency.Initialize(): Unable to extract AgencyType value for '{sname}' in Agencies.xml");
                    continue;
                }

                // Try and parse funding level
                if (String.IsNullOrWhiteSpace(sLevel) || !Enum.TryParse(sLevel, out StaffLevel staffing))
                {
                    Log.Warning($"Agency.Initialize(): Unable to extract StaffLevel value for '{sname}' in Agencies.xml");
                    continue;
                }

                // Try and parse call sign style
                if (String.IsNullOrWhiteSpace(csStyle) || !Enum.TryParse(csStyle, out CallSignStyle style))
                {
                    Log.Warning($"Agency.Initialize(): Unable to extract CallSignStyle value for '{sname}' in Agencies.xml");
                    style = CallSignStyle.LAPD;
                }

                // Load vehicle sets
                string[] catagories = { "Officer", "Supervisor" };
                var unitMapping = new Dictionary<UnitType, SpecializedUnit>();
                Agency agency = Agency.CreateAgency(type, sname, name, staffing, style);
                foreach (XmlNode unitNode in unitsNode.SelectNodes("Unit"))
                {
                    // Get type attribute
                    var attrValue = unitNode.GetAttribute("type");
                    if (!Enum.TryParse(attrValue, out UnitType unitType))
                    {
                        Log.Warning($"Agency.Initialize(): Unable to extract Unit type value '{attrValue ?? "null"}' for '{sname}' in Agencies.xml");
                        continue;
                    }

                    // Create unit
                    var unit = new SpecializedUnit(unitType, agency);

                    // Get derives attribute
                    if (Enum.TryParse(unitNode.GetAttribute("derives"), out UnitType unitDerives))
                    {
                        // @todo
                    }

                    // Load each catagory of vehicle sets
                    for (int i = 0; i < catagories.Length; i++)
                    {
                        // Officer or Supervisor node
                        var n = unitNode.SelectSingleNode(catagories[i]);
                        var sets = ParseVehicleSets(n, unit, agency);

                        if (i == 0)
                        {
                            unit.OfficerSets.AddRange(sets);
                        }
                        else
                        {
                            unit.SupervisorSets.AddRange(sets);
                        }
                    }

                    // Add unit to agency
                    agency.AddUnit(unit);
                }

                // Try and parse funding level
                if (!String.IsNullOrWhiteSpace(county) && Enum.TryParse(county, out County c))
                {
                    agency.BackingCounty = c;
                }

                // Set zones for agency
                if (agencyZones.ContainsKey(agency.ScriptName))
                {
                    agency.ZoneNames = agencyZones[agency.ScriptName].ToArray();
                }
                else
                {
                    Log.Warning($"Agency.Initialize(): Agency '{agency.ScriptName}' does not have any zones in its jurisdiction!");
                }

                // Add agency
                Agencies.Add(sname, agency);
            }

            // Clean up!
            GameFiber.Yield();
            document = null;
        }

        /// <summary>
        /// Parses a VehicleSets node
        /// </summary>
        /// <param name="n">The "Unit" node that contains the "VehicleSet" nodes</param>
        /// <param name="unit"></param>
        /// <returns>A list of parsed <see cref="VehicleSet"/> objects</returns>
        private List<VehicleSet> ParseVehicleSets(XmlNode n, SpecializedUnit unit, Agency agency)
        {
            var sets = new List<VehicleSet>();
            var nodes = n?.SelectNodes("VehicleSet");
            if (nodes != null && nodes.Count > 0)
            {
                foreach (XmlNode vn in nodes)
                {
                    // Ensure we have attributes
                    if (vn.Attributes == null)
                    {
                        Log.Warning($"Agency.ParseVehicleSets(): Vehicle item for '{agency.ScriptName}' has no attributes in Agencies.xml");
                        continue;
                    }

                    // Check for a chance attribute
                    if (vn.Attributes["chance"]?.Value == null || !int.TryParse(vn.Attributes["chance"].Value, out int probability))
                    {
                        probability = 10;
                    }

                    // Create vehicle info
                    var set = new VehicleSet(probability);

                    // Try and extract vehicles
                    if (!TryExtractVehicles(vn, set, agency))
                    {
                        // Logging happens within the method
                        continue;
                    }

                    // Try and extract Peds
                    if (!TryExtractPeds(vn, set, agency))
                    {
                        // Logging happens within the method
                        continue;
                    }

                    // Try and extract NonLethals
                    if (!TryExtractNonLethals(vn, set, agency))
                    {
                        // Logging happens within the method
                        continue;
                    }

                    // Try and extract Weapons
                    if (!TryExtractWeapons(vn, set, agency))
                    {
                        // Logging happens within the method
                        continue;
                    }

                    // Add vehicle set
                    sets.Add(set);
                }
            }

            return sets;
        }

        /// <summary>
        /// Extracts the HandGuns and LongGuns data from a VehicleSet XML node
        /// </summary>
        /// <param name="vn">The VehicleSet xml node</param>
        /// <param name="set">The <see cref="VehicleSet"/> to append the data to</param>
        /// <param name="agency">The <see cref="Agency"/> we are currently extracting for</param>
        /// <returns>true on success, false if the sub XML nodes doesnt exist</returns>
        private bool TryExtractWeapons(XmlNode vn, VehicleSet set, Agency agency)
        {
            string[] options = new[] { "HandGun", "LongGun" };
            foreach (var name in options)
            {
                // Grab nodes
                var nodes = vn.SelectSingleNode($"{name}s")?.SelectNodes(name);
                if (nodes == null || nodes.Count == 0)
                    continue;

                // Loop through each Ped node
                foreach (XmlNode node in nodes)
                {
                    // Extract the chance attribute
                    if (!Int32.TryParse(node.GetAttribute("chance"), out int chance) || chance == 0)
                    {
                        Log.Warning($"Agency.TryExtractWeapons(): Weapon entry for '{agency.ScriptName}' has no chance attribute in Agencies.xml");
                        continue;
                    }

                    // Create new meta
                    var modelName = node.InnerText;
                    var meta = new WeaponMeta(chance, modelName);

                    // Extras
                    var range = Enumerable.Range(1, 6);
                    foreach (int num in range)
                    {
                        // Search for each component
                        if (node.TryGetAttribute($"comp_{num}", out string compName))
                        {
                            // Add component
                            meta.Components.Add(compName);
                        }
                    }

                    // Add to VehicleSet
                    var metaSet = name.Equals("HandGun") ? set.HandGunMetas : set.LongGunMetas; 
                    metaSet.Add(meta);
                }
            }

            // Report success
            return true;
        }

        /// <summary>
        /// Extracts the Vehicles data from a VehicleSet XML node
        /// </summary>
        /// <param name="vn">The VehicleSet xml node</param>
        /// <param name="set">The <see cref="VehicleSet"/> to append the data to</param>
        /// <param name="agency">The <see cref="Agency"/> we are currently extracting for</param>
        /// <returns>true on success, false if the sub XML nodes doesnt exist</returns>
        private bool TryExtractVehicles(XmlNode vn, VehicleSet set, Agency agency)
        {
            // Grab nodes
            var nodes = vn.SelectSingleNode("Vehicles")?.SelectNodes("Vehicle");
            if (nodes == null || nodes.Count == 0)
                return false;

            // Loop through each Ped node
            foreach (XmlNode node in nodes)
            {
                // Extract the chance attribute
                if (!Int32.TryParse(node.GetAttribute("chance"), out int chance) || chance == 0)
                {
                    Log.Warning($"Agency.TryExtractVehicles(): Vehicle entry for '{agency.ScriptName}' has no chance attribute in Agencies.xml");
                    continue;
                }

                // Create new meta
                var modelName = node.InnerText;
                var meta = new VehicleModelMeta(chance, modelName);

                // Check for livery
                if (node.TryGetAttribute("livery", out int livery))
                {
                    meta.LiveryIndex = livery;
                }

                // Check for livery
                if (node.TryGetAttribute("color", out string colorName))
                {
                    meta.SpawnColor = Color.FromName(colorName);
                }

                // Extras
                var range = Enumerable.Range(1, 12);
                foreach (int num in range)
                {
                    // Search for each component
                    if (bool.TryParse(node.GetAttribute($"extra_{num}"), out bool ee))
                    {
                        // Add component
                        meta.Extras.AddOrUpdate(num, ee);
                    }
                }

                // Add to VehicleSet
                set.VehicleMetas.Add(meta);
            }

            // Report success
            return set.VehicleMetas.ItemCount > 0;
        }

        /// <summary>
        /// Extracts the Ped data from a VehicleSet XML node
        /// </summary>
        /// <param name="vn">The VehicleSet xml node</param>
        /// <param name="set">The <see cref="VehicleSet"/> to append the data to</param>
        /// <param name="agency">The <see cref="Agency"/> we are currently extracting for</param>
        /// <returns>true on success, false if the sub XML nodes doesnt exist</returns>
        private bool TryExtractPeds(XmlNode vn, VehicleSet set, Agency agency)
        {
            // Grab node
            var nodes = vn.SelectSingleNode("Peds")?.SelectNodes("Ped");
            if (nodes == null || nodes.Count == 0)
                return false;

            // Hash table of models
            var modelTable = new Dictionary<string, OfficerModelMeta>();

            // Loop through each Ped node
            foreach (XmlNode node in nodes)
            {
                // Local vars
                OfficerModelMeta meta = null;
                var variation = OutfitVariation.Dry;
                var modelName = node.InnerText;

                // Check for rainy or snowy variation!
                if (node.TryGetAttribute("rain_outfit", out bool rain) && rain)
                {
                    variation = OutfitVariation.Rainy;
                }
                else if (node.TryGetAttribute("snow_outfit", out bool snow) && snow)
                {
                    variation = OutfitVariation.Snowy;
                }

                // Create meta
                if (variation == OutfitVariation.Dry)
                {
                    // Extract the chance attribute
                    if (!Int32.TryParse(node.GetAttribute("chance"), out int chance) || chance == 0)
                        continue;

                    // Create new meta
                    meta = new OfficerModelMeta(chance, modelName);
                    modelTable.AddOrUpdate(modelName, meta);
                }
                else if (modelTable.ContainsKey(modelName))
                {
                    meta = modelTable[modelName];
                }
                else
                {
                    Log.Error($"AgenciesFile.TryExtractPeds: Outfit variation found for model '{modelName}' but there is no dry variation!");
                    return false;
                }

                // Check for randomize props
                if (node.TryGetAttribute("random_props", out bool randomize))
                {
                    meta.RandomizeProps(variation, randomize);
                }

                // Extract components
                foreach (var item in ComponentLookUp)
                {
                    // Search for each component
                    if (Int32.TryParse(node.GetAttribute($"comp_{item.Key}"), out int val))
                    {
                        // Check for texture index
                        if (!Int32.TryParse(node.GetAttribute($"tex_{item.Key}"), out int tex))
                        {
                            tex = 0;
                        }

                        // Add component
                        meta.SetComponent(variation, item.Value, val, tex);
                    }
                }

                // Extract props
                foreach (var item in PropLookUp)
                {
                    // Search for each component
                    if (Int32.TryParse(node.GetAttribute($"prop_{item.Key}"), out int val))
                    {
                        // Check for texture index
                        if (!Int32.TryParse(node.GetAttribute($"tex_{item.Key}"), out int tex))
                        {
                            tex = 0;
                        }

                        // Add component
                        meta.SetProp(variation, item.Value, val, tex);
                    }
                }

                // Add to VehicleSet
                set.OfficerMetas.Add(meta);
            }

            // Report success
            return set.OfficerMetas.ItemCount > 0;
        }

        /// <summary>
        /// Extracts the NonLethals data from a VehicleSet XML node
        /// </summary>
        /// <param name="vn">The VehicleSet xml node</param>
        /// <param name="set">The <see cref="VehicleSet"/> to append the data to</param>
        /// <param name="agency">The <see cref="Agency"/> we are currently extracting for</param>
        /// <returns>true on success, false if the sub XML nodes doesnt exist</returns>
        private bool TryExtractNonLethals(XmlNode vn, VehicleSet set, Agency agency)
        {
            // Grab nodes
            var nodes = vn.SelectSingleNode("NonLethals")?.SelectNodes("NonLethal");
            if (nodes == null || nodes.Count == 0)
                return false;

            // Loop through each Ped node
            foreach (XmlNode node in nodes)
            {
                // Create new meta
                var modelName = node.InnerText;
                set.NonLethalWeapons.Add(modelName);
            }
            // Report success
            return true;
        }
    }
}
