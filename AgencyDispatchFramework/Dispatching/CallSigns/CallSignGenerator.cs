﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgencyDispatchFramework.Dispatching
{
    internal abstract class CallSignGenerator
    {
        public abstract CallSign GetNew(UnitType unitType);
    }
}
