﻿using AgencyDispatchFramework.Extensions;
using AgencyDispatchFramework.Game;
using System;
using System.Xml;

namespace AgencyDispatchFramework
{
    /// <summary>
    /// Provides methods to extact common formatted XmlNodes and parse them into objects
    /// </summary>
    internal static class XmlHelper
    {
        /// <summary>
        /// Parses world state modifiers from an <see cref="XmlNode"/>
        /// </summary>
        /// <param name="node"></param>
        public static WorldStateMultipliers ExtractWorldStateMultipliers(XmlNode node)
        {
            // Ensure node isn't null
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            // Create instance
            var multipliers = new WorldStateMultipliers();

            // Itterate through each time of day
            foreach (TimePeriod period in Enum.GetValues(typeof(TimePeriod)))
            {
                var name = Enum.GetName(typeof(TimePeriod), period);
                var todNode = node.SelectSingleNode(name);
                if (todNode == null)
                {
                    throw new Exception($"[{node.GetFullPath()}]: Unable to extract '{name}' attribute from XmlNode");
                }

                // Itterate through each weather catagory
                foreach (WeatherCatagory catagory in Enum.GetValues(typeof(WeatherCatagory)))
                {
                    var attrName = Enum.GetName(typeof(WeatherCatagory), catagory).ToLowerInvariant();

                    // Extract and parse morning value
                    if (!Int32.TryParse(todNode.Attributes[attrName]?.Value, out int m))
                    {
                        throw new Exception($"[{todNode.GetFullPath()}]: Unable to extract '{attrName}' attribute on XmlNode");
                    }

                    // Set probability value
                    multipliers.SetProbability(period, catagory, m);
                }
            }

            return multipliers;
        }

        /// <summary>
        /// Parses world state modifiers from an <see cref="XmlNode"/>, and updates the values in the
        /// passed <see cref="WorldStateMultipliers"/>
        /// </summary>
        /// <param name="node"></param>
        public static void UpdateWorldStateMultipliers(XmlNode node, WorldStateMultipliers multipliers)
        {
            // Ensure node isn't null
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            // Itterate through each time of day
            foreach (TimePeriod period in Enum.GetValues(typeof(TimePeriod)))
            {
                var name = Enum.GetName(typeof(TimePeriod), period);
                var todNode = node.SelectSingleNode(name);
                if (todNode == null)
                {
                    continue;
                }

                // Itterate through each weather catagory
                foreach (WeatherCatagory catagory in Enum.GetValues(typeof(WeatherCatagory)))
                {
                    var attrName = Enum.GetName(typeof(WeatherCatagory), catagory).ToLowerInvariant();
                    if (todNode.TryGetAttribute(attrName, out string val))
                    {
                        // Extract and parse morning value
                        if (!Int32.TryParse(val, out int m))
                        {
                            // Log a warning
                            Log.Warning($"[{todNode.GetFullPath()}]: Malformed attribute value for '{attrName}' on XmlNode");
                            continue;
                        }

                        // Set probability value
                        multipliers.SetProbability(period, catagory, m);
                    }
                }
            }
        }
    }
}
