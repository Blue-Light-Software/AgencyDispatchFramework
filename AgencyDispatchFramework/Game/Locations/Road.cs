using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rage;

namespace AgencyDispatchFramework.Game.Locations
{
    public class Road : WorldLocation
    {
        public override LocationTypeCode LocationType => LocationTypeCode.Road;

        public Road()
        {

        }

        /// <summary>
        /// Converts our <see cref="ResidenceFlags"/> to intergers and returns them
        /// </summary>
        /// <remarks>
        /// Used for filtering locations based on flags
        /// </remarks>
        /// <returns>An array of filters as integers</returns>
        public override int[] GetIntFlags()
        {
            //return Flags.Select(x => (int)x).ToArray();
            return null;
        }
    }
}
