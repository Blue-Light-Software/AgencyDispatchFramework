using AgencyDispatchFramework.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyDispatchFramework.Dispatching.Assignments
{
    public class AssignedToCall : BaseAssignment
    {
        public AssignedToCall(ActiveEvent call)
        {
            Priority = (EventPriority)call.OriginalPriority;
        }
    }
}
