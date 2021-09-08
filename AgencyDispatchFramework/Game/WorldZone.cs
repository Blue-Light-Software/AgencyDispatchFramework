using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.Simulation;
using AgencyDispatchFramework.Xml;
using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// This class contains a series of spawnable locations within a specific zone
    /// </summary>
    public sealed class WorldZone : ISpawnable
    {
        /// <summary>
        /// Contains a hash table of zones
        /// </summary>
        /// <remarks>[ ZoneScriptName => ZoneInfo class ]</remarks>
        private static Dictionary<string, WorldZone> ZoneCache { get; set; } = new Dictionary<string, WorldZone>();

        /// <summary>
        /// Containts a hash table of regions, and thier zones
        /// </summary>
        private static Dictionary<string, List<string>> RegionZones { get; set; } = new Dictionary<string, List<string>>(16);

        /// <summary>
        /// Gets the crime level probability of this zone based on current time of day
        /// </summary>
        [BsonIgnore]
        public int Probability => (int)(CrimeInfo.GetTrueAverageCallCount(GameWorld.CurrentTimePeriod) * 1000);

        /// <summary>
        /// Gets the average daily crime calls
        /// </summary>
        [BsonId]
        public int Id { get; internal set; }

        /// <summary>
        /// Gets the Zone script name (ex: SANDY)
        /// </summary>
        public string ScriptName { get; private set; }

        /// <summary>
        /// Gets the full Zone name (Ex: Sandy Shores)
        /// </summary>
        public string DisplayName { get; internal set; }

        /// <summary>
        /// Gets the <see cref="Game.County"/> in game this zone belongs in
        /// </summary>
        public County County { get; internal set; }

        /// <summary>
        /// Gets the population density of the zone
        /// </summary>
        public Population Population { get; internal set; }

        /// <summary>
        /// Gets the zone size
        /// </summary>
        public ZoneSize Size { get; internal set; }

        /// <summary>
        /// Gets the social class of this zones citizens
        /// </summary>
        public SocialClass SocialClass { get; internal set; }

        /// <summary>
        /// Gets the primary zone type for this zone
        /// </summary>
        public List<ZoneFlags> Flags { get; internal set; }

        /// <summary>
        /// Gets the average daily crime calls
        /// </summary>
        [BsonIgnore]
        public CrimeProjection CrimeInfo { get; internal set; }

        /// <summary>
        /// Spawns a <see cref="CallCategory"/> based on the <see cref="WorldStateMultipliers"/> probabilites set
        /// </summary>
        [BsonIgnore]
        internal WorldStateProbabilityGenerator<CallCategory> CallCategoryGenerator { get; set; }

        /// <summary>
        /// Contains a list of police <see cref="Agency"/> instances that have jurisdiction in this <see cref="WorldZone"/>
        /// </summary>
        /// <remarks>
        /// Ensure you are grabbing the zone from <see cref="WorldZone.GetZoneByName(string)"/> otherwise this will be null
        /// </remarks>
        [BsonIgnore]
        private List<Agency> PoliceAgencies { get; set; }

        /// <summary>
        /// Gets the medical <see cref="Agency"/> that services this <see cref="WorldZone"/>
        /// </summary>
        /// <remarks>
        /// Ensure you are grabbing the zone from <see cref="WorldZone.GetZoneByName(string)"/> otherwise this will be null
        /// </remarks>
        [BsonIgnore]
        private Agency EmsAgency { get; set; }

        /// <summary>
        /// Gets the fire <see cref="Agency"/> that services this <see cref="WorldZone"/>
        /// </summary>
        /// <remarks>
        /// Ensure you are grabbing the zone from <see cref="WorldZone.GetZoneByName(string)"/> otherwise this will be null
        /// </remarks>
        [BsonIgnore]
        private Agency FireAgeny { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="WorldZone"/>. This constructor is designed to be used by <see cref="LiteDB"/>
        /// </summary>
        [BsonCtor]
        public WorldZone(int _id, string scriptName, string displayName, County county, Population population, ZoneSize size, SocialClass socialClass, BsonArray flags)
        {
            // Set properties
            Id = _id;
            ScriptName = scriptName ?? throw new ArgumentNullException("scriptName");
            DisplayName = displayName ?? throw new ArgumentNullException("displayName");
            County = county;
            Population = population;
            Size = size;
            SocialClass = socialClass;
            Flags = flags.Select(x => (ZoneFlags)Enum.Parse(typeof(ZoneFlags), x.AsString)).ToList();

            // Add to cache if not existing already
            if (!ZoneCache.ContainsKey(scriptName))
            {
                ZoneCache.Add(scriptName, this);
                PoliceAgencies = new List<Agency>();
            }
        }

        /// <summary>
        /// Creates a new instance of <see cref="WorldZone"/>.
        /// </summary>
        public WorldZone(string scriptName, string displayName, County county, Population population, ZoneSize size, SocialClass socialClass, List<ZoneFlags> flags)
        {
            // Set properties
            ScriptName = scriptName?.ToUpper() ?? throw new ArgumentNullException("scriptName");
            DisplayName = displayName ?? throw new ArgumentNullException("displayName");
            County = county;
            Population = population;
            Size = size;
            SocialClass = socialClass;
            Flags = flags;

            // Add to cache if not existing already
            if (!ZoneCache.ContainsKey(scriptName))
            {
                ZoneCache.Add(scriptName, this);
                PoliceAgencies = new List<Agency>();
            }
        }

        /// <summary>
        /// Gets a list of Police agencies that service this zone, in order or primary jurisdiction
        /// </summary>
        /// <returns></returns>
        public List<Agency> GetPoliceAgencies()
        {
            // Maybe we are not the cached version?
            return ZoneCache[ScriptName].PoliceAgencies;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="agencies"></param>
        internal void SetPoliceAgencies(params Agency[] agencies)
        {
            // Maybe we are not the cached version?
            ZoneCache[ScriptName].PoliceAgencies = new List<Agency>(agencies);
        }

        /// <summary>
        /// Indicates whether the given <see cref="Agency"/> has jurisdiction in this <see cref="WorldZone"/>
        /// </summary>
        /// <param name="agency"></param>
        /// <returns></returns>
        public bool DoesAgencyHaveJurisdiction(Agency agency)
        {
            if (agency.IsLawEnforcementAgency)
            {
                return ZoneCache[ScriptName].PoliceAgencies.Contains(agency);
            }

            return false;
        }

        /// <summary>
        /// Gets the total number of Locations in this collection, regardless of type
        /// </summary>
        /// <returns></returns>
        public int GetTotalNumberOfLocations()
        {
            // Add up location counts
            var count = LocationsDB.RoadShoulders.Query().Where(x => x.Zone.Id == Id).Count();
                count += LocationsDB.Residences.Query().Where(x => x.Zone.Id == Id).Count();

            // Final count
            return count;
        }

        /// <summary>
        /// Spawns the next <see cref="CallCategory"/> that will happen in this zone
        /// based on the crime probabilities set
        /// </summary>
        /// <returns>
        /// returns the next callout type on success. On failure, <see cref="CallCategory.Traffic"/>
        /// will always be returned
        /// </returns>
        public CallCategory GetNextRandomCrimeType()
        {
            if (CallCategoryGenerator.TrySpawn(out CallCategory calloutType))
            {
                return calloutType;
            }

            return CallCategory.Traffic;
        }

        /// <summary>
        /// This is where the magic happens. This method Gets a random <see cref="WorldLocation"/> from a pool
        /// of locations, applying filters and checking to see if the location is already in use
        /// </summary>
        /// <param name="type"></param>
        /// <param name="locationPool"></param>
        /// <param name="inactiveOnly">If true, will only return a <see cref="WorldLocation"/> that is not currently in use</param>
        /// <returns></returns>
        /// <seealso cref="https://github.com/mbdavid/LiteDB/issues/1666"/>
        private T GetRandomLocationFromPool<T>(ILiteQueryable<T> locationPool, FlagFilterGroup filters, bool inactiveOnly) where T : WorldLocation
        {
            // If we have no locations, return null
            if (!locationPool?.Exists() ?? false)
            {
                Log.Debug($"WorldZone.GetRandomLocationFromPool<T>(): Unable to pull a {typeof(T).Name} from zone '{ScriptName}' because there are no locations in the database");
                return null;
            }

            // Filter results
            var locations = locationPool.Include(x => x.Zone).Where(x => x.Zone.Id == Id).ToList();

            // Filtering by flags? Do this first so we can log debugging info if there are no available locations with these required flags in this zone
            if (filters != null && filters.Requirements.Count > 0)
            {
                locations = locations.Filter(filters).ToList();
                if (locations.Count == 0)
                {
                    Log.Warning($"WorldZone.GetRandomLocationFromPool<T>(): There are no locations of type '{typeof(T).Name}' in zone '{ScriptName}' using the following flags:");
                    Log.Warning($"\t{filters}");
                    return null;
                }
            }

            // Will any location work?
            if (inactiveOnly)
            {
                try
                {
                    // Find all locations not in use
                    locations = Dispatch.GetInactiveLocationsFromPool(locations);
                }
                catch (InvalidCastException ex)
                {
                    Log.Exception(ex, $"WorldZone.GetRandomLocationFromPool<T>(): Cast exception to {typeof(T).Name} from location pool. Logging exception data");
                    return null;
                }
            }

            // If no locations are available
            if (locations.Count == 0)
            {
                Log.Debug($"WorldZone.GetRandomLocationFromPool<T>(): Unable to pull an available '{typeof(T).Name}' location from zone '{ScriptName}' because they are all in use");
                return null;
            }

            // Load randomizer
            var random = new CryptoRandom();
            return random.PickOne(locations.ToArray());
        }

        /// <summary>
        /// Gets a random Side of the Road location in this zone
        /// </summary>
        /// <param name="inactiveOnly">If true, will only return a <see cref="WorldLocation"/> that is not currently in use</param>
        /// <returns>returns a random <see cref="SpawnPoint"/> on success, or null on failure</returns>
        public RoadShoulder GetRandomRoadShoulder(FlagFilterGroup filters, bool inactiveOnly = false)
        {
            // Get random location
            var queryable = LocationsDB.RoadShoulders.Query();
            return GetRandomLocationFromPool(queryable, filters, inactiveOnly);
        }

        /// <summary>
        /// Gets a random <see cref="Residence"/> in this zone
        /// </summary>
        /// <param name="inactiveOnly">If true, will only return a <see cref="WorldLocation"/> that is not currently in use</param>
        /// <returns>returns a random <see cref="Residence"/> on success, or null on failure</returns>
        public Residence GetRandomResidence(FlagFilterGroup filters, bool inactiveOnly = false)
        {
            // Get random location
            var queryable = LocationsDB.Residences.Query();
            return GetRandomLocationFromPool(queryable, filters, inactiveOnly);
        }

        /// <summary>
        /// Loads the specified zones from the database and from the Locations.xml into
        /// memory, caches the instances, and returns the total number of locations.
        /// </summary>
        /// <param name="names">An array of zones to load (should be all uppercase)</param>
        /// <returns>returns the number of locations loaded</returns>
        /// <param name="loaded">Returns the number of zones that were loaded (not cached yet).</param>
        public static WorldZone[] GetZonesByName(string[] names, out int loaded, out int totalLocations)
        {
            // Create instance of not already!
            if (ZoneCache == null)
            {
                ZoneCache = new Dictionary<string, WorldZone>();
            }

            // Local return variables
            totalLocations = 0;
            int zonesAdded = 0;
            List<WorldZone> zones = new List<WorldZone>();

            // Cycle through each child node (Zone)
            foreach (string zoneName in names)
            {
                // Create our spawn point collection and store it
                try
                {
                    var dbZone = GetZoneByName(zoneName);

                    // Add zone to return
                    zones.Add(dbZone);

                    // Up the location counters
                    totalLocations += dbZone.GetTotalNumberOfLocations();
                    zonesAdded++;
                }
                catch (FormatException e)
                {
                    Log.Error($"WorldZone.LoadZones(): Unable to load location data for zone '{zoneName}'. Missing node/attribute '{e.Message}'");
                    continue;
                }
                catch (FileNotFoundException)
                {
                    Log.Warning($"WorldZone.LoadZones(): Missing xml file for zone '{zoneName}'");
                    continue;
                }
                catch (Exception fe)
                {
                    Log.Exception(fe);
                    continue;
                }
            }

            loaded = zonesAdded;
            return zones.ToArray();
        }

        /// <summary>
        /// Gets a <see cref="WorldZone"/> instance by name from the database and fills its <see cref="CrimeProjection"/> 
        /// from the XML file.
        /// </summary>
        /// <param name="name">The script name of the zone as written in the Locations.xml</param>
        /// <returns>return a <see cref="WorldZone"/>, or null if the zone has not been loaded yet</returns>
        /// <exception cref="FileNotFoundException">thrown if the XML file for the zone does not exist"</exception>
        /// <exception cref="FormatException">thrown in the XML file is missing nodes and/or attributes</exception>
        public static WorldZone GetZoneByName(string name)
        {
            // If we have loaded this zone already, skip it
            if (ZoneCache.ContainsKey(name))
            {
                return ZoneCache[name];
            }

            // Check file exists
            string path = Path.Combine(Main.FrameworkFolderPath, "Locations", $"{name}.xml");
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"WorldZone.LoadZones(): Missing xml file for zone '{name}'");  
            }

            // Indicates whether the zone exists in the database
            var needToAdd = false;

            // Grab zone from database
            var dbZone = LocationsDB.WorldZones.FindOne(x => x.ScriptName.Equals(name));
            if (dbZone == null)
            {
                Log.Warning($"Attempted to fetch zone named '{name}' from database but it did not exist");
                needToAdd = true;
            }

            // Load XML document
            using (var file = new WorldZoneFile(path))
            {
                // Parse the XML contents. It is OK to pass null here!
                file.Parse(dbZone);

                // Do we need to add?
                if (needToAdd)
                {
                    var id = LocationsDB.WorldZones.Insert(dbZone);
                    dbZone.Id = id.AsInt32;
                }

                return dbZone;
            }
        }

        /// <summary>
        /// Adds a region
        /// </summary>
        /// <param name="name">The name of the region</param>
        /// <param name="zones">A list of zone names contained within this region</param>
        internal static void AddRegion(string name, List<string> zones)
        {
            RegionZones.Add(name, zones);
        }

        /// <summary>
        /// Gets a list of regions
        /// </summary>
        /// <returns>an array of region names found in the LSPDFR regions.xml file</returns>
        public static string[] GetRegions()
        {
            return RegionZones.Keys.ToArray();
        }

        /// <summary>
        /// Gets a list of zone names by the region name
        /// </summary>
        /// <param name="region">Name of the region, located in the LSPDFR regions.xml file</param>
        /// <returns>an array of zone names on success, otherwise null</returns>
        public static string[] GetZoneNamesByRegion(string region)
        {
            if (!RegionZones.ContainsKey(region))
            {
                return null;
            }

            return RegionZones[region].ToArray();
        }
    }
}
