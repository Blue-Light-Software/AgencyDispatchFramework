using System;
using System.Collections.Generic;

namespace AgencyDispatchFramework.Game
{
    public class PedVariationMeta
    {
        /// <summary>
        /// Gets a hash table of <see cref="PedComponent"/>s to spawn this <see cref="Ped"/> with.
        /// </summary>
        /// <remarks>Tuple{DrawableId, TextureId}</remarks>
        public Dictionary<PedComponent, Tuple<int, int>> Components { get; internal set; }

        /// <summary>
        /// Gets a hash table of <see cref="PedPropIndex"/>s to spawn this <see cref="Ped"/> with.
        /// </summary>
        /// <remarks>Tuple{DrawableId, TextureId}</remarks>
        public Dictionary<PedPropIndex, Tuple<int, int>> Props { get; internal set; }

        /// <summary>
        /// A bool indicating whether to randomize props
        /// </summary>
        public bool RandomizeProps { get; internal set; } = true;

        /// <summary>
        /// Constructor
        /// </summary>
        public PedVariationMeta()
        {
            Components = new Dictionary<PedComponent, Tuple<int, int>>();
            Props = new Dictionary<PedPropIndex, Tuple<int, int>>();
        }
    }
}
