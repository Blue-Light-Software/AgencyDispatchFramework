using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using AgencyDispatchFramework.Game.Locations;
using AgencyDispatchFramework.Scripting;
using Rage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// This class is responsible for generating crime related <see cref="PriorityCall"/>s.
    /// </summary>
    internal class RegionCrimeGenerator
    {
        /// <summary>
        /// Contains the last Call ID used
        /// </summary>
        protected static int NextCallId { get; set; }

        /// <summary>
        /// Randomizer method used to randomize callouts and locations
        /// </summary>
        protected static CryptoRandom Randomizer { get; set; }

        /// <summary>
        /// Contains the <see cref="WorldStateMultipliers"/> from the BaseProbabilities.xml file
        /// </summary>
        internal static Dictionary<CallCategory, WorldStateMultipliers> BaseCrimeMultipliers { get; set; }

        /// <summary>
        /// Indicates whether this CrimeGenerator is currently creating calls
        /// </summary>
        public bool IsRunning { get; protected set; }

        /// <summary>
        /// Gets the <see cref="RegionCrimeInfo"/> based on TimeOfDay
        /// </summary>
        public Dictionary<TimePeriod, RegionCrimeInfo> RegionCrimeInfoByTimePeriod { get; private set; }

        /// <summary>
        /// Gets the current crime level definition for the current <see cref="Agency"/>
        /// </summary>
        public CrimeLevel CurrentCrimeLevel { get; private set; }

        /// <summary>
        /// Gets a list of zones in this region
        /// </summary>
        public WorldZone[] Zones => CrimeZoneGenerator.GetItems();

        /// <summary>
        /// Gets a list of zones in this jurisdiction
        /// </summary>
        protected ProbabilityGenerator<WorldZone> CrimeZoneGenerator { get; set; }

        /// <summary>
        /// Spawn generator for random crime levels
        /// </summary>
        private static ProbabilityGenerator<Spawnable<CrimeLevel>> CrimeLevelGenerator { get; set; }

        /// <summary>
        /// Contains a Queue of <see cref="TimeSpan"/>s as a plan of when the remaining calls
        /// during this <see cref="TimePeriod"/> will be recieved.
        /// </summary>
        private ConcurrentQueue<TimeSpan> NextIncomingCallTimes { get; set; }

        /// <summary>
        /// GameFiber containing the CallCenter functions
        /// </summary>
        private GameFiber CrimeFiber { get; set; }

        static RegionCrimeGenerator()
        {
            // Create our crime level generator
            CrimeLevelGenerator = new ProbabilityGenerator<Spawnable<CrimeLevel>>();
            CrimeLevelGenerator.Add(new Spawnable<CrimeLevel>(6, CrimeLevel.None));
            CrimeLevelGenerator.Add(new Spawnable<CrimeLevel>(12, CrimeLevel.VeryLow));
            CrimeLevelGenerator.Add(new Spawnable<CrimeLevel>(20, CrimeLevel.Low));
            CrimeLevelGenerator.Add(new Spawnable<CrimeLevel>(30, CrimeLevel.Moderate));
            CrimeLevelGenerator.Add(new Spawnable<CrimeLevel>(20, CrimeLevel.High));
            CrimeLevelGenerator.Add(new Spawnable<CrimeLevel>(12, CrimeLevel.VeryHigh));

            // Create next random call ID
            Randomizer = new CryptoRandom();
            NextCallId = Randomizer.Next(21234, 34567);
        }

        /// <summary>
        /// Creates a new instance of <see cref="RegionCrimeGenerator"/>
        /// </summary>
        /// <param name="agency"></param>
        /// <param name="zones"></param>
        public RegionCrimeGenerator(WorldZone[] zones)
        {
            // Create instance variables
            RegionCrimeInfoByTimePeriod = new Dictionary<TimePeriod, RegionCrimeInfo>();
            CrimeZoneGenerator = new ProbabilityGenerator<WorldZone>();
            foreach (TimePeriod period in Enum.GetValues(typeof(TimePeriod)))
            {
                RegionCrimeInfoByTimePeriod.Add(period, null);
            }

            // Only attempt to add if we have zones
            if (zones.Length > 0)
            {
                CrimeZoneGenerator.AddRange(zones);
            }
            else
            {
                throw new ArgumentNullException(nameof(zones));
            }

            // Do initial evaluation
            EvaluateCrimeValues();

            // Determine our initial Crime level during this period
            CurrentCrimeLevel = CrimeLevelGenerator.Spawn().Value;
        }

        /// <summary>
        /// Begins a new <see cref="Rage.GameFiber"/> to spawn <see cref="CalloutScenario"/>s
        /// based on current <see cref="TimePeriod"/>
        /// </summary>
        public void Begin()
        {
            if (CrimeFiber == null)
            {
                IsRunning = true;

                // Register for Dispatch event
                GameWorld.OnTimePeriodChanged += GameWorld_OnTimeOfDayChanged;

                // Build call queue
                BuildCallQueue();

                // Start GameFiber
                CrimeFiber = GameFiber.StartNew(ProcessCrimeLogic);
            }
        }

        /// <summary>
        /// Stops this <see cref="RegionCrimeGenerator"/> from spawning anymore calls
        /// </summary>
        public void End()
        {
            if (IsRunning)
            {
                GameWorld.OnTimePeriodChanged -= GameWorld_OnTimeOfDayChanged;
                IsRunning = false;
                CrimeFiber?.Abort();
                CrimeFiber = null;
            }
        }

        /// <summary>
        /// Uses the <see cref="ProbabilityGenerator{T}"/> to spawn which zone the next crime
        /// will be commited in
        /// </summary>
        /// <returns>returns a <see cref="WorldZone"/>, or null on failure</returns>
        public WorldZone GetNextRandomCrimeZone()
        {
            if (CrimeZoneGenerator.TrySpawn(out WorldZone zone))
            {
                return zone;
            }

            return null;
        }

        /// <summary>
        /// Gets the average calls per specified <see cref="TimePeriod"/>
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public double GetAverageCrimeCalls(TimePeriod time)
        {
            return RegionCrimeInfoByTimePeriod[time].AverageCrimeCalls;
        }

        /// <summary>
        /// Itterates through each zone and calculates the <see cref="RegionCrimeInfo"/>
        /// </summary>
        protected void EvaluateCrimeValues()
        {
            // Clear old stuff
            RegionCrimeInfoByTimePeriod.Clear();

            // Declare vars
            int timeScaleMult = TimeScale.GetCurrentTimeScaleMultiplier();
            int msPerGameMinute = TimeScale.GetMillisecondsPerGameMinute();
            int hourGameTimeToMSRealTime = 60 * msPerGameMinute;

            // Loop through each time period and cache crime numbers
            foreach (TimePeriod period in Enum.GetValues(typeof(TimePeriod)))
            {
                // Create info struct
                var crimeInfo = new RegionCrimeInfo();

                // Determine our overall crime numbers by adding each zones
                // individual crime statistics
                if (Zones.Length > 0)
                {
                    foreach (var zone in Zones)
                    {
                        // Get average calls per period
                        var calls = zone.CrimeInfo.GetTrueAverageCallCount(period);
                        crimeInfo.AverageCrimeCalls += calls;
                    }
                } 

                // Get our average real time milliseconds per call
                if (crimeInfo.AverageCallsPerGameHour > 0)
                {
                    int realTimeMsPerCall = (int)(hourGameTimeToMSRealTime / crimeInfo.AverageCallsPerGameHour);
                    crimeInfo.AverageMillisecondsPerCall = realTimeMsPerCall;
                }

                // Add period statistics
                RegionCrimeInfoByTimePeriod[period] = crimeInfo;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="calls">The average crime calls for this time period</param>
        /// <returns></returns>
        public int GetNextCallCountByCrimeLevel(double calls)
        {
            int min = 0;
            int max = 0;

            // Adjust call frequency timer based on current Crime Level
            switch (CurrentCrimeLevel)
            {
                case CrimeLevel.VeryHigh:
                    max = Convert.ToInt32(calls * 2.5d);
                    min = Convert.ToInt32(calls * 1.75d);
                    break;
                case CrimeLevel.High:
                    max = Convert.ToInt32(calls * 1.75d);
                    min = Convert.ToInt32(calls * 1.25d);
                    break;
                case CrimeLevel.Moderate:
                    max = Convert.ToInt32(calls * 1.25d);
                    min = Convert.ToInt32(calls * 0.75d);
                    break;
                case CrimeLevel.Low:
                    max = Convert.ToInt32(calls * 0.70d);
                    min = Convert.ToInt32(calls * 0.45d);
                    break;
                case CrimeLevel.VeryLow:
                    max = Convert.ToInt32(calls * 0.40d);
                    min = Convert.ToInt32(calls * 0.20d);
                    break;
                default:
                    // None - This gets fixed later down
                    break;
            }

            return Randomizer.Next(min, max);
        }

        /// <summary>
        /// Generates a new <see cref="PriorityCall"/> within a set range of time,
        /// determined by the <see cref="Agency.OverallCrimeLevel"/>
        /// </summary>
        private void ProcessCrimeLogic()
        {
            // stored local variables
            int timesFailed = 0;
            int sleepTimer = TimeScale.GetMillisecondsPerGameMinute();

            // While we are on duty accept calls
            while (IsRunning)
            {
                // Always yield
                GameFiber.Sleep(sleepTimer);

                // Do we have another incomming call in the queue?
                if (!NextIncomingCallTimes.TryPeek(out TimeSpan time))
                {
                    continue;
                }

                // Is this call incoming now?
                if (time < World.DateTime.TimeOfDay)
                {
                    continue;
                }

                // Remove item
                if (!NextIncomingCallTimes.TryDequeue(out time))
                {
                    continue;
                }

                // Generate a new call
                var call = GenerateCall();
                if (call == null)
                {
                    // If we keep failing, then show the player a message and quit
                    if (timesFailed > 3)
                    {
                        // Display notification to the player
                        Rage.Game.DisplayNotification(
                            "3dtextures",
                            "mpgroundlogo_cops",
                            "Agency Dispatch Framework",
                            "~o~Region Crime Generator.",
                            $"Failed to generate a call too many times. Disabling the crime generator. Please check your ~y~Game.log~w~ for errors"
                        );

                        // Log the error
                        Log.Error("RegionCrimeGenerator.ProcessCrimeLogic(): Failed to generate a call 3 times. Please contact the developer.");

                        // Turn off
                        IsRunning = false;
                        break;
                    }

                    // Log as warning for developer
                    Log.Warning($"Failed to generate a PriorityCall. Trying again in 1 second.");

                    // Count
                    timesFailed++;
                }
                else
                {
                    // Register call so that it can be dispatched
                    Dispatch.AddIncomingCall(call);

                    // Do we have another incomming call in the queue?
                    if (NextIncomingCallTimes.TryPeek(out time))
                    {
                        // Determine random time till next call
                        Log.Debug($"Starting next call in {time.TotalMinutes} in-game minutes");
                    }
                    else
                    {
                        // Determine random time till next call
                        Log.Debug($"No more calls this TimePeriod");
                    }

                    // Reset
                    timesFailed = 0;
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="PriorityCall"/> with a crime location for the <paramref name="scenario"/>.
        /// This method does not add the call to <see cref="Dispatch"/> call queue
        /// </summary>
        /// <param name="scenario"></param>
        /// <returns></returns>
        internal PriorityCall CreateCallFromScenario(CalloutScenarioInfo scenario)
        {
            // Try to generate a call
            for (int i = 0; i < Settings.MaxLocationAttempts; i++)
            {
                try
                {
                    // Spawn a zone in our jurisdiction
                    WorldZone zone = GetNextRandomCrimeZone();
                    if (zone == null)
                    {
                        Log.Error($"RegionCrimeGenerator.CreateCallFromScenario(): Attempted to pull a zone but zone is null");
                        break;
                    }

                    // Get a random location!
                    WorldLocation location = GetScenarioLocationFromZone(zone, scenario);
                    if (location == null)
                    {
                        Log.Warning($"RegionCrimeGenerator.CreateCallFromScenario(): Zone '{zone.FullName}' does not have any available '{scenario.LocationTypeCode}' locations");
                        continue;
                    }

                    // Add call to the dispatch Queue
                    return new PriorityCall(NextCallId++, scenario, location);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }

            return null;
        }

        /// <summary>
        /// Creates a new <see cref="PriorityCall"/> using <see cref="WorldStateMultipliers"/>
        /// </summary>
        /// <returns></returns>
        public virtual PriorityCall GenerateCall()
        {
            // Spawn a zone in our jurisdiction
            WorldZone zone = GetNextRandomCrimeZone();
            if (zone == null)
            {
                Log.Error($"RegionCrimeGenerator.GenerateCall(): Attempted to pull a random zone but zone is null");
                return null;
            }

            // Try to generate a call
            for (int i = 0; i < Settings.MaxLocationAttempts; i++)
            {
                try
                {
                    // Spawn crime type from our spawned zone
                    CallCategory type = zone.GetNextRandomCrimeType();
                    if (!ScenarioPool.ScenariosByCalloutType[type].TrySpawn(out CalloutScenarioInfo scenario))
                    {
                        Log.Warning($"RegionCrimeGenerator.GenerateCall(): Unable to find a CalloutScenario of CalloutType '{type}' in '{zone.FullName}'");
                        continue;
                    }

                    // Get a random location!
                    WorldLocation location = GetScenarioLocationFromZone(zone, scenario);
                    if (location == null)
                    {
                        // Log this as a warning... May need to add more locations!
                        Log.Warning($"RegionCrimeGenerator.GenerateCall(): Zone '{zone.FullName}' does not have any available '{scenario.LocationTypeCode}' locations");
                        continue;
                    }

                    // Add call to the dispatch Queue
                    return new PriorityCall(NextCallId++, scenario, location);
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                    break;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a <see cref="WorldLocation"/> from a <see cref="WorldZone"/> for a <see cref="CalloutScenarioInfo"/>
        /// </summary>
        /// <param name="zone"></param>
        /// <param name="scenario"></param>
        /// <returns></returns>
        protected virtual WorldLocation GetScenarioLocationFromZone(WorldZone zone, CalloutScenarioInfo scenario)
        {
            switch (scenario.LocationTypeCode)
            {
                case LocationTypeCode.RoadShoulder:
                    return zone.GetRandomRoadShoulder(scenario.LocationFilters, true);
                case LocationTypeCode.Residence:
                    return zone.GetRandomResidence(scenario.LocationFilters, true);
            }

            return null;
        }

        #region Events

        /// <summary>
        /// Method called on event <see cref="GameWorld.OnTimePeriodChanged"/>
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GameWorld_OnTimeOfDayChanged(TimePeriod oldPeriod, TimePeriod period)
        {
            var oldLevel = CurrentCrimeLevel;

            // Change our Crime level during this period
            CurrentCrimeLevel = CrimeLevelGenerator.Spawn().Value;
            var name = Enum.GetName(typeof(TimePeriod), period);

            // Log change
            Log.Info($"RegionCrimeGenerator: The time of day is transitioning to {name}. Settings crime level to {Enum.GetName(typeof(CrimeLevel), CurrentCrimeLevel)}");

            // Determine message
            string current = Enum.GetName(typeof(CrimeLevel), CurrentCrimeLevel);
            if (CurrentCrimeLevel != oldLevel)
            {
                var oldName = Enum.GetName(typeof(CrimeLevel), CurrentCrimeLevel);
                var text = (oldLevel > CurrentCrimeLevel) ? "~g~decrease~w~" : "~y~increase~w~";

                // Show the player some dialog
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "~b~Call Center Update",
                    $"The time of day is transitioning to ~y~{name}~w~. Crime levels are starting to {text}"
                );
            }
            else
            {
                // Show the player some dialog
                Rage.Game.DisplayNotification(
                    "3dtextures",
                    "mpgroundlogo_cops",
                    "Agency Dispatch Framework",
                    "~b~Call Center Update",
                    $"The time of day is transitioning to ~y~{name}~w~. Crime levels are not expected to change"
                );
            }

            // Build call queue
            BuildCallQueue();
        }

        /// <summary>
        /// Builds the call queue for this current <see cref="TimePeriod"/>
        /// </summary>
        private void BuildCallQueue()
        {
            // Clear old junk
            NextIncomingCallTimes = new ConcurrentQueue<TimeSpan>();

            // Rebuild WorldZone probability generator. Probability equals TrueAverageCallCount * 10000
            CrimeZoneGenerator.Rebuild();

            // Get average number of calls this time
            var info = RegionCrimeInfoByTimePeriod[GameWorld.CurrentTimePeriod];

            // Multiply # of calls based on crime levels
            var numCalls = GetNextCallCountByCrimeLevel(info.AverageCrimeCalls);

            // Divided by percent we are through the current time period.
            var start = World.DateTime.TimeOfDay;
            var end = GameWorld.GetTimeUntilNextTimePeriod().Add(start);
            int maxMinutes = (int)((end - start).TotalMinutes) - 1;

            // If max minutes is really small, skip
            if (maxMinutes < 5) return;

            // Take that number create X amount of random TimeSpans
            var list = new List<TimeSpan>(numCalls);
            for (int i = 0; i < numCalls; i++)
            {
                int minutes = Randomizer.Next(1, maxMinutes);
                list.Add(start.Add(TimeSpan.FromMinutes(minutes)));
            }

            // Store TimeSpans in a queue ordered by total minutes
            var oList = list.OrderBy(x => x.TotalMinutes);
            foreach (var time in oList)
            {
                NextIncomingCallTimes.Enqueue(time);
            }
        }

        #endregion
    }
}
