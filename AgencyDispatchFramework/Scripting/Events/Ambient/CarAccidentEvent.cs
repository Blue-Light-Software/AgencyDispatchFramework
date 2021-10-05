using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyDispatchFramework.Scripting.Events
{
    internal class CarAccidentEvent : AmbientEvent
    {
        public override string Name => "ADF.CarAccident";

        public override void Process()
        {
            
        }

        public override void End()
        {

        }
    }
}
