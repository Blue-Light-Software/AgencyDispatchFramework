using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.Xml;
using LSPD_First_Response.Mod.API;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// Represents the Current Player Police/Sheriff Department
    /// </summary>
    public abstract class Agency
    {
        #region Static Properties

        /// <summary>
        /// Indicates whether the the Agency data has been loaded into memory
        /// </summary>
        private static bool IsInitialized { get; set; } = false;

        /// <summary>
        /// A dictionary of agencies
        /// </summary>
        private static Dictionary<string, Agency> Agencies { get; set; }

        /// <summary>
        /// A hash look up of agency districts, ids and zones
        /// </summary>
        protected static Dictionary<string, Dictionary<string, Tuple<int, HashSet<string>>>> DistrictData { get; private set; }

        #endregion

        #region Instance Properties

        /// <summary>
        /// Our lock object to prevent multi-threading issues
        /// </summary>
        private object _threadLock = new object();

        /// <summary>
        /// Gets the full string name of this agency
        /// </summary>
        public string FullName { get; private set; }

        /// <summary>
        /// Gets the full script name of this agency
        /// </summary>
        public string ScriptName { get; private set; }

        /// <summary>
        /// Gets the <see cref="Dispatching.CallSignStyle"/> this department uses when assigning 
        /// <see cref="CallSign/>s to <see cref="OfficerUnit"/>s
        /// </summary>
        public CallSignStyle CallSignStyle { get; protected set; }

        /// <summary>
        /// Gets the funding level of this department. This will be used to determine
        /// how frequent callouts will be handled by the other AI officers.
        /// </summary>
        public StaffLevel StaffLevel { get; protected set; }

        /// <summary>
        /// Contains a list of zones in this jurisdiction
        /// </summary>
        internal WorldZone[] Zones { get; set; }

        /// <summary>
        /// Contains a list of zone names in this jurisdiction
        /// </summary>
        internal string[] ZoneNames { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal Dictionary<string, District> Districts { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal Dictionary<UnitType, SpecializedUnit> Units { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal Dictionary<ShiftRotation, List<OfficerUnit>> OfficersByShift { get; set; }

        /// <summary>
        /// Contains a list of all <see cref="AIOfficerUnit"/>s currently on duty
        /// </summary>
        protected HashSet<OfficerUnit> OnDutyOfficers { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal Dictionary<TimePeriod, double> CallsByPeriod { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal int AverageDailyCalls { get; set; }

        /// <summary>
        /// 
        /// </summary>
        internal int IdealRosterSize { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Dispatching.Dispatcher"/> for this <see cref="Agency"/>
        /// </summary>
        internal Dispatcher Dispatcher { get; set; }

        /// <summary>
        /// Indicates whether this agency is active in game at the moment
        /// </summary>
        public bool IsActive { get; internal set; }

        /// <summary>
        /// Indicates whether this agency is a State wide agency (jurisdiction wise).
        /// </summary>
        /// <remarks>
        /// This being true changes the way the program handles calling for backup units when on duty
        /// </remarks>
        public bool IsStateAgency
        {
            get
            {
                var type = AgencyType;
                return (type == AgencyType.HighwayPatrol || type == AgencyType.StateParks || type == AgencyType.StatePolice);
            }
        }

        /// <summary>
        /// Indicates whether this agency is a law enforcement agency
        /// </summary>
        /// <remarks>
        /// This being true changes the way the program handles calling for backup units when on duty
        /// </remarks>
        public bool IsLawEnforcementAgency
        {
            get => Sector == ServiceSector.Police;
        }

        /// <summary>
        /// Gets or sets the backing county of this <see cref="Agency"/>
        /// </summary>
        public County BackingCounty { get; internal set; }

        /// <summary>
        /// Gets the <see cref="Dispatching.AgencyType"/> for this <see cref="Agency"/>
        /// </summary>
        public abstract AgencyType AgencyType { get; }

        /// <summary>
        /// Gets the <see cref="ServiceSector"/> of this agency
        /// </summary>
        public abstract ServiceSector Sector { get; }

        #endregion

        #region Static Methods 

        /// <summary>
        /// Loads the XML data that defines all police agencies, and thier jurisdiction
        /// </summary>
        internal static void Initialize()
        {
            if (IsInitialized) return;

            // Set internal flag to initialize just once
            IsInitialized = true;

            // Load and parse the xml file
            string path = Path.Combine(Main.FrameworkFolderPath, "Districts.xml");
            using (var file = new DistrictsFile(path))
            {
                // Parse XML
                file.Parse();

                // Fetch data
                DistrictData = file.Districts;
            }

            // Load and parse the xml file
            path = Path.Combine(Main.FrameworkFolderPath, "Agencies.xml");
            using (var file = new AgenciesFile(path))
            {
                // Parse XML
                file.Parse();

                // Fetch data
                Agencies = file.Agencies;
            }
        }

        /// <summary>
        /// Creates and returns an <see cref="Agency"/> instance based on <see cref="AgencyType"/>
        /// </summary>
        /// <param name="type"></param>
        /// <param name="sname"></param>
        /// <param name="name"></param>
        /// <param name="staffing"></param>
        /// <returns></returns>
        internal static Agency CreateAgency(AgencyType type, string sname, string name, StaffLevel staffing, CallSignStyle signStyle, string[] zoneNames)
        {
            switch (type)
            {
                case AgencyType.CityPolice:
                    return new PoliceAgency(sname, name, staffing, signStyle, zoneNames);
                case AgencyType.CountySheriff:
                case AgencyType.StateParks:
                    return new SheriffAgency(sname, name, staffing, signStyle, zoneNames);
                case AgencyType.StatePolice:
                case AgencyType.HighwayPatrol:
                    return new HighwayPatrolAgency(sname, name, staffing, signStyle, zoneNames);
                default:
                    throw new NotImplementedException($"The AgencyType '{type}' is yet supported");
            }
        }

        /// <summary>
        /// Gets an array of all zones under the Jurisdiction of the specified Agency sriptname
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string[] GetZoneNamesByAgencyName(string name)
        {
            name = name.ToLowerInvariant();
            if (Agencies.ContainsKey(name))
            {
                return Agencies[name].ZoneNames;
            }

            return null;
        }

        /// <summary>
        /// Gets the players current police agency
        /// </summary>
        /// <returns></returns>
        public static Agency GetCurrentPlayerAgency()
        {
            string name = Functions.GetCurrentAgencyScriptName().ToLowerInvariant();
            return (Agencies.ContainsKey(name)) ? Agencies[name] : null;
        }

        /// <summary>
        /// Gets the Agency data for the specied Agency
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Agency GetAgencyByName(string name)
        {
            name = name.ToLowerInvariant();
            return (Agencies.ContainsKey(name)) ? Agencies[name] : null;
        }

        #endregion Static Methods

        #region Instance Methods

        /// <summary>
        /// Creates a new instance of an <see cref="Agency"/>
        /// </summary>
        /// <param name="scriptName"></param>
        /// <param name="friendlyName"></param>
        /// <param name="staffLevel"></param>
        internal Agency(string scriptName, string friendlyName, StaffLevel staffLevel, CallSignStyle signStyle, string[] zoneNames)
        {
            // Set properties
            ScriptName = scriptName ?? throw new ArgumentNullException(nameof(scriptName));
            FullName = friendlyName ?? throw new ArgumentNullException(nameof(friendlyName));
            StaffLevel = staffLevel;
            CallSignStyle = signStyle;
            ZoneNames = zoneNames;

            // Initialize collections
            CallsByPeriod = new Dictionary<TimePeriod, double>();
            Units = new Dictionary<UnitType, SpecializedUnit>();
            OnDutyOfficers = new HashSet<OfficerUnit>();
            Districts = new Dictionary<string, District>();

            // Initialize districts
            InitDistricts();
        }

        /// <summary>
        /// Creates the dispatcher for this agency type
        /// </summary>
        /// <returns></returns>
        internal abstract Dispatcher CreateDispatcher();

        /// <summary>
        /// Assigns this agency within the jurisdiction of all the <see cref="Zones"/>
        /// </summary>
        protected abstract void AssignZones();

        /// <summary>
        /// Calculates the optimum patrols for this agencies jurisdiction
        /// </summary>
        /// <param name="zoneNames"></param>
        /// <returns></returns>
        protected abstract void CalculateAgencySize();

        /// <summary>
        /// Enables simulation on this <see cref="Agency"/> in the game
        /// </summary>
        internal virtual void Enable()
        {
            // Saftey
            if (IsActive) return;

            // --------------------------------------------------
            // NOTE: These invocations must be done in order!
            // --------------------------------------------------

            // Second, Assign this agency to the zones. This must be abstract, since each agency
            // type will assign themselves differently
            AssignZones();

            // Now we calculate agency size. This must be abstract, since each agency has
            // a custom way of assinging units.
            CalculateAgencySize();

            // Create dispatcher. This must be abstract, as each dispatcher ahdnles calls and 
            // dispatches units differently based on the agency type.
            Dispatcher = CreateDispatcher();

            // Finally, Create AI officer units to fill the roster
            CreateOfficerUnits();

            // Register for Shift changes with the primary dispatcher!
            Dispatch.OnShiftStart += Dispatch_OnShiftStart;

            // Finally, flag
            IsActive = true;
        }

        /// <summary>
        /// Disables simulation on this <see cref="Agency"/> in the game
        /// </summary>
        internal virtual void Disable()
        {
            // Un-Register for Shift changes!
            Dispatch.OnShiftStart -= Dispatch_OnShiftStart;

            // Dispose the dispatcher
            Dispatcher?.Dispose();

            // Dispose officer units
            DisposeAIUnits();

            // Reset districts
            Districts = new Dictionary<string, District>();
            InitDistricts();

            // flag
            IsActive = false;
        }

        /// <summary>
        /// Initializes this <see cref="Agency"/>'s districts
        /// </summary>
        protected virtual void InitDistricts()
        {
            // Persistant call sign generator?
            CallSignGenerator generator = null;
            if (CallSignStyle == CallSignStyle.Numeric)
            {
                generator = new NumericCallSignGenerator();
            }

            // Get all of our jurisdiction zones for this agency
            Zones = Zones ?? WorldZone.GetZonesByName(ZoneNames, out int loaded, out int locations);

            // Does this agency have an entry in the districts.xml?
            if (DistrictData.ContainsKey(ScriptName))
            {
                var data = DistrictData[ScriptName];
                foreach (var districtMeta in data)
                {
                    // Extract data
                    var districtName = districtMeta.Key;
                    var districtIndex = districtMeta.Value.Item1;
                    var zoneNames = districtMeta.Value.Item2;

                    // Get zones by name
                    var zones = Zones.Where(x => zoneNames.Contains(x.ScriptName)).ToArray();

                    // Are we using LAPD style callsigns? If so, each district gets its own generator!
                    if (CallSignStyle == CallSignStyle.LAPD)
                    {
                        generator = new LAPDCallSignGenerator(districtIndex);
                    }

                    // Create district
                    var district = new District(districtIndex, districtName, zones, generator);
                    Districts.Add(districtName, district);
                }
            }
            else
            {
                // Are we using LAPD style callsigns? If so, each district gets its own generator!
                if (CallSignStyle == CallSignStyle.LAPD)
                {
                    generator = new LAPDCallSignGenerator(1);
                }

                // Create district
                var district = new District(1, "Central", Zones, generator);
                Districts.Add("Central", district);
            }
        }

        /// <summary>
        /// Creates the <see cref="AIOfficerUnit"/>s and assigns them to <see cref="ShiftRotation"/>s
        /// </summary>
        protected virtual void CreateOfficerUnits()
        {
            // Fill the roster hash table
            OfficersByShift = new Dictionary<ShiftRotation, List<OfficerUnit>>();
            foreach (ShiftRotation period in Enum.GetValues(typeof(ShiftRotation)))
            {
                OfficersByShift.Add(period, new List<OfficerUnit>());
            }

            // Define which units create supervisors
            var supervisorUnits = new[] { UnitType.Patrol, UnitType.Traffic };

            // Create log entry
            Log.Debug("\t\tAgency Officer Units:");

            // For each unit type!
            foreach (SpecializedUnit unit in Units.Values)
            {
                // Each district
                foreach (District district in Districts.Values)
                {
                    // Get shift counts
                    var shiftCounts = unit.CalculatePatrolShiftCount(district);
                    var unitName = Enum.GetName(typeof(UnitType), unit.UnitType);

                    // Create officers for every shift
                    foreach (var shift in shiftCounts)
                    {
                        var shiftName = Enum.GetName(typeof(ShiftRotation), shift.Key);
                        var aiPatrolCount = shift.Value;
                        var toCreate = aiPatrolCount;

                        // Calculate sergeants, needs at least 3 units on shift
                        if (supervisorUnits.Contains(unit.UnitType) && aiPatrolCount > 2)
                        {
                            // Subtract an officer unit
                            toCreate -= 1;

                            // Create officer and add it to the shift roster
                            var officer = unit.CreateOfficerUnit(true, shift.Key, district);

                            // Add officer to rosters
                            OfficersByShift[shift.Key].Add(officer);
                            district.OfficersByShift[shift.Key].Add(officer);

                            // Log for debugging
                            Log.Debug($"\t\t\t[Supervisor] {officer.Persona.FullName} ({officer.CallSign.Value}) on {shiftName} shift for {ScriptName} as part of the {unitName} unit");
                        }

                        // Create officer units
                        for (int i = 0; i < toCreate; i++)
                        {
                            // Create officer and add it to the shift roster
                            var officer = unit.CreateOfficerUnit(false, shift.Key, district);

                            // Add officer to rosters
                            OfficersByShift[shift.Key].Add(officer);
                            district.OfficersByShift[shift.Key].Add(officer);

                            // Log for debugging
                            Log.Debug($"\t\t\t{officer.Persona.FullName} ({officer.CallSign.Value}) on {shiftName} shift for {ScriptName} as part of the {unitName} unit");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds the unit to the list
        /// </summary>
        /// <param name="unit"></param>
        internal void AddUnit(SpecializedUnit unit)
        {
            Units.AddOrUpdate(unit.UnitType, unit);
        }

        /// <summary>
        /// Gets an <see cref="Enumerable"/> of supported <see cref="UnitType"/>s for this <see cref="Agency"/>
        /// </summary>
        /// <returns></returns>
        public IEnumerable<UnitType> GetSupportedUnitTypes()
        {
            foreach (var unit in Units)
            {
                yield return unit.Key;
            }
        }

        /// <summary>
        /// @todo Creates the player <see cref="OfficerUnit"/> and adds them to this <see cref="Agency"/> roster.
        /// </summary>
        /// <returns></returns>
        internal OfficerUnit AddPlayerUnit(SimulationSettings s)
        {
            // -------------------------------------
            // Create players callSign
            // -------------------------------------

            CallSign callSign = null;
            if (CallSignStyle == CallSignStyle.LAPD)
            {
                // Get alpha index of the unit
                var unit = LAPDCallSignGenerator.GetUnitTypeChar(s.PrimaryRole);
                int unitTypeIndex = (int)unit % 32;
                callSign = new LAPDStyleCallsign(s.SelectedDistrict.Index, unitTypeIndex, s.Beat);
            }
            else
            {
                callSign = new NumericStyleCallsign(s.Beat);
            }

            // -------------------------------------
            // Create player
            // -------------------------------------
            var p = Rage.Game.LocalPlayer;
            var playerUnit = new PlayerOfficerUnit(p, this, callSign, s.PrimaryRole, s.SelectedDistrict, s.SelectedShift);

            // Return the player object
            return playerUnit;
        }

        /// <summary>
        /// Removes the unit from the OnDuty list
        /// </summary>
        /// <param name="officerUnit"></param>
        internal void RemoveOnDuty(OfficerUnit officerUnit)
        {
            //lock (_threadLock)
            //{
                OnDutyOfficers.Remove(officerUnit);
            //}
        }

        /// <summary>
        /// Disposes and clears all AI units
        /// </summary>
        private void DisposeAIUnits()
        {
            // Clear old police units
            if (OfficersByShift != null)
            {
                foreach (var officerUnits in OfficersByShift.Values)
                    foreach (var officer in officerUnits)
                        officer.Dispose();
            }
        }

        /// <summary>
        /// Method called when a <see cref="ShiftRotation"/> starts
        /// </summary>
        private void Dispatch_OnShiftStart(ShiftRotation shift)
        {
            // Ensure we have enough locations to spawn patrols at
            int aiPatrolCount = OfficersByShift[shift].Count;
            var locations = GetRandomShoulderLocations(aiPatrolCount);
            if (locations.Length < aiPatrolCount)
            {
                StringBuilder b = new StringBuilder("The number of RoadShoulders available (");
                b.Append(locations.Length);
                b.Append(") to spawn AI officer units is less than the number of total AI officers (");
                b.Append(aiPatrolCount);
                b.Append(") for '");
                b.Append(FullName);
                b.Append("' on ");
                b.Append(Enum.GetName(typeof(ShiftRotation), shift));
                b.Append(" shift.");
                Log.Warning(b.ToString());

                // Adjust count
                aiPatrolCount = locations.Length;
                if (aiPatrolCount == 0) return;
            }

            // Prevent race conditions
            lock (_threadLock)
            {
                // Tell new units they are on duty
                int i = 0;
                foreach (var unit in OfficersByShift[shift])
                {
                    // Temporary @todo
                    if (i >= locations.Length)
                        i = 0;

                    if (unit == null)
                    {
                        Log.Error("Officer unit if null, what the actual fuck?");
                    }
                    else if (!unit.IsAIUnit)
                    {
                        // Skip player
                        continue;
                    }

                    // Tell unit to get to work!
                    var spawnLocation = locations[i];
                    if (spawnLocation == null)
                    {
                        Log.Error("Spawnlocation is null, what the actual fuck?");
                    }

                    var aiOfficer = (AIOfficerUnit)unit;
                    var vector = spawnLocation.Position;
                    aiOfficer.StartDuty(vector);

                    // Add officer to the duty roster
                    OnDutyOfficers.Add(aiOfficer);
                    i++;
                }
            }
        }

        /// <summary>
        /// Gets a number of random <see cref="RoadShoulder"/> locations within
        /// this <see cref="Agency" /> jurisdiction
        /// </summary>
        /// <param name="desiredCount">The desired amount</param>
        /// <returns></returns>
        protected RoadShoulder[] GetRandomShoulderLocations(int desiredCount)
        {
            // Get a list of all locations
            var locs = new List<RoadShoulder>(desiredCount);
            foreach (var zone in Zones)
            {
                var items = LocationsDB.RoadShoulders.Include(x => x.Zone).Find(x => x.Zone.Id == zone.Id);
                foreach (RoadShoulder item in items)
                {
                    locs.Add(item);
                }
            }

            // Shuffle
            locs.Shuffle();
            return locs.Take(desiredCount).ToArray();
        }

        /// <summary>
        /// Gets an array of <see cref="OfficerUnit"/>s that are on duty. This method is thread safe
        /// </summary>
        public virtual OfficerUnit[] GetOnDutyOfficers()
        {
            //lock (_threadLock)
            //{
                return OnDutyOfficers.ToArray();
            //}
        }

        public override string ToString()
        {
            return FullName;
        }

        #endregion
    }
}
