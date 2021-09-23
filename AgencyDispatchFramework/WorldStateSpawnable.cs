namespace AgencyDispatchFramework
{
    /// <summary>
    /// An internal wrapper class that inherits the <see cref="ISpawnable"/> interface
    /// </summary>
    /// <typeparam name="U"></typeparam>
    public class WorldStateSpawnable<U> : ISpawnable
    {
        public int Probability => Multipliers.Calculate();

        public U Item { get; set; }

        public WorldStateMultipliers Multipliers { get; set; }

        public WorldStateSpawnable(U item, WorldStateMultipliers multipliers)
        {
            Item = item;
            Multipliers = multipliers;
        }
    }
}
