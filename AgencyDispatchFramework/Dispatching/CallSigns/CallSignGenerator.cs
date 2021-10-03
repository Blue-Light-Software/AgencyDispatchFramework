namespace AgencyDispatchFramework.Dispatching
{
    internal abstract class CallSignGenerator
    {
        public abstract CallSign Next(UnitType unitType, bool supervisor);
    }
}
