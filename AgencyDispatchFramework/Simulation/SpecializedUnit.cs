﻿using AgencyDispatchFramework.Dispatching;
using AgencyDispatchFramework.Game;
using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// An object that provides an interface to spawn <see cref="VehicleSet"/>s based on the
    /// <see cref="UnitType"/>
    /// </summary>
    public class SpecializedUnit : ICloneable
    {
        /// <summary>
        /// Gets the <see cref="UnitType"/> of this <see cref="SpecializedUnit"/>
        /// </summary>
        public UnitType UnitType { get; private set; }

        /// <summary>
        /// Containts a <see cref="ProbabilityGenerator{T}"/> list of <see cref="VehicleSet"/> for this agency
        /// </summary>
        public ProbabilityGenerator<VehicleSet> OfficerSets { get; internal set; }

        /// <summary>
        /// Containts a <see cref="ProbabilityGenerator{T}"/> list of <see cref="VehicleSet"/> for this agency
        /// </summary>
        public ProbabilityGenerator<VehicleSet> SupervisorSets { get; internal set; }

        /// <summary>
        /// Gets thje assigned <see cref="Agency"/> that this <see cref="SpecializedUnit"/> belongs to
        /// </summary>
        internal Agency AssignedAgency { get; private set; }

        /// <summary>
        /// Gets the optimum patrol count based on <see cref="ShiftRotation" />. Lazy Loaded
        /// </summary>
        internal Dictionary<TimePeriod, int> OptimumPatrols { get; set; }

        /// <summary>
        /// Gets the optimum patrol count based on <see cref="ShiftRotation" />. Lazy Loaded
        /// </summary>
        internal List<AIOfficerUnit> Roster { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="SpecializedUnit"/>
        /// </summary>
        /// <param name="type"></param>
        public SpecializedUnit(UnitType type, Agency agency)
        {
            AssignedAgency = agency;
            UnitType = type;
            OfficerSets = new ProbabilityGenerator<VehicleSet>();
            SupervisorSets = new ProbabilityGenerator<VehicleSet>();

            // Lazy load!
            OptimumPatrols = new Dictionary<TimePeriod, int>()
            {
                { TimePeriod.Night, 0 },
                { TimePeriod.EarlyMorning, 0 },
                { TimePeriod.LateMorning, 0 },
                { TimePeriod.Afternoon, 0 },
                { TimePeriod.EarlyEvening, 0 },
                { TimePeriod.LateEvening, 0 }

            };
            Roster = new List<AIOfficerUnit>();
        }

        /// <summary>
        /// Calculate the number of patrol cars needed to cover the number crime calls generated
        /// that are unique to this <see cref="SpecializedUnit"/>
        /// </summary>
        /// <returns></returns>
        internal Dictionary<ShiftRotation, int> CalculateShiftCount()
        {
            // Create
            var patrols = new Dictionary<ShiftRotation, int>();

            // Calculate
            patrols.Add(ShiftRotation.Day, GetOfficerCount(OptimumPatrols[TimePeriod.LateMorning], OptimumPatrols[TimePeriod.Afternoon]));
            patrols.Add(ShiftRotation.Swing, GetOfficerCount(OptimumPatrols[TimePeriod.EarlyEvening], OptimumPatrols[TimePeriod.LateEvening]));
            patrols.Add(ShiftRotation.Night, GetOfficerCount(OptimumPatrols[TimePeriod.Night], OptimumPatrols[TimePeriod.EarlyMorning]));

            // Returns
            return patrols;
        }

        /// <summary>
        /// Creates a new <see cref="AIOfficerUnit"/> and adds them to the roster
        /// </summary>
        /// <param name="supervisor"></param>
        internal AIOfficerUnit CreateOfficerUnit(bool supervisor, ShiftRotation shift)
        {
            // Grab specialized unit
            var generator = (supervisor) ? SupervisorSets : OfficerSets;

            // Grab vehicle set
            if (!generator.TrySpawn(out VehicleSet vehicleSet))
            {
                return null;
            }

            // Come up with a unique callsign for this unit
            try
            {
                // Create @TODO
                CallSign.TryParse("1A-1", out CallSign callSign);
                var officer = new AIOfficerUnit(vehicleSet, AssignedAgency, callSign, shift, supervisor);

                // Add to roster
                Roster.Add(officer);

                // Return the officer unit
                return officer;
            }
            catch (Exception e)
            {
                Log.Exception(e);
                return null;
            }
        }

        /// <summary>
        /// Returns the maximum of 2 officer counts, with a minimum of 1
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns></returns>
        private int GetOfficerCount(int v1, int v2)
        {
            return Math.Max(1, Math.Max(v1, v2));
        }

        /// <summary>
        /// Returns a new instance of <see cref="SpecializedUnit"/>
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            var clone = new SpecializedUnit(UnitType, AssignedAgency);

            // Clone officer sets
            foreach (var set in OfficerSets.GetItems())
            {
                clone.OfficerSets.Add((VehicleSet)set.Clone());
            }

            // Clone supervisor sets
            foreach (var set in SupervisorSets.GetItems())
            {
                clone.SupervisorSets.Add((VehicleSet)set.Clone());
            }

            return clone;
        }
    }
}
