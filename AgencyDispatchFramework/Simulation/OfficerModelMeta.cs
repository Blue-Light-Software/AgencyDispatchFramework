using AgencyDispatchFramework.Game;
using Rage;
using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Simulation
{
    /// <summary>
    /// Provides meta data used to spawn an officer <see cref="Ped"/> in the game world
    /// </summary>
    public class OfficerModelMeta : ISpawnable
    {
        /// <summary>
        /// Gets the chance that this meta will be used in a <see cref="ProbabilityGenerator{T}"/>
        /// </summary>
        public int Probability { get; protected set; }

        /// <summary>
        /// Gets or sets the officer <see cref="Rage.Ped"/>s <see cref="Rage.Model"/>
        /// </summary>
        public Model Model { get; set; }

        /// <summary>
        /// Contains a look up of <see cref="PedVariationMeta"/>s for each <see cref="OutfitVariation"/>
        /// </summary>
        private Dictionary<OutfitVariation, PedVariationMeta> VariationMetas { get; set; }

        /// <summary>
        /// Creates a new instance
        /// </summary>
        public OfficerModelMeta(int probability, Model model)
        {
            Probability = probability;
            Model = model;
            VariationMetas = new Dictionary<OutfitVariation, PedVariationMeta>();
        }

        /// <summary>
        /// Gets the appropriate <see cref="PedVariationMeta"/> based on the weather
        /// </summary>
        /// <param name="weather"></param>
        /// <returns></returns>
        public PedVariationMeta GetVariationMeta(WeatherSnapshot weather)
        {
            var variation = GetOutfitVariation(weather);
            return (VariationMetas.ContainsKey(variation) ? VariationMetas[variation] : VariationMetas[OutfitVariation.Dry]);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="component"></param>
        /// <param name="drawId"></param>
        /// <param name="texId"></param>
        public void SetComponent(OutfitVariation type, PedComponent component, int drawId, int texId)
        {
            // Ensure key exists
            if (!VariationMetas.ContainsKey(type))
            {
                VariationMetas.Add(type, new PedVariationMeta());
            }

            // Check to see if the component has been added before
            if (VariationMetas[type].Components.ContainsKey(component))
            {
                VariationMetas[type].Components[component] = new Tuple<int, int>(drawId, texId);
            }
            else
            {
                VariationMetas[type].Components.Add(component, new Tuple<int, int>(drawId, texId));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propIndex"></param>
        /// <param name="drawId"></param>
        /// <param name="texId"></param>
        public void SetProp(OutfitVariation type, PedPropIndex propIndex, int drawId, int texId)
        {
            // Ensure key exists
            if (!VariationMetas.ContainsKey(type))
            {
                VariationMetas.Add(type, new PedVariationMeta());
            }

            // Check to see if the component has been added before
            if (VariationMetas[type].Props.ContainsKey(propIndex))
            {
                VariationMetas[type].Props[propIndex] = new Tuple<int, int>(drawId, texId);
            }
            else
            {
                VariationMetas[type].Props.Add(propIndex, new Tuple<int, int>(drawId, texId));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        public void RandomizeProps(OutfitVariation type, bool value)
        {
            // Ensure key exists
            if (!VariationMetas.ContainsKey(type))
            {
                VariationMetas.Add(type, new PedVariationMeta());
            }

            VariationMetas[type].RandomizeProps = value;
        }

        /// <summary>
        /// Gets an <see cref="OutfitVariation"/> based on a <see cref="WeatherSnapshot"/>
        /// </summary>
        /// <param name="snapshot"></param>
        /// <returns></returns>
        public static OutfitVariation GetOutfitVariation(WeatherSnapshot snapshot)
        {
            if (snapshot.IsSnowing)
            {
                return OutfitVariation.Snowy;
            }
            else if (snapshot.RoadsAreWet)
            {
                return OutfitVariation.Rainy;
            }
            else
            {
                return GetOutfitVariation(snapshot.Weather);
            }
        }

        /// <summary>
        /// Converts a <see cref="Weather"/> to a <see cref="OutfitVariation"/>
        /// </summary>
        /// <returns></returns>
        public static OutfitVariation GetOutfitVariation(Weather weather)
        {
            // Weather
            switch (weather)
            {
                default:
                case Weather.Clear:
                case Weather.ExtraSunny:
                case Weather.Neutral:
                case Weather.Clouds:
                case Weather.Smog:
                case Weather.Foggy:
                    return OutfitVariation.Dry;
                case Weather.Blizzard:
                case Weather.Christmas:
                case Weather.Snowing:
                case Weather.Snowlight:
                    return OutfitVariation.Snowy;
                case Weather.Raining:
                case Weather.Clearing:
                case Weather.Overcast:
                case Weather.ThunderStorm:
                    return OutfitVariation.Rainy;
            }
        }
    }
}
