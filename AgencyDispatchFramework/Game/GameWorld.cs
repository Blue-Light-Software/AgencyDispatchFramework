﻿using AgencyDispatchFramework.Game.Locations;
using Rage;
using System;
using System.Drawing;
using System.Linq;
using static Rage.Native.NativeFunction;

namespace AgencyDispatchFramework.Game
{
    /// <summary>
    /// Provides methods to get and set information within the Game World
    /// </summary>
    /// <seealso cref="https://github.com/crosire/scripthookvdotnet/blob/main/source/scripting_v3/GTA/World.cs"/>
    public static class GameWorld
    {
        /// <summary>
        /// Our lock object to prevent threading issues
        /// </summary>
        private static object _threadLock = new object();

        /// <summary>
        /// Event called when the <see cref="CurrentTimePeriod"/> has changed in game
        /// </summary>
        public static event TimePeriodChangedEventHandler OnTimePeriodChanged;

        /// <summary>
        /// Event called when the weather changes in game
        /// </summary>
        public static event WeatherChangedEventHandler OnWeatherChange;

        /// <summary>
        /// Runs every 2 seconds
        /// </summary>
        private static GameFiber WorldWatchingFiber { get; set; }

        /// <summary>
        /// Credits to Albo1125
        /// </summary>
        public static int[] BlackListedNodeTypes = new int[] { 0, 8, 9, 10, 12, 40, 42, 136 };

        #region Weather & Effects

        internal static readonly string[] WeatherNames = {
            "EXTRASUNNY",
            "CLEAR",
            "CLOUDS",
            "SMOG",
            "FOGGY",
            "OVERCAST",
            "RAIN",
            "THUNDER",
            "CLEARING",
            "NEUTRAL",
            "SNOW",
            "BLIZZARD",
            "SNOWLIGHT",
            "XMAS",
            "HALLOWEEN"
        };

        /// <summary>
        /// Gets or sets the weather in game
        /// </summary>
        private static Weather LastKnownWeather { get; set; }

        /// <summary>
        /// Gets or sets the weather in game
        /// </summary>
        public static TimePeriod CurrentTimePeriod { get; internal set; }

        /// <summary>
        /// Gets or sets the weather in game
        /// </summary>
        public static Weather CurrentWeather
        {
            get
            {
                lock (_threadLock)
                {
                    return LastKnownWeather;
                }
            }
            set
            {
                if (Enum.IsDefined(typeof(Weather), value) && value != Weather.Unknown)
                {
                    lock (_threadLock)
                    {
                        Natives.SetWeatherTypeNow(WeatherNames[(int)value]);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the next weather to happen in game
        /// </summary>
        public static Weather NextWeather
        {
            get
            {
                var weatherHash = Natives.GetNextWeatherTypeHashName<uint>();
                for (int i = 0; i < WeatherNames.Length; i++)
                {
                    if (weatherHash == Rage.Game.GetHashKey(WeatherNames[i]))
                    {
                        return (Weather)i;
                    }
                }

                return Weather.Unknown;
            }
        }

        #endregion

        /// <summary>
        /// Sets the initial weather and Time of day without firing events
        /// </summary>
        internal static void Initialize()
        {
            // Set current stuff
            CurrentTimePeriod = GetCurrentWorldTimePeriod();
            LastKnownWeather = GetCurrentWeather();
        }

        /// <summary>
        /// Begins all internal <see cref="GameFiber"/> instances
        /// </summary>
        internal static void BeginFibers()
        {
            WorldWatchingFiber = GameFiber.StartNew(UpdateWorldState);
        }

        /// <summary>
        /// Every three seconds, this method checks for weather changes and TimePeriod changes
        /// </summary>
        private static void UpdateWorldState()
        {
            // While we are on duty accept calls
            while (Main.OnDutyLSPDFR)
            {
                try
                {
                    // Update weather
                    var currentWeather = GetCurrentWeather();
                    if (currentWeather != CurrentWeather)
                    {
                        Weather lastWeather = Weather.Unknown;
                        lock (_threadLock)
                        {
                            lastWeather = LastKnownWeather;
                            LastKnownWeather = currentWeather;
                        }

                        // Fire event
                        OnWeatherChange?.Invoke(lastWeather, currentWeather);
                        continue;
                    }

                    // Get current Time of Day and check for changes
                    var currentTimePeriod = GetCurrentWorldTimePeriod();
                    if (currentTimePeriod != CurrentTimePeriod)
                    {
                        // Set
                        var lastPeriod = CurrentTimePeriod;
                        CurrentTimePeriod = currentTimePeriod;

                        // Fire event
                        OnTimePeriodChanged?.Invoke(lastPeriod, currentTimePeriod);
                    }
                }
                catch (Exception e)
                {
                    Log.Exception(e);
                }

                // Wait
                GameFiber.Wait(2000);
            }
        }

        /// <summary>
        /// Gets the current <see cref="TimePeriod"/>
        /// </summary>
        /// <returns></returns>
        private static TimePeriod GetCurrentWorldTimePeriod()
        {
            var currentHour = World.TimeOfDay.Hours;
            var currentTimeOfDay = Parse(currentHour);
            return currentTimeOfDay;

            // Local function
            TimePeriod Parse(int hour)
            {
                if (hour < 4) return TimePeriod.Night;
                else if (hour < 8) return TimePeriod.LateMorning;
                else if (hour < 12) return TimePeriod.LateMorning;
                else if (hour < 16) return TimePeriod.Afternoon;
                else if (hour < 20) return TimePeriod.EarlyEvening;
                else return TimePeriod.LateEvening;
            }
        }

        /// <summary>
        /// Gets the remaining time until the next <see cref="TimePeriod"/> change (GameTime)
        /// </summary>
        /// <returns></returns>
        public static TimeSpan GetTimeUntilNextTimePeriod()
        {
            var current = World.TimeOfDay;
            var todaysTime = new TimeSpan(current.Hours, current.Minutes, current.Seconds);

            switch (CurrentTimePeriod)
            {
                case TimePeriod.Night:
                    return TimeSpan.FromHours(4).Subtract(todaysTime);
                case TimePeriod.EarlyMorning:
                    return TimeSpan.FromHours(8).Subtract(todaysTime);
                case TimePeriod.LateMorning:
                    return TimeSpan.FromHours(12).Subtract(todaysTime);
                case TimePeriod.Afternoon:
                    return TimeSpan.FromHours(16).Subtract(todaysTime);
                case TimePeriod.EarlyEvening:
                    return TimeSpan.FromHours(20).Subtract(todaysTime);
                default:
                    return TimeSpan.FromHours(24).Subtract(todaysTime);
            }
        }

        #region Weather Methods

        /// <summary>
        /// Gets the current in game weather using Natives
        /// </summary>
        /// <returns></returns>
        private static Weather GetCurrentWeather()
        {
            var weatherHash = Natives.GetPrevWeatherTypeHashName<uint>();
            for (int i = 0; i < WeatherNames.Length; i++)
            {
                if (weatherHash == Rage.Game.GetHashKey(WeatherNames[i]))
                {
                    return (Weather)i;
                }
            }

            return Weather.Unknown;
        }

        /// <summary>
		/// Transitions to the specified weather.
		/// </summary>
		/// <param name="weather">The weather to transition to</param>
		/// <param name="duration">The duration of the transition. If set to zero, the weather 
        /// will transition immediately</param>
		public static void TransitionToWeather(Weather weather, float duration)
        {
            if (weather != Weather.Unknown)
            {
                Natives.SetWeatherTypeOvertimePersist(WeatherNames[(int)weather], duration);
            }
        }

        /// <summary>
		/// Sets the specified weather.
		/// </summary>
		/// <param name="weather">The weather to transition to</param>
		public static void SetWeather(Weather weather)
        {
            if (weather != Weather.Unknown)
            {
                Natives.SetWeatherTypeNow(WeatherNames[(int)weather]);
            }
        }

        /// <summary>
		/// Sets the weather to a random weather.
		/// </summary>
		public static void RandomizeWeather()
        {
            Natives.SetRandomWeatherType();
        }

        /// <summary>
		/// Sets the rain, rain sounds and the creation of puddles in game.
		/// </summary>
		/// <param name="level">Strength of rain effects, between 0.0 and 1.0</param>
        /// <remarks>
        /// With an level higher than 0.5f, only the creation of puddles gets faster,
        /// rain and rain sound won't increase after that.
        /// 
        /// With an level of 0.0f rain and rain sounds are disabled and there won't be
        /// any new puddles.
        /// 
        /// To use the rain level of the current weather, call this native with -1f as level.
        /// </remarks>
        /// <seealso cref="https://docs.fivem.net/natives/?_0x643E26EA6E024D92"/>
		public static void SetRainLevel(float level)
        {
            Natives.SetRainLevel(level);
        }

        /// <summary>
        /// Gets the current world <see cref="WeatherSnapshot" />
        /// </summary>
        /// <returns></returns>
        public static WeatherSnapshot GetWeatherSnapshot()
        {
            return new WeatherSnapshot();
        }

        /// <summary>
        /// Gets a random weather list that makes sense based on the current in game DateTime
        /// </summary>
        /// <returns></returns>
        public static Weather[] GetRealisticWeatherListByDateTime() => GetRealisticWeatherListByDateTime(World.DateTime);

        /// <summary>
        /// Gets a random weather list that makes sense based on the supplied DateTime
        /// </summary>
        /// <returns></returns>
        public static Weather[] GetRealisticWeatherListByDateTime(DateTime inGameDate)
        {
            if (inGameDate.Month == 1 || inGameDate.Month.InRange(10, 12))
            {
                // Winter
                return new[]
                {
                    Weather.Blizzard,
                    Weather.Snowing,
                    Weather.Christmas,
                    Weather.Snowlight,
                    Weather.Raining,
                    Weather.Overcast,
                    //Weather.Halloween,
                    Weather.Foggy,
                    Weather.Clouds,
                    Weather.Clearing
                };
            }
            else if (inGameDate.Month.InRange(2, 5))
            {
                // Spring
                return new[]
                {
                    Weather.Raining,
                    Weather.Overcast,
                    Weather.Clouds,
                    Weather.Clearing,
                    Weather.Clear,
                    Weather.Neutral,
                    Weather.ThunderStorm
                };
            }
            else if (inGameDate.Month.InRange(6, 9))
            {
                return new[]
                {
                    Weather.Raining,
                    Weather.Overcast,
                    Weather.Clouds,
                    Weather.Clearing,
                    Weather.Clear,
                    Weather.Neutral,
                    Weather.ExtraSunny,
                    Weather.Smog
                };
            }
            else
            {
                // Fall
                return new[]
                {
                    Weather.Raining,
                    Weather.Overcast,
                    Weather.Clouds,
                    Weather.Clearing,
                    Weather.Clear,
                    Weather.Neutral,
                    Weather.ThunderStorm
                };
            }
        }

        #endregion Weather Methods

        #region Spawning Entity Methods 

        /// <summary>
        /// Attempts to spawn a <see cref="Rage.Vehicle"/> at the specified location safely, without collision of another
        /// <see cref="Entity"/> at the specified <see cref="Vector3"/>.
        /// </summary>
        /// <param name="model">The name of the <see cref="Model"/> to spawn</param>
        /// <param name="spawnPoint">The location to spawn the <see cref="Ped"/> at</param>
        /// <param name="delete">If true, checks for any other <see cref="Ped"/> or <see cref="Vehicle"/> entities at this location
        /// and deletes them if they are. If false, and there is an <see cref="Entity"/> at this location, this method will fail and return false</param>
        /// <param name="vehicle"></param>
        /// <returns></returns>
        public static bool TrySpawnVehicleAtPosition(Model model, SpawnPoint spawnPoint, bool delete, out Vehicle vehicle)
        {
            var flags = GetEntitiesFlags.ConsiderGroundVehicles | GetEntitiesFlags.ConsiderHumanPeds;
            var entities = Rage.World.GetEntities(spawnPoint.Position, 3f, flags);
            if (entities.Length > 0)
            {
                if (delete)
                {
                    // Delete each entity
                    foreach (var ent in entities)
                        ent.Delete();
                }
                else
                {
                    vehicle = default(Vehicle);
                    return false;
                }
            }

            // Create vehicle
            vehicle = new Vehicle(model, spawnPoint.Position, spawnPoint.Heading);
            return true;
        }

        /// <summary>
        /// Attempts to spawn a <see cref="Rage.Ped"/> at the specified location safely, without collision of another
        /// <see cref="Entity"/> at the specified <see cref="Vector3"/>.
        /// </summary>
        /// <param name="spawnPoint">The location to spawn the <see cref="Ped"/> at</param>
        /// <param name="group">The <see cref="PedVariantGroup"/> to select the ped model from</param>
        /// <param name="gender">The <see cref="PedGender"/> of the ped to spawn. If <see cref="PedGender.Unknown"/> is passed, it will be randomized.</param>
        /// <param name="delete">If true, checks for any other <see cref="Ped"/> or <see cref="Vehicle"/> entities at this location
        /// and deletes them if they are. If false, and there is an <see cref="Entity"/> at this location, this method will fail and return false</param>
        /// <param name="ped">if successful, containts the <see cref="Ped"/> spawned</param>
        /// <returns></returns>
        public static bool TrySpawnRandomPedAtPosition(SpawnPoint spawnPoint, PedVariantGroup group, PedGender gender, bool delete, out Ped ped)
        {
            // Randmonize gender
            var random = new CryptoRandom();
            if (gender == PedGender.Unknown)
            {
                gender = random.PickOne(PedGender.Male, PedGender.Female);
            }

            // Grab all peds in the specified variant group
            ped = default(Ped);
            var groupPeds = GamePed.PedModelsByVariant[group];
            if (groupPeds.Count == 0)
            {
                Log.Warning($"GameWorld.SpawnPedAtPosition(): PedVariantGroup named {group} has no peds in it");
                return false;
            }

            // Pull gender
            string pull = (gender == PedGender.Male) ? "_m_" : "_f_";
            var items = groupPeds.Where(x => x.Contains(pull)).ToArray();

            // Criteria cant be met
            if (items.Length == 0) return false;

            // Grab random name and attempt to spawn
            var name = items[random.Next(0, items.Length - 1)];
            return TrySpawnPedAtPosition(name, spawnPoint, delete, out ped);
        }

        /// <summary>
        /// Attempts to spawn a <see cref="Rage.Ped"/> at the specified location safely, without collision of another
        /// <see cref="Entity"/> at the specified <see cref="Vector3"/>.
        /// </summary>
        /// <param name="spawnPoint">The location to spawn the <see cref="Ped"/> at</param>
        /// <param name="gender">The <see cref="PedGender"/> of the ped to spawn. If <see cref="PedGender.Unknown"/> is passed, it will be randomized.</param>
        /// <param name="delete">If true, checks for any other <see cref="Ped"/> or <see cref="Vehicle"/> entities at this location
        /// and deletes them if they are. If false, and there is an <see cref="Entity"/> at this location, this method will fail and return false</param>
        /// <param name="ped">if successful, containts the <see cref="Ped"/> spawned</param>
        /// <returns></returns>
        public static bool TrySpawnRandomPedAtPosition(SpawnPoint spawnPoint, PedGender gender, bool delete, out Ped ped)
        {
            // Randmonize gender
            var random = new CryptoRandom();
            if (gender == PedGender.Unknown)
            {
                gender = random.PickOne(PedGender.Male, PedGender.Female);
            }

            // Pull gender
            string pull = (gender == PedGender.Male) ? "_m_" : "_f_";
            var items = Model.PedModels.Where(x => x.Name.Contains(pull)).ToArray();

            // Criteria cant be met
            if (items.Length == 0)
            {
                ped = default(Ped);
                return false;
            }

            // Grab random name and attempt to spawn
            var name = items[random.Next(0, items.Length - 1)];
            return TrySpawnPedAtPosition(name, spawnPoint, delete, out ped);
        }

        /// <summary>
        /// Attempts to spawn a <see cref="Rage.Ped"/> at the specified location safely, without collision of another
        /// <see cref="Entity"/> at the specified <see cref="Vector3"/>.
        /// </summary>
        /// <param name="pedName"></param>
        /// <param name="spawnPoint"></param>
        /// <param name="delete"></param>
        /// <param name="ped"></param>
        /// <returns></returns>
        public static bool TrySpawnPedAtPosition(Model pedName, SpawnPoint spawnPoint, bool delete, out Ped ped)
        {
            // Ensure no other entities are there
            var flags = GetEntitiesFlags.ConsiderGroundVehicles | GetEntitiesFlags.ConsiderHumanPeds;
            var entities = Rage.World.GetEntities(spawnPoint.Position, 1f, flags);
            if (entities.Length > 0)
            {
                if (delete)
                {
                    // Delete each entity
                    foreach (var ent in entities)
                        ent.Delete();
                }
                else
                {
                    ped = default(Ped);
                    return false;
                }
            }

            // Spawn ped
            ped = new Ped(pedName, spawnPoint.Position, spawnPoint.Heading);
            return true;
        }

        /// <summary>
        /// Creates a random ped at the specified position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Ped SpawnRandomPedAtPosition(Vector3 position)
        {
            return new Ped(position);
        }

        /// <summary>
        /// Creates a random ped at the specified position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static Ped SpawnRandomPedAtPosition(SpawnPoint position)
        {
            return new Ped(position.Position, position.Heading);
        }

        #endregion Spawning Entity Methods 

        /// <summary>
        /// Gets the closest in game vehicle node to this <see cref="Vector3"/> position
        /// </summary>
        /// <param name="pos">Starting point</param>
        /// <returns></returns>
        public static Vector3 GetClosestMajorVehicleNode(Vector3 pos)
        {
            bool success = Natives.GetClosestMajorVehicleNode<bool>(pos.X, pos.Y, pos.Z, out Vector3 node, 3.0f, 0f);
            return success ? node : Vector3.Zero;
        }

        /// <summary>
        /// Gets a safe <see cref="Vector3"/> position for a <see cref="Ped"/> using natives
        /// </summary>
        /// <param name="pos">Starting point to spawn a <see cref="Ped"/></param>
        /// <param name="safePedPoint"></param>
        /// <returns></returns>
        /// <seealso cref="http://www.dev-c.com/nativedb/func/info/b61c8e878a4199ca"/>
        public static unsafe bool GetSafeVector3ForPedNear(Vector3 pos, out Vector3 safePedPoint)
        {
            if (!Natives.GetSafeCoordForPed<bool>(pos.X, pos.Y, pos.Z, true, out Vector3 tempSpawn, 0))
            {
                tempSpawn = World.GetNextPositionOnStreet(pos);
                Entity nearbyentity = World.GetClosestEntity(tempSpawn, 25f, GetEntitiesFlags.ConsiderHumanPeds);
                if (nearbyentity.Exists())
                {
                    tempSpawn = nearbyentity.Position;
                    safePedPoint = tempSpawn;
                    return true;
                }
                else
                {
                    safePedPoint = tempSpawn;
                    return false;
                }
            }
            safePedPoint = tempSpawn;
            return true;
        }

        /// <summary>
        /// Gets the <see cref="WorldZone"/> based on the coordinates provided.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static WorldZone GetZoneAtLocation(Vector3 position)
        {
            var name = Natives.GetNameOfZone<string>(position.X, position.Y, position.Z);
            return WorldZone.GetZoneByName(name);
        }

        /// <summary>
        /// Gets the zone name based on the coordinates provided.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static string GetZoneNameAtLocation(Vector3 position, bool fullName = false)
        {
            // Script name?
            if (!fullName)
                return Natives.GetNameOfZone<string>(position.X, position.Y, position.Z);

            // Fetch zone
            var zone = GetZoneAtLocation(position);
            return zone?.DisplayName;
        }

        /// <summary>
        /// Gets the street name based on the provided location
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static string GetStreetNameAtLocation(Vector3 position)
        {
            return GetStreetNameAtLocation(position, out string crossingRoad);
        }

        /// <summary>
        /// Gets the street name based on the provided location, and its crossing intersection (if near)
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public static string GetStreetNameAtLocation(Vector3 position, out string crossingRoad)
        {
            uint nameHash = 0, crossingRoadHash = 0;
            Natives.GetStreetNameAtCoord(position.X, position.Y, position.Z, ref nameHash, ref crossingRoadHash);

            crossingRoad = Natives.GetStreetNameFromHashKey<string>(crossingRoadHash);
            return Natives.GetStreetNameFromHashKey<string>(nameHash);
        }

        /// <summary>
        /// Creates a checkpoint at the specified location, and returns the handle
        /// </summary>
        /// <remarks>
        /// Checkpoints are already handled by the game itself, so you must not loop it like markers.
        /// </remarks>
        /// <seealso cref="https://docs.fivem.net/docs/game-references/checkpoints/"/>
        /// <param name="type">The type of checkpoint to create.</param>
        /// <param name="pos">The position of the checkpoint</param>
        /// <param name="radius">The radius of the checkpoint cylinder</param>
        /// <param name="color">The color of the checkpoint</param>
        /// <returns>returns the handle of the checkpoint</returns>
        public static Checkpoint CreateCheckpoint(Vector3 pos, Color color, int type = 47, float radius = 5f, float nearHeight = 3f, float farHeight = 3f, bool forceGround = false, int number = 0)
        {
            return Checkpoint.Create(pos, color, type, radius, nearHeight, farHeight, forceGround, number);
        }
    }
}
